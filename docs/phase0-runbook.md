# Phase 0 runbook: steps that need a person with tenant rights

Everything that can be done from source is already in the repo. The steps below need an environment or tenant permissions and were **not** performed automatically.

## 1. DEV environment
Create a clean environment for EduCore. It must not be the Clinic Mgmt DEV01 environment (that holds the previous suite), and it must not reuse an environment named UAT.

- Type: **Developer** (free, single-owner) if you only need a portfolio build; **Sandbox** if others must sign in without individual licences.
- Region: same as the tenant. Dataverse: yes. Language and currency: set once, they cannot change (single currency is assumption A2).
- Name: `EduCore DEV`. Add a security group later if it is shared.

```
pac admin create --name "EduCore DEV" --type Developer --region <region> --currency <ISO code> --language <LCID>
pac auth create --name educoredev --environment <environment url>
```

## 2. First import (proves Phase 0)
```
.\scripts\build.ps1
pac solution import --path out\EduCore_unmanaged.zip --environment <dev url>
pac solution import --path out\EduFlows_unmanaged.zip --environment <dev url>
pac solution import --path out\EduApps_unmanaged.zip --environment <dev url>
```
Then confirm in the maker portal: publisher `EduPublisher` with prefix `edu`, three solutions at 1.0.0.0, and four environment variables in EduCore. **This is the first time the environment variable files are imported**; if they don't appear, the solution manifest needs root-component entries for them (not needed in the previous suite's export, which is why they were omitted).

Export back to source once, to prove the round trip:
```
.\scripts\export-solution.ps1 -Solution EduCore -EnvironmentUrl <dev url>
git diff --stat
```

## 3. Second environment (exit criterion: clean managed import)
Import `out\EduCore_managed.zip`, then EduFlows and EduApps, into a second environment (TEST). Check that it imports with no errors and the environment variables prompt for or take their settings.

## 4. Test users (one per persona)
The security model is tested with real users, not with an administrator. Create these four now; Principal and Parent follow with phase 8 and the portal.

| User | Persona | Team |
|---|---|---|
| test.teacher | Teacher | Teachers |
| test.frontoffice | Front Office / Admissions | Front Office |
| test.coordinator | Academic Coordinator | Coordinators |
| test.finance | Finance Officer | Finance |

Requirements: an Entra user for each, a licence that allows a model-driven app on Dataverse, and membership of the environment. If your tenant is a university tenant where you can't create users or assign licences, use a **Microsoft 365 Developer Program sandbox tenant** (free, includes sample users). Roles are assigned in phase 9, when they exist.

## 5. Azure DevOps (only when pipelines are wanted)
- Project and repository; push `main`.
- Variable group `edu-vars` with the service connection **names**: `SC_EDU_CHECKER`, `SC_EDU_TEST`, `SC_EDU_UAT`, `SC_EDU_PROD`.
- Environments `edu-test`, `edu-uat`, `edu-prod`; a named approver on UAT and PROD.
- Install the Power Platform Build Tools extension.

## Exit criteria for Phase 0
- [x] Repo with three solution sources that pack (managed and unmanaged) — verified locally
- [x] Naming and engineering conventions documented
- [x] Environment variables defined in source
- [x] Unit test project with a passing suite; build script — 13 tests pass
- [x] Pipeline skeleton with a shared deploy template and deployment settings files
- [ ] DEV environment created and the three solutions imported
- [ ] Round trip export to source verified
- [ ] Managed import to a second environment verified
- [ ] Four test users created
