using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    /// <summary>One grade band: a percentage floor and its letter. Kept plain so the rules are testable without Dataverse.</summary>
    public sealed class GradeBandInfo
    {
        public decimal Min { get; private set; }
        public string Letter { get; private set; }
        public GradeBandInfo(decimal min, string letter) { Min = min; Letter = letter; }
    }

    /// <summary>Pure rules for academics: timetable clashes, marks validation, percentage and grade letter.</summary>
    public static class AcademicsRules
    {
        public const string SubjectAssignment = "edu_subjectassignment";
        public const string ClassSchedule = "edu_classschedule";
        public const string Assessment = "edu_assessment";
        public const string StudentMark = "edu_studentmark";

        // ---- timetable ----

        /// <summary>Counts are other slots in the same term, weekday and period. Any non-zero count is a clash.</summary>
        public static string ValidateClash(int sameSection, int sameTeacher, int sameRoom)
        {
            if (sameSection > 0) return "The section already has a lesson in this period.";
            if (sameTeacher > 0) return "The teacher already has a lesson in this period.";
            if (sameRoom > 0) return "The room is already in use in this period.";
            return null;
        }

        // ---- marks ----

        public static string ValidateMarks(decimal? marks, decimal maxMarks)
        {
            if (!marks.HasValue) return null;
            if (marks.Value < 0) return "Marks cannot be negative.";
            if (marks.Value > maxMarks) return "Marks (" + marks.Value + ") cannot exceed the maximum (" + maxMarks + ").";
            return null;
        }

        public static string ValidateMaxMarks(decimal maxMarks)
        {
            return maxMarks > 0 ? null : "Maximum marks must be greater than zero.";
        }

        public static string ValidateWeightage(decimal weightage)
        {
            return weightage >= 0 && weightage <= 100 ? null : "Weightage must be between 0 and 100.";
        }

        /// <summary>Changing an assessment's maximum is blocked once marks exist for it.</summary>
        public static string ValidateMaxChange(decimal? oldMax, decimal newMax, bool marksExist)
        {
            return marksExist && oldMax.HasValue && oldMax.Value != newMax
                ? "The maximum marks cannot change once marks have been entered."
                : null;
        }

        public static decimal? Percentage(decimal? marks, decimal maxMarks)
        {
            if (!marks.HasValue || maxMarks <= 0) return null;
            return Math.Round(marks.Value / maxMarks * 100m, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>The letter of the highest band whose minimum the percentage reaches; null when there is no band.</summary>
        public static string GradeLetter(decimal? percentage, IEnumerable<GradeBandInfo> bands)
        {
            if (!percentage.HasValue || bands == null) return null;
            GradeBandInfo best = null;
            foreach (var b in bands)
                if (percentage.Value >= b.Min && (best == null || b.Min > best.Min)) best = b;
            return best == null ? null : best.Letter;
        }
    }
}
