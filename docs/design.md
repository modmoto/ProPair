# ProPair – Design

Pairing-Tool für Tabletop-Teamturniere. Start: The 9th Age (T9A).

## Ziel

- Paarungsmatrix pflegen: eigene Spieler (Zeilen) vs. gegnerische Spieler/Armeen (Spalten).
- Team trägt Einschätzungen gemeinsam ein – per Link, ohne Login.
- Während des Live-Pairings: Matrix öffnen, Züge eintragen, besten Pfad vorgeschlagen bekommen.

## Scope

**MVP**
- Turnier aus New Recruit importieren (Teams, Spieler, Listen).
- Teamformat 8v8, 5v5, 3v3.
- 16 T9A-Armeen als Auswahl (pro Spieler / Gegner).
- Matrix mit Dropdown pro Zelle, farbcodiert.
- Teilen per Link.

**Später**
- PDF-Export für offline.
- Live-Sync für Zuschauer (SignalR).
- Weitere Spielsysteme.

## UI-Grundsätze

- **Mobile first.** Basis-CSS ist das Handy (≥ 360px), größere Screens per `min-width`-Media-Queries.
- 8×8-Matrix passt ohne Scrollen auf ein 360px-Handy; eigene Spieler als fixierte erste Spalte, Gegner als fixierte Kopfzeile.
- Touch-Ziele ≥ 44px, Eingaben ≥ 16px (kein iOS-Zoom), native Dropdowns.
- Keine Hover-only-Funktionen: Liste per Tap auf den Spieler, Verlauf per Long-Press.

## Bewertungsskala

Default: 5 Stufen. Jedes Team hat eine Default-Skala (Stufen, Labels, Farben, Score). Beim Anlegen einer Turnier-Vorbereitung wird sie kopiert und ist für dieses Turnier fix.

| Wert | Farbe | Score |
|------|-------|-------|
| +    | grün     | +2 |
| 0/+  | hellgrün | +1 |
| 0    | gelb     | 0 |
| 0/-  | orange   | −1 |
| -    | rot      | −2 |

Optional ergänzbar, z. B. `++` (dunkelgrün).

## T9A-Armeen

Beast Herds, Daemon Legions, Dread Elves, Dwarven Holds, Empire of Sonnstahl, Highborn Elves, Infernal Dwarves, Kingdom of Equitaine, Ogre Khans, Orcs and Goblins, Saurian Ancients, Sylvan Elves, Undying Dynasties, Vampire Covenant, Vermin Swarm, Warriors of the Dark Gods.

Als Stammdaten pro Spielsystem, nicht hartcodiert.

## Pairing-Ablauf (Standard, ETC/WTC-Stil)

Eine Runde:
1. Beide Teams legen verdeckt einen **Defender**. Aufdecken.
2. Beide Teams legen verdeckt **2 Attacker** gegen den gegnerischen Defender. Aufdecken.
3. Jeder Defender wählt einen der 2 Attacker. → 2 Spiele fix.
4. Der abgelehnte Attacker geht zurück in den Pool.

Ende (abhängig von verbleibenden Spielern pro Team):
- **4 übrig:** Defender + 2 Attacker. Abgelehnte Attacker spielen gegeneinander, die letzten zwei ebenfalls.
- **3 übrig:** Defender + 2 Attacker. Abgelehnte Attacker spielen gegeneinander.

Damit:
- 8v8: Runde (8→6), Runde (6→4), Finale mit 4 → 2+2+4 Spiele.
- 5v5: Runde (5→3), Finale mit 3 → 2+3 Spiele.
- 3v3: Finale mit 3 → 3 Spiele.

Ablauf als Regel-Objekt modellieren (austauschbar pro Turnier/System). Tisch-/Szenariowahl ggf. später.

## Pairing-Assistent

Bedienung – alles in der Matrix, keine Listen (mobil per Daumen):
1. **Defender:** links unseren Namen antippen, oben deren → gespeichert. Defender-Zeile und -Spalte hervorgehoben, Rest blass (Farben bleiben leicht sichtbar). Defender erneut antippen = abwählen.
2. **Attacker:** Zellen antippen – in unserer Defender-Zeile deren 2 Attacker, in deren Defender-Spalte unsere 2. Kreuzung der Defender ist gesperrt (schraffiert). „Attacker eintragen“.
3. **Wahl:** in der Zeile antippen, wen wir nehmen; in der Spalte, wen sie nehmen. „Wahl eintragen“.
→ Gepaarte Spieler hüpfen raus, die Matrix wird kleiner. Rückgängig (erst lokale Taps, dann letzter Schritt) / Neu starten (mit Sicherheitsabfrage) in der Aktionsleiste unten.

Ansicht:
- Standard: nur verbleibende Spieler.
- Flip-Button oben rechts: ursprüngliche Matrix (Flip-Animation). Vergebene Spieler hellgrau, Matchups umrandet. Nach dem Pairing automatisch.
- Empfehlungen sind vorerst ausgeblendet (Solver läuft weiter im Backend), Details unter der Matrix kommen später.
- Demo-Turniere: Button „Demo-Werte füllen“ auf der Matrix-Seite (nur für Fake-Turniere).

Solver (`PairingSolver`, Domain Service):
- Vollständiger Spielbaum, memoisiert über Bitmasken der verbleibenden Spieler. 8v8 ≈ 0,2 s.
- Simultane Schritte pessimistisch: wir wählen, Gegner antwortet optimal (Maximin).
- Pro Option: **Worst case** (Gegner optimal) und **⌀** (Durchschnitt über alle Gegner-Antworten).
- Gegner-Sicht = negierte eigene Matrix. Unbewertete Felder zählen 0 (Hinweis in der UI).
- Läuft serverseitig; Offline im Client später möglich (Domain ist abhängigkeitsfrei).
- Später: gemischte Strategien (Nash pro Schritt) statt reinem Maximin.

`PairingSession` speichert nur die eingegebenen Züge; der Zustand wird über `PairingState` neu abgespielt (Undo = letzten Zug entfernen).

## Architektur

Hexagonal (Ports & Adapters). UI austauschbar, API ist die einzige Schnittstelle.

```
src/
  ProPair.Domain              Entities, Value Objects, Pairing-Regeln, Solver. Keine Abhängigkeiten.
  ProPair.Application         Use Cases, Ports (IMatrixRepository, ...).
  ProPair.Adapters.Rest       ASP.NET Core Web API, hostet die WASM-App.
  ProPair.Adapters.Mongo      MongoDB-Implementierung der Repository-Ports.
  ProPair.Adapters.NewRecruit Client für die New-Recruit-API.
  ProPair.Contracts           DTOs, geteilt zwischen API und Client.
  ProPair.Web                 Blazor WebAssembly (nur über Contracts + HTTP).
tests/
  ProPair.Domain.Tests
  ProPair.Application.Tests
```

- Stack: .NET 11, Blazor WebAssembly (hosted), MongoDB.
- Abhängigkeitsrichtung: Adapters → Application → Domain.
- Solver im Domain-Layer → kann auch im Client laufen (offline).

## Domänenmodell (DDD)

Taktisches DDD nach Evans/Vernon:
- Kleine Aggregate, Referenzen zwischen Aggregaten nur per Id.
- Invarianten werden im Aggregate Root geprüft, nie im Use Case.
- Value Objects immutable, Gleichheit über Werte.
- Änderungen erzeugen Domain Events.
- Eine Transaktion ändert genau ein Aggregat.

### Ubiquitous Language

| Begriff | Bedeutung |
|---------|-----------|
| GameSystem | Spielsystem (T9A) mit seinen Armeen. |
| Team | Unser Team, dauerhaft über Turniere hinweg. |
| Tournament | Ein Turnier: Format, Spielsystem, teilnehmende Teams. |
| Participant | Ein Team innerhalb eines Turniers, mit Roster. |
| Roster | Spieler eines Participants (Name, Armee, Liste). |
| Preparation | Vorbereitung unseres Teams auf ein Turnier. Hält Skala und Share-Links. |
| Matrix | Bewertungen unseres Rosters gegen einen Gegner-Participant. |
| RatingScale | Bewertungsstufen (Label, Farbe, Score). |
| Rating | Eine Stufe aus der RatingScale in einer Zelle. |
| PairingSession | Ein laufendes Pairing auf Basis einer Matrix. |

### Aggregate

**GameSystem** (Referenzdaten)
- Id, Name, Armeen.

**Team**
- Id, Name, `DefaultRatingScale`.
- Default-Skala frei änderbar; betrifft nur künftige Preparations.

**Tournament**
- Id, Name, GameSystemId, `TeamSize` (3/5/8).
- Participants (Entity): Id, Name, Roster.
- Invariante: Roster-Größe = TeamSize, Armeen aus dem GameSystem.
- Quelle: New-Recruit-Import (ExternalId = NR-Turnier-Id).

**Preparation** (Team × Tournament)
- Id, TeamId, TournamentId, OwnParticipantId.
- `RatingScale`: Kopie der Team-Default-Skala beim Anlegen, danach unveränderlich.
- `EditToken`, `ReadToken`.
- PlayerNotes: Spieler → Notiz zur Liste.
- Invariante: pro Team und Turnier genau eine Preparation.

**Matrix** (Preparation × Gegner)
- Id, PreparationId, OpponentParticipantId.
- Cells: (OwnPlayerId, OpponentPlayerId) → Rating; Notes: Zelle → Notiz.
- History: `RatingChanged` (Zelle, alt, neu, Autor, Zeitpunkt), append-only.
- Invariante: Rating muss in der Skala der Preparation liegen.
- Eigenes Aggregat, damit gleichzeitiges Arbeiten an verschiedenen Gegnern nicht kollidiert.

**PairingSession**
- Id, PreparationId, OpponentParticipantId, Züge (Defender, Attacker, Wahl).
- Invariante: Züge folgen dem Pairing-Ablauf.

### Value Objects

`RatingScale`, `RatingLevel`, `Rating`, `TeamSize`, `ArmyId`, `PlayerRef`, `CellKey`, `ShareToken`, `AuthorName`.

### Domain Services

- `PairingSolver`: berechnet den besten Pfad aus Matrix + Pairing-Zustand. Zustandslos.

## Zusammenarbeit, Notizen & Verlauf

- Eine gemeinsame Wertung pro Zelle, letzte Änderung gewinnt. Jede Änderung als `RatingChanged` (wer, wann, alt → neu).
- Autor: Name wird beim ersten Bearbeiten abgefragt, im Browser gespeichert.
- **Zelle:** Tap = Dropdown (schnell bewerten). Long-Press (Handy) / Rechtsklick (Desktop) = Bottom-Sheet mit Bewertung, Notiz zum Matchup und „Verlauf anzeigen“ (lädt bei Bedarf). Kein History-Icon.
- **Liste:** Tap auf Spielername = Liste + Notiz zur Liste („Achtung auf …“).
- Punkt oben rechts markiert Zellen/Spieler mit Notiz.
- Matchup-Notizen liegen im `Matrix`-Aggregat, Listen-Notizen in der `Preparation` (gelten für alle Gegner des Turniers).
- Leerer Text löscht eine Notiz. Max. 2000 Zeichen.

## Stand POC

- Seiten: Turnierauswahl/Import → Team wählen → Übersicht (Teilen, Gegner, Fortschritt) → Matrix.
- Matrix: Dropdown pro Zelle, Farben aus der Skala, Liste per Tap auf den Spieler.
- Speichern mit optimistischer Versionierung (Mongo), Retry im Use Case, Client schreibt sequenziell.
- Tests: Domain (Matrix, Tournament, Preparation), Application (Flow mit Fake-Quelle, In-Memory-Repos).

## Entscheidungen

| # | Thema | Entscheidung |
|---|-------|--------------|
| 1 | Skala | Default am Team (5 Stufen: +, 0/+, 0, 0/-, -). Kopie pro Turnier, dort fix. |
| 2 | Mehrere Bearbeiter | Eine Wertung pro Zelle, Verlauf mit Autor. |
| 3 | Share-Link | Zwei Tokens: Edit und Read-only. |
| 4 | .NET | .NET 11. |
| 5 | Gegner-Sicht | Negierte eigene Matrix. |
| 6 | Live-Sync | Nicht im MVP. SignalR später als Nice-to-have. |
| 7 | Matrizen | Pro Turnier eine Matrix je Gegnerteam. |
| 8 | Modellierung | DDD, Aggregate wie oben. |
| 9 | New Recruit | Import im MVP, zunächst Fake-Quelle. |
| 10 | UI | Mobile first. |
| 12 | Notizen/Verlauf | Long-Press/Rechtsklick-Sheet je Zelle, Listen-Notiz per Tap auf Spieler. |
| 11 | Pairing | Auswahl per Tap in der Matrix (Namen = Defender, Zellen = Attacker/Wahl). Rest-Matrix, Flip auf Vollmatrix (vergeben = grau). Empfehlungen vorerst aus. |

## Offene Fragen

- Tisch-/Szenariowahl im Pairing abbilden?
- Captain-Zugang ohne Login: eigener Admin-Link pro Team, oder Login (z. B. GitHub/Google)?
- Fallback, falls NR-Zugang fehlt: Turnier manuell anlegen?
- Format der Listen aus NR (Text/JSON) – prüfen, sobald Zugang da ist.
- Ist ein Tournament global (mehrere Teams nutzen dasselbe) oder gehört es nur unserem Team?
