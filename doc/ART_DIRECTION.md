# Trampoline — Art Direction

The visual reference for the Godot game, the same way `RULES.md` is the
rules reference. If a visual choice isn't covered here, follow the
pillars below and don't invent a new style.

Source material: the handmade original in `doc/real-game/` (photos taken
2026-08-03).

## The idea in one line

**Michel Cheenne's handmade board, sitting on a desk.** The game should
look like that object, not like a generic word-game app. Everything below
comes from the photos.

## Pillars

1. **Handmade, not glossy.** The frame is painted by hand: brushstrokes,
   wood showing through, a signature in the corner. Keep the imperfection
   in textures, but keep the UI shapes clean.
2. **The tile is the hero.** Two faces, one big letter, one small corner
   letter. Every other element on screen gets less contrast than the tiles.
3. **Bounce.** "Un jeu pour faire rebondir les mots": movement is where
   the game's name shows. Tiles land with a small squash-and-bounce, flip
   like real tokens, and finished words hop. Static screens stay calm.

## What the real game looks like (the reference)

| Element | Real object | Current Godot build | Direction |
|---|---|---|---|
| Yellow face | Pale butter-lime, matte | Saturated egg-yolk yellow, rounded | Paler, greener, matte |
| Green face | Muted sage/olive | (green) | Sage, clearly darker than yellow |
| Main letter | Slate-charcoal geometric sans (Century Gothic-like) | Baloo 2 ExtraBold (round, bubbly) | Geometric sans, semibold |
| Corner letter | Flush **square** in the top-right, filled with the *other* face's colour, small letter in ink | Floating rounded green "badge" overlapping the tile edge, like a notification count | Flush square inside the tile, no border, no overlap |
| Blank other face | Small **dark** square | `-` character | Dark square, no character |
| Tile shape | Near-square, barely rounded corners, tight to neighbours | Rounded, with wide gaps | Corner radius ≈ 6% of side, gap ≈ 4% |
| Board bed | Dark tray: tiles packed tight, dark gaps between them | Light-grey rounded boxes on green | Dark recessed slots, so tiles read as "in" the board |
| Frame | Hand-painted mint-green over wood, rust/brown wood showing, bevelled corners, "M. Cheenne" signature | Green wood texture, faint | Stronger brush/wear texture, bevel, signature bottom-right |
| Surroundings | Glossy yellow card folder with an orange satin ribbon bow; dark felt desk mat | Flat beige parchment | See "Screens" |

## Palette

The photos have an indoor colour cast. These values are corrected for
screen and become the only colours the game uses. Put them in one place
(`theme.tres` plus one `palette.gd` constants script) and don't hard-code
colours anywhere else.

| Token | Hex | Use |
|---|---|---|
| `tile_yellow` | `#EAE5A0` | Yellow face |
| `tile_green` | `#9CB46B` | Green face |
| `ink` | `#363A42` | Letters on both faces, primary text on light surfaces |
| `tile_edge` | `#C9B98A` | 1–2 px bottom edge / cork thickness under a tile |
| `tray` | `#2E2B22` | Board bed and empty slots (slots 6% lighter: `#3A3629`) |
| `frame_paint` | `#5FA373` | Painted frame, secondary buttons |
| `frame_shade` | `#3C7F5C` | Frame bevel shadow, pressed state |
| `frame_wood` | `#9A6B3F` | Wood showing through the paint (texture only) |
| `folder_yellow` | `#E6CF3A` | Start-screen background (the folder) |
| `ribbon` | `#E4733A` | The **single** primary action per screen ("Fin du tour", "Commencer") |
| `felt` | `#26282A` | In-game background (desk mat) |
| `cream_text` | `#F3EFD8` | Text on `felt` / `frame_paint` |
| `error` | `#C8463C` | Broken turn / invalid word only |
| `valid` | `#F6F1C0` | Short glow on a newly validated word |

Rules:
- `ribbon` orange appears **once** per screen. If two things are orange,
  one of them is wrong.
- `error` red only for rule violations, never for decoration.
- The yellow/green of the tiles never appears in UI chrome, so a tile is
  never confused with a button.

## Typography

- **Tiles and UI**: a geometric sans, close to the real tiles. Use **Jost**
  (OFL, Futura-like, Google Fonts): SemiBold for tile letters, Medium for
  buttons, Regular for body. It replaces Baloo 2, which is too rounded and
  cartoonish next to the original.
- **Signature accent**: a handwritten face (e.g. **Caveat**, OFL) used only
  for "M. Cheenne" on the frame and on the start screen, and for the
  tagline "Un jeu pour faire rebondir les mots". Nowhere else.
- Tile letter height ≈ 55% of the tile side; corner letter ≈ 22%, inside a
  square that is ≈ 30% of the side.
- French UI text: sentence case, a space before `:` `?` `!` (already done,
  keep it).

## Screens

### Start screen = the closed folder
- Full `folder_yellow` background, a soft glossy highlight (one wide
  diagonal gradient, nothing more).
- An orange ribbon bow (`ribbon`) as the logo mark across the top third;
  the title "Trampoline" set large in Jost Bold `ink`, the tagline under it
  in Caveat.
- Player count and modes as cards / buttons in `frame_paint` with
  `cream_text`. Today the title is small and the buttons float in empty
  space: make the title about 3× bigger and group the choices into two
  clear blocks, "Partie" (1–4 joueurs) and "Seul" (Jeu libre, Mots croisés).
- Transition into the game: the folder slides away and shows the board
  (a 300–400 ms slide is enough).

### Game screen = the board on the desk
- Background `felt` (dark), so the green frame and pale tiles pop, the
  same as in the photos.
- Board: painted frame, `tray` bed, tiles packed tight.
- Header on the felt: scores in `cream_text`; the current player highlighted
  with an orange underline, not with "(à vous de jouer)" text.
- The hand sits below the board on a strip of the same dark tray (a
  "rack"), so the tiles look the same in the hand and on the board.
- "Fin du tour" is the one `ribbon` button.

### Crossword mode
- Clue list styled like a paper slip (cream card, Jost, `ink`), slightly
  rotated (≤ 1°), referring back to the paper booklet the puzzles came from.

## Motion

Keep it short (≤ 250 ms) and never block input.

| Event | Animation |
|---|---|
| Tile dropped on a cell | Scale 1.12 → 0.94 → 1.0 with ease-out-back (the "trampoline" bounce) |
| Tile flipped | Scale X 1 → 0, swap face, 0 → 1 (120 ms total); the corner square swaps too |
| Drag | Tile lifts: scale 1.08, soft drop shadow, slight tilt (±3°) |
| Valid word scored | Letters hop one after another, left to right (30 ms stagger), and `+N` floats up in `ribbon` |
| Broken turn | Offending tiles shake twice horizontally, `error` border (already in place, keep it) |
| Board complete | Every row hops top-to-bottom; the ribbon bow comes back |

Sound later, same idea: wooden "tok" on drop, card "flick" on flip.

## Assets to produce

1. `tile_face.png`: 9-slice matte paper face with a faint fibre texture,
   tinted at runtime (one texture for both faces).
2. `board_frame_paint.png`: replace the current tinted wood with a
   painted-over-wood texture (green brushstrokes, some brown showing),
   plus bevelled corners.
3. `tray.png`: subtle dark wood or cork for the bed and the rack.
4. `felt.png`: low-contrast tileable felt noise.
5. `ribbon_bow.svg`: logo mark and app icon base.
6. Fonts: `Jost` (variable or 3 weights) and `Caveat` in `assets/fonts/`,
   with their OFL licence files.

CC0 sources (OpenGameArt, ambientCG, Poly Haven) or procedural are fine,
as `assets/textures/README.md` already does; note where each one came from.

## Do / don't

- **Do** check a change against the photo of the full board: if a
  screenshot next to it looks like a different game, revise.
- **Do** test at 480×854 and on a wide window; tiles must stay square.
- **Don't** add gradients, glows or drop shadows to UI chrome; depth
  belongs to the tiles and the frame only.
- **Don't** use emoji or icon fonts in-game; the "☰" menu glyph should
  become a small drawn icon in `cream_text`.
