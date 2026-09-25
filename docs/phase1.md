# Phase 1: reference data and calendar

## Built
| Item | Detail |
|---|---|
| Tables (Org-owned) | `edu_academicyear`, `edu_term`, `edu_calendarday`, `edu_grade`, `edu_course`, `edu_period`, `edu_gradeband`. Each has a form, views, and an alternate key on `edu_uniquekey` (LLD section 3) |
| Guardian on Contact | `contact.edu_preferredcontactchannel` (Email, SMS, Phone). Contact is the guardian (LLD C1); no custom guardian table |
| Rules (`src/EduCore.Plugins`) | `ReferenceRules` (pure) and `ReferenceDataPlugin` (pre-operation): fills `edu_uniquekey`, one current academic year, year and term date order, terms inside their year, `HH:mm` period times |
| Tests | `ReferenceRulesTests`: 11 tests; whole suite 24 passing |
| Seed data | `deploy/seed/phase1`: 1 year (2026-27), 3 terms, 5 calendar days, grades 1 to 12, 8 courses, 8 periods, 6 grade bands. Load with `pac data import --data deploy/seed/phase1/seed_phase1.zip` (DEV and UAT only) |

Imported into DEV (unmanaged) with the seed data; EduApps imports on top.

## Not done yet
- **Course to grade N:N.** Hand-written N:N XML was rejected by the importer in every variant tried (all failed with a null reference). Create it in the maker portal (Course, Relationships, Many-to-many, Grade), then run `scripts/export-solution.ps1` so the exported XML lands in source.
- **Plug-in step registration.** `ReferenceDataPlugin` is built and tested but no step is registered in the solution yet (needs the plug-in package pushed from DEV; conventions require the steps to be source-controlled after that). Until then `edu_uniquekey` is filled by the seed file only.
- Forms and views are generated defaults; the app sitemap entries come in Phase 8.
- The single-current-year rule is enforced by the plug-in only, so it is not active in DEV until the step is registered.
