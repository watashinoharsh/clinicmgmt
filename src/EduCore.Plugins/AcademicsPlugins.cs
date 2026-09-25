using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    /// <summary>
    /// Pre-operation, create and update on edu_classschedule (pre-image "pre"). Rejects a slot when the same
    /// section, teacher or room already has a lesson in the same term, weekday and period.
    /// </summary>
    public sealed class ClassSchedulePlugin : PluginBase
    {
        public ClassSchedulePlugin() : base(typeof(ClassSchedulePlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext ctx)
        {
            var context = ctx.PluginExecutionContext;
            var target = context.InputParameters.Contains("Target") ? context.InputParameters["Target"] as Entity : null;
            if (target == null) return;
            Entity pre = context.PreEntityImages.Contains("pre") ? context.PreEntityImages["pre"] : null;
            var row = ReferenceRules.Merge(target, pre);
            var data = new OrgDataAccess(ctx.InitiatingUserService);

            var assignmentRef = row.GetAttributeValue<EntityReference>("edu_subjectassignment");
            var period = row.GetAttributeValue<EntityReference>("edu_period");
            var weekday = row.GetAttributeValue<OptionSetValue>("edu_weekday");
            if (assignmentRef == null || period == null || weekday == null) return;

            var a = data.Retrieve(AcademicsRules.SubjectAssignment, assignmentRef.Id, "edu_section", "edu_staff", "edu_term");
            if (a == null) return;
            var term = a.GetAttributeValue<EntityReference>("edu_term");
            var section = a.GetAttributeValue<EntityReference>("edu_section");
            var teacher = a.GetAttributeValue<EntityReference>("edu_staff");
            string room = row.GetAttributeValue<string>("edu_room");

            var slot = new[] { Tuple.Create<string, object>("edu_weekday", weekday.Value), Tuple.Create<string, object>("edu_period", period.Id) };
            int sameSection = data.CountInSet(AcademicsRules.ClassSchedule, target.Id, "edu_subjectassignment",
                data.FindIds(AcademicsRules.SubjectAssignment, Guid.Empty, Tuple.Create<string, object>("edu_term", term.Id), Tuple.Create<string, object>("edu_section", section.Id)), slot);
            int sameTeacher = data.CountInSet(AcademicsRules.ClassSchedule, target.Id, "edu_subjectassignment",
                data.FindIds(AcademicsRules.SubjectAssignment, Guid.Empty, Tuple.Create<string, object>("edu_term", term.Id), Tuple.Create<string, object>("edu_staff", teacher.Id)), slot);
            int sameRoom = 0;
            if (!string.IsNullOrWhiteSpace(room))
                sameRoom = data.CountInSet(AcademicsRules.ClassSchedule, target.Id, "edu_subjectassignment",
                    data.FindIds(AcademicsRules.SubjectAssignment, Guid.Empty, Tuple.Create<string, object>("edu_term", term.Id)),
                    new[] { slot[0], slot[1], Tuple.Create<string, object>("edu_room", room.Trim()) });

            string error = AcademicsRules.ValidateClash(sameSection, sameTeacher, sameRoom);
            if (error != null) throw new InvalidPluginExecutionException(error);
            target["edu_uniquekey"] = ReferenceRules.UniqueKey(AcademicsRules.ClassSchedule, row);
        }
    }

    /// <summary>
    /// Pre-operation on edu_assessment update (filtering attribute edu_maxmarks) and create: validates maximum marks and
    /// weightage, and blocks a maximum change once marks exist.
    /// </summary>
    public sealed class AssessmentPlugin : PluginBase
    {
        public AssessmentPlugin() : base(typeof(AssessmentPlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext ctx)
        {
            var context = ctx.PluginExecutionContext;
            var target = context.InputParameters.Contains("Target") ? context.InputParameters["Target"] as Entity : null;
            if (target == null) return;
            Entity pre = context.PreEntityImages.Contains("pre") ? context.PreEntityImages["pre"] : null;
            var row = ReferenceRules.Merge(target, pre);

            decimal max = row.GetAttributeValue<decimal>("edu_maxmarks");
            string error = AcademicsRules.ValidateMaxMarks(max) ?? AcademicsRules.ValidateWeightage(row.GetAttributeValue<decimal>("edu_weightage"));
            if (error == null && pre != null && target.Contains("edu_maxmarks"))
            {
                bool marksExist = new OrgDataAccess(ctx.InitiatingUserService).Exists(AcademicsRules.StudentMark, "edu_assessment", target.Id);
                decimal? oldMax = pre.Contains("edu_maxmarks") ? pre.GetAttributeValue<decimal>("edu_maxmarks") : (decimal?)null;
                error = AcademicsRules.ValidateMaxChange(oldMax, max, marksExist);
            }
            if (error != null) throw new InvalidPluginExecutionException(error);
        }
    }

    /// <summary>
    /// Pre-operation, create and update on edu_studentmark (pre-image "pre"). Validates against the assessment maximum, sets the
    /// percentage and the grade letter from edu_gradeband, and fills edu_uniquekey.
    /// </summary>
    public sealed class StudentMarkPlugin : PluginBase
    {
        public StudentMarkPlugin() : base(typeof(StudentMarkPlugin)) { }

        protected override void ExecuteDataversePlugin(ILocalPluginContext ctx)
        {
            var context = ctx.PluginExecutionContext;
            var target = context.InputParameters.Contains("Target") ? context.InputParameters["Target"] as Entity : null;
            if (target == null) return;
            Entity pre = context.PreEntityImages.Contains("pre") ? context.PreEntityImages["pre"] : null;
            var row = ReferenceRules.Merge(target, pre);
            var data = new OrgDataAccess(ctx.InitiatingUserService);

            var assessmentRef = row.GetAttributeValue<EntityReference>("edu_assessment");
            if (assessmentRef == null) return;
            var assessment = data.Retrieve(AcademicsRules.Assessment, assessmentRef.Id, "edu_maxmarks");
            if (assessment == null) return;
            decimal max = assessment.GetAttributeValue<decimal>("edu_maxmarks");

            decimal? marks = row.Contains("edu_marksobtained") ? row.GetAttributeValue<decimal?>("edu_marksobtained") : null;
            string error = AcademicsRules.ValidateMarks(marks, max);
            if (error != null) throw new InvalidPluginExecutionException(error);

            decimal? pct = AcademicsRules.Percentage(marks, max);
            target["edu_percentage"] = pct;
            target["edu_gradeletter"] = AcademicsRules.GradeLetter(pct, LoadBands(data));
            target["edu_uniquekey"] = ReferenceRules.UniqueKey(AcademicsRules.StudentMark, row);
        }

        internal static IList<GradeBandInfo> LoadBands(IDataAccess data)
        {
            var bands = new List<GradeBandInfo>();
            foreach (var b in data.FindAll(ReferenceRules.GradeBand, new[] { "edu_minpercent", "edu_letter" }))
                bands.Add(new GradeBandInfo(b.GetAttributeValue<decimal>("edu_minpercent"), b.GetAttributeValue<string>("edu_letter")));
            return bands;
        }
    }
}
