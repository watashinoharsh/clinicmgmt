# Phase 4: academics

## Built
| Item | Detail |
|---|---|
| Tables (User-owned) | `edu_subjectassignment` (course, section, teacher, term), `edu_classschedule` (assignment, weekday, period, room), `edu_assessment` (assignment, type, maximum marks, weightage, date), `edu_studentmark` (assessment, student, marks, percentage, grade letter, remarks) |
| Timetable clash rule (`AcademicsRules`, `ClassSchedulePlugin`) | A slot is rejected when the same section, teacher or room already has a lesson in the same term, weekday and period (three separate checks, three distinct messages) |
| Marks rule (`StudentMarkPlugin`) | Marks must be 0 to the assessment maximum. Percentage and grade letter are set from `edu_gradeband` (highest band whose minimum is reached) |
| Assessment rule (`AssessmentPlugin`) | Maximum marks above zero, weightage 0 to 100, and the maximum cannot change once marks exist |
| Unique keys | Assignment `course|section|term`, schedule `assignment|weekday|period`, mark `assessment|student` |
| Tests | `AcademicsRulesTests`; whole suite 78 passing (grade letter boundaries, rounding, every clash type) |
| Seed data | `deploy/seed/phase4`: 24 subject assignments (6 sections x 4 subjects, Term 1), 48 lessons with no clashes, 48 assessments (quiz 20 marks, exam 50 marks) and 192 marks with percentage and letter |

Imported into DEV (unmanaged) with the seed data.

## Not done yet
- Plug-in step registration (see `docs/phase1.md`). Until then the clash and marks rules are not enforced in DEV. The seed file sets percentage, letter and keys itself.
- Teachers seeing only their own sections is a security-role item (Phase 9).
