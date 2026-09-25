using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    public sealed class TermInfo
    {
        public Guid Id { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        public TermInfo(Guid id, DateTime start, DateTime end) { Id = id; Start = start.Date; End = end.Date; }
    }

    /// <summary>Counts of daily attendance records for one student and term.</summary>
    public sealed class AttendanceCounts
    {
        public int Present, Late, Absent, Excused;
    }

    /// <summary>Pure attendance rules (LLD assumption A3): attended = Present + Late; Excused leaves the denominator.</summary>
    public static class AttendanceRules
    {
        public const string Attendance = "edu_attendance";
        public const string Summary = "edu_attendancesummary";

        // edu_attendancetype
        public const int Daily = 859680000;
        public const int Period = 859680001;
        // edu_attendancestatus
        public const int Present = 859680000;
        public const int Late = 859680001;
        public const int Absent = 859680002;
        public const int Excused = 859680003;

        /// <summary>The term whose dates contain <paramref name="date"/>, or null when none does.</summary>
        public static Guid? TermFor(DateTime date, IEnumerable<TermInfo> terms)
        {
            foreach (var t in terms)
                if (date.Date >= t.Start && date.Date <= t.End) return t.Id;
            return null;
        }

        /// <summary>A period record needs the lesson it belongs to; a daily record must not carry one.</summary>
        public static string ValidateRecord(int type, bool hasAssignment)
        {
            if (type == Period && !hasAssignment) return "A period attendance record needs a subject assignment.";
            if (type == Daily && hasAssignment) return "A daily attendance record cannot have a subject assignment.";
            return null;
        }

        /// <summary>Weekdays (Monday to Friday) from the term start to the earlier of the term end and <paramref name="asOf"/>, minus closures.</summary>
        public static int TeachingDays(DateTime termStart, DateTime termEnd, DateTime asOf, IEnumerable<DateTime> nonTeachingDays)
        {
            var closed = new HashSet<DateTime>();
            if (nonTeachingDays != null) foreach (var d in nonTeachingDays) closed.Add(d.Date);
            DateTime last = asOf.Date < termEnd.Date ? asOf.Date : termEnd.Date;
            int days = 0;
            for (DateTime d = termStart.Date; d <= last; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday) continue;
                if (closed.Contains(d)) continue;
                days++;
            }
            return days;
        }

        public static void Count(AttendanceCounts counts, int status)
        {
            switch (status)
            {
                case Present: counts.Present++; break;
                case Late: counts.Late++; break;
                case Absent: counts.Absent++; break;
                case Excused: counts.Excused++; break;
            }
        }

        /// <summary>(present + late) / (teaching days - excused) as a percentage to two places; null when nothing is countable.</summary>
        public static decimal? Percentage(AttendanceCounts c, int teachingDays)
        {
            int denominator = teachingDays - c.Excused;
            if (denominator <= 0) return null;
            decimal pct = (decimal)(c.Present + c.Late) / denominator * 100m;
            if (pct > 100m) pct = 100m;
            return Math.Round(pct, 2, MidpointRounding.AwayFromZero);
        }

        public static bool BelowThreshold(decimal? percentage, decimal threshold)
        {
            return percentage.HasValue && percentage.Value < threshold;
        }

        /// <summary>Alert at most once per <paramref name="dedupeDays"/> days for the same student.</summary>
        public static bool ShouldAlert(DateTime? lastAlertOn, DateTime today, int dedupeDays)
        {
            return !lastAlertOn.HasValue || (today.Date - lastAlertOn.Value.Date).TotalDays >= dedupeDays;
        }

        public static string Key(EntityReference student, DateTime date, int type, EntityReference assignment)
        {
            return EduKey.Build(student, date, type, EduKey.Or(assignment, "daily"));
        }
    }
}
