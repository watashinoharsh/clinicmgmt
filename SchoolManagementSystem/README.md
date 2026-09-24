# School Management System - deliverable pack (v2.2.0.0)

An end-to-end **School Management System** on Dynamics 365 / Dataverse, delivered as one solution (Dataverse unique name `ClinicSchoolSuite`, display name "School Management System") and prepared for UAT. It combines three earlier builds; the original clinic work is the school's **health centre (clinic) module**.

| Folder | What is in it |
|---|---|
| `1_Solution` | `SchoolManagementSystem_2.2.0.0_managed.zip` (for TEST/UAT/PROD) and `_unmanaged.zip` (for DEV). Import ONE of them. |
| `2_SampleData` | `UAT_SampleData.zip` - 51 clearly labelled test records (students, roll groups, medications, consent types, sample approvals, AI agent registry, clinic settings). |
| `3_Docs` | UAT guide with test scripts and role matrix, architecture/module map, AI hand-off, known limitations. |
| `4_Source` | Unpacked solution source for Git. |
| `6_Plugins` | 9 data-integrity plug-ins (C#), 44 unit tests, registration and verification guide. Already included in the solution zips. |
| `5_Pipeline` | Draft Azure DevOps pipeline (build managed, deploy TEST > UAT > PROD with approval gates). |

## What is inside the solution
- **Health centre / clinic** (from Clinicmgmt): appointments, visits, prescriptions, billing, inventory, lab, vaccination, referrals, insurance, 17 automation flows, Clinic Manager and Clinic Mobile apps.
- **School Medical Center** (from SchoolMedical): student profile, medical center visits with vitals and treatment, allergies, medications, vaccination records, wellbeing notes, form logic (JavaScript), 1 flow.
- **CE Phase 3 core** (from CEPhase3Core): approvals, notifications, consent, kiosk, attendance and muster, events and activities, behaviour, gradebook, academic reports, re-enrolment, documents.
- **New in this pack:** 9 server-side data-integrity plug-ins (18 steps); AI agent registry, AI suggestions (human review) and AI run log tables; 2 AI drafting flows (off until configured); 5 security roles; **one unified app "School Management System"** (the four original apps are kept); enrolment status additions from the enrolment diagram.

## Quick start (about 30 minutes, excluding import time)
1. Import the solution (`pac solution import --path <zip> --publish-changes`). Allow about 15 minutes.
2. Bind connections when prompted: Dataverse (`hcl_ClinicDataverse`, `smc_SchoolDataverse`), Outlook (`hcl_ClinicOutlook`). `hcl_AiHttp` is only needed for the AI flows.
3. Assign each UAT tester one of the Suite roles (see `3_Docs/UAT_Guide.md`).
4. Load the sample data (see `UAT_Guide.md`, section 3).
5. Open the app **School Management System** and follow the test scripts.

## Status - please read
- Built and imported into **DEV only** (Clinic Mgmt DEV01). **Nothing has been deployed to TEST, UAT or PROD.**
- No behaviour has been tested by real users yet. See `3_Docs/Known_Limitations.md` for what is not built (student/school lookups depend on Phase 1 objects that are not in DEV01, no SXP/PXP/Canvas apps, AI endpoint, field-level security profiles).
