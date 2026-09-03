# KeyGlance Developer Trial

A same-day working slice with three pieces:

- **KeyGlance.Server** — in-memory import-job queue with atomic execute-once claims.
- **MockTax** — WPF desktop stand-in for TaxCycle/ProFile.
- **KeyGlance.Helper** — Windows C# helper using Windows UI Automation plus foreground-window checks.

## Build

Requires .NET 8 SDK. The desktop pieces run on Windows.

```powershell
dotnet build KeyGlance.sln
dotnet test
```

## Run

Terminal 1:

```powershell
dotnet run --project src/KeyGlance.Server --urls http://localhost:5080
```

Terminal 2:

```powershell
.\scripts\seed-demo-jobs.ps1
```

Terminal 3:

```powershell
dotnet run --project src/MockTax -- "Margaret Buttle" 2025
```

Terminal 4:

```powershell
dotnet run --project src/KeyGlance.Helper -- --server http://localhost:5080 --once
```

## What is tested

Automated tests cover queue urgency, concurrent claim safety, exact client/year matching, field separation, and helper execute-once behavior. Windows UI acceptance steps are in `docs/acceptance-tests.md`.

## Honest limitations

The server queue is intentionally in-memory for the trial; a restart loses jobs/results. A production system needs durable storage, transactional claiming, authentication/TLS, audit logging, and stronger desktop integration where the tax product supports it. The desktop helper is Windows-only.
