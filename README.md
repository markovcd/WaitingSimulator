# Waiting Simulator 2026

A first-person game about the mundane tension of waiting. In line levels you step forward when the line moves, or the people behind you lose patience. In room levels you stay alert for your call while the room pulls at your attention.

- [DESIGN.md](DESIGN.md): the game design.
- [TECH.md](TECH.md): the technology plan.
- [docs/adr/](docs/adr/README.md): decisions behind the design.
- [docs/glossary.md](docs/glossary.md): one word for each thing.
- [AGENTS.md](AGENTS.md): how coding agents work in this repository.
- [TODO.md](TODO.md): work not yet started.

## Build and test

Requirements: the .NET SDK version in [global.json](global.json), [Git LFS](https://git-lfs.com) (run `git lfs install` once; binary assets are stored in LFS per [.gitattributes](.gitattributes)), and Docker for the gate. To open the game in the editor, Godot 4.7.2 (.NET build), the version [game/WaitingSimulator.Game.csproj](game/WaitingSimulator.Game.csproj) names.

- Gate (build, every test and a headless Godot start, in the gate image): `./gate.sh` (from Git Bash on Windows)
- Every test, without Docker or Godot: `dotnet build WaitingSimulator.slnx && dotnet test --solution WaitingSimulator.slnx --no-build`
- One test class: `dotnet test --project sim/WaitingSim.Tests --filter-class "*CliTests"`
- The headless runner: `dotnet run --project sim/WaitingSim.Runner -- help`
- Check a replay against its recorded hashes: `dotnet run --project sim/WaitingSim.Runner -- replay <file>` (`--record` rewrites the hashes)
- A tuning sweep, every trait against every scripted player bot: `dotnet run --project sim/WaitingSim.Runner -- sweep` (`--json` for a report a script can read)

| Project | What it is |
|---|---|
| `game` | The Godot client. Started with `-- --smoke`, it quits once ready; the gate runs it that way. |
| `sim/WaitingSim` | The sim: every game rule, no engine references. |
| `sim/WaitingSim.Runner` | Command-line program that plays levels headlessly. |
| `sim/WaitingSim.Tests` | Tests for the sim and the runner (xunit v3). Golden replays are in `Replays/`. |
| `data` | Data the sim reads: `tuning.json`, `traits.json` and the levels in `levels/`. The tests load the shipped files. |
| `ci` | The gate image and the script it runs. |
