# Work log · Step 4 · API · 2026-11-12 (in progress)
**Done:**
- T4.1 red: 6 tests (CL-1, CL-2, DB-1..DB-4); stubs so they compile
- T4.1 green: length constants, `GrantTrackerDbContext` mapping, `ConfiguredTimeZoneCalendar.Today()`, `Program.cs` (DbContext, clock, calendar, database created on start)
- New test folders `tests/Services/` and `tests/Data/` (tan approved), added to the Structure block in `CLAUDE.md`
- Log template `docs/work-log/TEMPLATE.md`; step 2 and 3 logs completed; a `./test.sh` check for log parts built, then removed (tan: over-engineering)

**Files touched:**
- `backend/src/GrantTracker.Api/`: `Data/GrantTrackerDbContext.cs` · `Services/ConfiguredTimeZoneCalendar.cs` (`.gitkeep`s removed) · `Domain/Grant.cs` · `Domain/GrantTask.cs` (constants) · `Program.cs` · `appsettings.json`
- `backend/tests/GrantTracker.Tests/`: `Data/GrantTrackerDbContextTests.cs` · `Services/ConfiguredTimeZoneCalendarTests.cs`
- `CLAUDE.md` (Structure block; Docs map; log rule points to the template) · `docs/ai-log.md`
- `docs/work-log/TEMPLATE.md` · `step-2-test-cases.md` · `step-3-business-rules.md` (Missing, Review it yourself)

### T4.1 test cases
| # | Case | Catches |
|---|------|---------|
| CL-1 | UTC configured, 2020-01-15 23:30Z → today 15 Jan | Real clock used, or a fixed time zone |
| CL-2 | `Australia/Sydney`, same instant → today 16 Jan | Configured time zone ignored |
| DB-1 | Closed grant + completed and pending task, read back in a new context → every field equal, `CreatedAt` still UTC | A field not mapped; `CreatedAt` read back as Unspecified |
| DB-2 | Two grants with the same reference number → refused | Unique index missing |
| DB-3 | Two tasks with no key → saved; two with the same key → refused | Unique index missing; no key saved as `""` |
| DB-4 | Delete a grant that has a task → refused, task still there | Cascade delete |

### Review
| # | Task | Result |
|---|------|--------|
| 1 | T4.1 green | 🔧 "Real clock used" not caught: the test instant was today's real date → moved to 2020-01-15 (tan approved) · 🔧 `GrantTrackerDbContext` constructor had no summary |
| 2 | T4.1 red | ✅ Fixed dates only; each test gets its own in-memory database; each summary has a "Catches" line |
| 3 | Schema (printed from the model) | ✅ Unique `ReferenceNumber` · unique `IdempotencyKey` where not null · `GrantId` · `DueDate` · `ON DELETE RESTRICT` · enums as `INTEGER` |

**Missing:** column lengths are not tested (SQLite does not enforce them; declared only) · the "where not null" filter on the key index cannot be proved (SQLite already allows many NULLs in a unique index).

**Review it yourself:**
1. `git diff`: only the files above changed.
2. `Program.cs`: the calendar is created at start-up, so a bad `App:TimeZone` stops the app at once.
3. `GrantTrackerDbContext.cs`: each line maps to one length, index, delete rule or converter you decided.
4. `ConfiguredTimeZoneCalendar.Today()`: reads only the injected clock, never `DateTime.Now`.

### Test
| # | Task | Red | Green | Break on purpose → result (all restored) |
|---|------|-----|-------|------------------------------------------|
| 1 | T4.1 | 6/6 failed (2 `NotImplementedException`, 4 on asserts) | 48/48 | Zone ignored → CL-2 red · no UTC converter → DB-1 red · reference not unique → DB-2 red · key not unique → DB-3 red · cascade → DB-4 red · real clock → CL-1, CL-2 red · Sydney hard-coded → CL-1 red |
| 2 | Start-up (by hand) | — | ✅ | Production restart keeps a row; Development restart empties it · `App__TimeZone=Mars/Olympus` → `TimeZoneNotFoundException`, app stops |

**Missing:** an unknown `App:TimeZone` stopping the start-up has no automated test; checked by hand.

**Test it yourself:** `./test.sh` → all green; `dotnet test backend` → 48 passed, then each group:

| Group | Run | Expect | Break on purpose → expect (then undo) |
|-------|-----|--------|----------------------------------------|
| Calendar | `dotnet test backend --filter ConfiguredTimeZoneCalendarTests` | 2 passed | `_timeProvider.GetUtcNow()` → `DateTimeOffset.UtcNow` → CL-1 and CL-2 fail |
| DbContext | `dotnet test backend --filter GrantTrackerDbContextTests` | 4 passed | `DeleteBehavior.Restrict` → `Cascade` → DB-4 fails |
| Start-up | `App__TimeZone=Mars/Olympus dotnet run --project backend/src/GrantTracker.Api` | App stops with `TimeZoneNotFoundException` | — |
