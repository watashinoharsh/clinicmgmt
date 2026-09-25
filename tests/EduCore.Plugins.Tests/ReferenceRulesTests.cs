using System;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace EduCore.Plugins.Tests
{
    public class ReferenceRulesTests
    {
        private static readonly Guid Year = new Guid("11111111-1111-1111-1111-111111111111");

        [Fact]
        public void Academic_year_key_is_the_lower_cased_name()
        {
            var row = new Entity("edu_academicyear");
            row["edu_name"] = " 2026-27 ";
            Assert.Equal("2026-27", ReferenceRules.UniqueKey(ReferenceRules.AcademicYear, row));
        }

        [Fact]
        public void Term_key_is_year_and_sequence()
        {
            var row = new Entity("edu_term");
            row["edu_academicyear"] = new EntityReference("edu_academicyear", Year);
            row["edu_sequence"] = 2;
            Assert.Equal(Year.ToString("D") + "|2", ReferenceRules.UniqueKey(ReferenceRules.Term, row));
        }

        [Fact]
        public void Calendar_day_key_is_the_date_only()
        {
            var row = new Entity("edu_calendarday");
            row["edu_date"] = new DateTime(2026, 12, 25, 13, 0, 0);
            Assert.Equal("2026-12-25", ReferenceRules.UniqueKey(ReferenceRules.CalendarDay, row));
        }

        [Fact]
        public void Period_grade_course_and_band_keys()
        {
            var period = new Entity("edu_period"); period["edu_sequence"] = 3;
            Assert.Equal("3", ReferenceRules.UniqueKey(ReferenceRules.Period, period));
            var grade = new Entity("edu_grade"); grade["edu_name"] = "Grade 5";
            Assert.Equal("grade 5", ReferenceRules.UniqueKey(ReferenceRules.Grade, grade));
            var course = new Entity("edu_course"); course["edu_coursecode"] = "MATH-5";
            Assert.Equal("math-5", ReferenceRules.UniqueKey(ReferenceRules.Course, course));
            var band = new Entity("edu_gradeband"); band["edu_letter"] = "A+";
            Assert.Equal("a+", ReferenceRules.UniqueKey(ReferenceRules.GradeBand, band));
        }

        [Fact]
        public void Missing_key_part_is_rejected()
        {
            Assert.Throws<ArgumentException>(() => ReferenceRules.UniqueKey(ReferenceRules.AcademicYear, new Entity("edu_academicyear")));
        }

        [Fact]
        public void Unknown_table_has_no_key()
        {
            Assert.Null(ReferenceRules.UniqueKey("edu_other", new Entity("edu_other")));
        }

        [Fact]
        public void Year_must_end_after_it_starts()
        {
            Assert.NotNull(ReferenceRules.ValidateAcademicYear(new DateTime(2026, 6, 1), new DateTime(2026, 6, 1)));
            Assert.Null(ReferenceRules.ValidateAcademicYear(new DateTime(2026, 6, 1), new DateTime(2027, 5, 31)));
            Assert.Null(ReferenceRules.ValidateAcademicYear(null, null));
        }

        [Fact]
        public void Term_must_sit_inside_its_year()
        {
            var ys = new DateTime(2026, 6, 1); var ye = new DateTime(2027, 5, 31);
            Assert.Null(ReferenceRules.ValidateTerm(new DateTime(2026, 6, 1), new DateTime(2026, 9, 30), ys, ye));
            Assert.NotNull(ReferenceRules.ValidateTerm(new DateTime(2026, 5, 31), new DateTime(2026, 9, 30), ys, ye));
            Assert.NotNull(ReferenceRules.ValidateTerm(new DateTime(2027, 4, 1), new DateTime(2027, 6, 1), ys, ye));
            Assert.NotNull(ReferenceRules.ValidateTerm(new DateTime(2026, 9, 30), new DateTime(2026, 9, 1), ys, ye));
        }

        [Fact]
        public void Period_times_are_validated()
        {
            Assert.Null(ReferenceRules.ValidatePeriod("08:30", "09:15"));
            Assert.NotNull(ReferenceRules.ValidatePeriod("8:30", "09:15"));
            Assert.NotNull(ReferenceRules.ValidatePeriod("08:30", "25:00"));
            Assert.NotNull(ReferenceRules.ValidatePeriod("09:15", "09:15"));
        }

        [Fact]
        public void Merge_prefers_the_written_value()
        {
            var stored = new Entity("edu_term", Year); stored["edu_sequence"] = 1; stored["edu_name"] = "T1";
            var target = new Entity("edu_term", Year); target["edu_sequence"] = 2;
            var merged = ReferenceRules.Merge(target, stored);
            Assert.Equal(2, merged.GetAttributeValue<int>("edu_sequence"));
            Assert.Equal("T1", merged.GetAttributeValue<string>("edu_name"));
        }
    }
}
