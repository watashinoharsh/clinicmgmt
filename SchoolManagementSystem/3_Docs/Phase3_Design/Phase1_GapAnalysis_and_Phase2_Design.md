# CE Phase 3 - Dataverse layer: Phase 1 (Discover) and Phase 2 (Design)

Environment: Clinic Mgmt DEV01 (the DEV environment), publisher prefix `hcl_`.
Status: **Phase 1 complete (read-only). Phase 2 is a DRAFT for approval. Nothing has been built or changed.**

Legend: **GIVEN** = stated in your brief/diagram summary. **PROPOSED** = my suggestion, needs your decision. **UNVERIFIED** = I could not confirm it in DEV01.

---------------------------------------------------------------------

## PHASE 1 - Discovery result

### What is actually in DEV01
Only solution-contained objects can be enumerated with the tooling available (`pac`; the Default solution cannot be exported and the Azure CLI sign-in is expired, so tables that live only in the Default solution are not visible).

| Solution | Publisher / prefix | Version | Contents |
|---|---|---|---|
| Clinicmgmt | Harsh / `hcl` | 1.3.0.0 | 17 flows, 4 roles (Clinic Admin/Doctor/Nurse/Receptionist), 2 apps, 4 env vars, 2 connection refs. Tables: hcl_clinicinvoice, hcl_clinicinvoiceline, hcl_clinicmedicine, hcl_clinicpayment, hcl_clinicservice, hcl_clinicsetting, hcl_doctorschedule, hcl_insurancepolicy, hcl_labresult, hcl_labtest, hcl_patientallergy, hcl_prescription, hcl_referral, hcl_vaccination, hcl_visit. Extends account, appointment, contact (9 custom cols), systemuser, task. One alternate key (appointment slot key). |
| SchoolMedical | SchoolMedicalPublisher / `smc` | 1.0.2.0 | Built earlier in this session outside the Phase 3 process. Tables: smc_studentprofile, smc_yeargroup, smc_rollgroup, smc_medicaldetail, smc_wellbeingnote, smc_allergy, smc_medicalcentervisit, smc_medicationtreatment, smc_medication, smc_medicationunit, smc_medicationfrequency, smc_vaccinationrecord. 5 global choices, 1 role, 1 flow, 1 app. |
| Default / Cr029d9 | system | - | Not readable. |

### Key finding
**No Phase 1 education objects are visible in DEV01**: no School Profile, Campus, Student Profile (school-level), Parent/Guardian, Staff Profile, Academic Year, Application-to-Enrolment stages, Curriculum/Class objects, Customer Service case customisations, or the timetable / Teams EDU work (items 21553, 21554, 21555, 22331). Either Phase 1 lives in another environment, or it sits in the Default solution and I cannot see it.

### Overlaps to resolve (no changes made)
- `hcl_` is already the prefix of an unrelated **clinic** model. Existing `hcl_visit`, `hcl_vaccination`, `hcl_patientallergy`, `hcl_referral`, `hcl_labresult`, `hcl_insurancepolicy`, `hcl_prescription` overlap conceptually with the Phase 3 **Medical** module, and `hcl_visit` with Kiosk/visits.
- `smc_studentprofile` overlaps the Phase 1 **Student Profile** concept. It has no school lookup and is not the "one student, one school context" model.
- Existing role names (Clinic Admin/Doctor/Nurse/Receptionist) are clinic roles, not Phase 3 roles.

### Gap analysis by module
EXISTS = present in DEV01 and reusable. PARTIAL = something overlapping is present. MISSING = not present. UNVERIFIED = would live in Phase 1 / Default and cannot be seen.

| Module | Needed objects | Status |
|---|---|---|
| Foundation: School Profile & Settings | School profile, settings | UNVERIFIED (hcl_clinicsetting is clinic settings, not this) |
| Foundation: Student Profile | school-level profile per contact | UNVERIFIED (smc_studentprofile = PARTIAL, wrong shape) |
| Foundation: Parent/Guardian | guardian link to student | UNVERIFIED |
| Foundation: Staff Profile | staff profile | UNVERIFIED |
| Foundation: Academic Year & Calendar | year, term/period | UNVERIFIED |
| Lifecycle: Application/Enrolment | Phase 1 base | UNVERIFIED |
| Curriculum & Classes | subject, class, class enrolment, teacher link | UNVERIFIED (brief says master data exists) |
| Attendance & Emergency Muster | attendance record, muster | MISSING |
| Academic Reporting | report, report version | MISSING |
| Re-enrolment | cycle, rules, cohort, record, rollover | MISSING |
| Approvals | matrix, matrix detail, content template, request, history, delegation | MISSING |
| Notifications | notification, recipient, rule, template | MISSING |
| Consent & Permissions | consent type, consent record | MISSING |
| Events/Excursions/Activities | activity, venue/transport, risk, participants, feedback, report | MISSING |
| Wellbeing & Safeguarding | case extensions on Customer Service `incident` | UNVERIFIED (Customer Service solution not visible) |
| Kiosk | kiosk record, visitor, gate pass, pickup authorisation | MISSING |
| Medical | medical detail, allergy, vaccination, visit | PARTIAL (clinic `hcl_` and `smc_` tables, wrong context) |
| Rewards & Consequences | behaviour/reward records | MISSING |
| Documents & Audit | document reference | MISSING |
| Integration: Gradebook | publish, component, result | MISSING |
| Integration: Timetable/cover import, Teams EDU sync | marked Done | UNVERIFIED - inspect first, do not recreate |

---------------------------------------------------------------------

## OPEN QUESTIONS (block the design or the build)

1. **Where is Phase 1?** Provide the Phase 1 solution names (or an unmanaged export), or the environment holding them. Until then every lookup to School Profile / Campus / Student Profile / Staff Profile / Academic Year is a **placeholder** (`PH1:`).
2. **Prefix collision.** `hcl_` is also used by the clinic model. Keep `hcl_` for Phase 3 in the same publisher, or use a new publisher/prefix?
3. **Existing clinic and `smc_` objects:** leave, deprecate (with your approval), or move to a separate solution? Phase 3 Medical should reuse or replace them - which?
4. **Timetable / Teams EDU (21553, 21554, 21555, 22331):** need to inspect the delivered solution before any extension.
5. **Customer Service case engine:** is the Customer Service solution installed where Phase 3 will be deployed? Which case types exist for Wellbeing & Safeguarding?
6. **The 13 design diagrams.** I only have your summary. Consent, Curriculum, Attendance/Muster, Wellbeing, Medical, Rewards and Documents have almost no GIVEN detail, so those designs are thin and marked PROPOSED.
7. **Roles.** Which Phase 3 security roles exist or are wanted (e.g. Teacher, Head of Year, Front Office, Nurse, Safeguarding Lead, Registrar, Parent portal user)?
8. **Ownership model.** Business-unit structure per school/campus (drives user vs organisation ownership).
9. **Approval request target.** One generic approval request table pointing at many record types, or per-type approval tables?
10. **Attendance:** existing daily/period attendance tables? Muster: one event per campus per drill?
11. **Gradebook alternate keys:** which external identifiers does the gradebook system send (publish id, component code, student number)?
12. **Fees:** which finance-system identifier is stored for the deposit/fee reference?
13. **Client extension layer:** which client-specific rules and mandatory fields, and who owns that solution?

---------------------------------------------------------------------

## PHASE 2 - Proposed design (DRAFT)

Conventions (PROPOSED, follow Phase 1 if they differ): prefix `hcl_`, lower-case schema names, singular tables, primary name column `hcl_name`, statuses as choice columns, lookups instead of copied text, Dataverse auditing ON for tables holding personal/sensitive or approval data. "PH1:" = lookup to an unverified Phase 1 table; **the column is created only after Q1 is answered**.

Ownership key: **U** = user/team owned, **O** = organisation owned.

### Solution structure (PROPOSED)
- `CEPhase3Core` - all tables, choices, relationships, keys, roles, FLS profiles, env vars below (reusable capability).
- `CEPhase3ClientExt` - client rules, extra mandatory fields, client-specific choice values, client business rules. Depends on Core.
- Nothing from the clinic (`Clinicmgmt`) or `SchoolMedical` solutions is moved without your approval (Q3).

### 1. Foundation extensions
Objects live in Phase 1 (UNVERIFIED). **Do not create** School Profile, Student Profile, Parent/Guardian, Staff Profile or Academic Year. Phase 3 modules reference them through `PH1:` lookups. If Q1 shows Academic Year/Period is absent, PROPOSED: `hcl_academicyear` (O: name, start, end, is current, school lookup) and `hcl_academicperiod` (O: name, year lookup, start, end, type choice term/semester/quarter).

### 2. Approvals
| Table | Own. | Columns | Status |
|---|---|---|---|
| hcl_approvalmatrix | O | name; request type (choice: Leave, Event, Activity, Parent volunteer, Venue/Building, Programme plan); school PH1; is active; effective from/to | request types GIVEN; rest PROPOSED |
| hcl_approvalmatrixdetail | O | matrix (N:1, required); year group PH1; campus PH1; school PH1; role (choice/lookup); staff profile PH1; resolver type (choice); approver PH1 (staff); escalation approver PH1 (optional); fallback approver PH1 (optional); priority (int); effective from/to; content template (N:1) | resolver rules, escalation/fallback optional, priority, effective dates GIVEN; column types PROPOSED |
| hcl_approvalcontenttemplate | O | name; request type; title template (text); body template (memo, tokens like `{{StudentName}}`) | GIVEN |
| hcl_approvalrequest | U | request type; regarding (see Q9); requester; approver PH1; matrix detail (N:1); status (Pending*, Approved, Rejected, Recheck); comments (memo); completion date; step order (int); parent request (self N:1) for sequential steps | outcomes + comments + completion date GIVEN; Pending*, step order, parent PROPOSED |
| hcl_approvalhistory | U | request (N:1); action (choice); actor; occurred on; comment | "audit history" GIVEN, table PROPOSED |
| hcl_approvaldelegation | O | delegator PH1; delegate PH1; request type; from/to; is active | "delegation optional" GIVEN, table PROPOSED |
Auditing: ON for request, history, delegation. Alternate key (PROPOSED): matrix detail = matrix + priority.

### 3. Notifications
| Table | Own. | Columns | Status |
|---|---|---|---|
| hcl_notification | U | title; message (memo); related record type (text) + related record id (text) + context (text); deep link URL; priority (choice); category (choice); status (New*, Sent, Delivered, Failed, Read) | title/message/related+context/deep link/priority/category/status(New) GIVEN; status values other than New PROPOSED |
| hcl_notificationrecipient | U | notification (N:1); recipient contact or user (two lookups); channel (choice); delivery status (choice); delivered on; read on | recipients + delivery tracking GIVEN; shape PROPOSED |
| hcl_notificationrule | O | name; event code (text); category; template (N:1); channels (multi-select choice); reminder interval, max reminders; stop condition code (choice/text); is active | event mapping, channels, reminders, stop conditions GIVEN |
| hcl_notificationrecipientrule | O | rule (N:1); recipient type (role/relationship/preference); role; relationship | GIVEN concept |
| hcl_notificationtemplate | O | name; channel; subject; body | GIVEN concept |
Auditing: ON for notification (delivery evidence). Alternate key: rule = event code + category.

### 4. Consent & Permissions (mostly PROPOSED, Q6)
- hcl_consenttype (O): name, category (choice), requires expiry, is sensitive.
- hcl_consentrecord (U): student profile PH1; guardian contact; consent type (N:1); status (Requested, Granted, Declined, Withdrawn, Expired); given on; expires on; source. Sensitive: FLS profile `hcl_ConsentSensitive`; auditing ON.

### 5. Curriculum & Classes
Brief says subject, class, teacher, enrolment already exist in CE (UNVERIFIED). **No tables proposed** until Q1 is answered.

### 6. Attendance, Emergency Muster & Kiosk
| Table | Own. | Columns | Status |
|---|---|---|---|
| hcl_kioskrecord | U | interaction type (Late arrival, Early departure, Visitor check-in, Student pickup); student profile PH1; visitor (N:1); gate pass (N:1); pickup authorisation (N:1); campus PH1; occurred on; outcome status (choice); escalation status (choice); linked case (N:1, `incident`) ; linked attendance record (N:1) | four types, one record per interaction, visitor/gate pass/pickup auth, outcome/escalation status, case creation GIVEN; choice values for status PROPOSED |
| hcl_visitor | U | name; organisation; id document ref (FLS); phone; email; visit purpose | GIVEN concept |
| hcl_gatepass | U | pass number; kiosk record; valid from/to; status | GIVEN concept |
| hcl_pickupauthorisation | U | student profile PH1; authorised person (contact); relationship; valid from/to; status; verified by PH1 | GIVEN concept |
| hcl_attendancerecord | U | student profile PH1; date; period PH1; status (Present, Absent, Late, Early leave, Excused); source (choice: Roll, Kiosk, Import); campus PH1 | PROPOSED (Q10) |
| hcl_musterevent / hcl_musterentry | U | event: campus, started/ended, type; entry: event, student profile PH1, status (Accounted, Missing, Unconfirmed), recorded by | PROPOSED (Q10) |
Alternate keys: attendance = student profile + date + period (idempotent import). Auditing ON for kiosk, pickup, muster.

### 7. Events & Activities
| Table | Own. | Columns | Status |
|---|---|---|---|
| hcl_activity | U | name; type (Incursion, Excursion, Camp, Tour, Offsite, Co-curricular, School/corporate event); campus PH1; academic year PH1; start/end; approval mode (Sequential, Parallel, None); status; payment link URL; requester | types + approval modes + payment link GIVEN |
| hcl_activityvenue / hcl_activitytransport | U | venue: name, address, contact; transport: provider, capacity, cost | GIVEN concept |
| hcl_activityrisk | U | activity (N:1); risk score (int); risk level (choice); assessment (memo); assessed by PH1 | risk score GIVEN |
| hcl_activityparticipant | U | activity (N:1); student profile PH1; eligibility (choice); consent record (N:1); attendance (choice); payment status (choice) | eligibility, consent, payment, attendance GIVEN |
| hcl_activityfeedback / hcl_activityreport | U | feedback: activity, respondent, rating, comment; report: activity, summary (memo), status | GIVEN concept |
Approvals reuse `hcl_approvalrequest` (no separate activity-approval table).

### 8. Wellbeing & Safeguarding
Extend the Customer Service `incident` table (Q5). PROPOSED columns: student profile PH1, wellbeing category (choice), confidentiality (choice), safeguarding flag, linked kiosk record. Sensitive columns under FLS `hcl_SafeguardingSensitive`, auditing ON. No new table until Q5.

### 9. Medical
PARTIAL. PROPOSED: build Phase 3 Medical on Phase 1 Student Profile (PH1) by re-creating in the Phase 3 solution only after Q3. Needs: medical condition, allergy, vaccination, first-aid/medical visit, medication. Sensitive fields under FLS `hcl_MedicalSensitive`, auditing ON. Existing clinic/`smc_` tables are not touched.

### 10. Rewards & Consequences (PROPOSED)
- hcl_behaviourcategory (O): name, type (Reward, Consequence), default points.
- hcl_behaviourrecord (U): student profile PH1; category (N:1); points; occurred on; recorded by PH1; description; linked case (optional). Auditing ON.

### 11. Gradebook
| Table | Own. | Columns | Status |
|---|---|---|---|
| hcl_gradebookpublish | U | publish external id; class PH1; academic period PH1; published on; published by PH1 | table GIVEN, columns PROPOSED |
| hcl_gradebookcomponent | U | publish (N:1); component code; name; weighting; assessment metadata (memo) | GIVEN (assessment metadata) |
| hcl_gradebookresult | U | component (N:1); student profile PH1; mark (decimal); grade (text); teacher comment (memo); reporting data (memo) | results, grades/marks, teacher comments, reporting data GIVEN |
Alternate keys (Q11, PROPOSED): publish = external id; component = publish + component code; result = component + student profile. **No sync-status column** (GIVEN: status is not stored in Dataverse). Auditing OFF (bulk data) - PROPOSED.

### 12. Academic Reporting
| Table | Columns | Status |
|---|---|---|
| hcl_academicreport (one per report context) | student profile PH1; school/campus PH1; academic year PH1; reporting cycle/period PH1; classification (choice); parent visible (bool); staff visible (bool); lifecycle state (choice); current version (N:1 to version) | GIVEN fields; lifecycle values PROPOSED |
| hcl_academicreportversion (one per version) | report (N:1); version number; state; effective from; expiry on; published by/on; reviewed by/on; SharePoint reference (URL text); access history (via auditing) | GIVEN fields |
Rule (GIVEN): a report file is accessible only if it is the approved CURRENT version and visibility is true. Enforced by the consuming flow/API; Dataverse holds `current version` + visibility. Auditing ON. Alternate key: version = report + version number.

### 13. Re-enrolment
| Table | Columns | Status |
|---|---|---|
| hcl_reenrolmentcycle | name; academic year PH1; opens/closes; status | GIVEN concept |
| hcl_reenrolmentrule | cycle (N:1); rule type (Eligibility, Blocking); description; blocking reason (Finance, Behaviour, Other) | GIVEN |
| hcl_reenrolmentcohort | cycle (N:1); year group PH1; campus PH1; capacity | GIVEN concept |
| hcl_reenrolment | cycle; student profile PH1; intent (Returning, Not returning, Undecided); deposit paid (bool); deposit reference (text); ready for promotion (bool); promotion blocked reason (choice); target year (year N+1) PH1; target year group PH1 | GIVEN |
| hcl_rolloverplan | source year PH1; target year PH1; year group mapping; capacity | GIVEN concept |
Fees: references and status only. Alternate key: re-enrolment = cycle + student profile. Auditing ON.

### 14. Documents & Audit (PROPOSED)
- hcl_documentreference (U): name; regarding record type/id; SharePoint URL; classification; uploaded by. Auditing ON.

### 15. Integration tables
Gradebook covered above. Timetable/cover import and Teams EDU: **inspect delivered solution first (Q4); no tables proposed.**

### Field-level security profiles (PROPOSED)
`hcl_MedicalSensitive`, `hcl_SafeguardingSensitive`, `hcl_ConsentSensitive`, `hcl_IdentityDocs` (passport/id document refs). Membership by role; roles per Q7.

### Environment variables (PROPOSED)
`hcl_SharePointSiteUrl`, `hcl_NotificationDeepLinkBase`, `hcl_PaymentLinkBase`, `hcl_DefaultReminderIntervalHours`. Connection references only where a solution flow needs one (none in this scope).

### ER summary (PROPOSED)
```
approvalmatrix 1--N approvalmatrixdetail N--1 approvalcontenttemplate
approvalrequest N--1 approvalmatrixdetail ; approvalrequest 1--N approvalhistory
notificationrule 1--N notificationrecipientrule ; notification 1--N notificationrecipient
kioskrecord N--1 visitor / gatepass / pickupauthorisation / incident
activity 1--N activityparticipant / activityrisk / activityvenue / activitytransport / activityfeedback / activityreport
gradebookpublish 1--N gradebookcomponent 1--N gradebookresult
academicreport 1--N academicreportversion ; academicreport N--1 academicreportversion (current)
reenrolmentcycle 1--N reenrolmentrule / reenrolmentcohort / reenrolment
all student-related tables --> PH1: Student Profile
```

### Dependency-ordered build list (after approval and Q1)
1. Foundation gaps (only if confirmed missing)  2. Approvals  3. Notifications  4. Consent  5. Curriculum (only if a gap is proven)  6. Attendance & Kiosk  7. Events & Activities  8. Wellbeing/case extensions  9. Medical  10. Rewards & Consequences  11. Gradebook  12. Academic Reporting  13. Re-enrolment  14. Extension layer. Roles, FLS profiles, env vars and app module: added with the batch that first needs them.

### Hand-off specs for out-of-scope components (what the developer needs from the model)
| Component | Needs from Dataverse |
|---|---|
| Approval flow(s) | matrixdetail resolver columns + priority; approvalrequest status/step/parent; content template tokens; history table |
| Notification flow / delivery | notificationrule (event code, channels, reminder, stop condition); notificationrecipient delivery status; notification status values |
| Reminder stop logic | rule stop-condition code; source record status columns (case resolved, volunteers reviewed) |
| Gradebook sync | alternate keys on publish/component/result; no sync-status column |
| Report publication (SharePoint) | report current version, visibility flags, version SharePoint reference, lifecycle state |
| Kiosk flow | kioskrecord type + outcome/escalation; case + attendance lookups |
| Re-enrolment promotion | intent, deposit, ready-for-promotion, blocked reason, target year/year group |
| Timetable/Teams EDU | to be confirmed after Q4 |
| Canvas/Power Pages, Power BI, Copilot | table/column names and security roles above |

---------------------------------------------------------------------
Approval needed before Phase 3 (Build): answers to Q1-Q3 at minimum, and your sign-off on this design.
