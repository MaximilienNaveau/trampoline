# Trampoline (Flutter rewrite) — vibe-coding ground rules

Flutter/Dart rewrite of the "Trampoline" word game (currently a Unity/C# app
in `../trampoline`). The user knows C++, C, Python, CMake and Nix well, but
has never written Flutter or Dart. These rules apply to everything under
this `trampoline-flutter/` directory and override the general defaults for
comments and commit granularity.

## 1. Minimalistic git commits
- One logical change per commit — don't bundle unrelated fixes/features.
- Short, imperative commit messages describing *what* changed.
- Never commit unless explicitly asked to.

## 2. Explain every line of Dart/Flutter
- The user doesn't know the language yet, so every non-trivial line needs a
  comment saying what it does.
- Where there's a natural match, tie it to something familiar: e.g. a Dart
  `class` with a constructor ~ a C++ class with a ctor; `async`/`await` ~
  Python's `asyncio`; a Flutter `Widget` build method ~ a pure function that
  returns a UI tree (no direct C++/Python analogue — say so).
- First time a new Dart/Flutter concept appears (null safety `?`/`!`,
  streams, widgets, `pubspec.yaml`, hot reload, etc.), explain it briefly
  even if it recurs later without a fresh comment.
- Trivial repeated boilerplate (e.g. every `);` closing a widget tree) does
  not need its own comment once the pattern has been explained.

## 3. Minimalistic code
- Simplest widget/function that does the job — no speculative abstractions,
  no unused scaffolding, no "just in case" flexibility.
- Don't add a package/dependency unless it's clearly needed for the task at
  hand.
