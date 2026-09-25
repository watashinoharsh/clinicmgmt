using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Xunit;

namespace EduCore.Plugins.Tests
{
    public class AttendanceRulesTests
    {
        private static readonly Guid A = new Guid("11111111-1111-1111-1111-111111111111");
        private static readonly Guid B = new Guid("22222222-2222-2222-2222-222222222222");

        private static readonly List<TermInfo> Terms = new List<TermInfo>
        {
            new TermInfo(A, new DateTime(2026, 6, 1), new DateTime(2026, 9, 30)),
            new TermInfo(B, new DateTime(2026, 10, 1), new DateTime(2027, 1, 31))
        };

        [Fact]
        public void Term_is_found_by_date_including_boundaries()
        {
            Assert.Equal(A, AttendanceRules.TermFor(new DateTime(2026, 6, 1), Terms));
            Assert.Equal(A, AttendanceRules.TermFor(new DateTime(2026, 9, 30, 15, 0, 0), Terms));
            Assert.Equal(B, AttendanceRules.TermFor(new DateTime(2026, 10, 1), Terms));
            Assert.Null(AttendanceRules.TermFor(new DateTime(2026, 5, 31), Terms));
        }

        [Fact]
        public void Period_records_need_an_assignment_and_daily_records_must_not_have_one()
        {
            Assert.NotNull(AttendanceRules.ValidateRecord(AttendanceRules.Period, false));
            Assert.Null(AttendanceRules.ValidateRecord(AttendanceRules.Period, true));
            Assert.NotNull(AttendanceRules.ValidateRecord(AttendanceRules.Daily, true));
            Assert.Null(AttendanceRules.ValidateRecord(AttendanceRules.Daily, false));
        }

        [Fact]
        public void Teaching_days_skip_weekends()
        {
            // Mon 1 Jun 2026 to Sun 14 Jun 2026: two working weeks
            Assert.Equal(10, AttendanceRules.TeachingDays(new DateTime(2026, 6, 1), new DateTime(2026, 9, 30), new DateTime(2026, 6, 14), null));
        }

        [Fact]
        public void Teaching_days_skip_closures_and_stop_at_the_term_end()
        {
            var closed = new[] { new DateTime(2026, 6, 3), new DateTime(2026, 6, 6) }; // Wed, and a Saturday that changes nothing
            Assert.Equal(4, AttendanceRules.TeachingDays(new DateTime(2026, 6, 1), new DateTime(2026, 6, 5), new DateTime(2026, 12, 1), closed));
        }

        [Fact]
        public void Teaching_days_are_zero_before_the_term_starts()
        {
            Assert.Equal(0, AttendanceRules.TeachingDays(new DateTime(2026, 6, 1), new DateTime(2026, 9, 30), new DateTime(2026, 5, 20), null));
        }

        [Fact]
        public void Percentage_counts_present_and_late_and_drops_excused_days()
        {
            var c = new AttendanceCounts { Present = 8, Late = 1, Absent = 1, Excused = 0 };
            Assert.Equal(90m, AttendanceRules.Percentage(c, 10));
            var e = new AttendanceCounts { Present = 8, Late = 0, Absent = 0, Excused = 2 };
            Assert.Equal(100m, AttendanceRules.Percentage(e, 10));
            var f = new AttendanceCounts { Present = 7, Late = 0, Absent = 1, Excused = 2 };
            Assert.Equal(87.5m, AttendanceRules.Percentage(f, 10));
        }

        [Fact]
        public void Percentage_is_null_when_nothing_is_countable_and_capped_at_100()
        {
            Assert.Null(AttendanceRules.Percentage(new AttendanceCounts(), 0));
            Assert.Null(AttendanceRules.Percentage(new AttendanceCounts { Excused = 5 }, 5));
            Assert.Equal(100m, AttendanceRules.Percentage(new AttendanceCounts { Present = 12 }, 10));
        }

        [Fact]
        public void Counting_maps_each_status()
        {
            var c = new AttendanceCounts();
            AttendanceRules.Count(c, AttendanceRules.Present); AttendanceRules.Count(c, AttendanceRules.Present);
            AttendanceRules.Count(c, AttendanceRules.Late); AttendanceRules.Count(c, AttendanceRules.Absent); AttendanceRules.Count(c, AttendanceRules.Excused);
            Assert.Equal(2, c.Present); Assert.Equal(1, c.Late); Assert.Equal(1, c.Absent); Assert.Equal(1, c.Excused);
        }

        [Fact]
        public void Threshold_and_weekly_alert_dedupe()
        {
            Assert.True(AttendanceRules.BelowThreshold(89.99m, 90m));
            Assert.False(AttendanceRules.BelowThreshold(90m, 90m));
            Assert.False(AttendanceRules.BelowThreshold(null, 90m));
            var today = new DateTime(2026, 9, 25);
            Assert.True(AttendanceRules.ShouldAlert(null, today, 7));
            Assert.False(AttendanceRules.ShouldAlert(today.AddDays(-6), today, 7));
            Assert.True(AttendanceRules.ShouldAlert(today.AddDays(-7), today, 7));
        }

        [Fact]
        public void Attendance_key_uses_daily_for_records_without_an_assignment()
        {
            var student = new EntityReference("edu_student", A);
            Assert.Equal(A.ToString("D") + "|2026-09-25|859680000|daily", AttendanceRules.Key(student, new DateTime(2026, 9, 25), AttendanceRules.Daily, null));
            Assert.Equal(A.ToString("D") + "|2026-09-25|859680001|" + B.ToString("D"), AttendanceRules.Key(student, new DateTime(2026, 9, 25), AttendanceRules.Period, new EntityReference("edu_subjectassignment", B)));
        }

        [Fact]
        public void Summary_key_is_student_and_term()
        {
            var row = new Entity("edu_attendancesummary");
            row["edu_student"] = new EntityReference("edu_student", A);
            row["edu_term"] = new EntityReference("edu_term", B);
            Assert.Equal(A.ToString("D") + "|" + B.ToString("D"), ReferenceRules.UniqueKey(AttendanceRules.Summary, row));
        }
    }
}
