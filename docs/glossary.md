# Glossary

One word for each thing, and the same word everywhere a person reads: the UI,
the docs, ADRs, comments, test names and commit messages.

- **Say** is the word. Use it in prose, whatever the code calls the thing.
- **Code** is the name in source. Where it differs from **Say**, it differs
  because renaming it would break something persisted or public (a file
  format, an API). A comment on it still uses **Say**.
- **Not** lists words that must not stand for the thing, and why where it
  helps.

Prefer the domain's words over the framework's. Code names are filled in as the code that introduces them lands.

## The game

| Say | Means | Code | Not |
|---|---|---|---|
| **level** | One playable scenario: a line, a room or a hybrid. Built once, used by both modes. | `Level` | map, stage (a **stage** is a patience band) |
| **mode** | The rules wrapper around a level: campaign or arcade. | | game type |
| **campaign** | The mode where a day of errands runs in order and state carries over. | | story mode |
| **arcade** | The mode where any level is played alone, scored, with no carry-over. | | |
| **line** | The queue of agents a line level is about. | | queue (in prose) |
| **newcomer** | An agent that arrives at the entrance mid-level and joins the back of the line. | `Arrived`, `LineStep.Arriving` | latecomer, spawn |
| **entrance** | Where a level's newcomers arrive. | `Level.Entrance` | door, spawn point |
| **exit** | Where people done with the line walk out of the room. | `Level.Exit`, `LineStep.Gone` | door, despawn |
| **lane** | The volume the line occupies; being in it is what makes you part of the line. | `Lane` | |
| **lane marking** | The lane painted on the floor, tinted by where the player stands against the line. | `LaneMarking` | line area, queue zone |
| **room** | A waiting room where you listen and watch for your call. | | |
| **call** | The moment your number or name is announced in a room. | | |
| **agent** | Anything that stands in a line or sits in a room and acts each tick: the player or an NPC. | `AgentId` | actor, entity (Godot words) |
| **player** | The agent driven by a person's input. | | |
| **the person ahead** / **the person behind** | The agents directly in front of and behind the player in the line. | | |
| **gap** | The distance that opens when the person ahead steps forward. | | |
| **grace window** | How long a gap can stay open without penalty. | `GraceWindowTicks` | grace period |
| **zone** | One of the spacing bands to the person ahead: bump, tight, loose, gap. | `Zone` | |
| **bump** | Walking into the person ahead. | | collision (in prose) |
| **ripple** | Line movement traveling back from the front with a delay. | `RippleTuning`, `LineStep` | wave |
| **spot hold** | How long the line keeps an agent's place after it steps out of the lane. | `SpotHoldTicks` | reservation, save |
| **peek** | Stepping out of the lane to look down the line, on a spot hold. | | lean out |
| **cut in** | Stepping into the line ahead of someone, anywhere but your own held place or the back; made only into a gap left open past its grace window. What the player does; an NPC doing it unprompted is a **cutter**. | `CutIn` | skip, jump the queue |
| **passed** | Someone a cut in gets ahead of who stood ahead of the agent's place. Each takes a hit, and the person cut in front of one more per person passed. | `StepOutTuning.PassedHit` | skipped (skip is ruled out with *cut in*) |
| **counter** | Where the clerk serves the front of the line: the front end of the lane. | | desk |
| **cutter** | An NPC that slides into an open gap ahead of the player: placed beside the line by the level, or a newcomer walking up it looking for one. | `CutterEntered` | line-cutter, skipper |
| **patience** | An NPC's 0–100 tolerance of the player. Hidden from the player except on easy. | `Patience` | mood, anger |
| **stage** | A band of patience with its own visible behavior, from calm through restless, annoyed and confronting to fed up; changes with hysteresis. | `Stage` | level, phase |
| **cue** | A visible or audible behavior that reveals a stage or an event. | `Cue` | tell, signal |
| **trait** | An NPC's personality as data: patient, anxious, aggressive, passive-aggressive, oblivious. | `Trait` | personality type, class |
| **kick-out** | What happens when patience reaches 0: pushed past, staff called, or the crowd turns. | `KickedOut` | game over |
| **distraction** | Anything that competes with watching the line or listening for the call. | | |
| **tally** | The count of each mistake an agent made on its way to the counter; the clerk names the worst. | `Tally`, `Mistake` | score, record |
| **result screen** | What shows once the player reaches the counter: the clerk's line, the time in line and the tally. | `ResultScreen` | game over screen |
| **clerk** | The person at the counter who ends a level with one line. | | NPC at the desk |
| **twist** | A seeded alternative outcome at the counter ("Wrong queue."). | | |

## The machinery

| Say | Means | Code | Not |
|---|---|---|---|
| **sim** | The engine-free library that owns every game rule. | `WaitingSim` | engine, backend, model |
| **client** | The Godot project that shows the sim and turns input into commands. | | frontend, view |
| **tick** | One fixed sim step (1/60 s). | `Tick` | frame (a frame is the client's) |
| **command** | One agent's input for one tick, validated by the sim. | `Command` | action, input event |
| **event** | Something the sim reports happened, for the client to show. | `SimEvent` | signal (a Godot word) |
| **snapshot** | The read-only sim state the client reads each frame. | `Snapshot` | |
| **driver** | The client part that steps the sim from frame times and blends what it shows between the last two snapshots. | `Driver` | game loop |
| **seed** | The number the sim's RNG starts from. | | |
| **replay** | A seed and a command stream, with state hashes. | `Replay` | recording, demo |
| **golden replay** | A replay kept in the tests whose hashes must not change. | | |
| **runner** | The command-line program that plays levels headlessly. | `WaitingSim.Runner` | |
| **tuning** | The data file of knobs the sim reads. | `Tuning` | config, settings (those are the player's) |
| **figure** | The crude humanoid an agent or the clerk is drawn as. | `Figure` | model, mesh, avatar |
| **debug overlay** | The on-screen readout of sim values for tuning: tick, seed, zones, grace, patience, stages. | `Overlay` | HUD (the HUD is the player's, set by difficulty) |
| **HUD** | The player's on-screen readout of the line, whose widgets the difficulty picks. | `Hud` | overlay (that is the debug overlay) |
| **difficulty** | How much the HUD tells the player: easy, normal or hard. The sim plays the same on every one. | `Difficulty` | mode, level (a level is a place) |
| **tuning reload** | Re-reading the data files in game (F5) and restarting the level on them. | `TuningReload` | |
| **settings** | The player's preferences: controls, volume, difficulty. | | options, config |
