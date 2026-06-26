---
name: update-progress
description: This skill should be used when the user asks to "update PROGRESS.md", "mark phase X as done", "ghi tiến độ", "cập nhật PROGRESS.md", "đánh dấu hoàn thành phase X" in the HungStore project.
---

Update `PROGRESS.md` in the HungStore project (`E:\ProjectVuiVe\WebSiteBanHang`) to reflect actual progress, following the exact format already established in the file — use the completed Phase 0 section as the canonical example of formatting.

## What to update

1. **Checkboxes**: change `[ ]` to `[x]` for each task actually finished. Do not tick a task that wasn't actually completed, and do not tick an entire phase if only some tasks are done.
2. **Dates**: fill "Bắt đầu" / "Hoàn thành" with absolute dates (e.g. `2026-06-19`), taken from the current date in context — never write relative terms like "hôm nay" or "hôm qua".
3. **Ghi chú / vấn đề phát sinh**: add a bullet for anything noteworthy that happened while implementing — environment quirks, package version issues, deviations from what `CLAUDE.md` originally specified, decisions made on the fly. Follow the style of the existing Phase 0 notes (specific, technical, references exact package/file names).
4. **Trạng thái hiện tại** (top of file): update to name the phase currently active or just completed.

## What NOT to do

- Do not add, remove, or rescope phases — that is a `CLAUDE.md` decision, not something this skill changes. If the user wants to change scope, point that out explicitly rather than silently updating both files.
- Do not mark a phase as fully done if `verify-phase` hasn't been run (or its checks haven't passed) for that phase — suggest running `verify-phase` first if it's unclear whether the phase actually meets the Definition of Done in `CLAUDE.md` section 5.
- Do not invent notes — only record things that actually happened in this session.
