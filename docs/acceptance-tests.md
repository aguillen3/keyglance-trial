# Acceptance Tests

Run the server on `http://localhost:5080`.

## 1 — Clean matching window
Start:
```powershell
dotnet run --project src/MockTax -- "Margaret Buttle" 2025
```
Seed a job for Margaret Buttle / 2025 with Box2, Box14 and Box22. Run helper with `--once`.
Expected: all values land and result is `imported`.

## 2 — Client mismatch
Open:
`MockTax - Margaret Butler 2025`
Seed:
`Margaret Buttle / 2025`
Expected: helper types nothing and reports `stopped` with client/year mismatch.

## 3 — Year mismatch
Open:
`MockTax - Margaret Buttle 2024`
Seed:
`Margaret Buttle / 2025`
Expected: helper types nothing and reports `stopped`.

## 4 — Box2 vs Box22
Use a job containing both Box2 and Box22 with different values.
Expected: each value appears in its exact named field.

## 5 — Read-only Box22
Start:
```powershell
dotnet run --project src/MockTax -- "Margaret Buttle" 2025 --readonly Box22
```
Expected: Box2/Box14 land; Box22 stays unchanged; helper reports `partial` and names `Box22`.

## 6 — Execute once
The server's claim is execute-once. The helper also has a local execution guard. The helper unit test calls the same job twice and verifies the second call performs no field input.

## 7 — Urgency
Add three jobs with different due dates, deliberately adding the soonest one last.
Expected: `/claim` returns the soonest due job first.

## 8 — Concurrency
Send simultaneous `/claim` requests against a queue containing one job.
Expected: exactly one request receives the job; all other requests receive 204.

## 9 — Two windows
Open:
- `MockTax - Margaret Buttle 2025`
- another MockTax window with a different client/year

Seed the Margaret Buttle 2025 job.
Expected: only the exact matching window is driven.
