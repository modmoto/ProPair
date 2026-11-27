# ProPair – Design

Pairing tool for tabletop team tournaments. Start: The 9th Age (T9A).

## Goal

- Maintain the pairing matrix: own players (rows) vs. opposing players/armies (columns).
- The team enters assessments together – via link, without login.
- During the live pairing: open the matrix, enter moves, get the best path suggested.

## Scope

**MVP**
- Import a tournament from New Recruit (teams, players, lists).
- Team format 3v3 to 8v8 (also 4v4, 6v6, 7v7). 2v2 is not supported.
- 16 T9A armies to choose from (per player / opponent).
- Matrix with a dropdown per cell, colour-coded.
- Share via link.

**Later**
- PDF export for offline use.
- Live sync for viewers (SignalR).
- Further game systems.

## UI principles

- **Mobile first.** The base CSS is the phone (≥ 360px), larger screens via `min-width` media queries.
- The 8×8 matrix fits a 360px phone without scrolling; own players as the fixed first column, opponents as the fixed header row.
- Touch targets ≥ 44px, inputs ≥ 16px (no iOS zoom), native dropdowns.
- No hover-only features: list via tap on the player, history via long-press.
- **One** subtle motion everywhere (220 ms, fade in with a 4 px offset, fade out the reverse): matrix build-up, flip, every pairing step, new matchups, sheets. `prefers-reduced-motion` turns it off.

## Rating scale

Default: 5 levels. Each team has a default scale (levels, labels, colours, score). When a tournament preparation is created, the scale is copied and fixed for that tournament.

| Value | Colour | Score |
|-------|--------|-------|
| +     | green       | +2 |
| 0/+   | light green | +1 |
| 0     | yellow      | 0 |
| 0/-   | orange      | −1 |
| -     | red         | −2 |

Optionally extendable, e.g. `++` (dark green).

## T9A armies

Beast Herds, Daemon Legions, Dread Elves, Dwarven Holds, Empire of Sonnstahl, Highborn Elves, Infernal Dwarves, Kingdom of Equitaine, Ogre Khans, Orcs and Goblins, Saurian Ancients, Sylvan Elves, Undying Dynasties, Vampire Covenant, Vermin Swarm, Warriors of the Dark Gods.

Modelled as master data per game system, not hard-coded.

## Pairing flow (standard, ETC/WTC style)

One round:
1. Both teams secretly place a **defender**. Reveal.
2. Both teams secretly place **2 attackers** against the opposing defender. Reveal.
3. Each defender chooses one of the 2 attackers. → 2 games fixed.
4. The declined attacker goes back into the pool.

End (depending on the remaining players per team):
- **4 left:** defender + 2 attackers. The declined attackers play each other, the last two as well.
- **3 left:** defender + 2 attackers. The declined attackers play each other.

This gives:
- 8v8: round (8→6), round (6→4), final with 4 → 2+2+4 games.
- 7v7: round (7→5), round (5→3), final with 3 → 2+2+3 games.
- 6v6: round (6→4), final with 4 → 2+4 games.
- 5v5: round (5→3), final with 3 → 2+3 games.
- 4v4: final with 4 → 4 games.
- 3v3: final with 3 → 3 games.

Model the flow as a rule object (replaceable per tournament/system). Table/scenario choice possibly later.

## Pairing assistant

Operation – everything happens in the matrix, no buttons per step (thumb-friendly on mobile):
1. **Defender:** tap our player on the left, then theirs on top → entered automatically. Defender row and column highlighted, the rest faded (colours stay slightly visible). Tapping a defender again = deselect.
2. **Attacker:** tap cells – in our defender's row their 2 attackers, in their defender's column our 2 attackers. The crossing of the two defenders is locked (hatched). After the 4th tap it is entered automatically: the cells not played fade and shrink slightly, the 4 attackers remain.
3. **Choice:** tap in the row who we take; in the column who they take → entered automatically.
→ Paired players fade out, the matrix gets smaller.
- **Undo** is fixed in the title bar next to the total score (the only spot that never moves): first unsaved taps, then the last step. No redo, no restart.
- Step changes are a smooth transition of the cells (same 220 ms motion); the matrix is only rebuilt on flip.

View:
- Default: only remaining players.
- Next to the title: total score of the fixed matchups (sum of scores, green/red) and x/8 paired.
- Below the matrix: fixed matchups as "Name UD [rating] VC Name" – rating centred between both sides; new matchups fade in.
- **Opponent view** (⇄ in the title bar): ratings mirrored (+ ↔ -, 0/+ ↔ 0/-), every cell outlined in red. Display only, not saved.
- Toggle in the empty top-left corner of the table (icon: four arrows pointing outward = full matrix, inward = remaining only). Taken players light grey, matchups outlined. Automatic after the pairing.
- Recommendations via the eye icon (default off): % marks next to names, ★ = best option, green rings, top 3 + most likely path.
- Demo tournaments: button "Fill demo values" on the matrix page (only for fake tournaments).

Solver (`PairingSolver`, domain service):
- Full game tree, memoised over bitmasks of the remaining players. 8v8 ≈ 0.2 s.
- Simultaneous steps assumed pessimistic: we choose, the opponent answers optimally (maximin).
- Per option: **worst case** (opponent optimal) and **⌀** (average over all opponent answers).
- Opponent view = negated own matrix. Unrated cells count as 0 (hint in the UI).
- Runs server-side; offline on the client possible later (domain has no dependencies).
- Later: mixed strategies (Nash per step) instead of pure maximin.

### Recommendations (levels 1 + 2 implemented, details in README)

Calculation:
- Roll up the game tree from the back: at the end every game from the matrix is known, before that each side picks its best. Opponent = same matrix, negated.
- Hidden steps as a mixed strategy (Nash per step, small LP): yields probabilities ("they play DL 45 % of the time") and thus the **most likely path** instead of just worst case.
- **Key players:** per player, how much worse the rest becomes if he is used up now → warning "no counter against SE afterwards (−3)".

Display (in the matrix, mobile):
- Defender: badge with expected final score at our name (★ = best), thin probability bar at the opponent's names.
- Attacker/choice: expected final score small in the selectable cells, best one with a green frame. ⚠ for key players.
- **Lists during the pairing:** toggle "Matrix | Lists" above the table. "Lists" shows the list of an active opponent (start: their defender); swipe left/right or use arrows/abbreviations to switch. Marks and notes from the matrix page are visible, also offline. Notes (line notes, list note, matchup notes "vs …") can be shown/hidden via the speech bubble at the top right, remembered per device; hiding also removes the mark colours. With the matrix expanded, already paired opponents are shown too: abbreviation greyed out, "(picked)" behind the army.
- **Attacker step:** as soon as their 2 attackers are tapped (and in the choice step), both lists stand side by side below the matrix.
- **Pre-calculation:** once when entering the pairing, only with a fully rated matrix (both modes, also with the eye switched off).
- Tapping a pick also selects it in the matrix (our side of the step); the other way round, the preview jumps to the matching pick.
- Tapping an option → **path preview**: likely matchups dashed in the matrix (numbered by round) + a timeline per round with the moves and the resulting matchups including their rating; step through rounds by swiping; final score with range (worst/best).
- "What if": press and hold an opponent's name = "assume they play that one" → recalculate.
- **Can be hidden:** the "Recommendations" switch (eye icon) next to the flip button. Default **off**, remembered per device – so that one is not unconsciously guided by numbers. Off = no numbers, no frames, no warnings.

Implementation in levels: (1) show existing maximin numbers, (2) path preview, (3) Nash probabilities, (4) key players + "what if".
Limit: only as good as the matrix; maybe an "uncertainty" slider later.

`PairingSession` only stores the entered moves; the state is replayed via `PairingState` (undo = remove the last move).

## Architecture

Hexagonal (ports & adapters). UI is replaceable, the API is the only interface.

```
src/
  ProPair.Domain              Entities, value objects, pairing rules, solver. No dependencies.
  ProPair.Application         Use cases, ports (IMatrixRepository, ...).
  ProPair.Adapters.Rest       ASP.NET Core Web API, hosts the WASM app.
  ProPair.Adapters.Mongo      MongoDB implementation of the repository ports.
  ProPair.Adapters.NewRecruit Client for the New Recruit API.
  ProPair.Contracts           DTOs, shared between API and client.
  ProPair.Web                 Blazor WebAssembly (only via contracts + HTTP).
tests/
  ProPair.Domain.Tests
  ProPair.Application.Tests
```

- Stack: .NET 11, Blazor WebAssembly (hosted), MongoDB.
- Dependency direction: adapters → application → domain.
- The domain layer also runs in the browser (same assembly): pairing rules and recommendations work offline. The UI contains no logic of its own.

## Domain model (DDD)

Tactical DDD after Evans/Vernon:
- Small aggregates, references between aggregates only by id.
- Invariants are checked in the aggregate root, never in the use case.
- Value objects are immutable, equality by value.
- Changes raise domain events.
- One transaction changes exactly one aggregate.

### Ubiquitous language

| Term | Meaning |
|------|---------|
| GameSystem | Game system (T9A) with its armies. |
| Team | Our team, lasting across tournaments. |
| Tournament | A tournament: format, game system, participating teams. |
| Participant | A team within a tournament, with a roster. |
| Roster | Players of a participant (name, army, list). |
| Preparation | Preparation of our team for a tournament. Holds the scale and share links. |
| Matrix | Ratings of our roster against one opposing participant. |
| RatingScale | Rating levels (label, colour, score). |
| Rating | One level from the RatingScale in a cell. |
| PairingSession | A running pairing based on a matrix. |

### Aggregates

**GameSystem** (reference data)
- Id, name, armies.

**Team**
- Id, name, `DefaultRatingScale`.
- Default scale freely changeable; only affects future preparations.

**Tournament**
- Id, name, GameSystemId, `TeamSize` (3–8).
- Participants (entity): Id, name, roster.
- Rosters are brought to TeamSize during sync (`Lineups`: dummies / surplus).
- Source: New Recruit import (ExternalId = NR tournament id).

**Preparation** (team × tournament)
- Id, TeamId, TournamentId, OwnParticipantId.
- `RatingScale`: copy of the team's default scale at creation, immutable afterwards.
- `EditToken`, `ReadToken`.
- PlayerNotes: player → note on the list.
- ListMarks: (player, line) → colour + note. Line by index + text; after a re-import found again via the text.
- Invariant: exactly one preparation per team and tournament.

**Matrix** (preparation × opponent)
- Id, PreparationId, OpponentParticipantId.
- Cells: (OwnPlayerId, OpponentPlayerId) → rating; notes: cell → note.
- History: `RatingChanged` (cell, old, new, author, time), append-only.
- Invariant: a rating must be in the preparation's scale.
- Own aggregate, so that working on different opponents at the same time does not collide.

**PairingSession**
- Id, PreparationId, OpponentParticipantId, moves (defender, attacker, choice).
- Invariant: moves follow the pairing flow.

### Value objects

`RatingScale`, `RatingLevel`, `Rating`, `TeamSize`, `ArmyId`, `PlayerRef`, `CellKey`, `ShareToken`, `AuthorName`.

### Domain services

- `PairingSolver`: calculates the best path from matrix + pairing state. Stateless.

## Collaboration, notes & history

- One shared rating per cell, last change wins. Every change as `RatingChanged` (who, when, old → new).
- Author: the name is asked for on the first edit and stored in the browser.
- **Cell:** tap = dropdown (quick rating). Long-press (phone) / right-click (desktop) = bottom sheet with the rating, a note on the matchup and "Show history" (loads on demand). No history icon.
- **List:** tap on a player's name = panel: **list on top, notes below**.
- **Marks in the list:** tap a single line → 6 colours (red, orange, yellow, green, blue, pink; colour applies immediately, the field stays open until OK) + a short note shown on the same line ("Careful, can take flamethrower"). Colours have no fixed meaning – the team decides. Remove mark = ✕.
- A dot at the top right marks cells/players with a note.
- Matchup notes live in the `Matrix` aggregate, list notes in the `Preparation` (apply to all opponents of the tournament).
- Empty text deletes a note. Max. 2000 characters.

## Status POC

- Pages: tournament selection/import → choose team → overview (share, opponents, progress) → matrix.
- Matrix: dropdown per cell, colours from the scale, list via tap on the player.
- Saving with optimistic versioning (Mongo), retry in the use case, client writes sequentially.
- Tests: domain (matrix, tournament, preparation), application (flow with fake source, in-memory repos).

## Decisions

| # | Topic | Decision |
|---|-------|----------|
| 1 | Scale | Default on the team (5 levels: +, 0/+, 0, 0/-, -). Copy per tournament, fixed there. |
| 2 | Multiple editors | One rating per cell, history with author. |
| 3 | Share link | Two tokens: edit and read-only. |
| 4 | .NET | .NET 11. |
| 5 | Opponent view | Negated own matrix. |
| 6 | Live sync | Not in the MVP. SignalR later as a nice-to-have. |
| 7 | Matrices | One matrix per opposing team per tournament. |
| 8 | Modelling | DDD, aggregates as above. |
| 9 | New Recruit | Import in the MVP, initially fake source. |
| 10 | UI | Mobile first. |
| 11 | Pairing | Selection by tap in the matrix (names = defender, cells = attacker/choice). Remaining matrix, flip to full matrix (taken = grey). Recommendations off for now. |
| 12 | Notes/history | Long-press/right-click sheet per cell, list note via tap on a player. |
| 13 | Recommendations | Can be hidden, default off (no psychological influence). |
| 15 | Recommendation modes | Optimal / Avoid minus ("-" 1.5×, "0/-" 1.25×); "Safeguard" removed (idea, see open questions); weighting only for the decision, everything shown is real points. Per pick: total score + this round, each with most likely value and its probability. Path: corner in the cell (black / black-light grey split / light grey per round), same corner on the "Round N" labels. |
| 16 | Offline | Local-first pairing: domain assembly also in the browser, moves local + queue, sync via complete move list, recommendations with round values pre-calculated by the backend. PWA: app starts without network after the first visit. |
| 14 | List marks | 6 colours + line note, list on top, notes below. |
| 17 | NR sync | Separate process (`Adapters.Sync`, Hangfire, Mongo storage), daily, today to +12 months. Only T9A 3rd Edition, only team tournaments, no leagues, nothing closed/past. API only enqueues "Update" (`ISyncQueue`). |
| 18 | List change | New list = new player id → ratings/notes start over, `ListChangedAt` → info icon. Hidden lists: last state stays, badge "No lists visible". |
| 19 | Team size | 3–8, most common player count of teams with 3 or more (tie: the larger). Army from the list text (NR does not deliver JSON before the tournament). |
| 20 | Non-players | A player is whoever has a list. Coaches, judges, NPCs are not imported; teams without players and with fewer members than the team size are dropped. |
| 21 | Incomplete rosters | Missing players/lists → "Dummy". Surplus lists stay out (the first ones count). Warning "Incomplete roster" in the tournament list, team choice, overview and matrix. |
| 22 | List format | Anti-corruption layer in the New Recruit adapter (`ArmyListTranslator`): header lines, point totals, footers and duplicate lists removed; placeholders ("kadrowicz") are not a list; unknown formats only lose total and footer. Army is detected from the raw text beforehand. List comparison counts only the units. |
| 23 | Release | One Docker image for app and sync (two processes, if one ends the container restarts). Sync URLs only reachable inside the container (no login). Azure DevOps like FadingFlame: build → Docker Hub, release via SSH + docker-compose behind Caddy. Own MongoDB in the compose file (no open port, own network), secrets only in Azure DevOps. |
| 24 | Who am I (without login) | In the edit link you pick via flyout which player of your own team you are (stored per team in the browser, changeable any time). Without a selection no opponent matrix. The player name is the author of all changes; the free name field goes away. Later replaced by login. |
| 25 | Matrix view | Own row highlighted. Tap a cell → flyout with ratings instead of a native select. Below the matrix a carousel (swipe/arrows) through the opponent lists: header with army and name, below it your own rating (+ delete), then the list with marks and notes. Team mates open their list with a close button, afterwards the carousel again. No automatic scrolling. |
| 26 | URLs | Speaking resources, one page per view: `/` tournaments · `/tournaments/{id}` choose team · `/preparation/{token}` overview · `…/opponent/{id}` matrix · `…/opponent/{id}/pairing` pairing matrix · `…/pairing/lists` lists. The API keeps its paths. |
| 27 | Recommendations | On by default in the pairing; the device remembers if you show/hide them. |

## Open questions

- Idea "Safeguard": opponent sees our move, maximise worst case (`PessimisticSolver`). Dropped for now – too pessimistic for hidden moves.

- Model table/scenario choice in the pairing?
- Captain access without login: own admin link per team, or login (e.g. GitHub/Google)?
- Sync rules from NR (`id_book` from reports?).
- Admin UI with login for sync/dashboard.
- With reserve players, let the user choose who plays (currently the first lists count).
- Is a tournament global (several teams use the same one) or does it belong only to our team?
