using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace EduCore.Plugins
{
    /// <summary>
    /// Pre-operation plug-in for the Phase 1 reference tables (create and update). Fills <c>edu_uniquekey</c>
    /// and enforces the table's validation rule. Register one step per table with a pre-image named "pre"
    /// on update. The class only reads the context; the rules live in <see cref="ReferenceRules"/>.
    /// </summary>
    public sealed class ReferenceDataPlugin : PluginBase
    {
        public ReferenceDataPlugin() : base(typeof(ReferenceDataPlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext ctx)
        {
            var context = ctx.PluginExecutionContext;
            var target = context.InputParameters.Contains("Target") ? context.InputParameters["Target"] as Entity : null;
            if (target == null) return;

            Entity pre = context.PreEntityImages.Contains("pre") ? context.PreEntityImages["pre"] : null;
            var row = ReferenceRules.Merge(target, pre);
            var data = new OrgDataAccess(ctx.InitiatingUserService);

            string error = Validate(row, target.Id, data);
            if (error != null) throw new InvalidPluginExecutionException(error);

            string key = ReferenceRules.UniqueKey(target.LogicalName, row);
            if (key != null) target["edu_uniquekey"] = key;
        }

        private static string Validate(Entity row, Guid id, IDataAccess data)
        {
            switch (row.LogicalName)
            {
                case ReferenceRules.AcademicYear:
                    bool isCurrent = row.GetAttributeValue<bool>("edu_iscurrent");
                    int others = isCurrent ? data.CountOthers(row.LogicalName, "edu_iscurrent", true, id) : 0;
                    return ReferenceRules.ValidateAcademicYear(row.GetAttributeValue<DateTime?>("edu_startdate"), row.GetAttributeValue<DateTime?>("edu_enddate"), isCurrent, others);
                case ReferenceRules.Term:
                    var year = row.GetAttributeValue<EntityReference>("edu_academicyear");
                    Entity yearRow = year == null ? null : data.Retrieve(ReferenceRules.AcademicYear, year.Id, "edu_startdate", "edu_enddate");
                    return ReferenceRules.ValidateTerm(row.GetAttributeValue<DateTime?>("edu_startdate"), row.GetAttributeValue<DateTime?>("edu_enddate"),
                        yearRow == null ? (DateTime?)null : yearRow.GetAttributeValue<DateTime?>("edu_startdate"),
                        yearRow == null ? (DateTime?)null : yearRow.GetAttributeValue<DateTime?>("edu_enddate"));
                case ReferenceRules.Period:
                    return ReferenceRules.ValidatePeriod(row.GetAttributeValue<string>("edu_starttime"), row.GetAttributeValue<string>("edu_endtime"));
                default:
                    return null;
            }
        }
    }
}
