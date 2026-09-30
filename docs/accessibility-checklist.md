# Accessibility Verification

## Automated checks

Verified on 2026-09-30 with the unpackaged WinUI 3 development build:

- Main menu visible interactive controls have non-empty AutomationProperties names.
- Settings overlay visible interactive controls have non-empty AutomationProperties names.
- Main menu and settings controls are keyboard-focusable.
- Settings can be opened from the title-bar button and closed with its close button.
- The hidden `InputSink` is excluded from Tab navigation while remaining available for programmatic focus.
- The pause, Store placeholder, and settings overlays expose named close/continue actions.

## Manual checks still required

These checks require a Windows desktop session with the OS features enabled:

- Enable Windows High Contrast and inspect main menu, pause, settings, and Store placeholder states.
- Start Narrator and verify that headings, buttons, quota status, pause actions, settings choices, and Store placeholder status are announced in a useful order.
- Navigate each overlay using only Tab, Shift+Tab, Enter, Space, and Escape.
- Confirm visible focus remains distinguishable in both normal and High Contrast modes.

The manual checks must be completed before claiming full accessibility acceptance in `todo.md` or a Store submission checklist.
