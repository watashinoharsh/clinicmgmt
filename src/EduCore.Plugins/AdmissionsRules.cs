using System;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    /// <summary>Pure rules for admissions: the applicant status machine and the applicant-to-student conversion.</summary>
    public static class AdmissionsRules
    {
        public const string Applicant = "edu_applicant";

        // statuscode values of edu_applicant. Applied, Under Review and Offer Made are Active; the rest are Inactive.
        public const int Applied = 1;
        public const int UnderReview = 859680001;
        public const int OfferMade = 859680002;
        public const int Enrolled = 2;
        public const int Rejected = 859680003;
        public const int Withdrawn = 859680004;

        /// <summary>statecode for a status: Active (0) for the first three, Inactive (1) otherwise.</summary>
        public static int StateOf(int status)
        {
            return status == Applied || status == UnderReview || status == OfferMade ? 0 : 1;
        }

        private static bool Allowed(int from, int to)
        {
            switch (from)
            {
                case Applied: return to == UnderReview || to == Withdrawn;
                case UnderReview: return to == OfferMade || to == Rejected || to == Withdrawn;
                case OfferMade: return to == Enrolled || to == Rejected || to == Withdrawn;
                default: return false; // Enrolled, Rejected and Withdrawn are terminal
            }
        }

        /// <summary><paramref name="from"/> is null on create, which must start at Applied. An unchanged status is always fine.</summary>
        public static string ValidateTransition(int? from, int to)
        {
            if (!from.HasValue) return to == Applied ? null : "A new applicant must start as Applied.";
            if (from.Value == to) return null;
            return Allowed(from.Value, to) ? null : "An applicant cannot move from " + Name(from.Value) + " to " + Name(to) + ".";
        }

        /// <summary>Enrolled is valid only when a student created from this applicant exists.</summary>
        public static string ValidateEnrolled(int status, bool studentExists)
        {
            return status == Enrolled && !studentExists
                ? "An applicant can be Enrolled only through the Enrol action, which creates the student."
                : null;
        }

        public static string ValidateRejected(int status, string reason)
        {
            return status == Rejected && string.IsNullOrWhiteSpace(reason)
                ? "A decision reason is required when an applicant is rejected."
                : null;
        }

        public static string Name(int status)
        {
            switch (status)
            {
                case Applied: return "Applied";
                case UnderReview: return "Under Review";
                case OfferMade: return "Offer Made";
                case Enrolled: return "Enrolled";
                case Rejected: return "Rejected";
                case Withdrawn: return "Withdrawn";
                default: return "status " + status;
            }
        }

        // ---- edu_EnrolApplicant ----

        /// <summary>Checks an applicant can be enrolled into a section: Offer Made, same academic year and grade.</summary>
        public static string ValidateEnrol(int status, EntityReference applicantYear, EntityReference applicantGrade, EntityReference sectionYear, EntityReference sectionGrade)
        {
            if (status != OfferMade) return "Only an applicant with an offer can be enrolled (status is " + Name(status) + ").";
            if (applicantYear != null && sectionYear != null && applicantYear.Id != sectionYear.Id)
                return "The section belongs to a different academic year from the application.";
            if (applicantGrade != null && sectionGrade != null && applicantGrade.Id != sectionGrade.Id)
                return "The section is for a different grade from the one applied for.";
            return null;
        }

        /// <summary>The student to create. Its id is fixed up front so the link and enrolment can reference it in the same transaction.</summary>
        public static Entity BuildStudent(Entity applicant, DateTime start)
        {
            if (applicant == null) throw new ArgumentNullException("applicant");
            var student = new Entity(PeopleRules.Student, Guid.NewGuid());
            student["edu_name"] = applicant.GetAttributeValue<string>("edu_name");
            student["edu_dateofbirth"] = applicant.GetAttributeValue<DateTime?>("edu_dateofbirth");
            student["edu_enrolmentdate"] = start.Date;
            student["edu_sourceapplicant"] = new EntityReference(Applicant, applicant.Id);
            return student;
        }

        public static Entity BuildGuardianLink(EntityReference student, EntityReference guardian, int relationship)
        {
            var link = new Entity(PeopleRules.StudentGuardian);
            link["edu_student"] = student;
            link["edu_guardian"] = guardian;
            link["edu_relationship"] = new OptionSetValue(relationship);
            link["edu_isprimary"] = true;
            link["edu_emergencycontact"] = true;
            link["edu_pickupauthorised"] = true;
            return link;
        }

        public static Entity BuildEnrolment(EntityReference student, EntityReference section, EntityReference year, DateTime start, string studentName)
        {
            var e = new Entity(PeopleRules.Enrolment);
            e["edu_name"] = (studentName ?? "Student") + " - enrolment";
            e["edu_student"] = student;
            e["edu_section"] = section;
            e["edu_academicyear"] = year;
            e["edu_startdate"] = start.Date;
            return e;
        }

        public static Entity MarkEnrolled(Guid applicantId)
        {
            var update = new Entity(Applicant, applicantId);
            update["statecode"] = new OptionSetValue(StateOf(Enrolled));
            update["statuscode"] = new OptionSetValue(Enrolled);
            return update;
        }
    }
}
