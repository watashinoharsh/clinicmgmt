# Integrity plug-ins - registration and verification

Source: this folder (`SchoolMgmtPlugins.csproj`, `Rules.cs`, `IntegrityPlugins.cs`, `IDataAccess.cs`, `PluginBase.cs`, `Tests/`).
Build: `dotnet build -c Release` produces `bin\Release\SchoolMgmtPlugins.1.0.0.nupkg` (a plug-in package, no assembly signing needed).
Local tests: `cd Tests; dotnet run -c Release` - 44 rule tests.

## Already included in the solution (v2.2.0.0)
The solution zips in `1_Solution` already contain the plug-in package and its 18 steps (9 plug-ins x Create and Update, PreOperation, synchronous, with a pre-image named `PreImage` on updates). Importing the solution registers them. You only need the steps below if you register the plug-ins by hand (for example from a different build).

## Manual registration (Plugin Registration Tool)
1. `pac tool prt`, connect with your work account.
2. Register New Package: pick the `.nupkg`, save to the School Management System solution.
3. For each plug-in type below register two steps: Message Create and Update; Stage PreOperation; Mode Synchronous; Execution Order 1; Filtering attributes: all. Step name `<Type>: <Message> of <table>`.
4. On each Update step register an image: Pre Image, Name and Entity Alias `PreImage`, all attributes.

| Plug-in type | Table |
|---|---|
| InsurancePolicyIntegrityPlugin | hcl_insurancepolicy |
| ApprovalMatrixDetailIntegrityPlugin | hcl_approvalmatrixdetail |
| ActivityIntegrityPlugin | hcl_activity |
| ConsentRecordIntegrityPlugin | hcl_consentrecord |
| ApprovalRequestIntegrityPlugin | hcl_approvalrequest |
| ReenrolmentIntegrityPlugin | hcl_reenrolment |
| MedicationTreatmentIntegrityPlugin | smc_medicationtreatment |
| VaccinationRecordIntegrityPlugin | smc_vaccinationrecord |
| MedicalCenterVisitIntegrityPlugin | smc_medicalcentervisit |

## Rules (all reject invalid data; none change data)
| Table | Rule | Message contains |
|---|---|---|
| hcl_insurancepolicy | Valid To not before Valid From | Valid To must be on or after |
| hcl_approvalmatrixdetail | Effective To not before Effective From | Effective To must be on or after |
| hcl_activity | Ends On not before Starts On | Ends On must be on or after |
| hcl_consentrecord | Granted needs Given On; Expires On not before Given On | Given On is required |
| hcl_approvalrequest | Approved/Rejected/Recheck needs Completion Date; Pending must have none; Approved or Rejected cannot be changed afterwards | Completion Date is required / already been recorded |
| hcl_reenrolment | Blocked needs a reason; ready for promotion only if intent Returning, not Blocked, no blocked reason | Promotion Blocked Reason is required / Returning |
| smc_medicationtreatment | End not before Start; visit's Treatment Type must include Medication; same student as the visit | must include Medication / same student |
| smc_vaccinationrecord | Vaccinated needs Administered Date and Parent Consent | Administered Date / Parent Consent |
| smc_medicalcentervisit | Blood pressure format systolic/diastolic; Notify Parent, Notification Message only for Return to Class / Send Home; Transfer To only for Hospital Transfer; cannot remove Medication while medication rows exist | Blood pressure must be / Notify Parent can only / Delete them first |

## Verification result (DEV, 17 live checks)
Every invalid record was rejected with the message above, and three valid control records were accepted. Test rows named `ZZ-TEST-plugin ...` remain in the DEV environment (2 medical center visits, 1 medication treatment, 2 approval requests); do not carry them to other environments.

## Notes
- Rules read related records using the plug-in's system user, so users need no extra privileges.
- To switch a rule off, disable its steps in the Plugin Registration Tool.
- Existing flows are compatible with the rules (the AI drafting flow only writes a notification message for Return to Class / Send Home).
