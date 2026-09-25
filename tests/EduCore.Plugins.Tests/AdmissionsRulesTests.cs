using System;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace EduCore.Plugins.Tests
{
    public class AdmissionsRulesTests
    {
        private const int Applied = AdmissionsRules.Applied, Review = AdmissionsRules.UnderReview, Offer = AdmissionsRules.OfferMade,
            Enrolled = AdmissionsRules.Enrolled, Rejected = AdmissionsRules.Rejected, Withdrawn = AdmissionsRules.Withdrawn;
        private static readonly Guid A = new Guid("11111111-1111-1111-1111-111111111111");
        private static readonly Guid B = new Guid("22222222-2222-2222-2222-222222222222");

        [Theory]
        [InlineData(Applied, Review)]
        [InlineData(Applied, Withdrawn)]
        [InlineData(Review, Offer)]
        [InlineData(Review, Rejected)]
        [InlineData(Review, Withdrawn)]
        [InlineData(Offer, Enrolled)]
        [InlineData(Offer, Rejected)]
        [InlineData(Offer, Withdrawn)]
        public void Allowed_transitions(int from, int to)
        {
            Assert.Null(AdmissionsRules.ValidateTransition(from, to));
        }

        [Theory]
        [InlineData(Applied, Offer)]
        [InlineData(Applied, Enrolled)]
        [InlineData(Applied, Rejected)]
        [InlineData(Review, Enrolled)]
        [InlineData(Review, Applied)]
        [InlineData(Offer, Applied)]
        [InlineData(Offer, Review)]
        [InlineData(Enrolled, Review)]
        [InlineData(Enrolled, Rejected)]
        [InlineData(Rejected, Review)]
        [InlineData(Withdrawn, Applied)]
        public void Blocked_transitions(int from, int to)
        {
            Assert.NotNull(AdmissionsRules.ValidateTransition(from, to));
        }

        [Fact]
        public void Unchanged_status_is_fine_even_when_terminal()
        {
            Assert.Null(AdmissionsRules.ValidateTransition(Enrolled, Enrolled));
        }

        [Fact]
        public void New_applicant_must_start_as_applied()
        {
            Assert.Null(AdmissionsRules.ValidateTransition(null, Applied));
            Assert.NotNull(AdmissionsRules.ValidateTransition(null, Offer));
        }

        [Fact]
        public void Enrolled_needs_a_student()
        {
            Assert.NotNull(AdmissionsRules.ValidateEnrolled(Enrolled, false));
            Assert.Null(AdmissionsRules.ValidateEnrolled(Enrolled, true));
            Assert.Null(AdmissionsRules.ValidateEnrolled(Offer, false));
        }

        [Fact]
        public void Rejected_needs_a_reason()
        {
            Assert.NotNull(AdmissionsRules.ValidateRejected(Rejected, null));
            Assert.NotNull(AdmissionsRules.ValidateRejected(Rejected, "  "));
            Assert.Null(AdmissionsRules.ValidateRejected(Rejected, "No places"));
            Assert.Null(AdmissionsRules.ValidateRejected(Withdrawn, null));
        }

        [Fact]
        public void State_follows_status()
        {
            Assert.Equal(0, AdmissionsRules.StateOf(Applied));
            Assert.Equal(0, AdmissionsRules.StateOf(Review));
            Assert.Equal(0, AdmissionsRules.StateOf(Offer));
            Assert.Equal(1, AdmissionsRules.StateOf(Enrolled));
            Assert.Equal(1, AdmissionsRules.StateOf(Rejected));
            Assert.Equal(1, AdmissionsRules.StateOf(Withdrawn));
        }

        [Fact]
        public void Enrol_checks_status_year_and_grade()
        {
            var y1 = new EntityReference("edu_academicyear", A); var y2 = new EntityReference("edu_academicyear", B);
            var g1 = new EntityReference("edu_grade", A); var g2 = new EntityReference("edu_grade", B);
            Assert.Null(AdmissionsRules.ValidateEnrol(Offer, y1, g1, y1, g1));
            Assert.NotNull(AdmissionsRules.ValidateEnrol(Review, y1, g1, y1, g1));
            Assert.NotNull(AdmissionsRules.ValidateEnrol(Offer, y1, g1, y2, g1));
            Assert.NotNull(AdmissionsRules.ValidateEnrol(Offer, y1, g1, y1, g2));
        }

        [Fact]
        public void Student_is_built_from_the_applicant_with_a_preset_id()
        {
            var applicant = new Entity("edu_applicant", A);
            applicant["edu_name"] = "Asha Rao";
            applicant["edu_dateofbirth"] = new DateTime(2019, 3, 4);
            var student = AdmissionsRules.BuildStudent(applicant, new DateTime(2026, 6, 1, 9, 30, 0));
            Assert.NotEqual(Guid.Empty, student.Id);
            Assert.Equal("Asha Rao", student.GetAttributeValue<string>("edu_name"));
            Assert.Equal(new DateTime(2019, 3, 4), student.GetAttributeValue<DateTime?>("edu_dateofbirth"));
            Assert.Equal(new DateTime(2026, 6, 1), student.GetAttributeValue<DateTime?>("edu_enrolmentdate"));
            Assert.Equal(A, student.GetAttributeValue<EntityReference>("edu_sourceapplicant").Id);
        }

        [Fact]
        public void Link_is_primary_and_enrolment_carries_year_and_section()
        {
            var student = new EntityReference("edu_student", A);
            var link = AdmissionsRules.BuildGuardianLink(student, new EntityReference("contact", B), 859680002);
            Assert.True(link.GetAttributeValue<bool>("edu_isprimary"));
            Assert.Equal(859680002, link.GetAttributeValue<OptionSetValue>("edu_relationship").Value);
            var enrolment = AdmissionsRules.BuildEnrolment(student, new EntityReference("edu_section", B), new EntityReference("edu_academicyear", A), new DateTime(2026, 6, 1), "Asha Rao");
            Assert.Equal("Asha Rao - enrolment", enrolment.GetAttributeValue<string>("edu_name"));
            Assert.Equal(B, enrolment.GetAttributeValue<EntityReference>("edu_section").Id);
        }

        [Fact]
        public void Mark_enrolled_sets_state_and_status()
        {
            var u = AdmissionsRules.MarkEnrolled(A);
            Assert.Equal(1, u.GetAttributeValue<OptionSetValue>("statecode").Value);
            Assert.Equal(Enrolled, u.GetAttributeValue<OptionSetValue>("statuscode").Value);
        }
    }
}
