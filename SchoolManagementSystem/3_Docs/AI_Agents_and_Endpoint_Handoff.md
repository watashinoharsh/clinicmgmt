# AI agents and endpoint hand-off

## 1. What is in the solution
| Component | Purpose |
|---|---|
| `hcl_aiagent` (AI Agents) | Registry. 12 agents seeded by the sample data, all **disabled** and **requires human review**: Class Update, Curriculum/Learning, Teams Tracker, Parent Insight, EduScope, Analytics/Insight, Attendance Compliance, Wellbeing Insight, Message Drafting, Compliance, Data Quality, Proactive Insights |
| `hcl_aisuggestion` (AI Suggestions) | Every AI output is stored here as **Draft**; a person sets Approved / Rejected, then it can be sent. Fields: agent, regarding record, audience, content, status, reviewed on, review comment |
| `hcl_aiagentrun` (AI Agent Runs) | Log of each run: trigger, status, times, input summary (no personal data), error, resulting suggestion |
| Flow "School - AI draft parent notification" | When a Medical Center Visit outcome is Return to Class or Send Home and the message is empty, asks the AI endpoint for a parent message and writes it into the visit's Notification Message for the nurse to review. Falls back to the standard approved wording if the AI call fails. **OFF by default** |
| Flow "Clinic - AI draft follow-up message" | When a clinic visit is closed with a follow-up date, creates a Notification in status **New** containing an AI-drafted reminder for staff to review. Falls back to standard wording. **OFF by default** |
| Env var `hcl_AiDraftEndpoint`, connection reference `hcl_AiHttp` | URL of the AI endpoint and the "HTTP with Microsoft Entra ID" connection used to call it. No keys are stored in the solution |

Rules built in: nothing is ever sent automatically; medical detail is not sent to the AI (`includeMedicalDetail: false`, only first name and visit time); every draft needs a person.

## 2. Endpoint contract (to be built - out of scope here)
Suggested host: Azure Function or API behind APIM, calling Azure AI Foundry / a Claude or Azure OpenAI model, secrets in Key Vault, protected by Entra ID (matches your regional architecture).

Request `POST {hcl_AiDraftEndpoint}`
```json
{
  "agent": "MessageDrafting",
  "audience": "Parent | Patient",
  "outcome": "ReturnToClass | SendHome",
  "purpose": "follow-up reminder",
  "studentFirstName": "Alex",
  "patientFirstName": "Sam",
  "visitTime": "10:35",
  "followUpDate": "2026-10-12",
  "tone": "calm, factual, reassuring",
  "maxWords": 90,
  "includeMedicalDetail": false
}
```
Response `200`
```json
{ "draft": "Your child, Alex, visited the medical center at 10:35. ..." }
```
Endpoint requirements: reject unknown agents; enforce maxWords; refuse to include medical details when `includeMedicalDetail` is false; log prompt and response for audit without storing names beyond retention rules; return `4xx/5xx` on failure (the flow then uses the standard wording).

## 3. Agents glossary -> data each agent needs
| Agent | Reads | Writes |
|---|---|---|
| Class Update | Class, lesson, activity records (Phase 1 curriculum) | `hcl_aisuggestion` (audience Parent/Student) |
| Curriculum / Learning | Curriculum, gradebook results | `hcl_aisuggestion` (Teacher) |
| Teams Tracker | Teams/Graph assignments, homework | `hcl_aisuggestion` (Parent) |
| Parent Insight | Outputs of the agents above | `hcl_aisuggestion` (Parent) |
| EduScope / Analytics / Proactive Insights | `hcl_attendancerecord`, `hcl_gradebookresult`, `hcl_behaviourrecord`, wellbeing notes, activities | `hcl_aisuggestion` (Teacher/Leadership), `hcl_aiagentrun` |
| Attendance Compliance | `hcl_attendancerecord` (status, date, source), kiosk records | `hcl_notification` (status New), `hcl_aisuggestion`, follow-up tasks |
| Wellbeing Insight | `smc_wellbeingnote`, behaviour records, medical details (needs sensitive-data approval) | `hcl_aisuggestion` (Staff) |
| Compliance / Data Quality | Consent records, approvals, missing required data | `hcl_aisuggestion`, `hcl_notification` |
| Message Drafting | Visit and follow-up data (implemented) | `smc_notificationmessage`, `hcl_notification` |

Not built: the agents themselves (Copilot Studio agents, AI Builder prompts, Azure AI Foundry hosting), the endpoint above, and EduScope. Enabling an agent means setting `Is Enabled` on its registry row and building the corresponding flow/agent.

## 3.1 Privacy note for approval
Health, safeguarding and behaviour data is sensitive. Before any agent reads it, confirm: lawful basis, data residency of the model, retention of prompts, and whether parents must be told. The two implemented flows deliberately send no medical detail.
