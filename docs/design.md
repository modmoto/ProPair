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

| Wert | Farbe | Score (Default) |
|------|-------|-----------------|
| ++   | dunkelgrün | +2 |
| +    | grün       | +1 |
| 0/+  | hellgrün   | +0.5 |
| 0    | gelb       | 0 |
| 0/-  | orange     | −0.5 |
| -    | rot        | −1 |

Skala ist konfigurierbar (Labels, Farben, Score) – siehe offene Fragen.

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

- Stack: .NET 8, Blazor WebAssembly (hosted), MongoDB.
- Abhängigkeitsrichtung: Adapters → Application → Domain.
- Solver im Domain-Layer → kann auch im Client laufen (offline).

## Domänenmodell (erste Skizze)

- **GameSystem**: Name, Armeen, Bewertungsskala.
- **Team**: Name, Spieler (Name, Armee, Liste, Notizen).
- **Matchup**: eigenes Team vs. Gegnerteam, Format (3/5/8), Matrix.
- **Matrix**: Zelle (eigener Spieler, Gegner) → Bewertung + optionale Notiz.
- **PairingSession**: Matchup + bisher gespielte Züge.
- **ShareLink**: geheimer Token → Zugriff auf Matchup (lesen/schreiben).

## Offene Fragen

Siehe unten – Entscheidungen werden hier eingetragen.

| # | Frage | Entscheidung |
|---|-------|--------------|
| 1 | | |
