# Waiting Simulator 2026 — Technology Plan

Status: **engine and language decided: Godot 4 + C#** (locked 2026-10-11). Pre-prototype. Companion to [DESIGN.md](DESIGN.md). Records the stack, why, and what to verify during the prototype.

Project constraints that drove the decision:
- **Platforms:** PC (Windows, Linux) and Steam Deck only. No consoles.
- **Team:** no prior experience with Godot or Unity.
- **Assets:** mostly AI-generated art and audio, normalized by script.
- **Workflow:** mostly AI-assisted ("vibe") coding with Claude Code.

## 1. What the design demands from the tech

Pulled from DESIGN.md; every choice below is checked against this list.

| Requirement | Source | Tech implication |
|---|---|---|
| First-person 3D, low-fi, crude models, stiff animation | §3 | No need for a high-end renderer. Iteration speed matters more than fidelity. |
| Strict rules, loose feel | §2.4, §10 | Gameplay outcomes must not depend on engine physics. Jank is cosmetic. |
| Sim separate from presentation, plain C# | §13.1 | Sim lives in its own assembly with no engine references. |
| Deterministic fixed tick, seeded RNG | §13.2 | Own tick loop and own PRNG; replays and leaderboards rely on it. |
| Player and NPC behind one agent interface, input as commands | §13.3-4 | Input is sampled into per-tick command structs and fed to the sim. |
| Imperfect information, audio cues, missable announcements | §2.3, §7 | Audio is a gameplay system: spatialization, occlusion, ducking, randomized barks. |
| Body language is the readout | §8 | Animation state machine driven by sim events, plus procedural head look and lean. |
| Gamepad from day one | §10 | Action-based input mapping, no hard-coded keys. |
| Arcade leaderboards split by difficulty, seeded twists | §4, §9 | Platform leaderboards; replays as a verification path later. |
| Multiplayer later, server authority, per-client visibility | §14 | Sim must run headless outside the engine. |

## 2. Stack at a glance

| Area | Choice |
|---|---|
| Engine | **Godot 4 (.NET build), C#. Decided.** Pin the latest stable 4.x at project start and upgrade deliberately. |
| Simulation | Separate `WaitingSim` C# class library on .NET 10 (the client targets the same), no engine references, unit-tested with xunit v3. Solution file: `WaitingSimulator.slnx`. |
| Game client | Godot project that references `WaitingSim` and does only presentation, input and audio. |
| Tick | 60 Hz fixed step, owned by the sim driver (accumulator), render interpolation between snapshots. |
| RNG | Own PCG32 or xoshiro128** in the sim with serializable state. Never `System.Random` or engine RNG in sim code. |
| Physics | Sim owns gameplay movement and collision (simple 2D circles vs. static shapes). Engine physics (Jolt) only for cosmetic wobble. |
| Audio | Godot built-in audio for the prototype. Evaluate FMOD Studio before room mode. AI-generated voice and SFX, variants made by script. |
| Animation | Humanoid clip library (Mixamo or similar), retargeted in Godot; `AnimationTree` state machine; procedural look-at and lean modifiers. |
| Art pipeline | AI-generated models and textures, normalized by a headless Blender script; one look-unifying shader; shared NPC skeleton (see §5.3). |
| Data and tuning | Traits, level scenarios and tuning knobs as JSON loaded by the sim. Hot reload in debug builds. |
| Input | Godot InputMap actions (keyboard, mouse, gamepad). Steam Input on top later. |
| Platform services | Steamworks via Steamworks.NET (or Facepunch.Steamworks): leaderboards, achievements, later networking. |
| Future netcode | Headless .NET server running `WaitingSim`; transport via Steam Networking Sockets or LiteNetLib. |
| Source control | Git + Git LFS for binaries (`.blend`, audio, textures). |
| CI | GitHub Actions: build and test `WaitingSim` on every push; headless Godot export for tagged builds. |

## 3. Engine decision: Godot 4 + C#

**Decided.** Unity 6 was the main alternative. It was rejected because its advantages don't apply to this project. Other engines (Unreal, web stacks, Bevy) were ruled out earlier as too heavy or too immature for a feel-driven game.

### Why Godot
- **No consoles.** Unity's biggest advantage (first-party console support) is irrelevant. Godot is excellent on Linux and Steam Deck, with small builds.
- **No prior engine experience.** Godot is smaller and faster to learn, and this game uses only a fraction of either engine.
- **AI-generated assets.** Unity's asset store advantage is gone. AI 3D tools output glTF, which Godot imports natively.
- **Fits the scope.** Crude models, a handful of rooms, tens of NPCs. Godot's light editor and fast play loop favor iterating on feel, which is where this game lives.
- **No licensing exposure.** MIT license, no revenue tiers or seat fees.
- **Text scenes and resources** merge cleanly in Git and are easy for Claude Code to read and edit.
- **Jolt built in** (the default for new 3D projects since 4.6), plenty for cosmetic wobble.

### Why C#
- **Compiler feedback for AI-assisted coding.** Claude Code sees build errors and fixes them before anything runs. With GDScript, many errors only show up at runtime.
- **A real engine-free simulation library.** The rules (§13.1 of DESIGN.md) become a plain .NET library that is unit-tested with `dotnet test` without launching Godot, reused by the headless runner and, later, by a multiplayer server.
- **Determinism is easier to enforce** with static types, structs and full control over RNG.
- **Modern .NET and tooling:** current C#, Rider or VS Code, Steamworks.NET and LiteNetLib.

### Accepted costs
- **Audio tooling is the weakest link.** Godot's built-in audio may be enough; otherwise FMOD through a community integration. Spike before room mode (§5.1).
- **No browser build.** C# web export in Godot is still experimental. Not a goal.
- **Most Godot tutorials are GDScript.** They translate to C# almost line for line.
- **Consoles would mean a third-party port** with weak C# support. Out of scope by decision; revisit only as a post-launch business question.

## 4. Simulation architecture

### 4.1 Project layout
```
/sim/WaitingSim/            .NET class library, zero engine references
/sim/WaitingSim.Tests/      xUnit tests, golden replays
/sim/WaitingSim.Runner/     console app: headless runs, bot players, tuning sweeps
/game/                      Godot project; references WaitingSim
/data/                      JSON: traits, levels, tuning presets
/art/                       raw AI outputs and Blender sources (LFS)
/tools/                     Blender normalization script, audio variant scripts
/docs/                      ADRs, glossary, agent rules, handoff notes
```

### 4.2 Tick and time
- 60 Hz fixed step. All durations are stored in ticks (grace window 1.5 s = 90 ticks), so timing rules are exact integers.
- The Godot side runs its own accumulator in `_Process`, steps the sim 0-N times per frame, and interpolates visuals between the previous and current snapshot. Do not tie the sim to `_PhysicsProcess` or engine physics settings.
- Pause, slow-motion debugging and fast-forward are all just "step N times".

### 4.3 Commands in, events out
- Each tick the client samples input into a `PlayerCommand` (move vector, look yaw, glance-back held, phone held, interact pressed). NPC brains produce the same command type, which satisfies §13.3-4 directly.
- The sim emits typed events: `GapOpened`, `GraceExpired`, `Bumped`, `StageChanged(agent, stage)`, `CutterEntered`, `LeftLane`, `ReenteredLane`, `CalledNumber`, `KickedOut`, `ReachedCounter`.
- Presentation maps events to animation, audio and camera. The sim never knows a sound exists.

### 4.4 Movement and the "strict rules, loose feel" split
This is the most important technical call in the plan.
- **The sim owns gameplay movement.** The player is a circle on the floor plane with velocity, acceleration and friction (the "slippery walking" from §10 is sim parameters, so it is deterministic and tunable). NPCs are circles too. Level collision is a few static boxes and segments exported from the Godot scene into level data.
- **Bump detection and rebound are computed in the sim** (circle overlap plus a small 1D impulse along the lane axis), so the zone table in §6 is exact.
- **Engine physics is cosmetic only.** Head bob, camera sway, the bag and coffee wobble and the NPC stumble are driven from sim events and never feed back into positions.
- Spacing zones are measured along the lane polyline (projection onto the lane), not as raw Euclidean distance, so curved and snaking lines work.
- Escape hatch: if hand-rolled collision becomes a burden in room levels, the engine character controller can drive the player and its position can be fed into the sim as input. This loses strict determinism for that mode, so treat it as a fallback.

### 4.5 Determinism
- Integer ticks for all timers, an own PRNG with serializable state, no `Dictionary` iteration-order dependence, no wall-clock reads, and no engine calls in sim code.
- Floats are fine for v1 positions; same build on the same platform replays identically in practice. Every N ticks, record a state hash in replays; a hash mismatch flags drift. Move to fixed-point only if cross-platform replay verification becomes a real requirement.
- **Golden replay tests:** commit recorded command streams plus expected final state hashes. CI fails if a code change silently alters outcomes.

### 4.6 Patience and agents
- `PatienceAgent` is plain data (value, trait id, current stage, hysteresis thresholds) plus a pure update function. Traits are JSON records, which keeps the "fixed vs. random per run" question (§8) a data change.
- NPC behavior: start with a small explicit state machine per agent (idle, shuffle, step forward, react, push past). Behavior trees are overkill at this scale.

### 4.7 Headless runner as a design tool
`WaitingSim.Runner` plays thousands of lines with scripted bot players (perfect, sloppy, distracted) and outputs patience curves and kick-out rates per trait mix. Use it to tune grace windows, drain rates and cutter frequency before playtests, and to sanity-check that difficulty modifiers in arcade mode behave as intended.

## 5. Presentation

### 5.1 Audio (gameplay-critical)
Room levels depend on hearing your call through noise, so audio is not polish.
- **Prototype:** Godot buses (Voices, Announcements, Ambience, Phone, UI), `AudioStreamPlayer3D` with attenuation, low-pass and reverb bus effects, raycast-based occlusion, and ducking when the phone is raised.
- **Before room mode, spike FMOD Studio** (free for small-budget indies) through the community Godot integration. Parameter-driven mixes (TV loudness, crowd density, "phone focus") and randomized containers for mumbles and coughs are what FMOD is good at. Adopt it if the spike shows the integration is stable from C#.
- **Announcements and voices:** AI-generated deadpan voices (ElevenLabs or similar). Variants (clear, mumbled, through a PA, half-masked by noise, other language) are produced by ffmpeg or sox scripts in `/tools/`. Number and name announcements are assembled from short fragments, so the sim can call any number.
- **SFX:** AI-generated or library sounds for sighs, coughs, shuffling, bumps and room ambience, with several variants each so repetition doesn't give cues away.

### 5.2 Animation
- A shared humanoid skeleton for all NPCs and the clerk. A clip library covering idle, shuffle, step, sigh, watch check, foot tap, lean in, tap shoulder, push past and stumble.
- An `AnimationTree` state machine keyed by `StageChanged` and movement events. Stage hysteresis lives in the sim, so animation never flickers.
- Procedural layers for readability: head look-at toward the player (especially when the player glances back), torso lean into personal space, and a little noise. Stiffness is on-brand, so no blending polish is needed.

### 5.3 Art (AI-generated)
- **Generation:** AI 3D tools (Meshy, Tripo, Rodin or similar) for props and clothing; AI image tools for textures, posters and signage. Keep the raw outputs in `/art/raw/` so they can be re-processed.
- **Normalization:** a headless Blender Python script in `/tools/` fixes scale and pivot, reduces triangle counts, strips or bakes materials onto the shared palette, and exports `.glb`. Every asset goes through it; nothing is imported by hand.
- **One look-unifying shader** over everything (flat or posterized lighting, muted color ramp, optional dithering). It hides inconsistencies between generated assets and *is* the "Simulator 20XX" look. Slight wrongness reads as style.
- **Characters:** all NPCs and the clerk share one humanoid skeleton so every patience-stage animation works on every body. Use MPFB2 base bodies or Mixamo's auto-rigger, with AI-generated clothing and textures on top.
- **Licensing:** check each tool's terms for commercial use, and fill in Steam's AI-generated content disclosure on the store page.
- Performance is a non-issue at expected counts (under 50 skinned characters on screen); no LOD or crowd system needed for v1.

### 5.4 UI
- Godot Control nodes. Deadpan HUD text ("POSITION: 14") reads sim snapshots only.
- Difficulty (§11) is purely a presentation flag that changes which HUD widgets are shown. The sim is identical across difficulties, which matches the design.

### 5.5 Input
- InputMap actions: `move`, `look`, `glance_back`, `interact`, `phone`. Hold vs. toggle for the phone is a setting in the input layer, not the sim.
- Look sensitivity and dead zones per device. Steam Input later gives free remapping on Steam Deck and gamepads.

## 6. Platform, services and multiplayer

- **Target:** Windows and Linux on Steam, Steam Deck verified. macOS optional. No consoles.
- **Leaderboards:** Steam leaderboards, one board per level × difficulty. Optionally attach replays as UGC for later verification. Seeded twists (§9) keep runs comparable.
- **Saves:** campaign state (battery, fatigue, reputation, paperwork) is a sim data object serialized to JSON. Steam Cloud syncs the save folder.
- **Multiplayer (later):** the same `WaitingSim` runs in a headless .NET server process. Clients send `PlayerCommand`s and receive filtered snapshots containing only what each player could see (§14 visibility doubles as anti-cheat). For co-op without dedicated servers, use host-authoritative play over Steam Networking Sockets (Steam relay handles NAT); use LiteNetLib if leaving Steam. Avoid the engine's high-level multiplayer API so networking stays engine-independent.

## 7. Tooling and workflow

- **Repo:** monorepo as in §4.1. Git LFS for `*.blend`, `*.wav`, `*.ogg`, `*.png`, `*.psd`, `*.glb`.
- **CI (GitHub Actions):** on every push, `dotnet test` the sim (fast, no engine). On tags, export with headless Godot and upload builds as artifacts.
- **Debug tools:** in-game overlay with patience values, zones, grace timer, RNG seed and tick; record-and-replay hotkey; a "step one tick" pause mode. These cost little given the architecture and are essential for tuning the hidden meter (§8 readability risk).
- **AI-assisted development (Claude Code):**
  - `AGENTS.md` (imported by `CLAUDE.md`) loads the engineering rules in `docs/agents/rules/`, adapted from the agentic-coding-practices repo. `simulation.md` and `godot.md` hold this project's architecture rules: no engine calls in `WaitingSim`, commands in, events out, ticks not seconds, no Godot 3 APIs.
  - Claude Code writes code, tests, scenes, shaders, data and tooling, and verifies with `dotnet build`, `dotnet test` and `godot --headless`. The human playtests, judges feel and visuals, and curates assets.
  - Keep `.tscn` files small; build repetitive content (NPC spawns, level geometry) from data in code, since large hand-edited scenes break easily.
  - Run Claude Code locally next to the editor so it can launch the game and read its output.
  - Feel is tuned in JSON with hot reload by the human, not by asking for constant changes. Report problems with debug-overlay values, not impressions.
- **Editor tooling:** a simple Godot editor plugin to export lane polylines, collision shapes and spawn points from a level scene into the level JSON the sim loads.

## 8. Risks and how the prototype tests them

| Risk | Mitigation / prototype check |
|---|---|
| Sim-owned movement feels worse than engine controller | Build the movement model first; tune inertia in the runner and in-game with live JSON reload. |
| Godot audio too limited for room mode | FMOD spike before room mode (§5.1). |
| AI assets look inconsistent | Mandatory normalization script plus the unifying shader; reject generations that don't survive both. |
| AI-written code breaks rules silently | Golden replay tests and unit tests in CI; the rules in `docs/agents/rules/`, enforced by analyzers where possible. |
| Float determinism drift | State hashes in replays plus golden replay tests in CI. |
| Hidden meter unreadable | Debug overlay plus runner data to separate "the tuning is wrong" from "the cue is unclear". |

## 9. Open technical questions

- FMOD vs. built-in audio: decided by the spike.
- Fixed-point positions: only if cross-platform replay verification is needed.
- Replay storage and leaderboard verification depth for arcade (trust client, spot-check replays, or full server re-sim).

## 10. First steps

The live list is [TODO.md](TODO.md).

1. Create the `sim/` solution: `WaitingSim`, tests and runner. Implement tick loop, PRNG, `PlayerCommand`, lane projection, spacing zones and the grace timer with unit tests.
2. Add `PatienceAgent` with stages and hysteresis, plus JSON traits. Write the runner's first bot sweep.
3. Create the Godot project: first-person camera, input actions, the sim driver with interpolation, and capsule placeholders for the person ahead, the person behind and the clerk.
4. Wire events to placeholder audio and animation, and add the debug overlay.
5. Play the §18 slice. Only then commit to art, FMOD or engine-specific tooling.
