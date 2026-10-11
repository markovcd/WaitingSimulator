# TODO

Work asked for and not yet started. Take an item off in the commit that lands it.

- Create `WaitingSimulator.sln` with `sim/WaitingSim`, `sim/WaitingSim.Tests` (xUnit) and `sim/WaitingSim.Runner`; shared `Directory.Build.props` (nullable, `TreatWarningsAsErrors`) and `Directory.Packages.props`; BannedApiAnalyzers for the sim's banned APIs (`simulation.md`).
- Add the GitHub Actions workflow that runs the gate, pinned and least-permission (`pipeline.md`).
- Add `.gitattributes` with Git LFS patterns for `*.blend`, `*.glb`, `*.png`, `*.psd`, `*.wav`, `*.ogg`.
- Sim core: tick loop, seeded RNG, the command type, lane projection, zones and the grace window, with tests.
- Patience: agents with traits, stages and hysteresis, traits loaded from `data/`; first runner sweep with scripted bot players.
- Golden replay format (versioned) and the first golden replay test.
- Godot client skeleton: first-person camera, input actions, the sim driver with interpolation, placeholder capsules for the person ahead, the person behind and the clerk.
- Wire sim events to placeholder audio and animation; debug overlay.
- Run the gate in a container image so local, CI and release mean the same thing (`pipeline.md`).
