# 0001. The game is built in Godot 4 with C#

- **Date:** 2026-10-11
- **Status:** Accepted

## Context

The game is first-person 3D with deliberately crude visuals, a handful of rooms and tens of NPCs. Its value is in feel: slippery walking, readable body language, missable audio cues. The project ships on PC (Windows, Linux) and Steam Deck only; consoles are out of scope. Nobody on the team has prior experience with Godot or Unity. Art and audio are mostly AI-generated. Most code is written with an AI coding agent.

The candidates were Godot 4 (C# or GDScript), Unity 6, Unreal 5, a web stack (Three.js, Babylon.js, PlayCanvas) and Bevy. The full comparison is in [TECH.md](../../TECH.md) §3.

## Decision

Build the game in Godot 4 (.NET build) with C#. Pin the latest stable 4.x and upgrade deliberately.

- **Godot over Unity:** Unity's main advantages (first-party consoles, the asset store) do not apply with no consoles and AI-generated assets. Godot is smaller to learn, has a faster iteration loop, text scenes that merge and that an agent can edit, and an MIT license with no revenue terms.
- **C# over GDScript:** the compiler catches an agent's mistakes before anything runs; the game rules become a plain .NET library that is tested without the engine and reused by a headless runner and a later server (ADR 0002); determinism is easier to enforce with static types.
- **Unreal, web stacks and Bevy** were ruled out as too heavy for the scope, too weak for a commercial desktop release, or too immature for a feel-driven game.

## Consequences

- Audio tooling is the weakest link. Godot's built-in audio is used for the prototype, and FMOD through a community integration is evaluated before room levels are built.
- No browser build: C# web export in Godot is experimental.
- Most Godot tutorials are GDScript; they translate to C# closely.
- A console port would go through a third party with weak C# support. Reopening that is a business decision and a new ADR.
