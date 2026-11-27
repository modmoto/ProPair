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
