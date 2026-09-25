# Naming and engineering conventions

## Dataverse
| Item | Convention | Example |
|---|---|---|
| Publisher | `EduPublisher`, prefix `edu`, option-value prefix 85968 (derived from the prefix) | |
| Table schema name | `edu_` + singular lower-case | `edu_student` |
| Column schema name | `edu_` + lower-case, no separators | `edu_dateofbirth` |
| Primary name column | `edu_name` (autonumber tables use their number column) | |
| Generated key column | `edu_uniquekey`, format built only by `EduKey.Build` | `<studentId>\|<yearId>` |
| Choice columns | Local choice unless shared by 2+ tables, then global `edu_<name>` | `edu_attendancetype` |
| Statuses | `statecode` / `statuscode` where a lifecycle exists | applicant, student |
| Relationships | Default names; lookups are `edu_<target>` | `edu_student` |
| Ownership | Reference data Org-owned; people and transactions User-owned. Fixed at creation | see LLD v2 section 3 |
| Security roles | `EDU - <Persona>` | `EDU - Teacher` |
| FLS profiles | `EDU <Area>` | `EDU Finance` |
| Environment variables | `edu_PascalCase`; no environment-specific defaults committed | `edu_AttendanceThreshold` |
| Connection references | `edu_<Connector>` | `edu_Dataverse` |
| Flows | `EDU - <Module> - <Trigger> - <Action>` | `EDU - Attendance - Nightly - Refresh summaries` |
| Custom APIs | `edu_<VerbNoun>` | `edu_EnrolApplicant` |

## Solutions
| Solution | Contains | Depends on |
|---|---|---|
| `EduCore` | Tables, choices, roles, FLS, plug-in package, environment variables | none |
| `EduFlows` | Cloud flows, connection references | EduCore |
| `EduApps` | Model-driven app, site map, dashboards | EduCore |

DEV holds unmanaged solutions. TEST, UAT and PROD receive **managed** solutions only, built by the pipeline from source. Nothing is edited directly in TEST, UAT or PROD.

## Plug-ins
- One class per rule. Rule logic is a pure function with unit tests; the plug-in class only reads the context and calls it.
- Synchronous steps use filtering attributes on every Update step and make at most one or two queries.
- Every plug-in step is source-controlled (unpacked solution), never registered by hand.
- Key strings for alternate keys come only from `EduKey.Build`. Integrations that upsert by key must use the same format.

## Source control
- `main` is always deployable. Work happens on short-lived feature branches and merges by pull request; the build runs unit tests, packs all three solutions and runs the solution checker.
- Export from DEV with `scripts/export-solution.ps1`, review `git diff --stat`, then commit. Commit messages say what changed and which phase it belongs to.
- No URLs, tenant identifiers, credentials or personal data in the repository. Per-environment values live in `deploy/settings.<env>.json`; secrets in a variable group or Key Vault.
