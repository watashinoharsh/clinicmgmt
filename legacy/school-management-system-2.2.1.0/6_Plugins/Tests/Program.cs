using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using SchoolMgmtPlugins;

/// <summary>Plain console test runner: each case names the rule, the record and the expected error count.</summary>
internal static class Program
{
    private sealed class FakeData : IDataAccess
    {
        public Dictionary<Guid, Entity> Visits = new Dictionary<Guid, Entity>();
        public HashSet<Guid> VisitsWithMedication = new HashSet<Guid>();
        public Entity Retrieve(string entityName, Guid id, params string[] columns)
        {
            Entity e; return Visits.TryGetValue(id, out e) ? e : null;
        }
        public bool Exists(string entityName, string lookupAttribute, Guid id) { return VisitsWithMedication.Contains(id); }
    }

    private static int _pass, _fail;
    private static readonly DateTime D1 = new DateTime(2026, 1, 1), D2 = new DateTime(2026, 2, 1);

    private static Entity E(string name, params object[] kv)
    {
        var e = new Entity(name, Guid.NewGuid());
        for (int i = 0; i < kv.Length; i += 2) e[(string)kv[i]] = kv[i + 1];
        return e;
    }
    private static OptionSetValue O(int v) { return new OptionSetValue(v); }
    private static OptionSetValueCollection Multi(params int[] v) { return new OptionSetValueCollection(v.Select(x => new OptionSetValue(x)).ToList()); }

    private static void Check(string name, IEnumerable<string> errors, int expected, string mustContain = null)
    {
        var list = errors.ToList();
        bool ok = list.Count == expected && (mustContain == null || list.Any(m => m.Contains(mustContain)));
        if (ok) _pass++; else _fail++;
        Console.WriteLine((ok ? "PASS  " : "FAIL  ") + name + (ok ? "" : "  -> got " + list.Count + " error(s): " + string.Join(" | ", list)));
    }

    private static int Main()
    {
        var data = new FakeData();

        // insurance policy / matrix detail / activity dates
        Check("insurance valid dates", Rules.InsurancePolicy(E("hcl_insurancepolicy", "hcl_validfrom", D1, "hcl_validto", D2)), 0);
        Check("insurance valid-to before valid-from", Rules.InsurancePolicy(E("hcl_insurancepolicy", "hcl_validfrom", D2, "hcl_validto", D1)), 1, "Valid To");
        Check("insurance open-ended (no valid-to)", Rules.InsurancePolicy(E("hcl_insurancepolicy", "hcl_validfrom", D1)), 0);
        Check("matrix detail bad dates", Rules.ApprovalMatrixDetail(E("hcl_approvalmatrixdetail", "hcl_effectivefrom", D2, "hcl_effectiveto", D1)), 1);
        Check("activity ends before start", Rules.Activity(E("hcl_activity", "hcl_startson", D2, "hcl_endson", D1)), 1);
        Check("activity valid", Rules.Activity(E("hcl_activity", "hcl_startson", D1, "hcl_endson", D2)), 0);

        // consent
        Check("consent granted without given-on", Rules.ConsentRecord(E("hcl_consentrecord", "hcl_consentstatus", O(Rules.ConsentGranted))), 1, "Given On");
        Check("consent granted with given-on", Rules.ConsentRecord(E("hcl_consentrecord", "hcl_consentstatus", O(Rules.ConsentGranted), "hcl_givenon", D1)), 0);
        Check("consent expires before given", Rules.ConsentRecord(E("hcl_consentrecord", "hcl_givenon", D2, "hcl_expireson", D1)), 1, "Expires On");
        Check("consent requested (no dates)", Rules.ConsentRecord(E("hcl_consentrecord", "hcl_consentstatus", O(813450000))), 0);

        // approval request
        Check("approval pending, no date", Rules.ApprovalRequest(E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalPending)), null), 0);
        Check("approval approved without completion date", Rules.ApprovalRequest(E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalApproved)), null), 1, "Completion Date is required");
        Check("approval pending with completion date", Rules.ApprovalRequest(E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalPending), "hcl_completiondate", D1), null), 1, "must be empty");
        Check("approval approved with date", Rules.ApprovalRequest(E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalApproved), "hcl_completiondate", D1), null), 0);
        var decided = E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalApproved), "hcl_completiondate", D1);
        Check("approved request changed to rejected", Rules.ApprovalRequest(E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalRejected), "hcl_completiondate", D1), decided), 1, "already been recorded");
        Check("approved request comment edit (status unchanged)", Rules.ApprovalRequest(E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalApproved), "hcl_completiondate", D1), decided), 0);
        var recheck = E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalRecheck), "hcl_completiondate", D1);
        Check("recheck moved to approved", Rules.ApprovalRequest(E("hcl_approvalrequest", "hcl_approvalstatus", O(Rules.ApprovalApproved), "hcl_completiondate", D2), recheck), 0);

        // re-enrolment
        Check("re-enrolment blocked without reason", Rules.Reenrolment(E("hcl_reenrolment", "hcl_reenrolmentstatus", O(Rules.ReenrolmentBlocked))), 1, "Blocked Reason");
        Check("re-enrolment blocked with reason", Rules.Reenrolment(E("hcl_reenrolment", "hcl_reenrolmentstatus", O(Rules.ReenrolmentBlocked), "hcl_promotionblockedreason", O(813450003))), 0);
        Check("ready + returning + no block", Rules.Reenrolment(E("hcl_reenrolment", "hcl_readyforpromotion", true, "hcl_intent", O(Rules.IntentReturning), "hcl_reenrolmentstatus", O(813450000))), 0);
        Check("ready but not returning", Rules.Reenrolment(E("hcl_reenrolment", "hcl_readyforpromotion", true, "hcl_intent", O(813450001))), 1, "Returning");
        Check("ready but blocked with reason (3 issues)", Rules.Reenrolment(E("hcl_reenrolment", "hcl_readyforpromotion", true, "hcl_intent", O(Rules.IntentReturning), "hcl_reenrolmentstatus", O(Rules.ReenrolmentBlocked), "hcl_promotionblockedreason", O(813450000))), 2);

        // medication treatment
        var studentA = new EntityReference("smc_studentprofile", Guid.NewGuid());
        var visitWithMed = E("smc_medicalcentervisit", "smc_treatmenttype", Multi(Rules.TreatmentMedication, 813460000), "smc_studentprofile", studentA);
        var visitNoMed = E("smc_medicalcentervisit", "smc_treatmenttype", Multi(813460000), "smc_studentprofile", studentA);
        data.Visits[visitWithMed.Id] = visitWithMed; data.Visits[visitNoMed.Id] = visitNoMed;
        Check("treatment on visit with Medication", Rules.MedicationTreatment(E("smc_medicationtreatment", "smc_medicalcentervisit", visitWithMed.ToEntityReference(), "smc_studentprofile", studentA), data), 0);
        Check("treatment on visit WITHOUT Medication", Rules.MedicationTreatment(E("smc_medicationtreatment", "smc_medicalcentervisit", visitNoMed.ToEntityReference(), "smc_studentprofile", studentA), data), 1, "must include Medication");
        Check("treatment for a different student", Rules.MedicationTreatment(E("smc_medicationtreatment", "smc_medicalcentervisit", visitWithMed.ToEntityReference(), "smc_studentprofile", new EntityReference("smc_studentprofile", Guid.NewGuid())), data), 1, "same student");
        Check("treatment end before start", Rules.MedicationTreatment(E("smc_medicationtreatment", "smc_medicalcentervisit", visitWithMed.ToEntityReference(), "smc_studentprofile", studentA, "smc_startdate", D2, "smc_enddate", D1), data), 1, "End Date");
        Check("treatment with no visit yet (nothing to check)", Rules.MedicationTreatment(E("smc_medicationtreatment"), data), 0);

        // vaccination record
        Check("vaccinated with consent and date", Rules.VaccinationRecord(E("smc_vaccinationrecord", "smc_isvaccinated", true, "smc_parentconsent", true, "smc_administereddate", D1)), 0);
        Check("vaccinated without date", Rules.VaccinationRecord(E("smc_vaccinationrecord", "smc_isvaccinated", true, "smc_parentconsent", true)), 1, "Administered Date");
        Check("vaccinated without consent", Rules.VaccinationRecord(E("smc_vaccinationrecord", "smc_isvaccinated", true, "smc_administereddate", D1)), 1, "Parent Consent");
        Check("not vaccinated (no requirements)", Rules.VaccinationRecord(E("smc_vaccinationrecord", "smc_isvaccinated", false)), 0);

        // medical center visit
        Check("blood pressure 120/80", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_bloodpressure", "120/80"), data), 0);
        Check("blood pressure ' 90 / 60 '", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_bloodpressure", " 90 / 60 "), data), 0);
        Check("blood pressure 12080", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_bloodpressure", "12080"), data), 1, "Blood pressure");
        Check("blood pressure 0/80", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_bloodpressure", "0/80"), data), 1);
        Check("blood pressure 120/-80", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_bloodpressure", "120/-80"), data), 1);
        Check("notify parent with Return to Class", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_finaloutcometype", O(Rules.OutcomeReturnToClass), "smc_notifyparent", true, "smc_notificationmessage", "Hello"), data), 0);
        Check("notify parent with Hospital Transfer", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_finaloutcometype", O(Rules.OutcomeHospitalTransfer), "smc_notifyparent", true), data), 1, "Notify Parent");
        Check("message with no outcome", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_notificationmessage", "Hello"), data), 1, "Notification Message");
        Check("transfer-to with Hospital Transfer", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_finaloutcometype", O(Rules.OutcomeHospitalTransfer), "smc_transferto", "City Hospital"), data), 0);
        Check("transfer-to with Send Home", Rules.MedicalCenterVisit(E("smc_medicalcentervisit", "smc_finaloutcometype", O(Rules.OutcomeSendHome), "smc_transferto", "City Hospital"), data), 1, "Transfer To");
        var v = E("smc_medicalcentervisit", "smc_treatmenttype", Multi(813460000)); data.VisitsWithMedication.Add(v.Id);
        Check("Medication removed while treatment rows exist", Rules.MedicalCenterVisit(v, data), 1, "Delete them first");
        var v2 = E("smc_medicalcentervisit", "smc_treatmenttype", Multi(Rules.TreatmentMedication)); data.VisitsWithMedication.Add(v2.Id);
        Check("Medication kept with treatment rows", Rules.MedicalCenterVisit(v2, data), 0);
        var v3 = new Entity("smc_medicalcentervisit"); v3["smc_treatmenttype"] = Multi(813460000);
        Check("new visit (no id) without Medication", Rules.MedicalCenterVisit(v3, data), 0);

        Console.WriteLine();
        Console.WriteLine("Passed: " + _pass + "  Failed: " + _fail);
        return _fail == 0 ? 0 : 1;
    }
}
