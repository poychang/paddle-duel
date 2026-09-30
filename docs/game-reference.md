# 1972 gameplay reference

This document separates sourced behavior from provisional implementation values. The project must not describe an unverified value as an exact reproduction of the 1972 hardware.

## Sourced behavior

| Behavior | Implementation | Source |
| --- | --- | --- |
| Two vertically controlled paddles return a ball | Implemented | [Wikipedia: Pong gameplay](https://en.wikipedia.org/wiki/Pong#Gameplay) |
| First player to 11 points wins | Implemented | [Wikipedia: Pong gameplay](https://en.wikipedia.org/wiki/Pong#Gameplay) |
| Paddle is divided into eight return-angle segments | Implemented | [Wikipedia: development history](https://en.wikipedia.org/wiki/Pong#Development_and_history) |
| Ball accelerates during a rally and resets after a miss | Implemented | [Wikipedia: development history](https://en.wikipedia.org/wiki/Pong#Development_and_history) |
| Paddles cannot reach the very top of the screen | Implemented as a configurable dead zone | [Wikipedia: development history](https://en.wikipedia.org/wiki/Pong#Development_and_history) |
| Original presentation used a black-and-white television and generated simple tones | Visual prototype implemented; original audio is not copied | [Wikipedia: development history](https://en.wikipedia.org/wiki/Pong#Development_and_history) |

## Values requiring measurement

The defaults in `Classic1972Rules` are playable engineering values, not claims about exact arcade hardware timing:

- Logical field dimensions and object sizes.
- Initial horizontal and vertical ball velocity.
- Per-hit acceleration and maximum velocity.
- Exact angle associated with each of the eight paddle segments.
- Paddle speed and top dead-zone height.
- Tone frequency and duration.

Before describing the game as behaviorally exact, capture a reliable original-hardware reference and record frame-by-frame measurements here. Keep all accepted values centralized in `Classic1972Rules` and add a regression test for each change.

## Provisional golden baseline

`Classic1972GoldenRegressionTests` locks the current deterministic engineering baseline: initial state, paddle bounds, wall bounce, and point reset. These tests protect accidental code changes; they are not measurements of original hardware. When a credible reference is measured, update the rules, this document, and the affected golden expectations together.

## Product boundary

The local two-player mode is the historical-fidelity target. Single-player AI, keyboard input, window controls, accessibility features, daily plays, and Microsoft Store purchases are modern product additions.