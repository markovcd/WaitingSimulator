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
- Play a level to the counter with a scripted bot: `dotnet run --project sim/WaitingSim.Runner -- play` (`--bot slow`, `--level <name>`, `--json`)
- A tuning sweep, every trait against every scripted player bot: `dotnet run --project sim/WaitingSim.Runner -- sweep` (`--json` for a report a script can read)
- Play: open `game/` in Godot 4.7.2 (.NET) and run it. WASD or the left stick walks, the mouse or the right stick looks, holding Q, the right mouse button or the left shoulder glances back (which is how you listen when the person behind makes small talk), F2 cycles the difficulty (easy, normal or hard: how much the HUD tells you; `-- --difficulty easy` starts on one), F3 toggles the debug overlay, F5 re-reads `data/` and restarts the level on it (a file with problems is shown and changes nothing), and Esc frees the mouse; a click in the window takes it back. A line at the bottom says how to play until you walk off your spot. The lane is marked on the floor: pale while you are in the line, amber while you are out with your place held, with a bar across the lane at your place that shrinks as the hold runs out, red once you are out of it. Reaching the counter, or being kicked out when the person behind runs out of patience, ends the level with a result screen; R or Start lines up again.

| Project | What it is |
|---|---|
| `game` | The Godot client. Started with `-- --smoke`, it plays a few frames of the level, logs the debug overlay and quits, or exits with code 1 on a data problem; the gate runs it that way. |
| `sim/WaitingSim` | The sim: every game rule, no engine references. |
| `sim/WaitingSim.Runner` | Command-line program that plays levels headlessly. |
| `sim/WaitingSim.Tests` | Tests for the sim, the runner and the client's engine-free parts (xunit v3). Golden replays are in `Replays/`. |
| `data` | Data the sim reads: `tuning.json`, `traits.json` and the levels in `levels/`. The tests load the shipped files. |
| `ci` | The gate image and the script it runs. |
