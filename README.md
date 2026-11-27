# ProPair

Pairing-Matrix für Tabletop-Teamturniere (The 9th Age). Design: [docs/design.md](docs/design.md).

## Lokal starten

Voraussetzungen: .NET 11 SDK, MongoDB.

```bash
mongod --dbpath .data/mongo --fork --logpath .data/mongod.log
```

```bash
dotnet run --project src/ProPair.Adapters.Rest --launch-profile http
```

App: http://localhost:5054

```bash
dotnet test
```

## Stand (POC)

- New Recruit ist gefakt (`FakeTournamentSource`): 4 Demo-Turniere (8er, 2× 5er, 3er).
- Ablauf: Turnier importieren → eigenes Team wählen → Gegner → Matrix bewerten.
- Edit-/Read-Link ohne Login. Verlauf wird gespeichert, aber noch nicht angezeigt.

## Pairing-Empfehlungen: der Algorithmus

![Pairing-Algorithmus](docs/pairing-algorithm.svg)

**Was er tut:** Er rechnet für den aktuellen Pairing-Schritt aus, welche Wahl uns am meisten bringt, wenn der Gegner genauso klug spielt. Annahme: Der Gegner bewertet jedes Spiel genau umgekehrt zu unserer Matrix (+2 für uns = −2 für ihn). Unbewertete Felder zählen 0.

**Wie:**
1. **Spielbaum:** Alle Verläufe bis zum Ende (Defender → Attacker → Wahl → nächste Runde). Am Ende jedes Zweigs ist der Endstand bekannt, die Summe der Matrix-Werte.
2. **Jeder verdeckte Schritt ist ein kleines Spiel:** Zeilen = unsere Möglichkeiten, Spalten = die des Gegners, Zelle = Wert von allem, was danach kommt (rekursiv). Größen: Defender 8×8, Attacker-Paare bis 21×21, Wahl 2×2.
3. **Lösung pro Schritt: Nash-Gleichgewicht** (gemischte Strategien, per Simplex). Beide legen verdeckt, also kann keiner auf den anderen reagieren. Das Gleichgewicht ist die Mischung, bei der sich keiner durch Abweichen verbessert. Ergebnis: ein fairer Erwartungswert und für jede Option eine Wahrscheinlichkeit.
4. **Memoisierung:** Zustände zu Rundenbeginn werden über die verbleibenden Spieler (Bitmasken) gecacht. 8v8 mit 3 vorhergesagten Pfaden: ca. 0,7 s.
5. **Ranking der Optionen:** erst Erwartungswert (auf 0,1 gerundet, so wie angezeigt), dann Worst case, dann Wahrscheinlichkeit. Gleich gute Optionen: die sicherere zuerst.
6. **Wahrscheinlichster Pfad:** Ab einer Option folgt er Schritt für Schritt den wahrscheinlichsten Zügen beider Seiten bis zum Ende. Ergebnis: gestrichelte Felder in der Matrix plus Zeitleiste.

**Beispiel (Bild rechts):** Verteidigt Ben, ist uns mindestens 0 sicher. Verteidigt Anna, sind +2 möglich, aber auch −2. Die beste Mischung ist 80 % Ben, 20 % Anna, fair erwartet +0,4. Ein rein pessimistischer Ansatz (der Gegner sieht unseren Zug) käme nur auf 0.

**Austauschbar:**
- `IPairingAdvisor`: der ganze Algorithmus. Die App kennt nur dieses Interface.
- `GameTreeAdvisor(IMatrixGameSolver)`: der Spielbaum. Wie ein einzelner Schritt gelöst wird, entscheidet der Solver:
  - `EquilibriumSolver`: Nash, der Standard.
  - `PessimisticSolver`: Maximin, der Gegner sieht unseren Zug (sichere Untergrenze).
- Registriert in `ProPair.Application/DependencyInjection.cs`.

**In der UI:** Das Augen-Symbol schaltet die Empfehlungen ein, standardmäßig sind sie aus. Gerechnet wird nur, wenn sie an sind.
