# KeyGlance Developer Trial

This is a small working slice with three pieces:

- **JobServer** — Node.js HTTP server with a file-backed queue.
- **MockTax** — .NET 8 WinForms app whose title is exactly `MockTax - {Client} {Year}`.
- **Helper** — .NET 8 Windows console app using Windows UI Automation plus `user32.dll`.

## Build

Requirements: Windows 10/11, Node.js 20+, and .NET 8 SDK.

### 1. Job server

```powershell
cd JobServer
npm start
```

The server listens on `http://localhost:3000`.

Add a job:

```powershell
$job = @{
  id = "job-001"
  client = "Margaret Buttle"
  year = 2025
  dueDate = "2026-09-02T09:00:00-07:00"
  fields = @{ Box2 = "40.00"; Box14 = "11019.84"; Box22 = "1101.96" }
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:3000/jobs -ContentType "application/json" -Body $job
```

### 2. MockTax

```powershell
dotnet run --project MockTax -- --client "Margaret Buttle" --year 2025
```

Readonly Box22 test:

```powershell
dotnet run --project MockTax -- --client "Margaret Buttle" --year 2025 --readonly Box22
```

### 3. Helper

```powershell
dotnet run --project Helper -- --server=http://localhost:3000
```

Put the matching MockTax window in the foreground before the helper types.

## What was tested / protected

1. **Correct client/year:** helper requires one exact window title before typing.
2. **Wrong client:** `Butler` does not match `Buttle`; helper types nothing and reports `stopped`.
3. **Wrong year:** 2024 does not match 2025; helper types nothing and reports `stopped`.
4. **Box2 vs Box22:** fields are located by their exact field name and are processed independently.
5. **Readonly Box22:** helper types, reads the field back, sees it did not land, and reports `partial` naming Box22. It never reports `imported`.
6. **Execute-once:** server atomically marks a job claimed before returning it. The helper also keeps a processed-ID set and refuses to touch a duplicate ID.
7. **Urgency:** `/claim` sorts all unclaimed jobs by `dueDate` and returns the earliest.
8. **Concurrency:** the claim read/mark/write is synchronous inside one Node event-loop turn, so concurrent callers cannot both claim the same job. `npm test` exercises this.
9. **Two MockTax windows:** the helper searches for the exact title. If more than one exact title exists it refuses to guess.

### Foreground-window safety

The helper checks the foreground HWND before every field, after focus, before every character, and after typing. If focus changes, it stops immediately and reports how many fields had already verified successfully.

### Honest limitations

- The queue is file-backed JSON, not a production database. It is appropriate for this same-day slice and a single Node server process, but production should use a transactional database with a real atomic claim (`UPDATE ... WHERE claimed_at IS NULL`, or equivalent).
- The helper's execute-once memory is process-local. The server-side claim is the durable guard against a second helper receiving the same job.
- `SendKeys` is intentionally used to exercise the desktop typing path. Real tax software can have controls that behave differently from MockTax.
- The result endpoint is idempotent for a job: after the first result, later results do not overwrite it.
- This sample does not implement authentication, TLS, retry/dead-letter queues, or a Windows service wrapper.

## Quick manual test matrix

Run one matching MockTax and a job containing Box2, Box14 and Box22: expected `imported`.

Change the window to `Margaret Butler 2025`: expected `stopped`, zero typed fields.

Change the window to `Margaret Buttle 2024`: expected `stopped`, zero typed fields.

Launch MockTax with `--readonly Box22`: expected `partial`, with Box22 in the reason.

Open two MockTax windows with different client/year combinations: only the exact matching title is driven.
