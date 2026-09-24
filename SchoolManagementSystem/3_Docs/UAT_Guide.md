# UAT guide - School Management System v2.1.1.0

## 1. Prerequisites
- A Dataverse environment with Dynamics 365 activities (appointment, task, contact, account) - a standard environment has these.
- A System Administrator to import the solution and assign roles.
- Each tester needs a licence that allows Dataverse model-driven apps.
- Test users must NOT have real patient/student data. All sample records are labelled `UAT` or `(UAT sample)`.

## 2. Setup checklist (administrator)
| # | Step | Done |
|---|---|---|
| 1 | Import `SchoolManagementSystem_2.1.1.0_managed.zip` (or unmanaged in DEV) | |
| 2 | Bind connection references: `hcl_ClinicDataverse`, `smc_SchoolDataverse` (Dataverse), `hcl_ClinicOutlook` (Office 365 Outlook). Leave `hcl_AiHttp` unbound unless AI drafting is being tested | |
| 3 | Turn on the flows (18 activate on import when connections are bound). The 2 AI flows stay OFF until an AI endpoint exists | |
| 4 | Environment variables: `hcl_SharePointSiteUrl`, `hcl_NotificationDeepLinkBase`, `hcl_PaymentLinkBase`, `hcl_DefaultReminderIntervalHours`, `hcl_AiDraftEndpoint` - set per environment, leave empty if not used | |
| 5 | Assign roles to testers (section 4) | |
| 6 | Load sample data (section 3) | |
| 7 | Share the app **School Management System** with the roles (already mapped to the 5 Suite roles and the clinic roles) | |

## 3. Load the sample data
```
pac auth create --environment <UAT environment URL>
pac data import --data 2_SampleData\UAT_SampleData.zip
```
Records use fixed IDs, so re-running the import updates them instead of duplicating. Contacts are created with "Do not email" set so no test emails go out. The clinic manager email is left blank on purpose so digest emails are skipped.

## 4. Roles (proposed - confirm with the business)
| Role | Intended user | Can do | Cannot do |
|---|---|---|---|
| Suite Administrator | Admin / super user | Full access to every table | - |
| Suite Teacher | Teachers | Attendance, behaviour, gradebook, activities, approvals requests, notifications, wellbeing notes, review AI suggestions; read students, allergies, medical | Edit medical records, clinic tables, kiosk |
| Suite Nurse | School nurse / clinic nurse | School medical (visits, treatment, allergies, vaccinations), clinic clinical tables, notifications, review AI suggestions | Edit attendance, gradebook, kiosk, billing |
| Suite Front Office | Reception / office | Kiosk, attendance, consent records, billing, re-enrolment, notifications | Edit medical, gradebook |
| Suite Leadership | Principals / managers | Read everything (including AI runs) | Create or edit anything |
The original Clinic roles (Admin, Doctor, Nurse, Receptionist) and School Medical Nurse are still present.
All roles grant access at organisation level (Global). Consent evidence and ID document reference columns are marked secured: only administrators can read them until field-level security profiles are added.

## 5. App map
Open **School Management System**. Areas: Student and Parent, Admissions and Re-enrolment, Attendance and Kiosk, Wellbeing and Behaviour, School Medical Center, Health Centre (Clinic), Curriculum and Assessment, Engagement and Communication, Approvals, Insights and AI. The earlier apps (Clinic Manager, Clinic Mobile, School Medical Center, CE Phase 3 Admin) still work.

## 6. Test scripts
Record Pass / Fail and the tester name. "Role" = who should run it.

| ID | Role | Steps | Expected |
|---|---|---|---|
| U01 | Nurse | Students > Student profiles | 6 UAT students listed with year and roll group |
| U02 | Nurse | Open UAT Student One > Medical / Wellbeing tabs | Allergy "Peanuts (UAT sample)" shows under Wellbeing; medical details and notes show for the other students |
| U03 | Nurse | Medical Center > Visits > New; student UAT Student Two; leave Reason blank; save | Blocked: Reason for visit and Observation are required |
| U04 | Nurse | Same visit: Blood pressure `12080` | Error on the field (format must be systolic/diastolic); save is blocked |
| U05 | Nurse | Blood pressure `150/95`, heart rate 120, respiratory rate 25, temperature 39.0 with unit C | One warning per out-of-range vital (4 warnings); save is allowed |
| U06 | Nurse | Temperature 98.6 with unit F | No temperature warning |
| U07 | Nurse | Treatment tab: choose Medication only | Medication treatment grid appears; first aid / rest / other text boxes hidden |
| U08 | Nurse | Add a Medication Treatment row (medication Paracetamol, dose 1) via the grid | Row is saved and linked to the visit |
| U09 | Nurse | Untick Medication on the visit and save | Save is blocked with a message to delete the medication rows first |
| U10 | Nurse | Outcome: Return to Class | "Notify Parent" and "Notification Message" become visible |
| U11 | Nurse | Change outcome to Hospital Transfer | Notify Parent and message hidden, message cleared, Notify set to No; "Transfer To" becomes visible |
| U12 | Nurse | Vaccination records > New, student UAT Student Five, save | Year Group and Roll Group are filled automatically (Year 8 / 8A) within about a minute (flow) |
| U13 | Nurse | Allergies > New for UAT Student Three; set severity, verification status | Saved; global choices show the expected values |
| U14 | Front Office | Health Centre (Clinic) > Lab tests > create test with unit and reference range 4 to 6; Nurse creates a lab result with value 8 | Flag becomes High, unit and range copied from the test (flow) |
| U15 | Front Office | Health Centre (Clinic) > Invoices > new invoice, add 2 lines, then a payment | Subtotal, tax, total, balance and status update; delete a line and totals recalculate |
| U16 | Front Office | Insurance policies: two policies for one patient, both "Is Primary" | Only the latest remains primary |
| U17 | Front Office | Attendance and Kiosk > Visitors > new; Gate pass; Kiosk record (Visitor check-in) | Records save and link together |
| U18 | Teacher | Attendance records: create Absent and Late records; Muster event with 2 entries | Records save; muster entries link to the event |
| U19 | Teacher | Activities > new Excursion with approval mode Sequential; add risk assessment and 2 participants | Saved; participant shows eligibility, consent, payment status |
| U20 | Teacher | Rewards and consequences: record Merit (+5) | Record saved with points |
| U21 | Teacher | Curriculum > Gradebook publishes: create publish with External Id `TEST-1`; create a second with the same External Id | Second is rejected (duplicate key) |
| U22 | Front Office | Re-enrolment: open UAT 2027 cycle; create a re-enrolment with status Blocked, reason Academic | Saved; "Academic" is available as a blocking reason |
| U23 | Teacher | Approvals > Approval requests > new (type Event); set status Approved with completion date; add history row | Saved; matrix and template lists show the UAT samples |
| U24 | Teacher | Notifications > new notification (status New) with a recipient | Saved; delivery status can be updated |
| U25 | Administrator | Insights and AI > AI agents | 12 agents listed, all disabled, all "Requires human review" |
| U26 | Nurse | Insights and AI > AI suggestions > new suggestion (status Draft), then set Approved and reviewer date | Saved; no message is sent automatically |
| U27 | Leadership | Open any list and try to edit or create | Records are read-only; no create button works |
| U28 | Front Office | Try to open Medical Center visits | Not visible / access denied |
| U29 | Front Office | Consent records: try to read the Evidence column | Column is hidden (secured) |
| U30 | Administrator | AI flows list | 2 AI flows exist and are OFF |

Defects: log the test ID, role, steps, expected vs actual, screenshot.

## 7. Out of scope for this UAT
Student search cards and barcode scanning, return-to-class pass, hospital finder, parent/staff portals (PXP/SXP), Canvas apps, Teams sync, finance integration, Power BI dashboards, real AI output. See `Known_Limitations.md`.
