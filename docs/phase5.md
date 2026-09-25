# Phase 5: attendance

## Built
| Item | Detail |
|---|---|
| Tables (User-owned) | `edu_attendance` (student, date, derived term, Daily or Period type, optional subject assignment, Present/Late/Absent/Excused, absence reason, recorded by) and `edu_attendancesummary` (student, term, teaching days, present, late, absent, excused, percentage, refreshed on) |
| Record rules (`AttendanceRules`, `AttendancePlugin`) | A Period record needs a subject assignment and a Daily record must not have one. The term is derived from the date. Recorded By defaults to the current user's staff record. Key `student|date|type|assignment-or-daily` |
| Summary maths | Attended = Present + Late; Excused days leave the denominator; teaching days = weekdays from term start to today (or term end) minus `edu_calendarday` rows (LLD A3). Percentage capped at 100, empty when nothing is countable |
| Custom API `edu_RefreshAttendanceSummary` | Contract in `solutions/EduCore/src/customapis` (TermId, optional AsOf; returns Refreshed). Logic in `RefreshAttendanceSummaryPlugin`: upserts one summary per student with an active enrolment in the term's year, keyed `student|term`. Meant to be called by the nightly flow and an on-demand button |
| Alert helpers | `BelowThreshold` and `ShouldAlert` (once per 7 days per student) for the low-attendance flow; the threshold comes from `edu_AttendanceThreshold` |
| Tests | `AttendanceRulesTests`; whole suite 89 passing (boundary dates, weekends, closures, excused maths, dedupe) |
| Seed data | `deploy/seed/phase5`: 960 daily records (24 students, 40 teaching days from 3 Aug to 25 Sep 2026, a few students with poor attendance) and 24 summary rows that match the records |

Imported into DEV (unmanaged) with the seed data.

## Not done yet
- **Cloud flows** (nightly summary refresh, low-attendance alert with dedupe through `edu_notificationlog`, on-demand button). Flows need connection references (Dataverse, Outlook) and a maker to bind them; the logic they call (the API and the alert helpers) is built and tested. Flow authoring stays with `EduFlows`, not yet started.
- Plug-in step registration and binding the API to `RefreshAttendanceSummaryPlugin` (see `docs/phase1.md`).
- `edu_notificationlog` is not a table yet (it arrives with the alert flow).
