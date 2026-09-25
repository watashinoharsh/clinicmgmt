using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    /// <summary>Pure rules for the Phase 1 reference tables. No Dataverse types beyond Entity, so they are unit tested directly.</summary>
    public static class ReferenceRules
    {
        public const string AcademicYear = "edu_academicyear";
        public const string Term = "edu_term";
        public const string CalendarDay = "edu_calendarday";
        public const string Grade = "edu_grade";
        public const string Course = "edu_course";
        public const string Period = "edu_period";
        public const string GradeBand = "edu_gradeband";

        /// <summary>The generated <c>edu_uniquekey</c> for a reference row, or null for a table without one.</summary>
        public static string UniqueKey(string table, Entity row)
        {
            if (row == null) throw new ArgumentNullException("row");
            switch (table)
            {
                case AcademicYear: return EduKey.Build(row.GetAttributeValue<string>("edu_name"));
                case Term: return EduKey.Build(row.GetAttributeValue<EntityReference>("edu_academicyear"), row.GetAttributeValue<int>("edu_sequence"));
                case CalendarDay: return EduKey.Build(row.GetAttributeValue<DateTime>("edu_date"));
                case Grade: return EduKey.Build(row.GetAttributeValue<string>("edu_name"));
                case Course: return EduKey.Build(row.GetAttributeValue<string>("edu_coursecode"));
                case Period: return EduKey.Build(row.GetAttributeValue<int>("edu_sequence"));
                case GradeBand: return EduKey.Build(row.GetAttributeValue<string>("edu_letter"));
                case PeopleRules.Staff: return EduKey.Build(row.GetAttributeValue<string>("edu_staffcode"));
                case PeopleRules.Section: return EduKey.Build(row.GetAttributeValue<EntityReference>("edu_grade"), row.GetAttributeValue<EntityReference>("edu_academicyear"), row.GetAttributeValue<string>("edu_name"));
                case PeopleRules.StudentGuardian: return EduKey.Build(row.GetAttributeValue<EntityReference>("edu_student"), row.GetAttributeValue<EntityReference>("edu_guardian"));
                case PeopleRules.Enrolment: return EduKey.Build(row.GetAttributeValue<EntityReference>("edu_student"), row.GetAttributeValue<EntityReference>("edu_academicyear"));
                default: return null;
            }
        }

        /// <summary>Returns an error message, or null when the row is valid. <paramref name="otherCurrentYears"/> counts other years already flagged current.</summary>
        public static string ValidateAcademicYear(DateTime? start, DateTime? end, bool isCurrent, int otherCurrentYears)
        {
            if (start.HasValue && end.HasValue && end.Value.Date <= start.Value.Date)
                return "The academic year must end after it starts.";
            if (isCurrent && otherCurrentYears > 0)
                return "Only one academic year can be current. Clear the flag on the other year first.";
            return null;
        }

        /// <summary>Terms must sit inside their academic year and not run backwards.</summary>
        public static string ValidateTerm(DateTime? start, DateTime? end, DateTime? yearStart, DateTime? yearEnd)
        {
            if (start.HasValue && end.HasValue && end.Value.Date <= start.Value.Date)
                return "The term must end after it starts.";
            if (start.HasValue && yearStart.HasValue && start.Value.Date < yearStart.Value.Date)
                return "The term starts before its academic year.";
            if (end.HasValue && yearEnd.HasValue && end.Value.Date > yearEnd.Value.Date)
                return "The term ends after its academic year.";
            return null;
        }

        /// <summary>Bell-schedule times are stored as text, so they must be valid 24-hour HH:mm and in order.</summary>
        public static string ValidatePeriod(string start, string end)
        {
            TimeSpan s, e;
            if (!TryTime(start, out s)) return "Start time must be HH:mm (24-hour).";
            if (!TryTime(end, out e)) return "End time must be HH:mm (24-hour).";
            if (e <= s) return "The period must end after it starts.";
            return null;
        }

        private static bool TryTime(string text, out TimeSpan value)
        {
            value = TimeSpan.Zero;
            if (text == null || text.Length != 5 || text[2] != ':') return false;
            int h, m;
            if (!int.TryParse(text.Substring(0, 2), out h) || !int.TryParse(text.Substring(3, 2), out m)) return false;
            if (h < 0 || h > 23 || m < 0 || m > 59) return false;
            value = new TimeSpan(h, m, 0);
            return true;
        }

        /// <summary>Latest value of a column: what the caller is writing, else the stored value.</summary>
        public static T Merged<T>(Entity target, Entity stored, string column)
        {
            if (target != null && target.Contains(column)) return target.GetAttributeValue<T>(column);
            return stored == null ? default(T) : stored.GetAttributeValue<T>(column);
        }

        public static Entity Merge(Entity target, Entity stored)
        {
            var merged = new Entity(target.LogicalName, target.Id);
            if (stored != null) foreach (KeyValuePair<string, object> kv in stored.Attributes) merged[kv.Key] = kv.Value;
            foreach (KeyValuePair<string, object> kv in target.Attributes) merged[kv.Key] = kv.Value;
            return merged;
        }
    }
}
