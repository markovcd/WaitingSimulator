# 01. Room mode needs its moment-to-moment play decided before it is built

- **Severity:** Low
- **Status:** Open: waiting on the four decisions below
- **Found:** 2026-10-13, on `main` at `3067303`

## What is open

Room levels are the second level type (DESIGN §5, §7): you sit in a waiting room and listen and watch for your call. §7 is an outline and §16 lists "how calls are delivered and how listening is made skill-based" as open. Building it means picking the core loop, which is a call on feel, so this is a proposal rather than code.

## Proposed first room: a take-a-number office

The smallest room that tests the idea, built on what the line level already has.

- **Your number.** The level hands the player a ticket number (the dropped ticket's ticket, now with a number on it). Checking it takes a look down at your hand, a hold like glance back.
- **Calls.** Every 8 to 20 s the clerk calls a number: a chime, the number on a board above the counter for a few seconds, and a spoken call. Most calls are other people's; each NPC whose number is called gets up and walks to the counter. Yours comes after a seeded number of calls.
- **Missing it.** After your number is called you have a window (about 10 s) to reach the counter. Miss it and you are skipped: the result is "SKIPPED", a failed level like a kick-out, or in campaign, a new number at the back.
- **Making listening a skill.** Calls are not always clean: some are mumbled (the voice only, board off), some only on the board (voice drowned by the TV), and the board shows numbers out of order now and then (a different counter's queue). On hard you get only those cues; easy shows "CALLED: 47 · YOURS: 52".
- **Patience reused.** Neighbors keep the patience component; only the triggers change (§8): sitting next to someone, putting your bag on the seat beside them, and small talk from the seat beside you, which already exists (`SmallTalk`), drain or recover it. A neighbor whose patience runs out reports you, which costs you your number. Distractions carry over unchanged: the dropped ticket now also hides your number until you pick it up.

## What reuses and what is new

- **Reused as is:** `Sim`, patience, traits and stages, faces and cues, the HUD by difficulty, the result screen, `SmallTalk`, `Tickets`, the runner and the replay format.
- **New in the sim:** seats as level data, sitting as a step, a call schedule drawn from the seed (`NumberCalled` events), the player's number and the skip window. Agents walk to the counter on their call through the existing `Exiting` walk.
- **New in the level format:** a level `type` ("line" or "room") and `seats`; both additions an old reader can ignore, so the level format stays at version 1 (saved-data.md).
- **New in the client:** a board above the counter, a chime and a call voice (placeholder tones until the asset pipeline), chairs.

## Decisions for Arek

1. **How the call reaches you:** voice and board together, with some calls voice-only or board-only (recommended), or one channel only.
2. **Missing your call:** skipped and the level fails (recommended for the prototype), or a new number at the back.
3. **What the neighbors care about:** seat distance and the bag (recommended), or noise from your phone first.
4. **Order:** a pure room first (recommended), or the hybrid "queue to take a number, then wait" from §5.

## Suggested build order

Each step is a commit with its tests, behind the level type so the post office is untouched: seats and sitting; the call schedule and `NumberCalled`; your number and the skip; the board, chime and voice; neighbors' patience triggers; a `room` level in `data/levels/`; the runner's `play` and `sweep` for a room.
