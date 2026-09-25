using System;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    /// <summary>
    /// Pre-operation plug-in for the reference and people tables that only need a generated key and a simple check
    /// (create and update). Fills <c>edu_uniquekey</c>, enforces the table's validation rule and, for an academic year
    /// flagged current, clears the flag on every other year in the same transaction. Register one step per table with a
    /// pre-image named "pre" on update. The rules live in <see cref="ReferenceRules"/>.
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

            string error = Validate(row, data);
            if (error != null) throw new InvalidPluginExecutionException(error);

            if (row.LogicalName == ReferenceRules.AcademicYear && row.GetAttributeValue<bool>("edu_iscurrent"))
                ClearOtherCurrentYears(target.Id, data, ctx.InitiatingUserService);

            string key = ReferenceRules.UniqueKey(target.LogicalName, row);
            if (key != null) target["edu_uniquekey"] = key;
        }

        private static string Validate(Entity row, IDataAccess data)
        {
            switch (row.LogicalName)
            {
                case ReferenceRules.AcademicYear:
                    return ReferenceRules.ValidateAcademicYear(row.GetAttributeValue<DateTime?>("edu_startdate"), row.GetAttributeValue<DateTime?>("edu_enddate"));
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

        private static void ClearOtherCurrentYears(Guid keepId, IDataAccess data, IOrganizationService service)
        {
            foreach (Guid id in data.FindIds(ReferenceRules.AcademicYear, keepId, Tuple.Create<string, object>("edu_iscurrent", true)))
            {
                var other = new Entity(ReferenceRules.AcademicYear, id);
                other["edu_iscurrent"] = false;
                service.Update(other);
            }
        }
    }
}
