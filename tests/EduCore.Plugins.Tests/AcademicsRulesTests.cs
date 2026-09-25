using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace EduCore.Plugins.Tests
{
    public class AcademicsRulesTests
    {
        private static readonly Guid A = new Guid("11111111-1111-1111-1111-111111111111");
        private static readonly Guid B = new Guid("22222222-2222-2222-2222-222222222222");
        private static readonly Guid C = new Guid("33333333-3333-3333-3333-333333333333");

        private static readonly List<GradeBandInfo> Bands = new List<GradeBandInfo>
        {
            new GradeBandInfo(90, "A+"), new GradeBandInfo(80, "A"), new GradeBandInfo(70, "B"),
            new GradeBandInfo(60, "C"), new GradeBandInfo(50, "D"), new GradeBandInfo(0, "F")
        };

        [Fact]
        public void Section_teacher_and_room_clashes_are_each_rejected()
        {
            Assert.NotNull(AcademicsRules.ValidateClash(1, 0, 0));
            Assert.NotNull(AcademicsRules.ValidateClash(0, 1, 0));
            Assert.NotNull(AcademicsRules.ValidateClash(0, 0, 1));
            Assert.Null(AcademicsRules.ValidateClash(0, 0, 0));
        }

        [Fact]
        public void Clash_messages_name_the_resource()
        {
            Assert.Contains("section", AcademicsRules.ValidateClash(1, 1, 1));
            Assert.Contains("teacher", AcademicsRules.ValidateClash(0, 1, 1));
            Assert.Contains("room", AcademicsRules.ValidateClash(0, 0, 2));
        }

        [Fact]
        public void Marks_above_the_maximum_or_negative_are_rejected()
        {
            Assert.NotNull(AcademicsRules.ValidateMarks(21m, 20m));
            Assert.NotNull(AcademicsRules.ValidateMarks(-1m, 20m));
            Assert.Null(AcademicsRules.ValidateMarks(20m, 20m));
            Assert.Null(AcademicsRules.ValidateMarks(0m, 20m));
            Assert.Null(AcademicsRules.ValidateMarks(null, 20m));
        }

        [Fact]
        public void Maximum_and_weightage_ranges()
        {
            Assert.NotNull(AcademicsRules.ValidateMaxMarks(0m));
            Assert.Null(AcademicsRules.ValidateMaxMarks(50m));
            Assert.NotNull(AcademicsRules.ValidateWeightage(101m));
            Assert.NotNull(AcademicsRules.ValidateWeightage(-1m));
            Assert.Null(AcademicsRules.ValidateWeightage(0m));
            Assert.Null(AcademicsRules.ValidateWeightage(100m));
        }

        [Fact]
        public void Maximum_cannot_change_once_marks_exist()
        {
            Assert.NotNull(AcademicsRules.ValidateMaxChange(20m, 25m, true));
            Assert.Null(AcademicsRules.ValidateMaxChange(20m, 20m, true));
            Assert.Null(AcademicsRules.ValidateMaxChange(20m, 25m, false));
            Assert.Null(AcademicsRules.ValidateMaxChange(null, 25m, true));
        }

        [Fact]
        public void Percentage_is_rounded_to_two_places()
        {
            Assert.Equal(85m, AcademicsRules.Percentage(17m, 20m));
            Assert.Equal(33.33m, AcademicsRules.Percentage(1m, 3m));
            Assert.Equal(66.67m, AcademicsRules.Percentage(2m, 3m));
            Assert.Null(AcademicsRules.Percentage(null, 20m));
            Assert.Null(AcademicsRules.Percentage(5m, 0m));
        }

        [Theory]
        [InlineData(100, "A+")]
        [InlineData(90, "A+")]
        [InlineData(89.99, "A")]
        [InlineData(80, "A")]
        [InlineData(79.5, "B")]
        [InlineData(60, "C")]
        [InlineData(50, "D")]
        [InlineData(49.99, "F")]
        [InlineData(0, "F")]
        public void Grade_letter_uses_the_highest_band_reached(double pct, string letter)
        {
            Assert.Equal(letter, AcademicsRules.GradeLetter((decimal)pct, Bands));
        }

        [Fact]
        public void Grade_letter_is_null_without_bands_or_percentage()
        {
            Assert.Null(AcademicsRules.GradeLetter(75m, new List<GradeBandInfo>()));
            Assert.Null(AcademicsRules.GradeLetter(null, Bands));
        }

        [Fact]
        public void Phase_four_unique_keys()
        {
            var sa = new Entity("edu_subjectassignment");
            sa["edu_course"] = new EntityReference("edu_course", A);
            sa["edu_section"] = new EntityReference("edu_section", B);
            sa["edu_term"] = new EntityReference("edu_term", C);
            Assert.Equal(A.ToString("D") + "|" + B.ToString("D") + "|" + C.ToString("D"), ReferenceRules.UniqueKey(AcademicsRules.SubjectAssignment, sa));

            var cs = new Entity("edu_classschedule");
            cs["edu_subjectassignment"] = new EntityReference("edu_subjectassignment", A);
            cs["edu_weekday"] = new OptionSetValue(859680002);
            cs["edu_period"] = new EntityReference("edu_period", B);
            Assert.Equal(A.ToString("D") + "|859680002|" + B.ToString("D"), ReferenceRules.UniqueKey(AcademicsRules.ClassSchedule, cs));

            var m = new Entity("edu_studentmark");
            m["edu_assessment"] = new EntityReference("edu_assessment", A);
            m["edu_student"] = new EntityReference("edu_student", B);
            Assert.Equal(A.ToString("D") + "|" + B.ToString("D"), ReferenceRules.UniqueKey(AcademicsRules.StudentMark, m));
        }
    }
}
