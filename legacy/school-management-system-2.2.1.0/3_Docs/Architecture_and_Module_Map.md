# Architecture and module map

How the delivered solution maps to the product overview, the architecture diagrams and the enrolment diagram you provided.

## 1. Product modules -> what exists
| Product module | Status | Where |
|---|---|---|
| Student & Parent Management | PARTIAL | `smc_studentprofile` (interim school-medical profile), Contact for parents. The Phase 1 school-level Student Profile / Parent Guardian are not in this environment |
| School / Organisation Management | PARTIAL | Year Group, Roll Group. School Profile and Campus are Phase 1, not present |
| Admissions & Enrolment | PARTIAL | Re-enrolment built (cycle, rules, cohort, re-enrolment, rollover, status, blocking reasons Finance/Behaviour/Academic/Other). Lead, application, offer (quote), order, onboarding are Phase 1 / Phase 4 |
| Curriculum Management | NOT BUILT | Phase 1 objects (subject, class, teacher, enrolment) |
| Attendance Management | BUILT (core) | Attendance record, muster event/entries, kiosk, visitor, gate pass, pickup authorisation |
| Wellbeing Management | PARTIAL | Wellbeing notes, medical details, allergies, behaviour categories/records. Wellbeing case/intervention on Customer Service cases not built |
| Engagement & Communication | BUILT (core) | Notification, recipients, rules, templates, consent, activities/excursions with risk, participants, feedback, report; approvals framework |
| Academic & Performance Insights | PARTIAL | Gradebook and academic reports tables; AI suggestion/agent tables. EduScope and Power BI not built |
| Payments & Finance Integration | PARTIAL | Clinic invoices/payments; re-enrolment deposit reference and status. No finance (FnO) integration |
| Integration Platform | NOT BUILT | Hand-off only (below) |

## 2. Enrolment diagram (Lead > Application > Offer > Enrolment > Onboarding > Re-enrolment)
| Stage | Status |
|---|---|
| Lead Management | Out of scope (Phase 4) |
| Contact/Account & Opportunity, Application stages, Student/Document/Waitlist entities | Phase 1 |
| Offer Management (Quote), Enrolment (Order), Onboarding | Phase 1 |
| **Re-enrolment process** | **Built**: cycle, eligibility/blocking rules, cohort, re-enrolment record with intent (Returning / Not returning / Undecided), status (Active (no blocks) / Blocked / Auto-enrolled (full year payment) / Confirmation required), blocked reason (Finance / Behaviour / Academic / Other), deposit paid + reference, ready-for-promotion, rollover plan |
| Academic Year Transition (update student status, finalise class placement) | Needs Phase 1 student and class objects; rollover plan table exists |

## 3. Architecture layers -> ownership
| Layer (from your diagrams) | Delivered here | Hand-off / not built |
|---|---|---|
| Core Education Platform: Dataverse tables, model-driven apps, roles | YES | - |
| Out of the box (Student & Parent, Case & Service, Events, Role-based security, Reporting) | Events and role-based security built; Customer Service cases not verified | Reporting/dashboards |
| Custom built for schools (Admissions, Lifecycle, Attendance & Compliance, Behaviour & Wellbeing, Fees & Payments) | Attendance, Behaviour & Wellbeing (partial), re-enrolment | Admissions, Lifecycle (Phase 1), Fees (finance system) |
| Agentic & AI layer (Compliance, Attendance, Data Quality agents, Proactive insights) | Agent registry, suggestion + run log tables, 2 drafting flows | Agents themselves (see `AI_Agents_and_Endpoint_Handoff.md`) |
| Integration & data services (APIM, Logic Apps, Functions, Power Automate) | Power Automate flows in solution | APIM/Logic Apps/Functions |
| Platform foundations (Azure, Entra ID, Power BI) | Uses Entra ID sign-in | Azure, Power BI |
| tmrw regional architecture (Front Door, StaffXP App Service, Graph API, AI Foundry MCP host, Redis, Key Vault, Cosmos DB, Dataverse) | Dataverse store only | Everything else: Azure resources, StaffXP/PXP/SXP front ends, Graph API |

## 4. Integration hand-offs (what each external piece needs from the Dataverse model)
| Integration | Uses |
|---|---|
| Microsoft Teams / MS Graph (class, student/teacher sync, assignments) | Existing timetable/Teams EDU objects (not visible here); gradebook publish/component/result for results |
| SharePoint (reports, documents) | `hcl_academicreportversion.hcl_sharepointref`, `hcl_documentreference.hcl_sharepointurl`, env var `hcl_SharePointSiteUrl` |
| Finance / FnO | `hcl_reenrolment.hcl_depositpaid` / `hcl_depositreference`, clinic invoice and payment tables. Store references and status only |
| Gradebook systems | Upsert by `hcl_gradebookpublish.hcl_externalid`, `hcl_gradebookcomponent.hcl_externalkey`, `hcl_gradebookresult.hcl_externalkey` (alternate keys, enforced). No sync status stored in Dataverse |
| Contact centre systems | Contact, notification and kiosk records |
| Power BI | All tables; suggested first datasets: attendance, behaviour, gradebook results, re-enrolment status |
| PXP / SXP / LXP | Table and column names in the solution; roles; notification and consent tables |
| Third-party SIS | Student profile and re-enrolment; needs Phase 1 keys |
