# Waiting Simulator 2026 — Design Doc

Status: early design, pre-prototype. This file records decisions made so far, what is deferred, and what is still open.

## 1. Pitch

A first-person "simulator" game about the mundane tension of waiting. In line levels you must step forward when the line moves, or the people behind you lose patience and eventually push past you or kick you out. In room levels you must stay alert for your call while a waiting room full of people and distractions pulls at your attention.

The premise is played straight. The comedy comes from deadpan presentation and deliberately janky simulator-style controls and physics.

Working title: **Waiting Simulator 2026** (earlier: Line Simulator 2026).

## 2. Pillars

1. **Stay alert while boredom pulls you away.** Line and room levels share this skill; only what you watch or listen for changes.
2. **Social pressure is the fail state.** Slapstick is not. Sighs, "excuse me?", a shoulder tap, someone stepping past you.
3. **Imperfect information.** You cannot see the whole line. You read rhythm from partial cues: the shoulders ahead, shuffling feet, distant bells and announcements.
4. **Strict rules, loose feel.** Gap timing and the patience system are precise. Controls, physics and animation supply the jank.
5. **Mundane-realistic tone.** Plausible events, restrained visuals, body language doing the storytelling. Absurdity comes from deadpan UI and simulator conventions, not wacky events.

## 3. View and Style

- First-person 3D.
- Low-fi "Simulator 20XX" aesthetic: crude models, stiff animation, floaty physics. Jank is expected and is part of the humor.
- Absurdly serious UI text (for example, "POSITION: 14").
- Muted palette, minimal UI.

## 4. Modes

### Campaign: "A Day of Errands"
- Fixed sequence of stops across one bad day (post office, doctor's waiting room, urząd or DMV, airport).
- State carries over: phone battery, fatigue, reputation, paperwork.
- Soft failure: getting kicked out puts you at the back of the line and costs time, which pushes the rest of the day later, so a bad morning snowballs.
- Home of the story and the deadpan humor.

### Arcade: individual levels
- Pick any line or room level from a menu. No carry-over.
- Hard failure: kicked out ends the run.
- Score chasing (time survived, patience hits taken, distractions handled).
- Unlockable levels and modifiers (faster line, grumpier neighbor, louder room).
- Leaderboards split by difficulty.

Levels are built once and reused. A level is a scenario; the mode is the rules wrapper around it (failure rule, carry-over, scoring).

## 5. Level Types

- **Line levels:** close the gap when the line moves.
- **Room levels:** listen and watch for your call (number, name, screen, receptionist).
- **Hybrids:** queue to take a number, then wait in a room listening for it; or get called, then pick the right line for the right window.

## 6. Line Mechanics

### The gap
- When the person ahead steps forward, a gap opens.
- A grace window (about 1.5 s, tunable) lets you close it without penalty.
- Stepping too early or too far bumps into the person ahead: awkward sound, small patience penalty, rebound.

### Spacing zones (starting values, measured to the person ahead)
| Zone | Distance | Effect |
|---|---|---|
| Bump | under ~0.4 m | one-off patience hit, rebound |
| Tight (ideal) | ~0.5-0.9 m | patience recovers slowly |
| Loose | ~0.9-1.5 m | no drain, no recovery |
| Gap | over ~1.5 m | grace timer starts, then drain |

### Cutters
If the gap stays open too long, a cutter slides in front. You lose position and the person behind blames you (large patience hit).

### The line ripple
The front moves in bursts, and the movement travels back with a variable delay. Reading and anticipating the ripple is the core skill.

### Leaving the line
- **Prototype rule:** leaving the lane volume means you are out of the line.
- **Later:** a spot-hold timer (about 5 s) so stepping out becomes a risky tool (pick up an item, peek down the line, let someone through). Costs: a patience hit, cutter risk, reputation. Possible save-a-spot item (jacket or bag).
- Implement the lane as a trigger volume. The player agent has an `InLane` state and fires `LeftLane` and `ReenteredLane` events from day one, so the hold timer is a small change later.

## 7. Room Mechanics (outline, needs detail)

- You are called via speaker, screen, or a receptionist. Announcements are easy to miss (mumbling, another language, TV noise, a coughing kid).
- Miss your call and you are skipped or bumped to the back.
- Attention budget: phone (battery drains), magazines, vending machine, TV, boredom and fatigue.
- Etiquette layer: seat choice (near door vs far, too close to someone), armrest and bag-on-seat politics, noise (loud calls, crunchy snacks), small talk traps (ignoring offends, engaging risks missing your call).
- Imperfect information: partial view of the receptionist's screen, your ticket number takes attention to check, unreliable rumors from other patients.
- Room types: doctor's office, DMV/urząd (take-a-number), airport gate, car repair shop.

## 8. Patience System

The heart of the game. See the stage table below for the readable behavior layer.

### Agents
Every NPC that depends on the player has a `PatienceAgent` with a value from 0 to 100 and a trait.
- Line mode: mainly the person directly behind you. People further back are affected indirectly.
- Room mode: neighbors, driven by noise, space and etiquette instead of speed.
- One shared component; only triggers differ.

### Drains
- Open gap past the grace window (continuous, the main one)
- Bumping the person ahead (one-off)
- Letting a cutter in (large one-off)
- Distractions that make you stall
- Staring back at them
- Room mode: loud phone, crunchy snacks, spreading your stuff, sitting too close

### Recovery (slow and small, so players cannot just park in a safe zone)
- Staying tight on the person ahead (trickle)
- Small social gestures (nod, letting someone pass)
- Letting someone ahead of you can cost position but buys patience (trade-off choice)

### Escalation stages
The meter is hidden. Behavior is the readout.

| Patience | NPC behavior |
|---|---|
| 100-70 | Neutral, idle shuffling |
| 70-50 | Sigh, checks watch |
| 50-30 | Foot-tapping, muttering |
| 30-15 | "Excuse me?", leans into your space |
| 15-0 | Shoulder tap, then pushes past or reports you |

Use hysteresis: once an NPC reaches a stage it does not instantly drop back when patience recovers slightly, so cues do not flicker.

### Personality traits (data on the agent)
- Patient: slow drain, forgives easily
- Anxious: fast drain, loud and obvious cues (easy to read)
- Aggressive: fast drain, skips polite stages
- Passive-aggressive: hides cues then snaps (hard to read)
- Oblivious: earbuds in, barely reacts, may drift into your space

Whether traits are fixed per level or random per run is **undecided and cheap to change**, because the trait is just data.

### Line mood
Optional soft shared layer: nearby annoyed NPCs add a small drain. Keep it simple at first.

### Kick-out
At 0 the NPC pushes past (you lose position), calls staff (warning), or turns the crowd against you. Campaign softens this; arcade makes it a hard fail.

### Tuning knobs
Grace window length, base drain, recovery rate, distraction frequency, cutter frequency, personality mix.

### Readability risk
Because the meter is hidden, players must be able to learn what each cue means. Early-stage cues must be clear, and the first stage change should read as a warning.

## 9. Payoff at the Front

A short mini-scene, no cutscene: step up, the clerk says one line, then the result screen. Reuse one clerk model and animation set; vary only text.

Outcomes:
1. **Success:** a flat, dry "Next."
2. **Wrong-window twist:** "Wrong queue." "Come back tomorrow." "We close in two minutes." The signature joke; use occasionally, not every level.
3. **Penalized success:** you made it, but earlier mistakes follow you (the clerk mentions the cutter, the people behind groan).

Mode behavior:
- Campaign: the outcome carries over (a wrong-window result can add a stop later; a good result can give a stamped form).
- Arcade: the outcome feeds the score. Twists are bonuses or modifiers, not run-enders.
- Twists are seeded per level (not random per run) so leaderboards stay comparable and players can learn.

Prototype scope: "reach the counter, clerk says one line, result screen." Add twists after the core loop feels good.

## 10. Controls (first draft)

- **Move:** WASD / left stick. Walking only; no run button.
- **Look:** mouse / right stick.
- **Glance back:** hold to turn your head toward the person behind you, release to snap forward. The cost is that the front of the line is out of view while you look back.
- **Interact:** one context button (tap a stranger, let someone ahead, pick up a dropped item).
- **Phone:** separate hold. Raises the phone and lowers the camera; the main temptation and the most common way to miss a gap. (Hold vs toggle: start with hold.)
- Plan for gamepad support from the start.

### Where the jank lives
- Slightly slippery walking (acceleration and a little inertia), so overshooting is easy and stopping precisely is a skill.
- Soft bump physics: hitting the person ahead rebounds you, they stumble, a sound plays.
- Exaggerated head bob and camera sway.
- Wobbly physics for bag, coffee and phone.

Rules stay strict; only execution feels clunky.

## 11. Difficulty and the HUD

Difficulty is how much the game tells you. The simulation is identical across all settings.
- **Hard (no HUD):** cues only (body language, audio, environment).
- **Normal:** a subtle hint layer (for example, a faint indicator when a gap has opened).
- **Easy (full HUD):** deadpan numbers such as "PATIENCE OF PERSON BEHIND: 62%", plus a gap timer.

Leaderboards split per difficulty. The no-HUD version must be fully readable on its own; playtest it first. If it only works with the HUD, the cues need work.

## 12. Distractions (not yet designed)

Candidates: phone buzz, slipping bag strap, dropped item, a call from your boss, a kid tugging your sleeve, a stranger starting small talk. Each should compete with watching the line, so the player constantly trades boredom relief against vigilance. Frequency, trade-offs and boredom/fatigue interaction are still open.

## 13. Architecture Ground Rules

These cost almost nothing now and keep multiplayer possible later.

1. Separate simulation from presentation. Patience, line movement, gap timing and failure rules are plain C# logic with no knowledge of cameras, animation or audio. Presentation listens to events (stage changes drive poses and sounds).
2. Tick-based, deterministic simulation: fixed timestep, no frame-rate dependence, all randomness through a seeded RNG. Helps replays, leaderboards and later server authority.
3. NPC and human players are both "an agent in the line" behind one interface. Agent state is plain data.
4. Player input is a command validated by the simulation ("step forward"), not a direct state change.
5. No `Game.Player` singleton; pass the agent around.

Engine: Godot 4 (C#) recommended, with Unity 6 as the fallback if consoles become a launch requirement. The simulation lives in an engine-agnostic netstandard2.1 library. See [TECH.md](TECH.md) for the full stack and rationale.

## 14. Multiplayer (future, out of v1)

Strong fits: players as each other's "person behind you" in a shared line; co-op line where one slow player hurts the whole team; competitive parallel lines with cutting and spot-saving; an asymmetric cutter mode; human neighbors in room mode.

Considerations: each client should only receive what that player could plausibly see (imperfect information doubles as anti-cheat). The 1.5 s grace window tolerates ordinary latency. A human reacting replaces the cue system, which changes tuning. Budget for disconnects and late joins when the time comes.

## 15. Competitive Landscape and Naming

Existing games with similar titles:
- *Line Simulator* (Steam, VR): about skipping the line, the opposite of this game.
- *Line Simulator* (itch.io jam game, Windows, first-person 3D): wait for a console while doing tasks or get kicked out. The closest in concept, but a one-week jam project whose pressure comes from tasks.
- *Queue Simulator* (itch.io): atmospheric walking sim.
- *Wait in Line Simulator* (itch.io): a game where you only wait.

Differentiator: the gap-and-patience loop, the person behind you as a character, and imperfect information as the core of the game. Avoid leaning on the jam game's task-juggling hook. Before committing to the name, search it on Steam and itch.io and check domain and social handles.

## 16. Open Questions

- Distractions: types, frequency, trade-offs.
- Room mode moment-to-moment play: how calls are delivered and how listening is made skill-based.
- Personality: fixed per level or random per run (cheap to defer).
- Phone: hold vs toggle (start with hold).
- Kick-out details: which outcome (push past, staff, crowd) in which situation.
- Final title.

## 17. Deferred (Later List)

- Spot-hold timer when leaving the lane, save-a-spot item, cutter risk while away
- Personality assignment rules
- Line mood (shared annoyance)
- Wrong-window twists and carry-over effects
- Multiplayer

## 18. Suggested First Prototype

The smallest slice that tells you whether the game is fun:
- One line level, first-person, WASD plus look and glance-back.
- One person ahead, one person behind with a `PatienceAgent` and the stage cues (animation and audio).
- Line movement in irregular bursts, the gap and grace window, the spacing zones, bump physics.
- One cutter event and the kick-out rule (out of the line).
- Reach the counter, one clerk line, result screen.
- Optional HUD toggle for debugging patience values.

If standing in that line is tense and readable without the HUD, the core works; build the room mode second, on the same systems.
