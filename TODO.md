# TODO

Work asked for and not yet started. Take an item off in the commit that lands it.

- Once the gate needs assets, the Gate workflow's checkout fetches LFS (`lfs: true`).
- The ripple: the line moves forward in bursts, starting at the front and travelling back with a variable delay, decided in the sim so the client and the runner share it. The runner's sweep has a stand-in (`SweepLine`: a lead stepping forward every few seconds, the person behind following); it moves into the sim and the client plays it, so gaps open on their own.
- Peek and gamble: step out of the lane to look down the line, and walk to a gap someone else left to cut in. The sim decides from positions, walking speed and the ripple whether you make it, with graduated patience costs; a miss closes the gap and loses the place. Builds on the spot-hold timer in DESIGN.md.
