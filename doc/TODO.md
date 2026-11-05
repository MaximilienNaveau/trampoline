# Trampoline — TODO

What's left for the Godot game to be fully playable and look finished.
Agreed with Maximilien on 2026-11-03. Each item is meant to be one (or a
few) atomic commits; tick it off once merged. Rules decisions also go
into `RULES.md`, visual ones into `ART_DIRECTION.md`.

Order: the **Playable** section first (a full game must run from start to
end), then **Polish**.

## Playable

### 1. Passing and the end of the game
Decided (recorded in `RULES.md`, "End of the game"): unlimited passing,
nobody eliminated; one hour of actual play, the round in progress is
finished when it runs out; best score wins, then fewest tiles left in
hand, then a shared win.

- [x] `GameSession`: play time, the last round, ending by time, winners
      with the tie-break (tested, saved with the game).
- [x] Header clock: time left, orange in the last 5 minutes, "Dernier
      tour"; only runs while a turn is being played.
- [x] The screen between turns no longer assumes a phone: "Au tour de
      Alice" / "C'est moi !".
- [x] That screen recaps the scores: every player's score, what the
      last turn changed (in orange), and the time left.
- [ ] Maybe: end early when every player passes two rounds in a row
      (not decided).

### 2. Score feedback during the turn
- [x] A live "+N" next to the current player's score while they build
      their turn (positive or negative: green letters cost −5), updated on
      every placement, move, flip or take-back, with any bonus it would
      win ("bonus +50").
- [x] When the turn ends, the "+N" floats into the score, and bonuses
      (+10 line, +30/+50 full word, +30 board) pop up separately.
- [x] `GameSession`: a way to ask "what would this turn score right now"
      without ending it (tested).
- [x] Nothing can be changed from "Fin du tour" until the next player
      presses "C'est moi !" (clicks, touches and keys are blocked while
      the points are counted).
- [x] Fixed on the way: a bonus was lost again at the next turn (scores
      were recomputed from the words alone).

### 3. Android build
- [x] Android export preset (portrait, icon, package name), committed;
      fixed so it actually exports (no Gradle build, ETC2/ASTC textures,
      the dictionary included — it was missing from every export).
- [x] A reproducible APK build: `nix build .#android` from `trampoline/`
      (Godot export templates + Android SDK from nixpkgs, in the Nix
      sandbox), also used by the release workflow on tags. Signed with a
      committed debug key (`trampoline/keys/`).
- [ ] A release signing key, kept out of the repo (GitHub secrets), for
      a store release.
- [ ] Test on a real phone: touch drag and drop, flipping, the tray's
      sideways scroll, performance, the screen fitting with no scrolling.
- [x] Automatic save at the start of every turn (names included), and a
      "Reprendre la partie" button on the start card; the Save/Load
      buttons and their file dialogs are gone. Free play isn't saved.
- [x] The dictionary loads in ~50 ms instead of ~3 s: the word list ships
      pre-normalized and sorted (`data/dictionaries/fr-words.txt`, rebuilt
      by `tools/build_word_list.gd`, checked up to date by the tests).
- [x] Thinner page margins and board rim, for bigger tiles on a phone.

### 4. To think about further
- [x] Moving tiles (2015 rules, see `RULES.md` "Moving and swapping
      tiles"): any tile moves during a turn, alone or in a block picked
      with a dotted selection box; dropping a tile on another swaps it into
      the hand; the turn can't end with a cell emptied that held a letter;
      each turn earns the board's change in worth.
- [x] "Comment jouer" cards for swapping, the selection box,
      "Recommencer", and arranging the hand.
- [x] "Recommencer" in the header: the board and hand back as they were
      when the turn began.
- [x] Under the hand: "Trier" (A to Z), "Tout jaune", "Tout vert"; a hand
      tile can be dragged to another place in the hand.
- [ ] A hand over 14 tiles (tiles taken back off the board) overflows its
      two rows: decide (cap? third row? scroll?).
- [x] `tests/test_save_game.gd` failed now and then: it looked for given
      letters only in the undrawn pile, which the deal had sometimes
      emptied of them. It now looks in the hands too.

## Polish

### 5. The hand on a play mat
- [ ] The hand (and the "À rendre" tray) sits on the background, drawn as
      a **play mat** (felt, like the desk mat in `doc/real-game/`), not on
      the plain dark page.
- [ ] Hand, tray and dragged tiles drawn as 2.5D blocks with their cork
      sides (like the tiles off the board in the "Comment jouer" cards),
      matching the board.

### 6. In-game motion
From `ART_DIRECTION.md` → "Motion" (each ≤ 250 ms, never blocking input):
- [ ] Tile dropped on a cell: squash-and-bounce.
- [ ] Tile flipped: the turn-over animation (narrow, swap face, widen),
      as in the "Comment jouer" cards.
- [ ] Drag: the tile lifts (slightly bigger, soft shadow, slight tilt).
- [ ] Valid word scored: letters hop left to right, `+N` floats up.
- [ ] Broken turn: offending tiles shake (keep the red border).
- [ ] Board complete: rows hop top to bottom, the ribbon bow comes back.

### 7. Header
- [ ] Buttons in Jost (the theme still uses Baloo 2), per the art
      direction.
- [ ] "☰" replaced by a small drawn icon.
- [x] Scoreboard: the current player underlined in orange instead of
      "(à vous de jouer)".
- [ ] Scores easier to read at a glance (one chip per player?).

### 8. End of game
- [ ] A proper end screen instead of the text banner: final scores
      ranked, the winner highlighted, eliminated players marked.
- [ ] "Rejouer" (same players) and "Menu" buttons; going back to the
      menu closes the lid over the box.

### 9. Sound
- [ ] Sound effects: pick up a tile, drop it (wooden "tok"), flip it
      (card "flick"). Free-licence sources noted in an assets README.
- [ ] Background music, quiet, looping; on/off in the ☰ menu (and sound
      effects on/off too).
