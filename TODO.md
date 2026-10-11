# TODO

Work asked for and not yet started. Take an item off in the commit that lands it.

- Godot client skeleton: first-person camera, input actions, the sim driver with interpolation, placeholder capsules for the person ahead, the person behind and the clerk. Once the gate needs assets, the Gate workflow's checkout fetches LFS (`lfs: true`).
- Wire sim events to placeholder audio and animation; debug overlay.
- Peek and gamble: step out of the lane to look down the line, and walk to a gap someone else left to cut in. The sim decides from positions, walking speed and the ripple whether you make it, with graduated patience costs; a miss closes the gap and loses the place. Builds on the spot-hold timer in DESIGN.md.
