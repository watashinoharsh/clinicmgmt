# Known limitations and open items (v2.1.1.0)

## Not built
- **Phase 1 links.** No Student Profile / School / Campus / Staff / Academic Year lookups on the Phase 3 tables (those objects are not in the source environment). Attendance, kiosk, consent, activity participants, behaviour, gradebook results, reports and re-enrolment records are therefore **not linked to a student** yet. This is the largest functional gap.
- Curriculum & Classes, Foundation extensions, Wellbeing/Safeguarding case extension (Customer Service cases), a Phase 3 Medical model on the Phase 1 student, timetable / Teams EDU (not inspected), client extension solution.
- Front ends: SXP, PXP, LXP, Canvas apps, student search cards and barcode scan, return-to-class pass, hospital finder, vaccination event batch screen. The backlog items marked "SXP / Canvas" are not built; the Dataverse tables and model-driven form logic they need are.
- Integrations: Teams, SharePoint, Finance (FnO), gradebook system, contact centre, Power BI, third-party SIS. Hand-off table in `Architecture_and_Module_Map.md`.
- AI: endpoint, agents, EduScope. Only the registry, review tables and 2 drafting flows exist, both off by default.
- Field-level security profiles: several attempts to import them failed. Three columns (consent evidence, visitor ID reference, pickup ID reference) are **secured**, so only administrators can see them until profiles are created in the maker portal or through the API.
- Student photo column, file-upload control on the Allergy "Medical Certificate" field is untested (the column exists; the control may show as text).

## Design assumptions to confirm
- The 5 Suite roles and their permissions are **proposed**. All grant organisation-wide access.
- `hcl_` is also the prefix of the clinic model; `smc_` is the school-medical prefix; both are in this one solution under one publisher record. Prefix strategy should be confirmed before production.
- Choice values (statuses, categories, agent types) are proposals unless marked GIVEN in the design.
- Overlap: the clinic tables (visit, vaccination, allergy, referral...) and the school-medical tables (visit, vaccination, allergy...) cover similar ideas for different contexts.

## Testing status
- Metadata read-back after each import confirmed the objects exist. Gradebook alternate-key rejection of duplicates was tested. **No user testing, no security-role testing, no end-to-end flow testing** has been done. The expected results in the UAT test scripts are design intent and may reveal defects.
- Flow expressions (including the two AI flows, invoice recalculation on delete, lab flag, primary policy) were written and imported but not executed. The delete-triggered flows depend on the connector returning the deleted row's lookup value.
- The form script (`smc_/scripts/medicalcentervisit.js`) passed a syntax check only; it has not been run in a browser.
- The Clinic slot-guard change (clear key on cancel) needs a real booking test.

## Deployment
- Imported into **DEV only**. UAT/TEST/PROD deployment needs a named approver, the pipeline in `5_Pipeline`, and connection bindings. Import of the full suite takes about 15 minutes.
- Sample data (51 records) is for UAT only and should not be loaded into production.
