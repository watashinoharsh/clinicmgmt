# Phase 3 merge: CEPhase3Core and ClinicSchoolSuite into EduCore

## Comparison
| | CEPhase3Core 1.0.0.0 | ClinicSchoolSuite 2.0.0.0 |
|---|---|---|
| Publisher / prefix | Harsh / `hcl` (81345) | Harsh / `hcl` (81345) |
| Custom tables | 40 (school Phase 3) | 70: the same 40, plus clinic (`hcl_clinic*`, visit, prescription, lab, vaccination, referral, insurance, ...) and school medical center (`smc_*`) |
| Phase 3 tables | 40 | 40, byte-identical to CEPhase3Core |
| Other components | 4 env vars, 1 app, 1 site map | 9 env vars, 4 apps, 4 site maps, 18 workflows, 5 formula sets, 4 web resources, 3 connection references, Dataverse search |

CEPhase3Core is a strict subset of ClinicSchoolSuite, so merging both is the same as merging the Phase 3 part of ClinicSchoolSuite.

## What was merged
- 40 tables and their relationships into `solutions/EduCore` (Contact and SystemUser stubs were left out).
- 4 environment variables: `edu_DefaultReminderIntervalHours`, `edu_NotificationDeepLinkBase`, `edu_PaymentLinkBase`, `edu_SharePointSiteUrl` (also added to `deploy/settings.*.json`, empty values).
- App `edu_Phase3Admin` and site map `edu_Phase3SiteMap` into `solutions/EduApps`.
- Renamed `hcl_` to `edu_` and option values `81345xxxx` to `85968xxxx`. "Clinic Management" descriptions became "EduCore".

## Not merged
Everything clinic (appointments, invoices, prescriptions, labs, insurance, ...), the `smc_` school medical tables, the 18 clinic workflows, formulas, web resources, connection references, and the clinic env vars.

## Follow-ups
- Overlap with the LLD: `edu_attendancerecord` (Phase 3) versus `edu_attendance` and `edu_attendancesummary` (LLD), and `edu_notification*` versus `edu_notificationlog`. Decide which stays.
- `edu_NotificationDeepLinkBase` overlaps `edu_PortalBaseUrl`.
- Security roles were not in the source (the app's two role ids were dropped); add `EDU - <Persona>` roles and attach them to the app.
- Data from an existing `hcl_` environment will not carry over: names and option values changed.
- Solution versions unchanged at 1.0.0.0.
