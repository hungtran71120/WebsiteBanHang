---
name: verify-phase
description: This skill should be used when the user asks to "verify the phase", "check if the phase is done", "check Definition of Done", "verify phase", "kiểm tra phase đã xong chưa" in the ShopeeClone project, typically before marking a phase complete in PROGRESS.md.
---

Verify that the current phase of the ShopeeClone project (`E:\ProjectVuiVe\WebSiteBanHang`) meets the Definition of Done stated in `CLAUDE.md` section 5: the feature runs end-to-end, has tests, and `PROGRESS.md` is updated.

## Step 1 — Run checks in order, from the repo root

Stop and report immediately if a step fails — do not continue to the next step and do not mark anything as passing if an earlier step failed.

```
dotnet build
dotnet test
```

Then:

```
cd client
npm run build
```

(`npm run build` runs `vue-tsc -b` type-checking followed by `vite build`, so it covers both type errors and build errors.)

## Step 2 — Cross-check against PROGRESS.md

Read `PROGRESS.md` for the phase currently being verified. Check:
- Are all task checkboxes for this phase ticked `[x]`?
- Is "Bắt đầu" / "Hoàn thành" filled in?
- If the phase added new functionality, is there at least one test exercising it in the relevant `tests/*` project (don't just trust the checklist — grep for test files touching the new code)?

## Step 3 — Report

Produce a pass/fail summary table for: build, dotnet test, frontend build, PROGRESS.md checklist completeness, test coverage of new code. If anything is incomplete, list exactly what's missing — do not say "looks good" if any check failed or PROGRESS.md is stale.

If everything passes and `PROGRESS.md` isn't yet updated for this phase, tell the user to run the `update-progress` skill — do not edit `PROGRESS.md` yourself as part of this skill.
