---
name: university-test-log
description: Create or update UniversityDecisionHub's editable Word test log whenever Aygul sends test results, terminal output, API responses or test screenshots, or asks to record testing. Does not apply to the separate C++ Mini Shell project.
---

# University test log

Use this skill automatically when the user supplies UniversityDecisionHub test evidence. A pasted command without output is a proposed procedure, not a test result. Do not rerun a test merely to document supplied output; especially do not replay requests that mutate data.

## Canonical document

Maintain `Doc/Backend_Test_Log.docx` in the active UniversityDecisionHub checkout. Create it if absent; otherwise read it and preserve historical records and manual edits. Keep it separate from `Doc/Implementation_Log.docx`. Append a numbered test/run entry for each new result, rather than creating scattered duplicate files. If the user explicitly wants separate reports, follow that request. A repeat run gets its own entry; a repeated paste of the same evidence does not. Extend an entry when new evidence completes the same run.

Use the documents skill and bundled dependencies for Word editing. Keep plain English, black text and simple headings/paragraphs or plain tables, without decorative styling. Preserve the existing document format. This is a documentation workflow during assistant turns, not a background monitor.

## Evidence rules

Record:
- Next test ID and a clear title.
- Date reported in Europe/Istanbul, and execution date/time only if known. Do not invent timestamps.
- Evidence source: user-provided output, screenshot, or assistant-executed test. Do not claim independent execution of user results.
- Environment/version details only when evidenced; include relevant prerequisites.
- Exact command/request and inputs, with credentials and personal data removed.
- Expected behavior from requirements or assertions; observed behavior from the supplied evidence.
- Status: Succeeded, Failed, Partially verified, Blocked, or Not run. Separate build status from test status and vulnerability/dependency audit status.
- Individual results when supplied, or clearly labeled group summaries with exact evidenced totals. Never infer test cases from source as if they appeared in the run output.
- Warnings, errors, meaningful limits and pending follow-up checks.

An HTTP response body alone does not prove its status code. Model/DTO checks do not prove live PostgreSQL constraints, authenticated HTTP behavior or migration application. A build is not a complete test suite. If output is incomplete, record the supported portion and request only the missing detail needed for a stronger conclusion. Do not mark all results successful merely because some PASS lines appear.

## Completion and scope

Save without removing prior tests. Verify that old records remain and totals/status agree with the supplied output. Attempt document rendering and visual review; if unavailable, keep the editable document and disclose the limitation. If Word locks the file, report that rather than overwriting another copy silently.

User-supplied results authorize documentation, not code fixes, database writes, migrations, pushes or messages to teammates. Suggest the next check without performing unrequested mutating tests. Summarize the outcome and link the updated document. Ordinary test-result recording does not require a new implementation entry unless actual implementation also changed.
