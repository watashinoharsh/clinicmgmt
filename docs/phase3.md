# Phase 3: admissions

## Built
| Item | Detail |
|---|---|
| `edu_applicant` (User-owned) | Autonumber `APP-{yyyy}-{00000}`, date of birth, applying grade, academic year, primary guardian (Contact), previous school, application and offer dates, decision reason. Form and views generated |
| Status reasons | Active state: Applied, Under Review, Offer Made. Inactive state: Enrolled, Rejected, Withdrawn |
| `edu_student.edu_sourceapplicant` | Lookup to the applicant (the only link, per LLD C9) |
| Status machine (`AdmissionsRules`, `ApplicantPlugin`) | Applied to Under Review or Withdrawn; Under Review to Offer Made, Rejected or Withdrawn; Offer Made to Enrolled, Rejected or Withdrawn. Enrolled, Rejected and Withdrawn are terminal. New applicants start as Applied. Enrolled is valid only if a student with this source applicant exists. A decision reason is required when Rejected |
| Custom API `edu_EnrolApplicant` | Contract in `solutions/EduCore/src/customapis` (request: ApplicantId, SectionId, optional StartDate and Relationship; response: StudentId, Created). Logic in `EnrolApplicantPlugin`: creates the student, the primary guardian link and the enrolment, and marks the applicant Enrolled in one transaction. Idempotent: an applicant that already has a student returns that student. Checks the applicant has an offer and that the section matches the applicant's year and grade |
| Tests | `AdmissionsRulesTests`; whole suite 61 passing (includes every allowed and blocked transition) |
| Seed data | `deploy/seed/phase3`: 9 applicants across Applied (3), Under Review (2), Offer Made (2), Rejected (1) and Withdrawn (1) with their guardian contacts. Load with `pac data import --data deploy/seed/phase3/seed_phase3.zip` |

Imported into DEV (unmanaged) with the seed data.

## Changed from Phase 1
The single current academic year rule now clears the flag on the other years in the same transaction (as LLD section 5 says) instead of rejecting the save.

## Not done yet
- **Plug-in step registration** (same open item as Phases 1 and 2). Until the plug-in package is pushed and steps are registered: the status machine is not enforced, and `edu_EnrolApplicant` exists as a contract with no implementation bound to it. Registration for this phase: `ApplicantPlugin` (pre-op create, and update filtered on `statuscode` and `edu_decisionreason`, pre-image `pre`); `EnrolApplicantPlugin` bound to the Custom API (set as its plug-in type).
- The Enrol button on the applicant form (Phase 8, app work) and the duplicate detection rule (name + date of birth + guardian).
- Decision reason is enforced in the plug-in; the same-row Business Rule is not built.
