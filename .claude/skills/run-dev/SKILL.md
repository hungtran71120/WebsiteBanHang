---
name: run-dev
description: This skill should be used when the user asks to "run the project", "start the dev servers", "run backend and frontend", "chạy thử dự án", "start dev server", or wants to manually verify the HungStore app works by running it locally.
---

Start both the backend and frontend of the HungStore project (`E:\ProjectVuiVe\WebSiteBanHang`) for local manual verification, then report back the URLs — do not just claim it works without checking.

## Step 1 — Start the backend

Run in the background from the repo root:

```
dotnet run --project src/API/HungStore.API.csproj
```

Wait a few seconds, then verify it's actually up:

```
curl -s -o /dev/null -w "%{http_code}" http://localhost:<port>/health
```

Find the actual port from the `dotnet run` output (look for "Now listening on:") rather than assuming a fixed port — it can vary by launch profile.

## Step 2 — Start the frontend

Run in the background from `client/`:

```
npm run dev
```

Read the Vite output for the actual local URL (Vite picks a free port, typically starting at 5173 but not guaranteed).

## Step 3 — Report

Tell the user:
- Backend health check result (pass/fail) and the Swagger UI URL (`http://<host>:<port>/swagger`).
- Frontend dev server URL from the Vite output.

## Stopping the servers

When the user is done verifying, stop the background processes. On Windows, filter by command line to avoid killing unrelated `dotnet`/`node` processes, e.g.:

```powershell
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -like '*HungStore.API*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
```

Do the equivalent for the `node.exe` process running the Vite dev server if it needs to be stopped. Never kill processes blindly by name alone (`Stop-Process -Name dotnet`) since that could affect unrelated dotnet processes on the machine.
