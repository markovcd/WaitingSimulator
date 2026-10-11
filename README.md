# Waiting Simulator 2026

A first-person game about the mundane tension of waiting. In line levels you step forward when the line moves, or the people behind you lose patience. In room levels you stay alert for your call while the room pulls at your attention.

- [DESIGN.md](DESIGN.md): the game design.
- [TECH.md](TECH.md): the technology plan.
- [docs/adr/](docs/adr/README.md): decisions behind the design.
- [docs/glossary.md](docs/glossary.md): one word for each thing.
- [AGENTS.md](AGENTS.md): how coding agents work in this repository.
- [TODO.md](TODO.md): work not yet started.

## Build and test

Requirements: the .NET SDK version in [global.json](global.json), and [Git LFS](https://git-lfs.com) (run `git lfs install` once; binary assets are stored in LFS per [.gitattributes](.gitattributes)). Godot 4 (.NET build) will be needed once the client exists.

- Gate (build and every test): `dotnet test --solution WaitingSimulator.slnx`
- One test class: `dotnet test --project sim/WaitingSim.Tests --filter-class "*CliTests"`
- The headless runner: `dotnet run --project sim/WaitingSim.Runner -- help`

| Project | What it is |
|---|---|
| `sim/WaitingSim` | The sim: every game rule, no engine references. |
| `sim/WaitingSim.Runner` | Command-line program that plays levels headlessly. |
| `sim/WaitingSim.Tests` | Tests for the sim and the runner (xunit v3). |
| `data` | Tuning the sim reads (`tuning.json`); the tests load the shipped files. |
