---
name: university-implementation-log
description: Maintain UniversityDecisionHub's editable Word implementation log after each implementation, fix, refactor or migration change, and when the user requests implementation documentation. Excludes unrelated projects and explanation-only turns.
---

# University implementation log

## Target and scope

Use the current UniversityDecisionHub repository root. The canonical editable document is `Doc/Implementation_Log.docx`; this is separate from requirements, diagrams, the test log and API contract documents. The original workspace is `C:/Users/aygul/OneDrive/Desktop/web project/UniversityDecisionHub` but prefer the active checkout when the project moves.

After every authorized implementation batch in this project, update this document before the final response. The user has requested these ongoing documentation updates; they do not need separate confirmation. This skill does not authorize extra implementation, team messages, database application, pushes or deployment. It is not a scheduler or background watcher.

## Evidence and editing

Read the existing document before editing. Inspect the relevant changes, migrations, request scope and observed checks. Preserve manual edits and historical entries; never rebuild the document from a stale source or overwrite unrelated content. Do not treat all dirty files as assistant changes. Log actual work, including partial or failed implementation, without claiming pending work succeeded.

Use the available documents skill and bundled workspace dependencies for Word editing and visual QA. Keep plain English, black text, normal headings and simple lists/paragraphs; no decorative design. Dates use Europe/Istanbul. Unknown historical dates must be labeled as recorded retrospectively, not invented.

Append the next numbered entry for a new implementation. Extend the existing entry when finishing the same batch instead of duplicating it. Update the current status and remaining integration sections to reflect the new evidence. Keep past test totals as historical results.

Each entry includes:
- Date, title, request and authorized scope.
- What changed and why, including relevant behavior and validation.
- Files/folders, endpoints and DTO/shared contract changes, using project-relative paths inside the document.
- Database keys/constraints, migration names and whether generated, applied or still pending; preserve legacy-data caveats.
- Checks actually executed, observed results and meaningful limitations. Distinguish compilation/model checks from live HTTP/database integration.
- Remaining decisions, teammate dependencies and handoffs. Never claim Sami or Ozan agreed without evidence.
- Git/publication status only when verified; generated files are not commits or deployments.

Never copy credentials, connection strings, tokens or personal production data into the log. Do not paste all source code or noisy build output; summarize every meaningful change with enough detail for the team to understand it.

Verify saved contents and attempt the document render/review workflow. If the renderer is unavailable, retain the editable file and clearly disclose that visual verification is pending; do not claim layout verification. If the document is missing or locked, preserve existing data and report the exact blocker rather than replacing user work. Finish with a concise link to the updated document and any material verification limitation.
