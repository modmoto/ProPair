# Handover

Stand: 27.11.2026, Commit `092579a` auf `main`. Für eine neue Session (z. B. Cloud), die ohne Vorgeschichte weitermacht.

## Was ist ProPair

Pairing-Matrix für T9A-Teamturniere (3–8 Spieler pro Team). Matrix bewerten, per Link teilen (Edit/Read ohne Login), Live-Pairing-Assistent mit Empfehlungen (Nash-Spielbaum), offline-fähig (PWA). Details: [design.md](design.md), [README](../README.md).

## Arbeitsweise (vom User gewünscht)

- Antworten auf Deutsch, kurz und prägnant, kein AI-Blabla.
- Mobile first: jede UI-Änderung bei 375px prüfen.
- Entscheidungen ins Design-Doc (Tabelle „Entscheidungen“), nach jedem Schritt committen und pushen (`main`, Repo `modmoto/ProPair`, privat).
- Strikt DDD + hexagonal: Logik nur in Domain/Application, UI austauschbar, UI ohne eigene Logik.
- Nicht bei jedem Klick Netz oder Neuberechnung: Pairing ist local-first (Domain läuft im Browser).

## Setup

- .NET 11 SDK, MongoDB (lokal: `mongod --dbpath .data/mongo --fork --logpath .data/mongod.log`).
- Auf dem Mac des Users: `/usr/local/share/dotnet` muss vorn im PATH stehen (das x64-dotnet in `/usr/local/bin` hat kein .NET 11).
- App: `dotnet run --project src/ProPair.Adapters.Rest --launch-profile http` → http://localhost:5054
- Sync: `dotnet run --project src/ProPair.Adapters.Sync --launch-profile http` → http://localhost:5090
- Tests: `dotnet test` (87 Domain + 13 Application, alle grün).

### New-Recruit-Zugang

- Lokal als User-Secrets (`--id propair`): `NewRecruit:Login`, `NewRecruit:Password`.
- Cloud/Server: Env-Vars `NewRecruit__Login`, `NewRecruit__Password`. **Sind in der Cloud nicht gesetzt** – ohne sie startet der Sync-Prozess nicht (Rest + Demo-Turniere laufen trotzdem).
- Zugangsdaten nie ausgeben, loggen oder committen. Secrets nur maskiert auflisten.
- NR protokolliert jeden Aufruf: Requests nacheinander, Pausen, ein Hangfire-Worker.

## Setup auf Windows (Umzug vom Mac)

Für Claude auf dem Windows-PC: diese Schritte der Reihe nach ausführen bzw. prüfen. Was der User selbst tun muss, ist markiert.

1. **Werkzeuge** (PowerShell):
   ```powershell
   winget install Git.Git GitHub.cli MongoDB.Server MongoDB.DatabaseTools
   ```
   .NET 11 SDK von https://dotnet.microsoft.com/download/dotnet/11.0 installieren. Prüfen: `dotnet --list-sdks` zeigt 11.x.
   MongoDB läuft danach als Windows-Dienst auf `localhost:27017` (kein `mongod --fork` wie auf dem Mac). Prüfen: `Get-Service MongoDB`.
2. **Repo** (privat, User meldet sich an):
   ```powershell
   gh auth login
   gh repo clone modmoto/ProPair
   ```
3. **`.claude/launch.json`**: `runtimeExecutable` ist der Mac-Pfad `/usr/local/share/dotnet/dotnet`. Auf Windows lokal auf `dotnet` ändern, **nicht committen** (der Mac braucht den Pfad).
4. **New-Recruit-Zugang – macht der User selbst** (Claude fragt nicht nach den Werten):
   ```powershell
   dotnet user-secrets set "NewRecruit:Login" "<login>" --id propair
   dotnet user-secrets set "NewRecruit:Password" "<passwort>" --id propair
   ```
   Landet in `%APPDATA%\Microsoft\UserSecrets\propair\secrets.json`.
5. **Datenbank**, eine von zwei Varianten:
   - Mit allen Bewertungen, Markierungen und Pairing-Ständen: Der User kopiert den Ordner `.data/propair-dump` vom Mac (liegt nicht im Git) auf den PC, z. B. per USB-Stick oder Cloud-Laufwerk, ins Repo nach `.data\propair-dump`. Dann:
     ```powershell
     mongorestore --drop .data\propair-dump
     ```
   - Nur Turniere, frisch von New Recruit: Sync starten (Schritt 6), dann
     ```powershell
     Invoke-RestMethod -Method Post "http://localhost:5090/sync/backfill?from=2025-11-27&to=2026-11-27"
     Invoke-RestMethod -Method Post "http://localhost:5090/sync/upcoming"
     ```
6. **Starten und prüfen**:
   ```powershell
   dotnet test
   dotnet run --project src/ProPair.Adapters.Rest --launch-profile http
   dotnet run --project src/ProPair.Adapters.Sync --launch-profile http
   ```
   App http://localhost:5054 zeigt die Turnierliste, Sync http://localhost:5090 die Übersicht. Mit Dump: `curl http://localhost:5054/api/tournaments` liefert ca. 56 Turniere.
7. **Präferenzen merken**: Die Punkte unter „Arbeitsweise“ als Memory anlegen (lagen auf dem Mac nur lokal).

## Architektur

```
src/
  ProPair.Domain              Aggregate (Team, Tournament, Preparation, Matrix, PairingSession), Pairing-Regeln,
                              Solver/Advisor, ArmyDetector. Läuft auch im Browser.
  ProPair.Application         Use Cases + Ports (Repositories, ITournamentSource, ISyncQueue, IClock).
  ProPair.Contracts           DTOs.
  ProPair.Adapters.Mongo      Repos (optimistische Versionierung) + Hangfire-Job-Storage (Prefix "hangfire").
  ProPair.Adapters.NewRecruit Api/NewRecruitClient (echt) + Fake/ (Demo-Turniere).
  ProPair.Adapters.Rest       Web API, hostet Blazor WASM. Nutzt nur die Demo-Quelle, reiht Syncs ein.
  ProPair.Adapters.Sync       Eigener Prozess: Hangfire-Server, täglicher Sync, /sync/*-URLs, Dashboard /hangfire.
  ProPair.Web                 Blazor WASM (Pages/, Services/LocalPairing.cs = local-first Pairing).
```

## New-Recruit-API (verifiziert)

- `POST https://www.newrecruit.eu/api/<endpoint>`, Header `NR-Login` / `NR-Password`, JSON-Body.
- `tournaments` `{start, end, id_game_system, page}` → `{tournaments:[{_id,name,start,end,status}], total}`, 50/Seite. Status 0 pending, 1 open, 2 ongoing, 3 closed. Liefert überlappende Events (Ligen).
- `tournament` `{id}` → `{_id, name, showlists, type (1 = Team), teams:[{name, players:[{name, lists:[text]}]}]}`. Keine Ids für Spieler/Teams, keine Daten. `showlists=false` → Teams ohne Spieler.
- `reports` `{id_tournament}` → strukturierte Listen (`id_book`, `report_list`), erst nach gespielten Spielen.
- System: nur „The 9th Age 3rd Edition“ = 736602462.
- Listen sind Text: Kopfzeile „Armee“, „Name - Armee“ o. ä., teils französisch, Fußzeile „Created with newrecruit.eu“.

## Sync-Regeln (umgesetzt)

- Täglich 04:00 UTC, heute bis +12 Monate. Überspringt Einzelturniere, 2er-Teams, geschlossen/vorbei, Ligen (> 4 Tage).
- Mitglieder ohne Liste (Coaches, Judges, NPCs) sind keine Spieler und werden nicht importiert – vor der Begrenzung auf 8.
- Liste verborgen → letzter Stand bleibt, Badge „Keine Listen sichtbar“.
- Liste geändert → neue Spieler-Id (Bewertungen/Notizen weg), `ListChangedAt` → Info-Icon in der Matrix.
- Armee aus Text: Namen + Übersetzungen, sonst typische Einheiten; leer → „unknown“ („?“).
- Force-Update im Turnier (öffentlich, später Admin-UI). Backfill `POST /sync/backfill?from&to` nur zum Testen.

## Offene Punkte

- Listenformat bereinigen (Kopf-/Fußzeilen, zwei Listen pro Spieler).
- Ungleich große Kader: Pairing braucht gleiche Größe – Lösung offen. Seit dem Coach-Filter selten (Reservespieler mit Liste, fehlende Liste, TO-Teams).
- Einzel-Sync eines neuen Turniers kennt kein Datum (nur über die Suche).
- Lokale DB enthält noch den 2v2-„Warpi cup“ (vor dem Filter importiert).
- Regeln aus NR syncen (`id_book`?), Admin-UI mit Login, PDF-Export, Tisch-/Szenariowahl.
- Weitere offene Fragen: Design-Doc, Abschnitt „Offene Fragen“.

## Stolperfallen

- `wwwroot/index.html` lädt `_framework/blazor.webassembly.js` ohne Fingerprint-Platzhalter – sonst hängt die veröffentlichte PWA auf „Loading“.
- Service Worker nur im Publish-Build.
- Hangfire.Mongo: `CheckQueuedJobsStrategy.TailNotificationsCollection` (Standalone-mongod ohne Replica Set). Newtonsoft.Json auf 13.0.4 gepinnt (CVE).
- Blazor-Komponente heißt `NewRecruitStatus`, weil es in Pairing schon ein `SyncStatus`-Enum gibt.
- Daten des Users in der lokalen DB (Markierungen, Pairing-Stand) nicht verändern; Teständerungen zurücksetzen.
