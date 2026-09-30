# Project Guidelines

## Project Intent

- This is a Windows 10/11 PC game inspired by early 1972 coin-operated electronic table-tennis games.
- Treat the local two-player mode as the historical-fidelity target. Single-player AI, keyboard input, window controls, daily plays, and Store purchases are modern additions.
- Do not use `Pong` or `Atari` as the product name, or copy their logos, fonts, recordings, screenshots, source code, or other branded assets. See `docs/game-reference.md` for sourced behavior and unverified values.

## Architecture

- `src/Arcade1972.Core` owns deterministic game rules and must remain independent of WinUI, Windows storage, and Microsoft Store APIs.
- `src/Arcade1972.App` owns WinUI 3 rendering, input, windowing, persistence adapters, and Store adapters.
- `tests/Arcade1972.Tests` exercises Core behavior and platform-independent service abstractions with fake clocks and fake gateways.
- Keep game constants in `Classic1972Rules`; do not hide physics values in XAML or code-behind.
- Preserve the 120 Hz fixed-step simulation. Rendering frequency, DPI, and window size must not alter game results.
- Add abstractions at platform boundaries such as time, storage, and Store commerce. Do not add abstractions around simple in-memory game state.

## Product Rules

- A match ends when a player reaches 11 points.
- Daily free plays use UTC date boundaries. A completed match consumes one play; exiting or crashing before completion does not.
- A match spanning UTC midnight belongs to its start date. Clock rollback must not grant another daily allowance.
- Free plays are consumed before paid plays.
- Paid balances must come from Store-managed consumables. Never emulate paid balance with local JSON, embedded secrets, or client-side HMAC.
- Display Store-provided product names and formatted prices; do not hard-code `NT$30` or `NT$100` in runtime UI.
- Keep purchase and fulfillment operations idempotent and retryable with persisted tracking IDs.

## Windows UI

- Default to the resizable custom-chrome window. Full screen is an explicit player action, never the first-run default.
- Keep the game presentation black and white, geometric, and restrained. Do not introduce standard business-app layouts or decorative cards into the playfield.
- Custom title-bar controls must have non-client passthrough regions so they remain clickable.
- Pause and clear held input when an overlay opens, focus is lost, the window is minimized, or a controller disconnects. Resume only when the previous state was actively playing.
- Preserve keyboard navigation and visible focus states for interactive controls.

## Code Style

- Use nullable reference types and existing C# naming/style conventions.
- Prefer immutable records and pure state transitions in Core.
- Keep code-behind focused on UI orchestration; move reusable rules and state machines into Core.
- Add comments only for non-obvious constraints or platform behavior.
- Do not introduce SharpDX. Prefer supported Windows App SDK and Win2D APIs when a custom renderer is needed.

## Build and Test

Use PowerShell from the repository root:

```powershell
dotnet restore pong-in-1972.sln
dotnet build pong-in-1972.sln -c Debug -p:Platform=x64
dotnet test tests/Arcade1972.Tests/Arcade1972.Tests.csproj -c Debug
dotnet build pong-in-1972.sln -c Release -p:Platform=x64
```

- After the first substantive edit, run the narrowest relevant test or App build before making adjacent changes.
- Add regression tests for rule, quota, entitlement, AI, or transaction-state changes.
- For WinUI interaction changes, verify build plus the affected behavior through UI Automation or a manual launch when automation is unavailable.
- Before finishing, run `get_errors` or the available editor diagnostics for touched C#/XAML files.

## Task and Git Workflow

- `todo.md` is the execution queue. Work on one item from `進行中` at a time unless the user reprioritizes it.
- When an item is complete, run its validation, move it to `已完成`, remove stale or duplicate entries, and commit code, tests, docs, and `todo.md` together.
- Before finishing an item, decide whether it produced a non-obvious, verified, reusable lesson. If so, update `docs/dev-knowledge.md` in the same commit. Do not add routine implementation notes or unverified guesses.
- Keep `docs/dev-knowledge.md` curated: revise or remove entries when APIs, tooling, or project constraints make them obsolete.
- Keep commits focused and use Conventional Commit-style messages such as `feat:`, `fix:`, `test:`, or `docs:`.
- Do not commit `bin/`, `obj/`, `.vs/`, AppPackages, certificates, package caches, or secrets.
- Do not amend, rebase, force-push, or push unless the user explicitly asks. Never revert unrelated user changes.

## Current Constraints

- The current App is an unpackaged WinUI 3 development build.
- Packaged MSIX and real Store integration require the Visual Studio Windows App SDK, MSIX Packaging, and Windows SDK workloads plus Partner Center identity and products.
- Do not claim Store purchasing, gift-card validation, or MSIX certification until the corresponding `todo.md` item and verification are complete.