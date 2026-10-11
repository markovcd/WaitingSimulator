# The simulation

The game's rules live in `WaitingSim`, a plain .NET library. The Godot project shows what the sim decided. These rules are what make replays, leaderboards, headless tuning and later multiplayer possible; [ADR 0002](../../adr/0002-the-simulation-is-an-engine-free-library-that-owns-gameplay.md) records why.

## The sim knows nothing about the engine

`WaitingSim` references no Godot assembly, no rendering, no audio and no input API. It does not log through the engine and does not read files the engine owns. Anything the sim needs from a level (lane polylines, collision shapes, spawn points) arrives as data.

**How to apply:** if a rule needs something only the engine has, the engine hands it in as data or a command. Never add a Godot reference to the sim "for now".

## Deterministic, tick by tick

- The sim advances in fixed ticks (60 per second). Every duration is stored and compared in ticks, never seconds or frames.
- Time is an argument. Nothing in the sim reads a wall clock, `DateTime.Now`, `Stopwatch` or frame delta.
- All randomness goes through the sim's own seeded RNG, whose state is part of the sim state. Never `System.Random`, `Guid.NewGuid` or an engine RNG in sim code.
- No outcome depends on hash-set or dictionary iteration order, thread timing or float formatting.
- The same seed and the same command stream produce the same state hash on every tick, whatever frame rate drives the sim.

**How to apply:** where a banned API can be caught by an analyzer (BannedApiAnalyzers), it is, so the build fails instead of a reviewer having to notice.

## Commands in, events out

- A player and an NPC brain both produce the same per-tick command type. The sim validates a command; it never takes a state change from outside.
- The sim reports what happened as typed events (a gap opened, a stage changed, a cutter entered) and exposes read-only snapshots. Presentation subscribes to events and reads snapshots; it never writes sim state.
- No global player. An agent is passed to whatever needs it.

## The sim alone decides gameplay positions

Movement, collision between agents and with the level, bumps and rebounds, spacing zones and lane membership are computed in the sim. Engine physics is cosmetic: wobble, sway and stumbles driven by sim events, never fed back into a position.

## Tuning is data

Grace windows, drain and recovery rates, trait definitions, cutter frequency and every other knob live in JSON under `data/`, loaded by the sim. One place knows each number; a magic constant in code that a designer would tune is a missing data field. Level, tuning, save and replay files are persisted formats under `saved-data.md`: versioned, with an upgrade step for each version raise.

## Golden replays are the sim's snapshots

A golden replay is a recorded seed and command stream with the expected state hashes. A change that alters one is a behavior change: re-record it only when the change was the point, say so in the commit message, and never re-record to make a red test green (`honest-tests.md`).
