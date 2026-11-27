# Handover

Status: 27.11.2026, commit `092579a` on `main`. For a new session (e.g. cloud) that continues without prior history.

## What is ProPair

Pairing matrix for T9A team tournaments (3–8 players per team). Rate the matrix, share it via link (edit/read without login), live pairing assistant with recommendations (Nash game tree), works offline (PWA). Details: [design.md](design.md), [README](../README.md).

## Working style (requested by the user)

- Talk to the user in German, short and concise, no AI fluff. Everything written in the repo (code, comments, UI texts, docs) is in English.
- Mobile first: check every UI change at 375px.
- Record decisions in the design doc (table "Decisions"), commit and push after each step (`main`, repo `modmoto/ProPair`, private).
- Strict DDD + hexagonal: logic only in domain/application, UI replaceable, UI without its own logic.
- No network or recalculation on every click: pairing is local-first (the domain runs in the browser).

## Setup

- .NET 11 SDK, MongoDB (local: `mongod --dbpath .data/mongo --fork --logpath .data/mongod.log`).
- On the user's Mac: `/usr/local/share/dotnet` must come first in the PATH (the x64 dotnet in `/usr/local/bin` has no .NET 11).
- App: `dotnet run --project src/ProPair.Adapters.Rest --launch-profile http` → http://localhost:5054
- Sync: `dotnet run --project src/ProPair.Adapters.Sync --launch-profile http` → http://localhost:5090
- Tests: `dotnet test` (87 domain + 13 application, all green).

### New Recruit access

- Locally as user secrets (`--id propair`): `NewRecruit:Login`, `NewRecruit:Password`.
- Cloud/server: env vars `NewRecruit__Login`, `NewRecruit__Password`. **Not set in the cloud** – without them the sync process does not start (Rest + demo tournaments still run).
- Never print, log or commit credentials. List secrets only masked.
- NR logs every call: requests one after another, pauses, one Hangfire worker.

## Setup on Windows (move from the Mac)

For Claude on the Windows PC: run these steps in order, or check them. Steps the user must do themselves are marked.

1. **Tools** (PowerShell):
   ```powershell
   winget install Git.Git GitHub.cli MongoDB.Server MongoDB.DatabaseTools
   ```
   Install the .NET 11 SDK from https://dotnet.microsoft.com/download/dotnet/11.0. Check: `dotnet --list-sdks` shows 11.x.
   MongoDB then runs as a Windows service on `localhost:27017` (no `mongod --fork` as on the Mac). Check: `Get-Service MongoDB`.
2. **Repo** (private, the user signs in):
   ```powershell
   gh auth login
   gh repo clone modmoto/ProPair
   ```
3. **`.claude/launch.json`**: `runtimeExecutable` is the Mac path `/usr/local/share/dotnet/dotnet`. On Windows change it locally to `dotnet`, **do not commit** (the Mac needs the path).
4. **New Recruit access – the user does this themselves** (Claude does not ask for the values):
   ```powershell
   dotnet user-secrets set "NewRecruit:Login" "<login>" --id propair
   dotnet user-secrets set "NewRecruit:Password" "<password>" --id propair
   ```
   Stored in `%APPDATA%\Microsoft\UserSecrets\propair\secrets.json`.
5. **Database**, one of two variants:
   - With all ratings, marks and pairing states: the user copies the folder `.data/propair-dump` from the Mac (not in Git) to the PC, e.g. via USB stick or cloud drive, into the repo at `.data\propair-dump`. Then:
     ```powershell
     mongorestore --drop .data\propair-dump
     ```
   - Only tournaments, freshly from New Recruit: start the sync (step 6), then
     ```powershell
     Invoke-RestMethod -Method Post "http://localhost:5090/sync/backfill?from=2025-11-27&to=2026-11-27"
     Invoke-RestMethod -Method Post "http://localhost:5090/sync/upcoming"
     ```
6. **Start and check**:
   ```powershell
   dotnet test
   dotnet run --project src/ProPair.Adapters.Rest --launch-profile http
   dotnet run --project src/ProPair.Adapters.Sync --launch-profile http
   ```
   App http://localhost:5054 shows the tournament list, sync http://localhost:5090 the overview. With the dump: `curl http://localhost:5054/api/tournaments` returns about 56 tournaments.
7. **Remember preferences**: Create the points under "Working style" as memory (they were only local on the Mac).

## Architecture

```
src/
  ProPair.Domain              Aggregates (Team, Tournament, Preparation, Matrix, PairingSession), pairing rules,
                              solver/advisor, ArmyDetector. Also runs in the browser.
  ProPair.Application         Use cases + ports (repositories, ITournamentSource, ISyncQueue, IClock).
  ProPair.Contracts           DTOs.
  ProPair.Adapters.Mongo      Repos (optimistic versioning) + Hangfire job storage (prefix "hangfire").
  ProPair.Adapters.NewRecruit Api/NewRecruitClient (real) + Fake/ (demo tournaments).
  ProPair.Adapters.Rest       Web API, hosts Blazor WASM. Only uses the demo source, enqueues syncs.
  ProPair.Adapters.Sync       Separate process: Hangfire server, daily sync, /sync/* URLs, dashboard /hangfire.
  ProPair.Web                 Blazor WASM (Pages/, Services/LocalPairing.cs = local-first pairing).
```

## New Recruit API (verified)

- `POST https://www.newrecruit.eu/api/<endpoint>`, headers `NR-Login` / `NR-Password`, JSON body.
- `tournaments` `{start, end, id_game_system, page}` → `{tournaments:[{_id,name,start,end,status}], total}`, 50 per page. Status 0 pending, 1 open, 2 ongoing, 3 closed. Returns overlapping events (leagues).
- `tournament` `{id}` → `{_id, name, showlists, type (1 = team), teams:[{name, players:[{name, lists:[text]}]}]}`. No ids for players/teams, no data. `showlists=false` → teams without players.
- `reports` `{id_tournament}` → structured lists (`id_book`, `report_list`), only after games have been played.
- System: only "The 9th Age 3rd Edition" = 736602462.
- Lists are text: header "Army", "Name - Army" or similar, partly French, point total, footer "Created with newrecruit.eu". Rarely other export formats.

## Sync rules (implemented)

- Daily 04:00 UTC, today to +12 months. Skips single tournaments, 2-player teams, closed/past, leagues (> 4 days).
- A player is whoever has a list; coaches, judges, NPCs are not imported (domain: `Lineups`).
- Team size = most common player count (3–8). Missing → "Dummy", surplus left out; UI shows "Incomplete roster".
- List hidden → last state stays, badge "No lists visible".
- List changed → new player id (ratings/notes gone), `ListChangedAt` → info icon in the matrix.
- Lists arrive cleaned (ACL `Adapters.NewRecruit/Acl/ArmyListTranslator`): units only, placeholder = no list.
- Army from the raw text: names + translations, otherwise typical units; empty → "unknown" ("?").
- Force update in the tournament (public, admin UI later). Backfill `POST /sync/backfill?from&to` for testing only.

## Open points

- Reserve players: choose yourself who plays (currently the first lists count).
- Single sync of a new tournament has no date (only via search).
- The local DB still contains the 2v2 "Warpi cup" (imported before the filter).
- Sync rules from NR (`id_book`?), admin UI with login, PDF export, table/scenario choice.
- Further open questions: design doc, section "Open questions".

## Pitfalls

- `wwwroot/index.html` loads `_framework/blazor.webassembly.js` without a fingerprint placeholder – otherwise the published PWA hangs on "Loading".
- Service worker only in the publish build.
- Hangfire.Mongo: `CheckQueuedJobsStrategy.TailNotificationsCollection` (standalone mongod without replica set). Newtonsoft.Json pinned to 13.0.4 (CVE).
- The Blazor component is called `NewRecruitStatus`, because `SyncStatus` enum already exists in Pairing.
- The user's data in the local DB (marks, pairing state) must not be changed; reset test changes.
