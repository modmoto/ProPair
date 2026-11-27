# ProPair – Design

Pairing-Tool für Tabletop-Teamturniere. Start: The 9th Age (T9A).

## Ziel

- Paarungsmatrix pflegen: eigene Spieler (Zeilen) vs. gegnerische Spieler/Armeen (Spalten).
- Team trägt Einschätzungen gemeinsam ein – per Link, ohne Login.
- Während des Live-Pairings: Matrix öffnen, Züge eintragen, besten Pfad vorgeschlagen bekommen.

## Scope

**MVP**
- Teamformat 8v8, 5v5, 3v3.
- 16 T9A-Armeen als Auswahl (pro Spieler / Gegner).
- Matrix mit Dropdown pro Zelle, farbcodiert.
- Teilen per Link.

**Später**
- Turnier aus New Recruit importieren (Listen + Teams).
- Notizen pro Liste / Matchup ("Achtung auf X").
- PDF-Export für offline.
- Live-Sync für Zuschauer (SignalR).
- Pairing-Assistent (bester Pfad).
- Weitere Spielsysteme.

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

- Zustand: verbleibende Spieler beider Teams + aktuelle Phase.
- Jede Phase ist ein simultaner, verdeckter Zug → Spiel mit unvollständiger Information.
- Ansatz: Spielbaum mit Memoization (8v8: max. 2⁸·2⁸ Teilmengen – machbar).
- Bewertung eines Zustands: Summe der Scores der fixen Spiele + Wert des Rests.
- Strategie wählbar:
  - **Maximin** (worst case, "was passiert, wenn der Gegner optimal legt").
  - **Gemischte Strategie** (Nash pro Phase via LP) – genauer, später.
- UI: aktuellen Zustand anklicken → Top-Optionen mit erwartetem Ergebnis.
- Gegner-Sicht = negierte eigene Matrix. Wir nehmen an, dass die Teamwertung stimmt.
- Fokus: Captain bedient das Pairing.

## Architektur

Hexagonal (Ports & Adapters). UI austauschbar, API ist die einzige Schnittstelle.

```
src/
  ProPair.Domain              Entities, Value Objects, Pairing-Regeln, Solver. Keine Abhängigkeiten.
  ProPair.Application         Use Cases, Ports (IMatrixRepository, ...).
  ProPair.Adapters.Rest       ASP.NET Core Web API, hostet die WASM-App.
  ProPair.Adapters.Mongo      MongoDB-Implementierung der Repository-Ports.
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
- Später Quelle für den New-Recruit-Import.

**Preparation** (Team × Tournament)
- Id, TeamId, TournamentId, OwnParticipantId.
- `RatingScale`: Kopie der Team-Default-Skala beim Anlegen, danach unveränderlich.
- `EditToken`, `ReadToken`.
- Invariante: pro Team und Turnier genau eine Preparation.

**Matrix** (Preparation × Gegner)
- Id, PreparationId, OpponentParticipantId.
- Cells: (OwnPlayerId, OpponentPlayerId) → Rating, Notiz.
- History: `RatingChanged` (Zelle, alt, neu, Autor, Zeitpunkt), append-only.
- Invariante: Rating muss in der Skala der Preparation liegen.
- Eigenes Aggregat, damit gleichzeitiges Arbeiten an verschiedenen Gegnern nicht kollidiert.

**PairingSession** (später)
- Id, MatrixId, bisherige Züge (Defender, Attacker, Wahl).
- Invariante: Züge folgen dem Pairing-Ablauf.

### Value Objects

`RatingScale`, `RatingLevel`, `Rating`, `TeamSize`, `ArmyId`, `PlayerRef`, `CellKey`, `ShareToken`, `AuthorName`.

### Domain Services

- `PairingSolver`: berechnet den besten Pfad aus Matrix + Pairing-Zustand. Zustandslos.

## Zusammenarbeit & Verlauf

- Eine gemeinsame Wertung pro Zelle, letzte Änderung gewinnt.
- Jede Änderung wird als `RatingChange` gespeichert (wer, wann, alt → neu).
- Autor: Name wird beim ersten Öffnen des Links abgefragt, im Browser gespeichert.
- UI: kein History-Icon. Rechtsklick (mobil: Long-Press) auf Zelle → Kontextmenü mit "Verlauf".
- MVP-Fallback: Verlauf nur speichern, Anzeige später.

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

## Offene Fragen

- Tisch-/Szenariowahl im Pairing abbilden?
- Captain-Zugang ohne Login: eigener Admin-Link pro Team, oder Login (z. B. GitHub/Google)?
- MVP ohne New Recruit: Turnier, Gegner und Roster manuell durch den Captain anlegen?
- Ist ein Tournament global (mehrere Teams nutzen dasselbe) oder gehört es nur unserem Team?
