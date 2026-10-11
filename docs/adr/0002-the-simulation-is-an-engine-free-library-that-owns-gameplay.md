# 0002. The simulation is an engine-free library that owns gameplay

- **Date:** 2026-10-11
- **Status:** Accepted

## Context

The design (DESIGN.md §2, §13) asks for strict, precise rules with a loose, janky feel; for deterministic play that supports replays, comparable leaderboards and seeded twists; and for an architecture that allows server-authoritative multiplayer later. Engine physics and frame-driven updates are neither deterministic nor exact, and rules written into engine scripts can only be tested by running the engine.

## Decision

All game rules live in `WaitingSim`, a .NET class library with no engine references, driven in fixed 60 Hz ticks with its own seeded RNG. Agents (player and NPCs) send per-tick commands; the sim validates them and reports typed events and read-only snapshots.

The sim also owns gameplay movement and collision: agents are circles on the floor plane with velocity and friction, and level collision is simple static shapes exported from the level as data. Bumps, rebounds, spacing zones and lane membership are computed in the sim. Engine physics is cosmetic only.

The rules that follow from this are in [docs/agents/rules/simulation.md](../agents/rules/simulation.md).

## Consequences

- Every rule is unit-testable with `dotnet test`, and a headless runner can play thousands of lines for tuning.
- Replays are a seed plus a command stream; golden replays in the tests catch silent behavior changes.
- The same library can run in a headless multiplayer server.
- Movement and collision are hand-written instead of taken from the engine. If that proves too costly in room levels, letting the engine's character controller drive the player and feeding its position into the sim is the fallback, at the price of strict determinism in that mode; taking it is a new ADR.
- Floats are used for positions. Same-build replays are stable in practice; state hashes in replays detect drift. Fixed-point is adopted only if cross-platform replay verification becomes a requirement.
