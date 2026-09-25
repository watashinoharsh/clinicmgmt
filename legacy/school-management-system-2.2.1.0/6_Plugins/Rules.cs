using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

namespace SchoolMgmtPlugins
{
    /// <summary>
    /// Data integrity rules for the School Management System. Each rule receives the record as it will be
    /// after the operation (target merged over the pre-image) and returns error messages
    /// (nothing returned = valid). No rule changes data.
    /// </summary>
    public static class Rules
    {
        // Option set values (see solution): hcl_ = 813450xxx, smc_ = 813460xxx
        public const int ConsentGranted = 813450001;

        public const int ApprovalPending = 813450000;
        public const int ApprovalApproved = 813450001;
        public const int ApprovalRejected = 813450002;
        public const int ApprovalRecheck = 813450003;

        public const int IntentReturning = 813450000;
        public const int ReenrolmentBlocked = 813450001;

        public const int TreatmentMedication = 813460001;
        public const int OutcomeReturnToClass = 813460000;
        public const int OutcomeSendHome = 813460001;
        public const int OutcomeHospitalTransfer = 813460002;

        private static readonly Regex BloodPressure = new Regex(@"^\s*(\d+)\s*/\s*(\d+)\s*$", RegexOptions.Compiled);

        // ---------------------------------------------------------------- helpers
        private static DateTime? Date(Entity e, string name) { return e.GetAttributeValue<DateTime?>(name); }
        private static bool? Flag(Entity e, string name) { return e.GetAttributeValue<bool?>(name); }
        private static int? Choice(Entity e, string name)
        {
            var v = e.GetAttributeValue<OptionSetValue>(name);
            return v == null ? (int?)null : v.Value;
        }
        private static EntityReference Lookup(Entity e, string name) { return e.GetAttributeValue<EntityReference>(name); }
        private static bool HasText(Entity e, string name) { return !string.IsNullOrWhiteSpace(e.GetAttributeValue<string>(name)); }

        private static bool ContainsChoice(Entity e, string name, int value)
        {
            var c = e.GetAttributeValue<OptionSetValueCollection>(name);
            if (c == null) return false;
            foreach (var o in c) if (o.Value == value) return true;
            return false;
        }

        // ---------------------------------------------------------------- hcl_insurancepolicy
        public static IEnumerable<string> InsurancePolicy(Entity e)
        {
            var from = Date(e, "hcl_validfrom"); var to = Date(e, "hcl_validto");
            if (from.HasValue && to.HasValue && to.Value < from.Value)
                yield return "Valid To must be on or after Valid From.";
        }

        // ---------------------------------------------------------------- hcl_approvalmatrixdetail
        public static IEnumerable<string> ApprovalMatrixDetail(Entity e)
        {
            var from = Date(e, "hcl_effectivefrom"); var to = Date(e, "hcl_effectiveto");
            if (from.HasValue && to.HasValue && to.Value < from.Value)
                yield return "Effective To must be on or after Effective From.";
        }

        // ---------------------------------------------------------------- hcl_activity
        public static IEnumerable<string> Activity(Entity e)
        {
            var start = Date(e, "hcl_startson"); var end = Date(e, "hcl_endson");
            if (start.HasValue && end.HasValue && end.Value < start.Value)
                yield return "Ends On must be on or after Starts On.";
        }

        // ---------------------------------------------------------------- hcl_consentrecord
        public static IEnumerable<string> ConsentRecord(Entity e)
        {
            var status = Choice(e, "hcl_consentstatus");
            var given = Date(e, "hcl_givenon"); var expires = Date(e, "hcl_expireson");
            if (status == ConsentGranted && !given.HasValue)
                yield return "Given On is required when consent is Granted.";
            if (given.HasValue && expires.HasValue && expires.Value < given.Value)
                yield return "Expires On must be on or after Given On.";
        }

        // ---------------------------------------------------------------- hcl_approvalrequest
        public static IEnumerable<string> ApprovalRequest(Entity merged, Entity pre)
        {
            var status = Choice(merged, "hcl_approvalstatus");
            var completed = Date(merged, "hcl_completiondate");
            bool decided = status == ApprovalApproved || status == ApprovalRejected || status == ApprovalRecheck;
            if (decided && !completed.HasValue)
                yield return "Completion Date is required when the request is Approved, Rejected or set to Recheck.";
            if ((status == null || status == ApprovalPending) && completed.HasValue)
                yield return "Completion Date must be empty while the request is Pending.";
            if (pre != null)
            {
                var before = Choice(pre, "hcl_approvalstatus");
                if ((before == ApprovalApproved || before == ApprovalRejected) && before != status)
                    yield return "A decision (Approved or Rejected) has already been recorded. Create a new request instead of changing it.";
            }
        }

        // ---------------------------------------------------------------- hcl_reenrolment
        public static IEnumerable<string> Reenrolment(Entity e)
        {
            var status = Choice(e, "hcl_reenrolmentstatus");
            var intent = Choice(e, "hcl_intent");
            var ready = Flag(e, "hcl_readyforpromotion") == true;
            var blockedReason = Choice(e, "hcl_promotionblockedreason");
            if (status == ReenrolmentBlocked && blockedReason == null)
                yield return "A Promotion Blocked Reason is required when the status is Blocked.";
            if (ready)
            {
                if (intent != IntentReturning)
                    yield return "Only students whose intent is Returning can be ready for promotion.";
                if (status == ReenrolmentBlocked)
                    yield return "A blocked re-enrolment cannot be ready for promotion.";
                if (blockedReason != null)
                    yield return "Clear the Promotion Blocked Reason before marking the student ready for promotion.";
            }
        }

        // ---------------------------------------------------------------- smc_medicationtreatment
        public static IEnumerable<string> MedicationTreatment(Entity e, IDataAccess data)
        {
            var start = Date(e, "smc_startdate"); var end = Date(e, "smc_enddate");
            if (start.HasValue && end.HasValue && end.Value < start.Value)
                yield return "End Date must be on or after Start Date.";

            var visitRef = Lookup(e, "smc_medicalcentervisit");
            if (visitRef == null) yield break;
            var visit = data.Retrieve("smc_medicalcentervisit", visitRef.Id, "smc_treatmenttype", "smc_studentprofile");
            if (visit == null) yield break;
            if (!ContainsChoice(visit, "smc_treatmenttype", TreatmentMedication))
                yield return "The visit's Treatment Type must include Medication before medication can be recorded.";
            var student = Lookup(e, "smc_studentprofile");
            var visitStudent = Lookup(visit, "smc_studentprofile");
            if (student != null && visitStudent != null && student.Id != visitStudent.Id)
                yield return "The Student Profile must be the same student as the Medical Center Visit.";
        }

        // ---------------------------------------------------------------- smc_vaccinationrecord
        public static IEnumerable<string> VaccinationRecord(Entity e)
        {
            if (Flag(e, "smc_isvaccinated") == true)
            {
                if (!Date(e, "smc_administereddate").HasValue)
                    yield return "Administered Date is required when the student is recorded as vaccinated.";
                if (Flag(e, "smc_parentconsent") != true)
                    yield return "A student cannot be recorded as vaccinated without Parent Consent.";
            }
        }

        // ---------------------------------------------------------------- smc_medicalcentervisit
        public static IEnumerable<string> MedicalCenterVisit(Entity merged, IDataAccess data)
        {
            if (HasText(merged, "smc_bloodpressure"))
            {
                var m = BloodPressure.Match(merged.GetAttributeValue<string>("smc_bloodpressure"));
                if (!m.Success || int.Parse(m.Groups[1].Value) <= 0 || int.Parse(m.Groups[2].Value) <= 0)
                    yield return "Blood pressure must be in the format <systolic>/<diastolic> using whole numbers above 0, for example 120/80.";
            }

            var outcome = Choice(merged, "smc_finaloutcometype");
            bool notifyAllowed = outcome == OutcomeReturnToClass || outcome == OutcomeSendHome;
            if (!notifyAllowed && Flag(merged, "smc_notifyparent") == true)
                yield return "Notify Parent can only be set when the Final Outcome Type is Return to Class or Send Home.";
            if (!notifyAllowed && HasText(merged, "smc_notificationmessage"))
                yield return "A Notification Message can only be entered when the Final Outcome Type is Return to Class or Send Home.";
            if (outcome != OutcomeHospitalTransfer && HasText(merged, "smc_transferto"))
                yield return "Transfer To can only be entered when the Final Outcome Type is Hospital Transfer.";

            // Medication removed from Treatment Type while medication rows still exist.
            if (merged.Id != Guid.Empty && !ContainsChoice(merged, "smc_treatmenttype", TreatmentMedication)
                && data.Exists("smc_medicationtreatment", "smc_medicalcentervisit", merged.Id))
                yield return "This visit still has Medication Treatment records. Delete them first, or keep Medication selected in Treatment Type.";
        }
    }
}
