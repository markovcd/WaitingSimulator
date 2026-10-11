# Waiting Simulator 2026

A first-person game about the mundane tension of waiting. In line levels you step forward when the line moves, or the people behind you lose patience. In room levels you stay alert for your call while the room pulls at your attention.

- [DESIGN.md](DESIGN.md): the game design.
- [TECH.md](TECH.md): the technology plan.
- [docs/adr/](docs/adr/README.md): decisions behind the design.
- [docs/glossary.md](docs/glossary.md): one word for each thing.
- [AGENTS.md](AGENTS.md): how coding agents work in this repository.
- [TODO.md](TODO.md): work not yet started.

## Build and test

Pre-prototype: there is no code yet. Once the solution exists (see [TODO.md](TODO.md)):

- Requirements: the .NET SDK version Godot 4 uses, and Godot 4 (.NET build) for the client.
- Gate: `dotnet test WaitingSimulator.sln`
