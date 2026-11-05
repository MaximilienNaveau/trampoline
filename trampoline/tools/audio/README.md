# Music and sound effects generators

Everything in `assets/audio/` is generated from source by these scripts, and
isn't committed (only Godot's `.import` files are). From the `trampoline/`
folder:

```sh
nix build .#audio   # the game's music and sound effects, in the Nix sandbox
nix run .#audio     # the same, copied into assets/audio/ for the Godot editor
```

The exports and the tests (`nix build .#linux`, `nix flake check`, CI) use
`.#audio` on their own. To change a sound and listen to it straight away:

```sh
nix develop .#audio
python3 tools/audio/make_music.py   # -> assets/audio/music/*.ogg (seamless loops)
python3 tools/audio/make_sfx.py     # -> assets/audio/sfx/*.wav
```

Two runs don't give bit-identical files (fluidsynth), but they sound the same.

- `arrange.py`: turns a lead sheet (melody + chord symbols) into a small band
  in the spirit of Georges Brassens's accompaniments: a nylon guitar playing
  the "pompe" (soft strum on 1 and 3, short accented strum on 2 and 4, a swung
  upstroke every other bar), an upright bass in two-feel or walking, and a
  lead instrument (guitar, then clarinet on the second chorus). It writes
  MIDI, renders it with fluidsynth, and loops it seamlessly by rendering the
  tune three times and keeping the middle one.
- `make_music.py`: the tunes. Edit the melody strings (`note:beats`, `r` for
  a rest, bars separated by `|`) or the chord strings to change them.
- `make_sfx.py`: synthesised physical sounds (trampoline, wooden tiles) and
  short musical cues played by the same band.

## Where the music comes from

- `folk_au_clair_de_la_lune`, `folk_ah_vous_dirai_je_maman`: traditional
  French songs from the 18th century, anonymous, in the public domain. The
  chords, the second-chorus variations and the arrangement are new.
- `original_le_rebond`, `original_la_grille`: original melodies written for
  Trampoline.
- Nothing is taken from Brassens's own songs, which stay under copyright
  until the end of 2051 (he died in 1981). Only the general style is borrowed: the
  guitar/bass instrumentation and the swing feel, which copyright doesn't
  cover.

## Licences of the tools

- Sounds are rendered with the GeneralUser GS soundfont by S. Christian
  Collins (GeneralUser GS License v2.0, free and redistributable; it allows
  using the soundfont to make music, commercial or not).
- fluidsynth (LGPL), sox (GPL/LGPL), ffmpeg (LGPL/GPL), mido (MIT),
  numpy/scipy (BSD) are build tools only: none of their code ships in the game.
