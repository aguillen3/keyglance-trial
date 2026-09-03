# KeyGlance Developer Trial — Claude Code Instructions

## Goal
Build a small, safe vertical slice for KeyGlance:
1. ASP.NET Core job server
2. WPF MockTax desktop application
3. Windows C# helper that claims and safely imports jobs

AI tools are explicitly encouraged. Keep the implementation simple, readable, and testable.

## Non-negotiable safety invariants

### 1. Never import into the wrong return
The MockTax window title is exactly:
`MockTax - {Client} {Year}`

The helper must find an exact title match. Never use fuzzy matching.
- `Margaret Buttle` must NOT match `Margaret Butler`.
- 2024 must NOT match 2025.
- If no exact match exists, report `stopped` and type nothing.
- If more than one exact matching window exists, report `stopped` rather than guessing.

### 2. Never report imported without read-back verification
Typing is not proof.
For every requested field:
- locate the exact control by AutomationId
- type the value
- read the value back
- compare exact expected value
Only report `imported` if every requested field reads back exactly.

### 3. Execute-once
Server `/claim` must atomically select the most urgent unclaimed job and mark it claimed while holding one lock.
A claimed job must never be handed out again by that server instance.

The helper also has a local `JobExecutionGuard`. A job ID can enter execution only once in one helper process. A duplicate is reported as stopped and is never typed again.

### 4. Foreground-window safety
Before typing and before every field, verify that the exact target HWND is still the foreground window.
If it changes:
- stop immediately
- do not type anything further
- report `stopped` and how far the helper got

The implementation also checks focus before sending each character. This is defense-in-depth, not a guarantee against every possible OS-level race.

### 5. Exact field targeting
Use WPF AutomationProperties.AutomationId:
- RecipientName
- Box2
- Box14
- Box22

Never identify Box2/Box22 by screen position, order, or label similarity.

### 6. Read-only simulation
`MockTax --readonly Box22` makes Box22 ignore keyboard input while remaining visible.
The helper must discover this only through read-back and report `partial`, naming Box22.

## API

GET `/claim`
- returns one job
- earliest `dueDate` first
- marks it claimed atomically
- returns 204 if no job is available

POST `/jobs`
- adds a job; useful for demos/tests

POST `/jobs/{id}/result`
- records `imported`, `partial`, or `stopped`
- includes landed fields and reason

## Development rules
- Prefer explicit code over clever abstractions.
- Do not add Kubernetes, queues, databases, or cloud infrastructure for this trial.
- Do not weaken a safety invariant to make a test pass.
- Do not log sensitive tax values unnecessarily.
- Build and test after each component.
- Be honest about Windows-only and in-memory limitations.

## Acceptance tests
1. clean matching window
2. client mismatch
3. year mismatch
4. Box2 and Box22 remain distinct
5. `--readonly Box22` becomes partial
6. same job cannot execute twice
7. earliest due date wins
8. concurrent claims never duplicate
9. two MockTax windows: only the exact target is driven

## Suggested Claude Code workflow
1. Read this file and inspect the repository.
2. Build/test server.
3. Build MockTax.
4. Build/test helper core logic.
5. Run Windows desktop acceptance tests.
6. Do not claim tests passed unless they were actually run.
