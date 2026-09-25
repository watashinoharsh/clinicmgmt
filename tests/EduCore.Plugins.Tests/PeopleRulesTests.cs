using System;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace EduCore.Plugins.Tests
{
    public class PeopleRulesTests
    {
        private static readonly Guid A = new Guid("11111111-1111-1111-1111-111111111111");
        private static readonly Guid B = new Guid("22222222-2222-2222-2222-222222222222");
        private static readonly Guid C = new Guid("33333333-3333-3333-3333-333333333333");

        [Fact]
        public void Missing_state_and_state_zero_are_active()
        {
            Assert.True(PeopleRules.IsActive(null));
            Assert.True(PeopleRules.IsActive(new OptionSetValue(0)));
            Assert.False(PeopleRules.IsActive(new OptionSetValue(1)));
        }

        [Fact]
        public void A_second_primary_guardian_is_rejected()
        {
            Assert.NotNull(PeopleRules.ValidatePrimary(true, 1));
            Assert.Null(PeopleRules.ValidatePrimary(true, 0));
            Assert.Null(PeopleRules.ValidatePrimary(false, 2));
        }

        [Fact]
        public void First_guardian_defaults_to_primary_unless_chosen()
        {
            Assert.True(PeopleRules.DefaultPrimary(null, 0));
            Assert.False(PeopleRules.DefaultPrimary(null, 1));
            Assert.False(PeopleRules.DefaultPrimary(false, 0));
            Assert.True(PeopleRules.DefaultPrimary(true, 3));
        }

        [Fact]
        public void Link_name_joins_student_and_guardian_and_is_capped()
        {
            Assert.Equal("Asha Rao - Meera Rao", PeopleRules.LinkName(" Asha Rao ", "Meera Rao"));
            Assert.Equal("? - ?", PeopleRules.LinkName(null, " "));
            Assert.Equal(200, PeopleRules.LinkName(new string('a', 300), "b").Length);
        }

        [Fact]
        public void Enrolment_cannot_end_before_it_starts()
        {
            Assert.NotNull(PeopleRules.ValidateDates(new DateTime(2026, 9, 2), new DateTime(2026, 9, 1)));
            Assert.Null(PeopleRules.ValidateDates(new DateTime(2026, 9, 1), new DateTime(2026, 9, 1)));
            Assert.Null(PeopleRules.ValidateDates(new DateTime(2026, 9, 1), null));
        }

        [Fact]
        public void Section_must_belong_to_the_enrolment_year()
        {
            Assert.Null(PeopleRules.ValidateSectionYear(A, A));
            Assert.NotNull(PeopleRules.ValidateSectionYear(A, B));
            Assert.Null(PeopleRules.ValidateSectionYear(null, B));
        }

        [Fact]
        public void One_active_enrolment_per_student_per_year()
        {
            Assert.NotNull(PeopleRules.ValidateOneActive(true, 1));
            Assert.Null(PeopleRules.ValidateOneActive(true, 0));
            Assert.Null(PeopleRules.ValidateOneActive(false, 4));
        }

        [Fact]
        public void Capacity_counts_the_row_being_saved()
        {
            Assert.Null(PeopleRules.ValidateCapacity(true, 30, 29));
            Assert.NotNull(PeopleRules.ValidateCapacity(true, 30, 30));
            Assert.Null(PeopleRules.ValidateCapacity(false, 30, 99));
            Assert.Null(PeopleRules.ValidateCapacity(true, null, 99));
        }

        [Fact]
        public void Placement_sets_or_clears_section_and_grade()
        {
            var e = new Entity("edu_student", A);
            PeopleRules.ApplyPlacement(e, new EntityReference("edu_section", B), new EntityReference("edu_grade", C));
            Assert.Equal(B, e.GetAttributeValue<EntityReference>("edu_currentsection").Id);
            Assert.Equal(C, e.GetAttributeValue<EntityReference>("edu_currentgrade").Id);
            PeopleRules.ApplyPlacement(e, null, null);
            Assert.Null(e.GetAttributeValue<EntityReference>("edu_currentsection"));
            Assert.Null(e.GetAttributeValue<EntityReference>("edu_currentgrade"));
        }

        [Fact]
        public void Phase_two_unique_keys()
        {
            var staff = new Entity("edu_staff"); staff["edu_staffcode"] = "T-001";
            Assert.Equal("t-001", ReferenceRules.UniqueKey(PeopleRules.Staff, staff));

            var section = new Entity("edu_section");
            section["edu_grade"] = new EntityReference("edu_grade", A);
            section["edu_academicyear"] = new EntityReference("edu_academicyear", B);
            section["edu_name"] = "5-A";
            Assert.Equal(A.ToString("D") + "|" + B.ToString("D") + "|5-a", ReferenceRules.UniqueKey(PeopleRules.Section, section));

            var link = new Entity("edu_studentguardian");
            link["edu_student"] = new EntityReference("edu_student", A);
            link["edu_guardian"] = new EntityReference("contact", B);
            Assert.Equal(A.ToString("D") + "|" + B.ToString("D"), ReferenceRules.UniqueKey(PeopleRules.StudentGuardian, link));

            var enrolment = new Entity("edu_enrolment");
            enrolment["edu_student"] = new EntityReference("edu_student", A);
            enrolment["edu_academicyear"] = new EntityReference("edu_academicyear", C);
            Assert.Equal(A.ToString("D") + "|" + C.ToString("D"), ReferenceRules.UniqueKey(PeopleRules.Enrolment, enrolment));
        }
    }
}
