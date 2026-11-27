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
- Pairing-Assistent (bester Pfad).
- Weitere Spielsysteme.

## Bewertungsskala

Default: 5 Stufen. Pro Team anpassbar (Stufen, Labels, Farben, Score).

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
- Gegner-Matrix = negierte eigene Matrix (Annahme, siehe offene Fragen).

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

## Domänenmodell (erste Skizze)

- **GameSystem**: Name, Armeen, Bewertungsskala.
- **Team**: Name, Spieler (Name, Armee, Liste, Notizen).
- **Matchup**: eigenes Team vs. Gegnerteam, Format (3/5/8), Matrix.
- **Matrix**: Zelle (eigener Spieler, Gegner) → aktuelle Bewertung + optionale Notiz.
- **RatingChange**: Zelle, alter/neuer Wert, Autor, Zeitpunkt. Append-only.
- **PairingSession**: Matchup + bisher gespielte Züge.
- **ShareLink**: geheimer Token → Zugriff auf Matchup (lesen/schreiben).

## Zusammenarbeit & Verlauf

- Eine gemeinsame Wertung pro Zelle, letzte Änderung gewinnt.
- Jede Änderung wird als `RatingChange` gespeichert (wer, wann, alt → neu).
- Autor: Name wird beim ersten Öffnen des Links abgefragt, im Browser gespeichert.
- UI: kein History-Icon. Rechtsklick (mobil: Long-Press) auf Zelle → Kontextmenü mit "Verlauf".
- MVP-Fallback: Verlauf nur speichern, Anzeige später.

## Entscheidungen

| # | Thema | Entscheidung |
|---|-------|--------------|
| 1 | Skala | Pro Team anpassbar, Default 5 Stufen (+, 0/+, 0, 0/-, -). |
| 2 | Mehrere Bearbeiter | Eine Wertung pro Zelle, Verlauf mit Autor. |
| 3 | Share-Link | Zwei Tokens: Edit und Read-only. |
| 4 | .NET | .NET 11. |

## Offene Fragen

- Gegner-Sicht im Assistenten: negierte eigene Matrix oder eigene Einschätzung des Gegners?
- Echtzeit-Sync (SignalR) im MVP oder Reload reicht?
- Ein Team, viele Matchups (Gegnerteams) – oder Matrix pro Turnier gruppiert?
- Tisch-/Szenariowahl im Pairing abbilden?
