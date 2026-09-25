# Phase 2: people

## Built
| Item | Detail |
|---|---|
| Tables (User-owned) | `edu_staff`, `edu_section`, `edu_student`, `edu_studentguardian`, `edu_enrolment`. Forms, views and alternate keys included |
| Student | Autonumber `edu_studentcode` (`STU-{yyyy}-{00000}`) with the alternate key on it. `edu_currentgrade` and `edu_currentsection` are derived (plug-in). Status reasons: Active; Graduated, Withdrawn, Transferred |
| Enrolment | Status reasons: Active; Completed, Transferred, Withdrawn |
| Guardian link | Guardian is a Contact. Relationship, primary, emergency contact and pickup flags live on the link |
| Rules (`PeopleRules`, `StudentGuardianPlugin`, `EnrolmentPlugin`) | One primary guardian per student, first guardian defaults to primary; one Active enrolment per student per year; section must belong to the enrolment year; capacity counts Active enrolments; student placement derived from the latest Active enrolment; unique keys for staff, section, link and enrolment |
| Tests | `PeopleRulesTests`, 10 tests; whole suite 34 passing |
| Seed data | `deploy/seed/phase2`: 6 staff, 6 sections (grades 1 to 3, capacity 30), 24 students, 30 guardian contacts and links, 24 Active enrolments. Load after Phase 1 with `pac data import --data deploy/seed/phase2/seed_phase2.zip` |

Imported into DEV (unmanaged) with the seed data.

## Not done yet
- **Plug-in step registration** (same open item as Phase 1). Capacity, one-active and derived placement are not enforced in DEV until steps are registered. Steps to register: `ReferenceDataPlugin` (pre-op create and update on the Phase 1 tables plus `edu_staff` and `edu_section`); `StudentGuardianPlugin` (pre-op create and update, pre-image `pre`); `EnrolmentPlugin` (pre-op create and update with pre-image `pre`; post-op create, update, delete with pre-image `pre`).
- **`edu_student.edu_sourceapplicant`** waits for `edu_applicant` (admissions phase).
- **FLS** on `edu_nationalid` and `edu_feeconcessionpercent` is a Phase 9 item; the columns are not secured yet.
- Guardian phone is mandatory by form rule and Contact duplicate detection on email (LLD section 3) are not built yet.
