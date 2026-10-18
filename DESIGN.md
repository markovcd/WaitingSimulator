# Waiting Simulator 2026 — Design Doc

Status: early design, pre-prototype. This file records decisions made so far, what is deferred, and what is still open.

## 1. Pitch

A first-person "simulator" game about the mundane tension of waiting. In line levels you must step forward when the line moves, or the people behind you lose patience and eventually push past you or kick you out. In room levels you must stay alert for your call while a waiting room full of people and distractions pulls at your attention.

The premise is played straight. The comedy comes from deadpan presentation and deliberately janky simulator-style controls and physics.

Audience: neurodivergent players first. The game turns the unwritten rules of waiting (when to step up, how long a glance back may last, what counts as cutting in) into a strict, readable system, and the joke is how absurd those rules are, never the player.

Working title: **Waiting Simulator 2026** (earlier: Line Simulator 2026).

## 2. Pillars

1. **Stay alert while boredom pulls you away.** Line, room and crowd levels share this skill; only what you watch or listen for changes.
2. **Social pressure is the fail state.** Slapstick is not. Sighs, "excuse me?", a shoulder tap, someone stepping past you.
3. **Imperfect information.** You cannot see the whole line. You read rhythm from partial cues: the shoulders ahead, shuffling feet, distant bells and announcements.
4. **Strict rules, loose feel.** Gap timing and the patience system are precise. Controls, physics and animation supply the jank.
5. **Mundane-realistic tone.** Plausible events, restrained visuals, body language doing the storytelling. Absurdity comes from deadpan UI and simulator conventions, not wacky events.
6. **Unwritten rules made legible.** Every social rule is a precise mechanic the game can show: easy's numbers are a feature, not a crutch. Sensory overload (noise, crowds, too many cues at once) is fair game as pressure.

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
- **Crowd levels:** get served from a crowd with no visible order, without crossing the people who were there first.
- **Hybrids:** queue to take a number, then wait in a room listening for it; or get called, then pick the right line for the right window.

### Crowd levels

A crowd pressing against a counter with no line: a bar, a deli counter with no numbers, tram doors opening, an airport gate when boarding is called, a bakery on a Sunday morning. It reuses the line's bodies, bumps and patience; nothing about it is fighting. Designed single-player first, with the two rules multiplayer needs (blocking has a limit, blame follows the cause) in from the start.

- **The invisible line.** The crowd has no visible order, but every agent remembers who arrived before it. Getting served ahead of someone who was there first costs their patience, and that of anyone who saw it. The player cannot see the order; noticing and remembering who came after you is the skill.
- **Catching the clerk's eye.** The clerk's glance sweeps along the front edge, and an agent at the front, facing the clerk, when the glance passes is served next. Waving or calling out catches it sooner but drains everyone near you. The clerk half-remembers the invisible line, so a blatant jump gets "I think she was first." Two agents in the glance at once are settled by the invisible line, never by timing.
- **Squeezing forward.** The crowd is denser near the front. When someone is served and leaves, a hole opens, and whoever leans in first takes it, decided on the tick. Leaning in is a short push, a bump that does not bounce, and costs the person squeezed past some patience.
- **Cutters block and get blocked.** A cutter is someone who arrived after you drifting forward past you. You stop one by holding your shoulder in its path, or by telling the clerk "I was here first" when it tries to be served, which works only if the sim recorded you as first. Blocking has a limit: everyone around the blocker loses patience with it, and a crowd lets a blocked agent through after a while, so nobody can be held back forever.
- **Losing is social pressure.** Push too many people to fed up and staff (a bouncer, at a bar) move you out; the softer loss is being edged to the back with no way forward. Staff blame whoever caused the drain, which the sim records, never whoever stands nearest.

**As built** (the deli counter, `data/levels/deli-counter.json`, 28 people along a 7 m counter; F6 or `-- --level deli-counter` plays it): everything above but these.
- Bodies are soft. People overlapping ease apart a quarter of the overlap each tick, the one moving in giving way most, so a press from the back ripples forward through the crowd like a fluid. Everyone the crowd moves sways a little and keeps leaning toward its spot when the way is blocked, so nobody stands exactly still.
- The glance lands on whoever is at the front within reach of it, facing the clerk or not.
- Blocking costs nothing by itself; its limit is the crowd letting the shover through after four seconds of shoving. Charging the blocker would charge everyone standing at the front, since everyone behind presses toward it.
- The softer loss is not built. Once staff move someone out, the person who called them is calm again.
- Hold F to wave; E says "I was here first".
- Starting values (`crowd` in `data/tuning.json`): the glance sweeps 1 m/s and lands within 0.35 m (1.2 m when waving), "Yes?" lasts 1 s, service 6 to 15 s, a newcomer every 10 to 20 s with one in seven pushing straight for the counter, sight 2.5 m, the clerk remembers anyone who arrived 45 s earlier, being served ahead costs 10, a false "I was here first" 20, being shoved (pressed into faster than 0.6 m/s) 6 a second. A press is 0.3 of walking pace and a sway 0.15, changing every 0.5 to 1.5 s.

Open questions are in §16.

### After Ego (boss level, parked)

A reproduction of a real night club, After Ego in Szczecin. You are not sober, and the line is not a line: it is a crowd pressing against the bar, and you have to really fight the cutters. When you finally reach the bar, it turns out you are on the wrong side, and you have to start over.

The layout, seen from above:

```text
                              N
 +-------------------------------------------------------+
 |                     DANCE FLOOR                       |
 |                                                       |
 |                                      +----------------+
 |      crowd at the selling side       |  rest of the   |
 |   o  o  o  o      o  o  o            |  dance floor   |
 |  ^                                   |  (north-east)  |
 |  |      +---------------------------------------------|
 |  |      |      BAR: sells on the north side only      |
 |  |      +---------------------------------------------|
 |  +--<--  o  o  o  YOU  o  o  o   crowd at the wrong   |
 |  round      o      o      o         side              |
 |  the west end                                         |
 +---------------------------------------- ENTRANCE -----+
```

- **The bar** runs east to west. Its east end meets the wall, so the only way round it is past its west end.
- **The dance floor** fills the north of the room, and the rest of it the north-east.
- **The entrance** is in the south-east, parallel to the bar. It lets you in on the bar's south side, the side that does not sell.
- **The bar sells only on its north side**, facing the dance floor. People crowd both sides.
- **The run:** you come in, push through the south crowd to the bar, are sent to the other side, go round the west end and start over in the north crowd.

Needs mechanics that do not exist yet: a crowd instead of a line (crowd levels, above), being drunk, fighting cutters, a bar reachable from both sides, and the wrong-side twist (a wrong-window twist, §9). Open questions are in §16.

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
- **Prototype rule:** a level places cutters beside the line. Once a gap has stayed open past its grace window for a further delay (about 2 s), the nearest cutter walks to the middle of it and slides in; if the gap closes first, it stops where it is and waits for the next one. Letting one in is a mistake on the result screen.

### The line ripple
The front moves in bursts, and the movement travels back with a variable delay. Reading and anticipating the ripple is the core skill.
- The person at the counter is served for 9 to 27 s, then leaves to the side and walks out of the room. The next steps up to the counter.
- Each person behind notices the person ahead moving off and steps up after a reaction delay of their own, stopping at the spacing they like.
- A burst moves the line by the spacing of whoever steps up to the counter, so some bursts open a gap and some only loosen the line.
- The line never runs out: newcomers keep arriving at the entrance, every 27 to 54 s, walk up beside the line to the back and join it, with a trait drawn at random. Nobody arrives while the back of the line is near the end of the lane.
- Some newcomers (about one in seven, at most two cutters waiting at once) come to cut in: one walks up beside the line looking for a gap left open too long and slides into it like any cutter. Reaching the front without finding one, it gives up and walks back to join the back.

### Leaving the line
- **Spot hold:** stepping out of the lane holds your place for about 5 s, so stepping out is a risky tool (pick up an item, peek down the line, let someone through). The person behind does not step into a held place, but if the line moves on, the gap it leaves counts against you as usual. Step back in ahead of the same person and you are back; stay out past the hold and the place is gone. Step in beside the person just ahead of your place, close enough to bump them, and you are falling in behind them: the place stays held until you stand behind them, and if the hold runs out first the line refuses you.
- **Cutting in (the gamble):** step into the lane anywhere but your held place or the back of the line and you cut in ahead of whoever is behind that spot. You make it only into a gap the person behind has left open past its grace window, and only with room to stand clear of a bump on both sides, so in the line's normal rhythm there is no way in; a gap left open too long is the opening. Made it: the person behind takes a small patience hit, since they left the gap. Missed: the line refuses you a place until you step out again, you lose your held place, and the person you tried to cut takes a hit. Standing in the lane refused a place blocks the person behind you: their patience drains as if you held a gap open past its grace window, and when it runs out they push past you.
- **The line pushes back:** everyone you pass (who stood ahead of your place and ends up behind you) takes a hit, and the person you cut in front of takes one more for each person passed. Your place is your held place, or the back of the line once it is gone. Cutting into a stale gap one place ahead is survivable; one far up the line empties the patience of the person you cut in front of, who pushes past you. The front of the line never has a gap, so walking up beside the line and stepping in at the counter is always a miss.
- **Later:** cutter risk while away, reputation, a save-a-spot item (jacket or bag).

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
Every NPC that depends on the player has patience, a value from 0 to 100, and a trait.
- Line mode: mainly the person directly behind you. People further back are affected indirectly.
- Room mode: neighbors, driven by noise, space and etiquette instead of speed.
- One shared component; only triggers differ.

### Drains
- Open gap past the grace window (continuous, the main one)
- Bumping the person ahead (one-off)
- Letting a cutter in (large one-off)
- Distractions that make you stall
- Staring back at them: a glance back longer than 1.5 s is staring, and drains them (8 a second) until you look away. Glancing back while they make small talk is listening, not staring. Easy shows "STARING AT PERSON BEHIND".
- Room mode: loud phone, crunchy snacks, spreading your stuff, sitting too close

### Recovery (slow and small, so players cannot just park in a safe zone)
- Staying tight on the person ahead (trickle)
- Small social gestures (nod, letting someone pass)
- Letting someone ahead of you can cost position but buys patience (trade-off choice). **Prototype:** step out of the lane (your place is held) and press interact: the person behind goes ahead into your place and gets 15 patience back, your held place moves to just behind them with the hold started over, and stepping back in there is returning, as is stepping straight back in beside them and then back behind them. Easy shows "E: LET THE PERSON BEHIND GO AHEAD" while you can.

### Escalation stages
The meter is hidden. Behavior is the readout.

| Patience | NPC behavior | Face |
|---|---|---|
| 100-70 | Neutral, idle shuffling | Slight smile |
| 70-50 | Sigh, checks watch | Flat mouth, brows just knit |
| 50-30 | Foot-tapping, muttering | Brows down, frown, narrowed eyes |
| 30-15 | "Excuse me?", leans into your space | Angry brows, frown, mouth open |
| 15-0 | Shoulder tap, then pushes past or reports you | Furious: brows hard down, eyes slits, shouting |

A face shows the stage the NPC's trait shows, so a passive-aggressive one keeps a straight face until it snaps.

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
- **Prototype rule:** the NPC pushes past the person ahead of it, who loses their place: a held place is gone, and the line refuses them one until they step out. For the player this ends the level with a "KICKED OUT" result screen, even when it happens the moment they reach the counter.

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
- Reaching the counter means stepping right up to it at the front of the line, about as close as a bump to a person.
- Penalized success in the prototype: the clerk's line names the worst mistake made on the way (cutting in, a failed cut in, a lost place, a gap left open past its grace window, a bump), otherwise a flat "Next.". The result screen shows the time in line and every mistake counted.

## 10. Controls (first draft)

- **Move:** WASD / left stick. Walking only; no run button.
- **Look:** mouse / right stick.
- **Glance back:** hold to turn your head toward the person behind you, release to snap forward. The cost is that the front of the line is out of view while you look back, and held too long it is staring (§8).
- **Interact:** one context button (tap a stranger, let someone ahead, pick up a dropped item). E or the gamepad's X; it lets the person behind go ahead today.
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

- **Prototype:** normal shows "GAP" while a gap is open in front of the player; easy shows the patience of the person behind and the grace left on a gap. Normal is the default; F2 cycles the difficulty in game, and the game remembers it between launches.

Leaderboards split per difficulty. The no-HUD version must be fully readable on its own; playtest it first. If it only works with the HUD, the cues need work.

## 12. Distractions

Each distraction competes with watching the line, so the player constantly trades boredom relief against vigilance. Each is optional tuning data, timed by the sim's seeded RNG.

- **Dropped ticket:** every 20 to 40 s the player's ticket slips and lands on the floor just outside the lane beside them, with a paper flutter. Picking it up means walking within reach of it, which means stepping out of the lane: the spot hold and the line moving on apply as usual. Only one lies on the floor at a time, and it only slips while the player stands in the line. Reaching the counter without it, the clerk sends the player away: "NO TICKET", a failed level like a kick-out. Easy shows "TICKET DROPPED", normal "TICKET".
- **Small talk from behind:** every 25 to 50 s the person behind starts talking to the player for 3 to 5 s, unless they are annoyed or worse. Listening means holding glance back for most of it (60%), so the front of the line is out of view while you do. Listened to, they get a little patience back (5); ignored, they take a hit (15). Easy shows "SMALL TALK: LISTEN (Q)", normal "TALK"; a "..." floats over their head and they mumble.

Still open: other candidates (phone buzz, slipping bag strap, a call from your boss, a kid tugging your sleeve), and how distractions interact with boredom and fatigue.

## 13. Architecture Ground Rules

These cost almost nothing now and keep multiplayer possible later.

1. Separate simulation from presentation. Patience, line movement, gap timing and failure rules are plain C# logic with no knowledge of cameras, animation or audio. Presentation listens to events (stage changes drive poses and sounds).
2. Tick-based, deterministic simulation: fixed timestep, no frame-rate dependence, all randomness through a seeded RNG. Helps replays, leaderboards and later server authority.
3. NPC and human players are both "an agent in the line" behind one interface. Agent state is plain data.
4. Player input is a command validated by the simulation ("step forward"), not a direct state change.
5. No `Game.Player` singleton; pass the agent around.

Engine: **Godot 4 with C# (decided).** The simulation lives in an engine-free .NET library. Target platforms are PC and Steam Deck. See [TECH.md](TECH.md) for the full stack and rationale.

## 14. Multiplayer (future, out of v1)

Strong fits: players as each other's "person behind you" in a shared line; co-op line where one slow player hurts the whole team; competitive parallel lines with cutting and spot-saving; an asymmetric cutter mode; human neighbors in room mode; crowd levels, where players block and outwait each other (§5).

Considerations: each client should only receive what that player could plausibly see (imperfect information doubles as anti-cheat). The 1.5 s grace window tolerates ordinary latency. A human reacting replaces the cue system, which changes tuning. Budget for disconnects and late joins when the time comes.

## 15. Competitive Landscape and Naming

Existing games with similar titles:
- *Line Simulator* (Steam, VR): about skipping the line, the opposite of this game.
- *Line Simulator* (itch.io jam game, Windows, first-person 3D): wait for a console while doing tasks or get kicked out. The closest in concept, but a one-week jam project whose pressure comes from tasks.
- *Queue Simulator* (itch.io): atmospheric walking sim.
- *Wait in Line Simulator* (itch.io): a game where you only wait.

Differentiator: the gap-and-patience loop, the person behind you as a character, and imperfect information as the core of the game. Avoid leaning on the jam game's task-juggling hook. Before committing to the name, search it on Steam and itch.io and check domain and social handles.

## 16. Open Questions

- Distractions beyond those in §12, and their interaction with boredom and fatigue.
- Room mode moment-to-moment play: how calls are delivered and how listening is made skill-based.
- Personality: fixed per level or random per run (cheap to defer).
- Phone: hold vs toggle (start with hold).
- Kick-out details: which outcome (push past, staff, crowd) in which situation.
- Final title.
- Crowd levels (§5): how many people a crowd holds, how often the clerk's glance passes, how long a block holds before the crowd lets someone through, and how the player is meant to read the invisible line without a HUD.
- After Ego (§5), parked: how many people stand on each side, what being drunk changes for you, and what starting over keeps (time, tally).

## 17. Deferred (Later List)

- Save-a-spot item, cutter risk while away
- Personality assignment rules
- Line mood (shared annoyance)
- Wrong-window twists and carry-over effects
- Multiplayer

## 18. Suggested First Prototype

The smallest slice that tells you whether the game is fun:
- One line level, first-person, WASD plus look and glance-back.
- One person ahead, one person behind with patience and the stage cues (animation and audio).
- Line movement in irregular bursts, the gap and grace window, the spacing zones, bump physics.
- One cutter event and the kick-out rule (out of the line).
- Reach the counter, one clerk line, result screen.
- Optional HUD toggle for debugging patience values.

If standing in that line is tense and readable without the HUD, the core works; build the room mode second, on the same systems.
