# LLD v2: EduCore School Management System (Dataverse)

**Prefix:** `edu_` (own publisher). **Solutions:** `EduCore`, `EduApps`, `EduFlows`.
**Baseline:** LLD v1 (20 tables). This version fixes six defects, adds 7 tables, and adds the security model, business logic breakdown, build plan and scale notes.
**Status:** DESIGN. Nothing is built. **GIVEN** = in v1 or the brief. **DECISION** = new in v2, reversible until build. **CONFIRM** = needs your answer.

---

## 1. Assumptions and open decisions

| # | Item | Default used in this LLD | Status |
|---|---|---|---|
| A1 | Clean rebuild, not an extension of `ClinicSchoolSuite`. Reused: admissions concepts, plugin/test pattern, pipeline skeleton | as stated | CONFIRM |
| A2 | K-12, single school, single campus, single currency, one time zone, one language | as stated | CONFIRM |
| A3 | Percentage attendance policy: attended = Present + Late; Excused days leave the denominator; denominator = teaching days (term days minus weekends minus `edu_calendarday`) | as stated | CONFIRM |
| A4 | Homeroom-based roster. No electives or split-subject groups in v1 (extension: `edu_subjectenrolment`) | as stated | CONFIRM |
| A5 | One teacher per course per section per term. Co-teaching would change the assignment key | as stated | CONFIRM |
| A6 | `edu_attendance` stays a Standard table (about 600K rows per school-year at 500 students, 200 days, 6 periods). Elastic is documented as the scale option, not built | as stated | DECISION |
| A7 | Payments are recorded, not processed (no gateway, no PCI scope) | as stated | DECISION |
| A8 | Every teacher who logs in has a Dataverse user. `edu_staff` exists for people without a login | as stated | CONFIRM |
| A9 | Regulation unstated. Designed for GDPR-style data minimisation and audit | as stated | CONFIRM |
| A10 | Parent portal (Power Pages) and teacher register (Canvas) are designed for but built last | stretch phase | DECISION |

---

## 2. Design decisions that change v1

| # | v1 | v2 | Why |
|---|---|---|---|
| C1 | Custom `edu_guardian` table | **Contact** is the guardian. `edu_studentguardian` links Student to Contact | Contact exists in every Dataverse environment (no CE licence needed) and is the identity Power Pages uses for sign-in. A custom guardian would force a duplicate identity for the parent portal. Email is optional in v1 but portal sign-in needs it |
| C2 | Composite alternate keys that include lookups and nullable columns | **One generated text column `edu_uniquekey` per table**, alternate key on that column | The existing repo already uses this pattern (`hcl_slotkey`, `hcl_externalkey`). Upsert needs every key value supplied, so a null in a key (daily attendance had no assignment) is fragile. Format: values joined by `\|`, `DAILY` standing in for null. A pre-operation plugin fills it when the caller doesn't supply it. Integrations must build the same string |
| C3 | Student holds only current section and grade | **`edu_enrolment`** holds history; student columns are derived by a plugin | Sections are recreated each year, so promotion would erase history. Scope requires enrolment history |
| C4 | Invoice total is a rollup of a table that isn't in the model | Add **`edu_invoiceline`**. Totals are **stored columns maintained by a plugin**; `dueamount` is a calculated column of two stored columns | Rollups are asynchronous, so a payment plugin reading one could see a stale total. Also, a rollup column can't be "Required" |
| C5 | Marks check as a Business Rule | **Plugin** | A Business Rule can't compare against a column on a related row |
| C6 | Report card attendance % as a rollup | **`edu_attendancesummary`** table refreshed by a scheduled flow | A rollup can't take a term parameter or compute a ratio, and there was no term link or holiday calendar |
| C7 | Report card with one overall grade only | Add **`edu_reportcardline`** (per subject snapshot) | A report card lists subjects |
| C8 | Overlap rule checked section only; times as Date and Time | **`edu_period`** bell-schedule table; slot key = assignment + weekday + period; plugin checks section, teacher and room | Teacher and room clashes are the common failures. Equality checks replace time-overlap maths |
| C9 | Two-way lookups (`applicant.convertedstudent` and `student.sourceapplicant`) | Keep **`student.sourceapplicant` only** | Two lookups can drift apart |
| C10 | Ownership stated only for applicant | Explicit ownership per table (section 3) | Ownership type can't be changed after creation |
| C11 | Status as choice columns | **`statecode` / `statuscode`** for applicant and student | Native views, inactive-is-read-only behaviour |
| C12 | Grade letter hard-coded | **`edu_gradeband`** lookup table | Configurable scale |

---

## 3. Table catalogue (26 custom tables + extended Contact)

Ownership: **Org** = organisation-owned reference data. **User** = user or team owned. "Key" = generated `edu_uniquekey` format. Unchanged tables keep the v1 field list.

| # | Table | Own. | Key columns | `edu_uniquekey` | vs v1 |
|---|---|---|---|---|---|
| 1 | `edu_applicant` | User | name, DOB, applying grade, year, primary guardian (Contact), applicant number (autonumber), previous school, application and offer dates, decision reason | none (duplicate detection rule on name + DOB + guardian) | changed |
| 2 | `edu_student` | User | name, `studentcode` (autonumber `STU-{DATETIMEUTC:yyyy}-{SEQNUM:5}`), DOB, gender, current grade and section (derived), enrolment date, source applicant, national ID *(FLS)*, fee concession % *(FLS)* | `studentcode` (AK) | changed |
| 3 | `edu_studentguardian` | User | student, guardian (Contact), **relationship**, **is primary**, emergency contact, pickup authorised | `student\|guardian` | changed |
| 4 | `edu_academicyear` | Org | name, start, end, is current | `name` | v1 |
| 5 | `edu_term` | Org | name, year, start, end, sequence | `year\|sequence` | v1 |
| 6 | `edu_calendarday` | Org | date, year, type (Holiday, Closure, Teacher-only), description | `date` | **new** |
| 7 | `edu_grade` | Org | name, sequence | `name` | v1 |
| 8 | `edu_section` | User | name, grade, year, class teacher, capacity | `grade\|year\|name` | v1 |
| 9 | `edu_course` | Org | name, `coursecode`, is mandatory; native N:N to grade | `coursecode` | v1 |
| 10 | `edu_staff` | User | name, `staffcode`, email, staff type, user account (optional), is active | `staffcode` | v1 (role choice renamed staff type; not a security role) |
| 11 | `edu_subjectassignment` | User | course, section, staff, term | `course\|section\|term` | v1 |
| 12 | `edu_period` | Org | name, start time (text HH:mm), end time, sequence | `sequence` | **new** |
| 13 | `edu_classschedule` | User | subject assignment, weekday, **period**, room | `assignment\|weekday\|period` | changed |
| 14 | `edu_enrolment` | User | student, section, year, start, end, status, exit reason | `student\|year` | **new** |
| 15 | `edu_attendance` | User | student, date, **term** (derived), **type** (Daily, Period), subject assignment (period only), status, absence reason, recorded by | `student\|date\|type\|assignment-or-DAILY` | changed |
| 16 | `edu_attendancesummary` | User | student, term, teaching days, present, late, absent, excused, percentage, refreshed on | `student\|term` | **new** |
| 17 | `edu_assessment` | User | name, subject assignment, type, max marks, weightage, date | none | v1 |
| 18 | `edu_studentmark` | User | assessment, student, marks obtained, percentage, grade letter, remarks | `assessment\|student` | changed |
| 19 | `edu_gradeband` | Org | min %, letter, GPA points | `letter` | **new** |
| 20 | `edu_reportcard` | User | student, term, overall %, overall grade, attendance %, status (Draft, Published), generated on, published on | `student\|term` | v1 |
| 21 | `edu_reportcardline` | User | report card, subject assignment, final %, letter, teacher comment | `reportcard\|assignment` | **new** |
| 22 | `edu_feestructure` | Org | grade, term, fee type, amount | `grade\|term\|feetype` | v1 |
| 23 | `edu_invoice` | User | invoice number (autonumber), student, bill-to guardian, term, total, paid, **due (calculated = total - paid)**, due date, status | `invoicenumber` | changed |
| 24 | `edu_invoiceline` | User | invoice, fee type, description, quantity, amount | none | **new** |
| 25 | `edu_payment` | User | invoice, amount, date, method, reference, status (Cleared, Reversed), recorded by | none | changed (reversal instead of delete) |
| 26 | `edu_notificationlog` | User | recipient (Contact), student, type, channel, sent on, status | none | v1 (recipient now Contact) |
| - | `Contact` (extended) | User | preferred contact channel. Guardian phone is mandatory by form rule; duplicate detection on email | none | replaces `edu_guardian` |

**Relationship notes**
- **N:N:** course to grade (native, no attributes). Every other many-to-many carries data, so it is an explicit table: `edu_studentguardian`, `edu_enrolment`.
- **Rollups:** none in the critical path. A rollup is allowed only for a non-critical display value, for example a student's open-invoice count, where an hour of lag is acceptable.
- **Activity table:** none. The notification log is a normal table; a custom activity (timeline view) is a possible later change, and can't be undone once made.
- **Elastic table:** none in v1 (A6).

---

## 4. Detail: new and changed tables

### edu_student
- Status via `statecode`/`statuscode`: Active | Graduated, Withdrawn, Transferred (Inactive).
- `edu_currentgrade`, `edu_currentsection`: **read-only on the form**, set by the enrolment plugin from the active enrolment.
- `edu_nationalid` and `edu_feeconcessionpercent` are new, secured by FLS profiles (section 6).

### edu_applicant
- Status via `statuscode`. Active state: Applied, Under Review, Offer Made. Inactive state: Enrolled, Rejected, Withdrawn.
- **Allowed transitions** (enforced by plugin): Applied to Under Review or Withdrawn; Under Review to Offer Made, Rejected or Withdrawn; Offer Made to Enrolled, Rejected or Withdrawn. Enrolled is terminal.
- Enrolled is valid only if a student with `sourceapplicant` = this applicant exists. The Custom API creates that student in the same transaction (section 5). The plugin can't tell who made the change; it verifies the resulting data instead.
- Decision reason required when Rejected (Business Rule, same-row).

### edu_studentguardian
- `edu_relationship` (Father, Mother, Legal Guardian, Other) is on this link, not on the guardian, because one person can hold a different relationship to each child.
- Exactly one **is primary** per student (plugin).

### edu_enrolment
- Status: Active | Completed, Transferred, Withdrawn.
- Rules (plugin): one Active enrolment per student per year; the section must belong to the enrolment's academic year; capacity check counts Active enrolments in the section.

### edu_attendance
- `edu_type` makes daily and period records explicit, so a null assignment no longer represents "daily".
- `edu_term` is derived from the date (plugin), which lets summaries group by term without date-range queries.
- Recorded by is defaulted from the current user's `edu_staff` link, not a mandatory manual pick (`createdby` already audits the actor).

### edu_attendancesummary
- Built and refreshed by the nightly flow and an on-demand button. Uses upsert by `student|term`.
- Formula (A3): `percentage = (present + late) / (teachingdays - excused)`. `teachingdays` = term days up to today, minus weekends, minus `edu_calendarday` rows.

### edu_classschedule and edu_period
- `edu_period` holds the bell schedule once. A slot is assignment + weekday + period.
- **Plugin checks (pre-operation, same term):** section, teacher and room must each be unique for weekday + period.

### edu_studentmark
- Plugin on Create/Update: validate `marks <= assessment.maxmarks`; set `edu_percentage`; set `edu_grade` from `edu_gradeband`.
- Changing an assessment's max marks is blocked once marks exist for it.

### edu_reportcard and edu_reportcardline
- Lines are a snapshot written at generation. Subject % = sum(mark % x weightage) / sum(weightage of graded assessments). Overall % = average of subject %. Weighting by course credit is a future change.
- A Published report card and its lines are immutable (plugin blocks update and delete).

### edu_invoice, edu_invoiceline, edu_payment
- `edu_totalamount` and `edu_paidamount` are stored currency columns maintained by the plugin on line and payment create, update and delete. `edu_dueamount` is a calculated column.
- Status changes: Draft, Sent, Partially Paid, Paid are plugin-driven from payments. **Overdue** is set by the daily flow (time-based, not event-based).
- Lines can be edited only in Draft (plugin). Payments are reversed (status Reversed), never deleted; delete privilege is withheld from every role.
- Invoice generation from `edu_feestructure` is the Custom API `edu_GenerateInvoice(student, term)`, so each invoice and its lines are created atomically. A flow loops over students.

---

## 5. Business logic breakdown

| Requirement | Implementation | Justification |
|---|---|---|
| Applicant to Student conversion | **Custom API `edu_EnrolApplicant` + plugin** (button on the form) | Must create student, guardian links, enrolment and update the applicant as one transaction. A flow has no rollback, so a failure would leave an orphan student. Idempotent: if a student with this `sourceapplicant` exists, return it |
| Applicant status transitions | Plugin (pre-op) | State machine; also the Enrolled integrity check |
| Unique keys (`edu_uniquekey`) | Plugin (pre-op) | Fills the key when absent; rejects a key that doesn't match its parts |
| Enrolment rules (one active, year match, capacity) | Plugin | Concurrent saves must not both succeed |
| Student current grade and section | Plugin on enrolment change | Derived data must not be hand-edited |
| Timetable clash (section, teacher, room) | Plugin | Must block the save synchronously |
| Marks validation, percentage, grade letter | Plugin | Cross-table validation; unit-testable pure function |
| Invoice total, paid, status | Plugin on line and payment change | No eventual-consistency tolerance for money |
| Invoice generation | Custom API + plugin | Atomic per invoice |
| Single current academic year | Plugin | Sets the previous current year to No in the same transaction |
| Published report card immutability | Plugin | Security roles can't lock by status |
| Term and attendance term derivation | Plugin | Server-side, so the API path behaves like the UI |
| Attendance summary refresh | Scheduled flow (nightly) plus on-demand | Batch, fan-out, non-transactional |
| Low-attendance alert | Scheduled flow. Threshold in env var `edu_AttendanceThreshold` (default 90). Skip if the same type was logged for that student in the last 7 days | Notification only; connectors needed; dedupe via `edu_notificationlog` |
| Fee reminder and Overdue status | Scheduled flow. `edu_FeeReminderDaysBefore` (default 7) | Time-based |
| Report card generation | Custom API `edu_GenerateReportCards(term, section)` called by a flow | The calculation is transactional per student and unit-testable. A pure flow would be slow and hard to test |
| Report card published notice | Flow on status change | Notification only |
| Required-if fields (decision reason, phone) | Business Rule | Same-row, no code needed |
| Duplicate applicant or guardian | Duplicate detection rule | Warns, doesn't hard-block (siblings share contact details) |

---

## 6. Security model

Single business unit. Users are grouped in teams (Teachers, Front Office, Coordinators, Finance). Business units are not used, since a single school gains nothing from them (see section 8 for multi-school).

**Scope:** O = Organisation, U = owner only. C/R/W/D = create, read, write, delete. "-" = none.

| Table group | Front Office | Teacher | Academic Coordinator | Finance Officer | Principal (read-only) | Parent (Power Pages) |
|---|---|---|---|---|---|---|
| Reference (year, term, calendar, grade, course, period, gradeband) | R·O | R·O | CRWD·O | R·O | R·O | - |
| Applicants | CRW·O | - | R·O | - | R·O | own application (stretch) |
| Student, Contact (guardian), student-guardian | CRW·O | R·O | RW·O | R·O | R·O | own children, R |
| Enrolment | CRW·O | R·O | CRWD·O | R·O | R·O | own children, R |
| Section, subject assignment, staff | R·O | R·O | CRWD·O | - | R·O | - |
| Class schedule | R·O | R·O | CRWD·O | - | R·O | - |
| Attendance | CRW·O | R·O; C, W·U | CRWD·O | - | R·O | own children, R |
| Attendance summary | R·O | R·O | R·O | - | R·O | own children, R |
| Assessments and marks | - | CRWD·U | R·O | - | R·O | - |
| Report card and lines | - | R·O; W·U (Draft comments) | CRWD·O (publish) | - | R·O | own children, **Published only**, R |
| Fee structure, invoices, lines, payments | - | - | - | CRW·O (**no D**) | R·O | own invoices, R |
| Notification log | R·O | R·O | R·O | R·O | R·O | - |

- System Administrator is the built-in role and is not customised.
- **Parent access** is enforced by Power Pages table permissions through student-guardian, not by a Dataverse role.
- **Field-level security.** Two profiles:
  - `EDU Identity`: `edu_student.edu_nationalid`; members: Front Office and Coordinators.
  - `EDU Finance`: `edu_student.edu_feeconcessionpercent`; members: Finance Officer.
- Teachers read all students, which is normal school practice. No medical or safeguarding data is stored in this model (out of scope), so that exposure does not arise here.
- **Cover teachers** can't write another teacher's attendance under User scope. The answer is a Coordinator reassigning ownership or an access team. This is a known limitation, documented rather than hidden.
- **Auditing:** ON for student, guardian link, applicant, enrolment, attendance, marks, report card, invoice, payment. Attendance volume makes auditing there a storage decision (CONFIRM).

---

## 7. UI plan (model-driven app `EDU School Admin`)

- **Site map areas:** Admissions, Students, Academics, Attendance, Finance, Staff, Setup.
- **Main forms:** Student (tabs: Summary, Guardians, Enrolment history, Attendance, Marks and reports, Fees), Applicant (with convert button), Invoice (lines and payments subgrids).
- **Quick create:** Applicant, Guardian link, Payment, Attendance record.
- **Views:** Active students; Applicants by status; Students below attendance threshold; Overdue invoices; Draft report cards; personal view: My sections. Public views for shared filters, personal views only for individuals.
- **Dashboards:** Admin (application funnel, fees collected vs outstanding, attendance trend); Teacher (my sections today, registers due, ungraded assessments); Front Office (applications to process, offers awaiting response, today's absences to phone).
- **Better fit elsewhere (flag, not force):**
  - Taking a register is much faster in a **Canvas app on a phone** than in an editable grid.
  - Parent self-service (view attendance and report cards, pay invoices, apply) belongs in **Power Pages** using Entra External ID.

---

## 8. ALM and solution structure

- **Publisher:** new publisher, prefix `edu`. **Solutions:** `EduCore` (tables, plugin package, roles, FLS, env vars), `EduFlows` (flows, connection references), `EduApps` (app module, dashboards, sitemap). Unmanaged in DEV, managed in TEST and PROD.
- **Plugins:** one plugin package; each rule is a pure function with unit tests (same pattern as `6_Plugins/Rules.cs` and its tests). The pipeline builds the package from source, so the binary can't drift.
- **Naming:** tables `edu_singular`; columns `edu_lowercase`; choices `edu_*`; flows `EDU - <Module> - <Trigger> - <Action>`; roles `EDU - Teacher`.
- **Environment variables:** `edu_AttendanceThreshold`, `edu_FeeReminderDaysBefore`, `edu_SenderMailbox`, `edu_PortalBaseUrl`. No production-looking defaults. **Connection references:** `edu_Dataverse`, `edu_Outlook`.
- **Source control:** `pac solution unpack` per solution, plugins and tests alongside, deployment settings file per environment. Branch `main` plus feature branches with PR and solution checker. Pipeline reuses the skeleton in `5_Pipeline`, adding a plugin build and test stage, a settings file, and a solution-checker service connection of its own.
- **Sample data:** a seed dataset (about 60 students, 1 year, 3 terms) loaded with the Configuration Migration tool, DEV and UAT only.

---

## 9. Build plan (dependency-ordered)

| Phase | Deliver | Exit criteria |
|---|---|---|
| 0 | Environment, publisher, solutions, repo, naming, env vars, pipeline skeleton, 4 test users (one per persona) | Solution exports and imports cleanly to a second environment |
| 1 | Reference data and calendar: year, term, calendar day, grade, course (+N:N), period, gradeband. Guardian on Contact | Seed data loaded; single-current-year plugin tested |
| 2 | People: student, student-guardian, staff, section, enrolment (+ plugins, derived columns) | One active enrolment enforced; capacity enforced |
| 3 | Admissions: applicant, status machine, `edu_EnrolApplicant` Custom API | Conversion is atomic and idempotent; failure leaves nothing behind |
| 4 | Academics: subject assignment, period and class schedule (clash plugin), assessment, marks (validation, grade) | All three clash types rejected; marks above max rejected |
| 5 | Attendance: attendance, term derivation, summary, nightly flow, low-attendance alert with dedupe | Summary matches a hand-calculated sample; alert sent once per week |
| 6 | Fees: fee structure, invoice, lines, payment, totals plugin, `edu_GenerateInvoice`, reminder and Overdue flow | Totals correct under line and payment create, update, delete; reversal works |
| 7 | Report cards: `edu_GenerateReportCards`, immutability plugin, publish flow | Hand-calculated grades match; Published rows are immutable |
| 8 | App: sitemap, forms, views, 3 dashboards | Each persona completes its scenario in the UI |
| 9 | Security hardening: roles, FLS, auditing; **test each persona with a real user** | Access matrix in section 6 verified, including denials |
| 10 | Managed export, pipeline run to TEST, README, architecture diagram, demo script | Clean managed deploy with settings file |
| Stretch | Canvas register app, Power Pages parent portal, Power BI | Only after phase 10 |

Each phase ships its own forms and views and unit tests. Nothing waits until the end.

---

## 10. Risks and scale (where this changes for many schools)

| Area | Single school (this design) | Multi-school / multi-tenant change |
|---|---|---|
| Ownership | Single business unit | Business unit per school; roles scope to business unit (Local/Deep); add a school lookup or use the owning business unit as the discriminator |
| Keys | `studentcode`, `staffcode`, invoice number unique globally | Prefix keys with school code, or make uniqueness per school |
| Reference data | One calendar and one grade set | Per-school calendars; shared central templates |
| Attendance volume | About 600K rows per school-year | 100 schools reach tens of millions of rows per year. Move to Elastic tables or offload history to Fabric/Synapse Link and archive. Elastic gives up transactions, joins and rollups, so summaries must be event-driven |
| Storage cost | Small | Dataverse database capacity is priced per GB; design an archive policy |
| Throughput | Fine | Service-protection limits per user and the Power Automate request allowance constrain bulk loops such as invoice generation and nightly summaries; move heavy jobs to Custom APIs or Azure Functions |
| Plugin cost | Synchronous rules stay cheap | Keep every synchronous rule to one or two queries; use filtering attributes on all Update steps |
| Parents across schools | One Contact per guardian | One global Contact linked to students in several schools; portal scoping needs a school filter |
| Academic year rollover | Manual with a script | Needs a rollover Custom API (promote, close old sections, open new) and a tested rollback |
| Privacy | Auditing, no medical data | Retention and erasure policy, data residency per tenant, DPIA |
| Environment strategy | One environment | Environment per school for strict isolation, or one shared environment with business-unit isolation. Trade-off: isolation and blast radius versus admin cost and cross-school reporting |
| Licensing | Every teacher licensed | Per-user cost at scale, so consider a Canvas register app for occasional users |

---

## 11. Traceability to the brief

| Brief module | Covered by |
|---|---|
| Admissions and enrolment | applicant, Custom API, enrolment |
| SIS | student, Contact, student-guardian, enrolment, academic year, term |
| Academics | course, section, staff, subject assignment, period, class schedule, assessment, marks, report card and lines, gradeband |
| Attendance | attendance, summary, calendar day |
| Fees and billing | fee structure, invoice, lines, payment |
| Staff (light) | staff; subject-teacher-section assignment is `edu_subjectassignment` |
| Communication | notification log, flows |
| Reporting and dashboards | 3 dashboards; Power BI stretch |
