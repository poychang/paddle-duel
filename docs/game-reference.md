# 1972 gameplay reference

This document records the sourced gameplay behavior and the current deterministic implementation baseline.

## Sourced behavior

| Behavior | Implementation | Source |
| --- | --- | --- |
| Two vertically controlled paddles return a ball | Implemented | [Wikipedia: Pong gameplay](https://en.wikipedia.org/wiki/Pong#Gameplay) |
| First player to 11 points wins | Implemented | [Wikipedia: Pong gameplay](https://en.wikipedia.org/wiki/Pong#Gameplay) |
| Paddle is divided into eight return-angle segments | Implemented | [Wikipedia: development history](https://en.wikipedia.org/wiki/Pong#Development_and_history) |
| Ball accelerates during a rally and resets after a miss | Implemented | [Wikipedia: development history](https://en.wikipedia.org/wiki/Pong#Development_and_history) |
| Paddles cannot reach the very top of the screen | Implemented as a configurable dead zone | [Wikipedia: development history](https://en.wikipedia.org/wiki/Pong#Development_and_history) |
| Original presentation used a black-and-white television and generated simple tones | Visual prototype implemented; original audio is not copied | [Wikipedia: development history](https://en.wikipedia.org/wiki/Pong#Development_and_history) |

## Golden baseline

`Classic1972GoldenRegressionTests` locks the current deterministic baseline: initial state, paddle bounds, wall bounce, and point reset. These tests protect accidental code changes in the current version.

## Product boundary

The local two-player mode is the historical-fidelity target. Single-player AI, keyboard input, window controls, daily plays, and Microsoft Store purchases are modern product additions.