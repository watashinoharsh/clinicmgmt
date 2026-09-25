using System;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    /// <summary>Pure rules for the Phase 2 people tables (student guardian, enrolment). Tested without Dataverse.</summary>
    public static class PeopleRules
    {
        public const string Student = "edu_student";
        public const string StudentGuardian = "edu_studentguardian";
        public const string Section = "edu_section";
        public const string Enrolment = "edu_enrolment";
        public const string Staff = "edu_staff";

        /// <summary>statecode 0 is Active. A row with no state yet (create) counts as Active.</summary>
        public static bool IsActive(OptionSetValue state)
        {
            return state == null || state.Value == 0;
        }

        // ---- student guardian ----

        /// <summary>Exactly one guardian is primary per student. Setting a second primary is rejected.</summary>
        public static string ValidatePrimary(bool isPrimary, int otherPrimaries)
        {
            return isPrimary && otherPrimaries > 0
                ? "This student already has a primary guardian. Clear it on the other link first."
                : null;
        }

        /// <summary>The first guardian linked to a student becomes primary when the caller did not choose.</summary>
        public static bool DefaultPrimary(bool? requested, int existingLinks)
        {
            if (requested.HasValue) return requested.Value;
            return existingLinks == 0;
        }

        public static string LinkName(string student, string guardian)
        {
            string s = string.IsNullOrWhiteSpace(student) ? "?" : student.Trim();
            string g = string.IsNullOrWhiteSpace(guardian) ? "?" : guardian.Trim();
            string name = s + " - " + g;
            return name.Length <= 200 ? name : name.Substring(0, 200);
        }

        // ---- enrolment ----

        public static string ValidateDates(DateTime? start, DateTime? end)
        {
            return start.HasValue && end.HasValue && end.Value.Date < start.Value.Date
                ? "The enrolment cannot end before it starts."
                : null;
        }

        /// <summary>The section must belong to the enrolment's academic year.</summary>
        public static string ValidateSectionYear(Guid? sectionYear, Guid? enrolmentYear)
        {
            if (!sectionYear.HasValue || !enrolmentYear.HasValue) return null;
            return sectionYear.Value == enrolmentYear.Value
                ? null
                : "The section belongs to a different academic year from this enrolment.";
        }

        /// <summary>One Active enrolment per student per academic year.</summary>
        public static string ValidateOneActive(bool isActive, int otherActiveInYear)
        {
            return isActive && otherActiveInYear > 0
                ? "This student already has an active enrolment in this academic year."
                : null;
        }

        /// <summary>Active enrolments in a section cannot exceed its capacity.</summary>
        public static string ValidateCapacity(bool isActive, int? capacity, int otherActiveInSection)
        {
            if (!isActive || !capacity.HasValue) return null;
            return otherActiveInSection + 1 > capacity.Value
                ? "The section is full (capacity " + capacity.Value + ")."
                : null;
        }

        /// <summary>A student's current placement comes from the latest Active enrolment; both are cleared when there is none.</summary>
        public static void ApplyPlacement(Entity studentUpdate, EntityReference section, EntityReference grade)
        {
            if (studentUpdate == null) throw new ArgumentNullException("studentUpdate");
            studentUpdate["edu_currentsection"] = section;
            studentUpdate["edu_currentgrade"] = grade;
        }
    }
}
