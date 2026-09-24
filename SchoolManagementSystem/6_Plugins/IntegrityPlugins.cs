using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace SchoolMgmtPlugins
{
    /// <summary>
    /// Base class for the data integrity plug-ins. Register each derived class on Create and Update of its table,
    /// stage PreOperation (20), synchronous. For Update, register a pre-image named "PreImage" with all columns.
    /// The rules run on the record as it will be after the operation and reject invalid data with a clear message.
    /// </summary>
    public abstract class IntegrityPluginBase : PluginBase
    {
        private const int MaxDepth = 8;

        protected IntegrityPluginBase(Type pluginType) : base(pluginType) { }

        protected abstract string EntityName { get; }

        protected abstract IEnumerable<string> Check(Entity merged, Entity preImage, IDataAccess data);

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null) throw new ArgumentNullException("localPluginContext");
            var context = localPluginContext.PluginExecutionContext;

            if (context.Depth > MaxDepth) return;
            if (!context.InputParameters.Contains("Target")) return;
            var target = context.InputParameters["Target"] as Entity;
            if (target == null || target.LogicalName != EntityName) return;

            var message = context.MessageName;
            if (message != "Create" && message != "Update") return;

            Entity pre = null;
            if (message == "Update" && context.PreEntityImages.Contains("PreImage"))
                pre = context.PreEntityImages["PreImage"];

            var merged = new Entity(target.LogicalName, target.Id);
            if (pre != null) foreach (var a in pre.Attributes) merged[a.Key] = a.Value;
            foreach (var a in target.Attributes) merged[a.Key] = a.Value;

            var errors = Check(merged, pre, new OrgDataAccess(localPluginContext.PluginUserService)).ToList();
            if (errors.Count == 0) return;

            localPluginContext.Trace(EntityName + " rejected: " + string.Join(" | ", errors));
            throw new InvalidPluginExecutionException(string.Join(Environment.NewLine, errors));
        }
    }

    public class InsurancePolicyIntegrityPlugin : IntegrityPluginBase
    {
        public InsurancePolicyIntegrityPlugin(string unsecure, string secure) : base(typeof(InsurancePolicyIntegrityPlugin)) { }
        protected override string EntityName { get { return "hcl_insurancepolicy"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.InsurancePolicy(m); }
    }

    public class ApprovalMatrixDetailIntegrityPlugin : IntegrityPluginBase
    {
        public ApprovalMatrixDetailIntegrityPlugin(string unsecure, string secure) : base(typeof(ApprovalMatrixDetailIntegrityPlugin)) { }
        protected override string EntityName { get { return "hcl_approvalmatrixdetail"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.ApprovalMatrixDetail(m); }
    }

    public class ActivityIntegrityPlugin : IntegrityPluginBase
    {
        public ActivityIntegrityPlugin(string unsecure, string secure) : base(typeof(ActivityIntegrityPlugin)) { }
        protected override string EntityName { get { return "hcl_activity"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.Activity(m); }
    }

    public class ConsentRecordIntegrityPlugin : IntegrityPluginBase
    {
        public ConsentRecordIntegrityPlugin(string unsecure, string secure) : base(typeof(ConsentRecordIntegrityPlugin)) { }
        protected override string EntityName { get { return "hcl_consentrecord"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.ConsentRecord(m); }
    }

    public class ApprovalRequestIntegrityPlugin : IntegrityPluginBase
    {
        public ApprovalRequestIntegrityPlugin(string unsecure, string secure) : base(typeof(ApprovalRequestIntegrityPlugin)) { }
        protected override string EntityName { get { return "hcl_approvalrequest"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.ApprovalRequest(m, p); }
    }

    public class ReenrolmentIntegrityPlugin : IntegrityPluginBase
    {
        public ReenrolmentIntegrityPlugin(string unsecure, string secure) : base(typeof(ReenrolmentIntegrityPlugin)) { }
        protected override string EntityName { get { return "hcl_reenrolment"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.Reenrolment(m); }
    }

    public class MedicationTreatmentIntegrityPlugin : IntegrityPluginBase
    {
        public MedicationTreatmentIntegrityPlugin(string unsecure, string secure) : base(typeof(MedicationTreatmentIntegrityPlugin)) { }
        protected override string EntityName { get { return "smc_medicationtreatment"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.MedicationTreatment(m, d); }
    }

    public class VaccinationRecordIntegrityPlugin : IntegrityPluginBase
    {
        public VaccinationRecordIntegrityPlugin(string unsecure, string secure) : base(typeof(VaccinationRecordIntegrityPlugin)) { }
        protected override string EntityName { get { return "smc_vaccinationrecord"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.VaccinationRecord(m); }
    }

    public class MedicalCenterVisitIntegrityPlugin : IntegrityPluginBase
    {
        public MedicalCenterVisitIntegrityPlugin(string unsecure, string secure) : base(typeof(MedicalCenterVisitIntegrityPlugin)) { }
        protected override string EntityName { get { return "smc_medicalcentervisit"; } }
        protected override IEnumerable<string> Check(Entity m, Entity p, IDataAccess d) { return Rules.MedicalCenterVisit(m, d); }
    }
}
