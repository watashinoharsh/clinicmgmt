using System;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    /// <summary>
    /// Pre-operation, create and update on edu_studentguardian (pre-image "pre" on update).
    /// Names the link, defaults the first guardian to primary, and allows one primary per student.
    /// </summary>
    public sealed class StudentGuardianPlugin : PluginBase
    {
        public StudentGuardianPlugin() : base(typeof(StudentGuardianPlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext ctx)
        {
            var context = ctx.PluginExecutionContext;
            var target = context.InputParameters.Contains("Target") ? context.InputParameters["Target"] as Entity : null;
            if (target == null) return;
            Entity pre = context.PreEntityImages.Contains("pre") ? context.PreEntityImages["pre"] : null;
            var row = ReferenceRules.Merge(target, pre);
            var data = new OrgDataAccess(ctx.InitiatingUserService);

            var student = row.GetAttributeValue<EntityReference>("edu_student");
            var guardian = row.GetAttributeValue<EntityReference>("edu_guardian");
            if (student == null || guardian == null) return;

            bool creating = context.MessageName == "Create";
            if (creating && !target.Contains("edu_isprimary"))
            {
                int existing = data.CountWhere(PeopleRules.StudentGuardian, Guid.Empty, Tuple.Create<string, object>("edu_student", student.Id));
                row["edu_isprimary"] = target["edu_isprimary"] = PeopleRules.DefaultPrimary(null, existing);
            }

            bool isPrimary = row.GetAttributeValue<bool>("edu_isprimary");
            int others = isPrimary
                ? data.CountWhere(PeopleRules.StudentGuardian, target.Id, Tuple.Create<string, object>("edu_student", student.Id), Tuple.Create<string, object>("edu_isprimary", true))
                : 0;
            string error = PeopleRules.ValidatePrimary(isPrimary, others);
            if (error != null) throw new InvalidPluginExecutionException(error);

            if (!target.Contains("edu_name"))
            {
                var s = data.Retrieve(PeopleRules.Student, student.Id, "edu_name");
                var g = data.Retrieve("contact", guardian.Id, "fullname");
                target["edu_name"] = PeopleRules.LinkName(s == null ? null : s.GetAttributeValue<string>("edu_name"), g == null ? null : g.GetAttributeValue<string>("fullname"));
            }
            target["edu_uniquekey"] = ReferenceRules.UniqueKey(PeopleRules.StudentGuardian, row);
        }
    }

    /// <summary>
    /// Enrolment rules. Register pre-operation (stage 20) on create and update with pre-image "pre", and
    /// post-operation (stage 40) on create, update and delete (delete needs pre-image "pre" too).
    /// Pre-operation validates and fills edu_uniquekey; post-operation refreshes the student's current placement.
    /// </summary>
    public sealed class EnrolmentPlugin : PluginBase
    {
        public EnrolmentPlugin() : base(typeof(EnrolmentPlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext ctx)
        {
            var context = ctx.PluginExecutionContext;
            var data = new OrgDataAccess(ctx.InitiatingUserService);
            Entity pre = context.PreEntityImages.Contains("pre") ? context.PreEntityImages["pre"] : null;

            if (context.Stage == 20)
            {
                var target = context.InputParameters["Target"] as Entity;
                if (target == null) return;
                PreOperation(target, ReferenceRules.Merge(target, pre), data);
            }
            else if (context.Stage == 40)
            {
                Entity row = context.MessageName == "Delete" ? pre : ReferenceRules.Merge((Entity)context.InputParameters["Target"], pre);
                if (row == null) return;
                var student = row.GetAttributeValue<EntityReference>("edu_student");
                if (student != null) RefreshPlacement(student, data, ctx.InitiatingUserService);
                // a moved enrolment can leave the previous student without a placement
                var oldStudent = pre == null ? null : pre.GetAttributeValue<EntityReference>("edu_student");
                if (oldStudent != null && student != null && oldStudent.Id != student.Id) RefreshPlacement(oldStudent, data, ctx.InitiatingUserService);
            }
        }

        private static void PreOperation(Entity target, Entity row, IDataAccess data)
        {
            var student = row.GetAttributeValue<EntityReference>("edu_student");
            var section = row.GetAttributeValue<EntityReference>("edu_section");
            var year = row.GetAttributeValue<EntityReference>("edu_academicyear");
            if (student == null || year == null) return;
            bool active = PeopleRules.IsActive(row.GetAttributeValue<OptionSetValue>("statecode"));

            string error = PeopleRules.ValidateDates(row.GetAttributeValue<DateTime?>("edu_startdate"), row.GetAttributeValue<DateTime?>("edu_enddate"));
            if (error == null && active)
            {
                int otherActive = data.CountWhere(PeopleRules.Enrolment, target.Id,
                    Tuple.Create<string, object>("edu_student", student.Id), Tuple.Create<string, object>("edu_academicyear", year.Id), Tuple.Create<string, object>("statecode", 0));
                error = PeopleRules.ValidateOneActive(true, otherActive);
            }
            if (error == null && section != null)
            {
                var sec = data.Retrieve(PeopleRules.Section, section.Id, "edu_academicyear", "edu_capacity");
                var secYear = sec == null ? null : sec.GetAttributeValue<EntityReference>("edu_academicyear");
                error = PeopleRules.ValidateSectionYear(secYear == null ? (Guid?)null : secYear.Id, year.Id);
                if (error == null && active && sec != null)
                {
                    int inSection = data.CountWhere(PeopleRules.Enrolment, target.Id,
                        Tuple.Create<string, object>("edu_section", section.Id), Tuple.Create<string, object>("statecode", 0));
                    int? capacity = sec.Contains("edu_capacity") ? sec.GetAttributeValue<int>("edu_capacity") : (int?)null;
                    error = PeopleRules.ValidateCapacity(true, capacity, inSection);
                }
            }
            if (error != null) throw new InvalidPluginExecutionException(error);
            target["edu_uniquekey"] = ReferenceRules.UniqueKey(PeopleRules.Enrolment, row);
        }

        private static void RefreshPlacement(EntityReference student, IDataAccess data, IOrganizationService service)
        {
            var latest = data.FindFirst(PeopleRules.Enrolment, new[] { "edu_section" }, "edu_startdate",
                Tuple.Create<string, object>("edu_student", student.Id), Tuple.Create<string, object>("statecode", 0));
            EntityReference section = latest == null ? null : latest.GetAttributeValue<EntityReference>("edu_section");
            EntityReference grade = null;
            if (section != null)
            {
                var sec = data.Retrieve(PeopleRules.Section, section.Id, "edu_grade");
                grade = sec == null ? null : sec.GetAttributeValue<EntityReference>("edu_grade");
            }
            var update = new Entity(PeopleRules.Student, student.Id);
            PeopleRules.ApplyPlacement(update, section, grade);
            service.Update(update);
        }
    }
}
