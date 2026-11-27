# ProPair

Pairing matrix for tabletop team tournaments (The 9th Age). Design: [docs/design.md](docs/design.md).

## Run locally

Requirements: .NET 11 SDK, MongoDB.

```bash
mongod --dbpath .data/mongo --fork --logpath .data/mongod.log
```

```bash
dotnet run --project src/ProPair.Adapters.Rest --launch-profile http
```

App: http://localhost:5054

In Rider: use the shared run configs from `.run/`. They need Chrome, because Rider rejects the system default browser for Blazor.

Sync process (New Recruit → Mongo, Hangfire):

```bash
dotnet user-secrets set "NewRecruit:Login" "<login>" --id propair
```

```bash
dotnet user-secrets set "NewRecruit:Password" "<password>" --id propair
```

```bash
dotnet run --project src/ProPair.Adapters.Sync --launch-profile http
```

Sync: http://localhost:5090 (dashboard `/hangfire`, local only). On servers: env vars `NewRecruit__Login` / `NewRecruit__Password`.

```bash
dotnet test
```

## Release (Azure DevOps)

One Docker image (`Dockerfile`) for the app (port 80) and the sync process. The sync only listens inside the container (`SYNC_URLS`, default `http://127.0.0.1:5090`).

Build locally:

```bash
docker build -t propair .
```

Set up in Azure DevOps (New pipeline → GitHub → ProPair → "Existing Azure Pipelines YAML file"):

1. `/azure-pipelines.yml`, name `pro-pair.build`: tests, pushes `modmoto/pro-pair` to Docker Hub.
2. `/release-pipeline.yml`: deploys every successful `main` build via SSH (`docker-compose.token.yml`, network `caddy`, container `pro-pair-prod`).

Secret variables of the release pipeline: `DB_SECRET`, `NEW_RECRUIT_LOGIN`, `NEW_RECRUIT_PASSWORD`.
Service connections: `official-docker-modmoto`, `HetziPrivate`.

MongoDB runs as part of the compose file (`mongo-db-pro-pair-prod`, no open port). `DB_SECRET` (letters and digits only) becomes the password of `admin` on the first deployment; changing it later only works in the database itself.

Domain and backup live in the repo `modmoto/host-bootstrap` (`caddy/Caddyfile`, `db-backups/README.md`).

Sync immediately (on the server, otherwise only at 04:00 UTC):

```bash
docker exec pro-pair-prod bash -c 'exec 3<>/dev/tcp/127.0.0.1/5090; printf "POST /sync/upcoming HTTP/1.1\r\nHost: localhost\r\nContent-Length: 0\r\nConnection: close\r\n\r\n" >&3; head -1 <&3'
```

## New Recruit sync

- Separate process `ProPair.Adapters.Sync` with Hangfire (jobs in Mongo, prefix `hangfire`). The API only enqueues.
- Daily at 04:00 UTC: team tournaments "The 9th Age 3rd Edition" (system 736602462) from today to +12 months.
- Skipped: single tournaments, 2-player teams, closed/past tournaments, leagues (> 4 days).
- Only players with a list are imported: coaches, judges and NPCs are not imported.
- Team size 3–8 (most common player count). Missing players/lists → "Dummy"; surplus lists stay out; the UI shows "Incomplete roster".
- Lists hidden: badge "No lists visible", the last known state stays.
- List changed: the player gets a new id → ratings/notes for them start over, info icon in the matrix.
- Lists are cleaned during sync (anti-corruption layer in the New Recruit adapter): header lines, point totals, footers and duplicate lists are dropped; placeholders without units do not count as a list.
- Army: detected from the list text (names + translations FR/DE/ES/IT/PL, otherwise typical units). Not recognised → "?".
- "Update from New Recruit" in a tournament (public for now, admin UI later).
- Manual: `POST /sync/upcoming`, `POST /sync/tournaments/{nrId}`, `POST /sync/backfill?from=…&to=…` (also past tournaments, for testing only).
- NR logs every call: requests one after another, 400 ms pause, one worker.

## Status (POC)

- Tournaments from New Recruit (sync) plus 4 demo tournaments (`FakeTournamentSource`).
- Flow: choose tournament → choose own team → opponent → rate the matrix.
- Edit/read link without login. History is saved, but not shown yet.

## Pairing recommendations: the algorithm

![Pairing algorithm](docs/pairing-algorithm.svg)

**What it does:** For the current pairing step it calculates which choice gains us the most if the opponent plays just as well. Assumption: the opponent rates every game exactly the opposite of our matrix (+2 for us = −2 for them). Unrated cells count as 0.

**How:**
1. **Game tree:** All lines to the end (defender → attackers → choice → next round). At the end of every branch the final score is known: the sum of the matrix values.
2. **Every hidden step is a small game:** rows = our options, columns = the opponent's options, cell = value of everything that follows (recursively). Sizes: defenders 8×8, attacker pairs up to 21×21, choice 2×2.
3. **Solution per step: Nash equilibrium** (mixed strategies, via simplex). Both sides choose in secret, so neither can react to the other. The equilibrium is the mix where no one improves by deviating. Result: a fair expected value and a probability for each option.
4. **Memoisation:** States at the start of a round are cached by the remaining players (bitmasks). 8v8 with 3 predicted paths: about 0.7 s.
5. **Ranking of the options:** first expected value (rounded to 0.1, as displayed), then worst case, then probability. Equally good options: the safer one first.
6. **Most likely path:** From an option on, it follows the most likely moves of both sides step by step until the end. Result: dashed cells in the matrix plus a timeline.

**Example (picture on the right):** If Ben defends, we are at least 0 safe. If Anna defends, +2 is possible, but also −2. The best mix is 80 % Ben, 20 % Anna, fair expected +0.4. A purely pessimistic approach (the opponent sees our move) would only reach 0.

**Replaceable:**
- `IPairingAdvisor`: the whole algorithm. The app only knows this interface, per mode via `IPairingAdvisorProvider`.
- `GameTreeAdvisor(IMatrixGameSolver)`: the game tree. The solver decides how a single step is solved:
  - `EquilibriumSolver`: Nash, the default.
  - `PessimisticSolver`: maximin, the opponent sees our move (safe lower bound). Currently no mode of its own, only for comparison/tests.
- Registered in `ProPair.Application/DependencyInjection.cs`.

**Modes** (switchable in the recommendation panel, remembered per device):

| Mode | What counts | Technical |
|------|-------------|-----------|
| Optimal | highest fair expected value, plus and minus weighted equally | Nash, linear scoring |
| Avoid minus | avoiding bad matchups comes before winning good ones | Nash, "-" counts 1.5×, "0/-" 1.25× (`AdviceModes.MinusFactor`) |

Graded and deliberately moderate: every minus point adds a 0.25 surcharge. A "-" (−2) then costs as much as "+" and "0/+" together bring, a half minus gets half the surcharge. The weighting only feeds into the decision (`GameTreeAdvisor(solver, weight)`). Everything shown (total score, matchups, final scores, probabilities) is real points.

**Values per recommendation:** large the **total score** (most likely value + how likely exactly this value is), small **this round** (the same for the current round). Calculated exactly: from the option on, both sides follow their strategies (mixed, each option with its probability). All paths are walked through and the probabilities per result are summed, without simulation.

**In the UI:** The eye icon switches recommendations on; by default they are off. Calculation only happens while they are on. The likely path appears in the matrix as a corner at the top right of the cell: black = next round, black/light grey split = the one after, light grey = third. The "Round 1/2/3" labels in the timeline carry the same corner.

## Offline: local-first pairing

Internet connections at tournaments are often gone. Pairing must therefore never depend on the network:

- **Logic only in the domain:** Pairing rules (`PairingState`) and the recommendation algorithm (`IPairingAdvisor`) exist once in the domain project. Server and browser load the same assembly; the UI only calls them.
- **Clicks are local:** Every step is executed immediately on the device with the domain rules (`LocalPairing`). No click waits for the network.
- **Saving in the background:** The moves sit in a queue in the browser (localStorage) and are sent as a complete list to `PUT …/pairing/moves` as soon as the network is back (retry every 10 s). One captain enters the data, so the rule is: last list wins. The server replays them with the domain rules.
- **Everything pre-calculated:** When opening, the round values of both modes are loaded. Recommendations are remembered per mode and state; after the shown mode, the device calculates the other one in the background. Switching modes and undo are therefore instant.
- **Recommendations offline:** The expensive part (value of every round start, at 8v8 at most 5,685 numbers) is calculated once in the backend when opening (`GET …/pairing/values?mode=`) and cached in the browser. Afterwards the device only recalculates the current step, without network.
- **Opened without network:** The last saved state from the device is used.
- **Viewers** (read link) fetch the state every 15 s.
- Status shown below the title: "Saved" / "Not saved yet – sent as soon as the network is back" / "Offline".

- **PWA:** A service worker caches the app files (only in the published build; in development mode it deliberately caches nothing). After the first visit the app also starts without network, including reloads, and loads the last saved pairing state from the device. It can be installed on the phone via "Add to Home Screen" like an app. New versions load on the next opening, once all tabs of the app have been closed.

Test offline:

```bash
dotnet publish src/ProPair.Adapters.Rest -c Release -o /tmp/propair
```

```bash
dotnet /tmp/propair/ProPair.Adapters.Rest.dll --contentRoot /tmp/propair --urls http://localhost:5080
```

Open the page once, stop the server, reload.
