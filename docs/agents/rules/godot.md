# The Godot client

## Godot 4 and C# only

The client is Godot 4 (.NET build) with C#. No GDScript, and no Godot 3 API: Godot 3 names (`KinematicBody`, `Spatial`, `yield`, `instance()`, `move_and_slide` with arguments, `export` keyword) are wrong here. Before using an API, check it exists in the pinned version (the `Godot.NET.Sdk` version in the game's project file; the gate image repeats it, and a test holds the two equal).

Where Godot requires a shape that `code-style.md` would not choose (for example `partial` node classes, or the visibility Godot's source generators need), Godot wins, and only for those classes.

## The client presents; it never decides

A node script turns input into commands for the sim, and sim events and snapshots into pictures, animation and sound. A rule written in a node script is a bug in where it lives: move it into `WaitingSim` with a test. The difficulty setting changes only which HUD widgets show.

## Drivable without a window

Everything a feature does can be checked headlessly (`agent-drivability.md`):

- The sim runner (`WaitingSim.Runner`) plays any level with scripted or recorded commands and reports state as `--json`.
- Every debug overlay value is also available from the runner or a log line.
- Feel problems are reported and reproduced with recorded replays and overlay values, not descriptions.
- Launching the real game is for what only eyes can judge (`running-the-app` skill).

## Scenes stay small

A `.tscn` holds what is placed by hand. Repetitive content (NPC spawns, a line of agents, level geometry derived from data) is built in code from data. A scene file growing past what a person would edit in the editor is a refactor to propose.

## Assets go through the pipeline

Models, textures and sounds enter the game only through the scripts in `tools/` (normalization, audio variants). A hand-imported asset is a hack under `agent-drivability.md`: the pipeline is missing a step.
