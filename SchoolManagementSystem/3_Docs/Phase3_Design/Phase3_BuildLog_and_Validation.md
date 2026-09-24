# CE Phase 3 - Build log, validation and hand-off (DEV only)

Environment: Clinic Mgmt DEV01. Solution: **CEPhase3Core 1.0.0.0** (unmanaged, publisher Harsh, prefix `hcl_`).
Source of truth: `C:\Downloads\CEPhase3Core_1_0_0_0` = exact export of DEV01. Nothing was promoted beyond DEV.

## Change log (risk classification)
| # | Change | Risk | Result |
|---|---|---|---|
| 1 | Import new solution CEPhase3Core: 40 new tables, 239 relationships, 3 alternate keys, 4 env vars, 1 model-driven app + sitemap | LOW (all new objects; only references to existing `contact` and `systemuser`, nothing altered) | Succeeded (3m48s), publish succeeded |
| 2 | Add field-level security profiles hcl_IdentityDocs, hcl_ConsentSensitive (3 attempts) | LOW | FAILED "unexpected error" on import; rolled back; not deployed |

No existing table, column or Phase 1 object was altered or deleted.

## Built (module -> tables)
- Approvals: approvalcontenttemplate, approvalmatrix, approvalmatrixdetail, approvalrequest, approvalhistory, approvaldelegation
- Notifications: notificationtemplate, notificationrule, notificationrecipientrule, notification, notificationrecipient
- Consent: consenttype, consentrecord
- Kiosk & Attendance: visitor, gatepass, pickupauthorisation, kioskrecord, attendancerecord, musterevent, musterentry
- Events & Activities: activity, activityvenue, activitytransport, activityrisk, activityparticipant, activityfeedback, activityreport
- Rewards & Consequences: behaviourcategory, behaviourrecord
- Gradebook: gradebookpublish, gradebookcomponent, gradebookresult (alternate keys on external id / external key; no sync-status column; auditing off)
- Academic Reporting: academicreport, academicreportversion (current version lookup, parent/staff visibility, SharePoint reference)
- Re-enrolment: reenrolmentcycle, reenrolmentrule, reenrolmentcohort, reenrolment, rolloverplan
- Documents: documentreference
- Env vars: hcl_SharePointSiteUrl, hcl_NotificationDeepLinkBase, hcl_PaymentLinkBase, hcl_DefaultReminderIntervalHours (no values set)
- App: "CE Phase 3 Admin" (visible to System Administrator / System Customizer only)
- Auditing: ON for all tables except the 3 gradebook tables. Sensitive columns marked secured: consentrecord.evidence, visitor.iddocumentref, pickupauthorisation.iddocumentref.

## NOT built (and why)
| Item | Reason |
|---|---|
| Student Profile / School / Campus / Staff / Academic Year lookups on every student-related table | Phase 1 objects not visible in DEV01 (Open Question 1). Additive later once names are confirmed. Until then Approvals->approver, Attendance, Kiosk, Consent, Activity participants, Re-enrolment, Gradebook results, Reports, Behaviour records have **no link to a student** |
| Foundation extensions, Curriculum & Classes | Phase 1 objects; nothing proven missing |
| Wellbeing/Safeguarding case extensions | Customer Service `incident` not verifiable (Q5) |
| Medical | Overlaps clinic `hcl_` and `smc_` tables (Q3) |
| Timetable / Teams EDU | Must be inspected first (Q4); not visible |
| Security roles | Which roles are wanted is unanswered (Q7); no Phase 3 roles exist, so only admins can use the tables |
| Field-level security profiles | Import of profile XML failed; columns are secured, so only admins can read/write them until profiles are created (maker portal or Web API) |
| Extension layer solution (CEPhase3ClientExt) | Client rules not specified (Q13) |
| Kiosk -> case lookup, Wellbeing links | Depends on `incident` |
| Git branch/PR, Azure DevOps pipeline, TEST/UAT/PROD promotion (Phase 5) | Not available here; no PROD/TEST access used |

## Validation performed (Phase 4, read-only)
- Metadata read-back from DEV01: 40 tables present, 3 alternate keys, 4 env vars, app and sitemap present, 1 multi-select column (notificationrule.channels).
- Local check before import: every form/view column reference resolves; no duplicate relationship names.
- Not performed: behavioural tests, security-role tests (no roles exist), test data.

## Test matrix (to run once roles and Phase 1 links exist)
| Module | Happy path | Missing data | Permission denial |
|---|---|---|---|
| Approvals | Request resolves matrix detail, Approved/Rejected/Recheck recorded with completion date and history row | No matching matrix detail -> fallback | Non-approver cannot set outcome |
| Notifications | New -> Sent -> Delivered; recipients get delivery status | Rule without template | Recipient cannot see others' notifications |
| Consent | Granted with expiry; Expired after date | No consent record for participant | Non-authorised cannot read evidence (secured) |
| Kiosk/Attendance | Four interaction types create a record; outcome/escalation set | Visitor without gate pass | Front office cannot read ID document refs |
| Activities | Sequential/parallel/none approval modes; risk score stored | Participant without consent | Student data not visible to unrelated staff |
| Gradebook | Upsert by external key idempotent (5,000+ rows) | Duplicate external key rejected | Read-only role cannot edit results |
| Reports | Only current approved version with visibility true is accessible (enforced in API/flow) | Version without SharePoint reference | Parent cannot read staff-only report |
| Re-enrolment | Intent, deposit, ready-for-promotion, blocked reason | Cycle without cohort | Parent cannot edit blocking rules |

## Hand-off for flow / plug-in / API developers
- Approval routing: `approvalmatrixdetail` (resolvertype, rolecode, priority, effective dates, escalationafterhours, contenttemplate), `approvalrequest` (approvalstatus, steporder, parentrequest), `approvalhistory`.
- Notifications: `notificationrule` (eventcode, category, channels, reminder interval/max, stopcondition), `notificationrecipient` (deliverystatus), `notification` (notificationstatus starts New, deeplinkurl).
- Gradebook sync: upsert on `hcl_gradebookpublish.hcl_externalid`, `hcl_gradebookcomponent.hcl_externalkey`, `hcl_gradebookresult.hcl_externalkey`.
- Report publishing: `academicreport` (currentversion, parentvisible, staffvisible, lifecyclestate), `academicreportversion` (versionstate, sharepointref, effective/expiry).
- Kiosk: `kioskrecord` (interactiontype, outcomestatus, escalationstatus).
- Re-enrolment: `reenrolment` (intent, depositpaid, readyforpromotion, promotionblockedreason).

## Rollback
CEPhase3Core is a new solution with no dependants. Rollback = delete the solution (removes the 40 tables and data) - **requires your explicit approval**; not done.
