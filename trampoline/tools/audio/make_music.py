"""Build the candidate music loops into assets/audio/music/.

Two traditional French tunes (public domain: 18th century, anonymous) with
new chords and a swung second chorus, and two original tunes written for
Trampoline. All arranged "a la Brassens": nylon guitar pompe, upright bass,
a lead instrument. Run from the project root inside `nix develop .#audio`:

    python3 tools/audio/make_music.py
"""

import os

from arrange import CLARINET, NYLON_GUITAR, Pass, Tune, build_loop

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "assets", "audio", "music")

# --- Folk 1: "Au clair de la lune" (trad.), AABA in C. ----------------------
_LUNE_A = "C5:1 C5:1 C5:1 D5:1 | E5:2 D5:2 | C5:1 E5:1 D5:1 D5:1 | C5:4 |"
_LUNE_A_SWING = (
    "r:.5 C5:.5 C5:.5 C5:.5 D5:.5 D#5:.5 E5:1 | E5:1 r:.5 E5:.5 D5:1.5 C5:.5 |"
    " C5:.5 E5:1 D5:.5 D5:1 B4:.5 D5:.5 |"
)
AU_CLAIR_DE_LA_LUNE = Tune(
    name="folk_au_clair_de_la_lune",
    tempo=126,
    meter=4,
    chords=(
        "C | Am D7 | Dm7 G7 | C |"
        " C | Am D7 | Dm7 G7 | C |"
        " G E7 | A7 D7 | D7 | G G7 |"
        " C | Am D7 | Dm7 G7 | C G7"
    ),
    passes=[
        Pass(
            _LUNE_A + _LUNE_A
            + " D5:1 D5:1 D5:1 D5:1 | A4:2 A4:2 | D5:1 C5:1 B4:1 A4:1 | G4:4 |"
            " C5:1 C5:1 C5:1 D5:1 | E5:2 D5:2 | C5:1 E5:1 D5:1 D5:1 | C5:2 r:2",
            lead=NYLON_GUITAR, bass="two",
        ),
        Pass(
            _LUNE_A_SWING + " C5:3 r:1 |"
            + _LUNE_A_SWING + " C5:2 G4:.5 A4:.5 B4:1 |"
            " D5:.5 D5:.5 D5:.5 D5:1 r:.5 G#4:.5 B4:.5 | A4:1.5 C#5:.5 A4:1.5 r:.5 |"
            " D5:.5 C5:.5 B4:.5 A4:.5 F#4:1 A4:1 | G4:2 r:.5 G4:.5 A4:.5 B4:.5 |"
            + _LUNE_A_SWING + " C5:2 r:1 G4:1",
            lead=CLARINET, bass="walk", lead_velocity=80,
        ),
    ],
)

# --- Folk 2: "Ah ! vous dirai-je, maman" (trad.), written in C, played in F. --
_MAMAN_PLAIN = (
    "C5:1 C5:1 G5:1 G5:1 | A5:1 A5:1 G5:2 | F5:1 F5:1 E5:1 E5:1 | D5:1 D5:1 C5:2 |"
    " G5:1 G5:1 F5:1 F5:1 | E5:1 E5:1 D5:2 | G5:1 G5:1 F5:1 F5:1 | E5:1 E5:1 D5:2 |"
    " C5:1 C5:1 G5:1 G5:1 | A5:1 A5:1 G5:2 | F5:1 F5:1 E5:1 E5:1 | D5:1 D5:1 C5:2"
)
_MAMAN_OPEN = (
    "r:.5 C5:.5 C5:.5 G5:1 G5:.5 G#5:1 | A5:.5 A5:1 F5:.5 G5:2 |"
    " r:.5 F5:.5 F5:.5 F5:.5 E5:.5 E5:.5 C5:1 |"
)
AH_VOUS_DIRAI_JE_MAMAN = Tune(
    name="folk_ah_vous_dirai_je_maman",
    tempo=132,
    meter=4,
    transpose=-7,
    chords=(
        "C | F C | Dm7 Am7 | G7 C |"
        " C7 F | A7 D7 | C Dm7 | C G7 |"
        " C | F C | Dm7 Am7 | G7 C"
    ),
    passes=[
        Pass(_MAMAN_PLAIN, lead=NYLON_GUITAR, bass="two"),
        Pass(
            _MAMAN_OPEN + " D5:.5 D5:.5 B4:.5 D5:.5 C5:2 |"
            " G5:.5 G5:1 Bb5:.5 A5:1 F5:1 | E5:.5 E5:.5 C#5:.5 E5:.5 D5:1 C5:1 |"
            " G5:.5 G5:1 E5:.5 F5:1 A5:1 | G5:.5 E5:.5 E5:1 D5:1 r:1 |"
            + _MAMAN_OPEN + " D5:.5 E5:.5 D5:.5 B4:.5 C5:1 r:1",
            lead=CLARINET, bass="walk", lead_velocity=80,
        ),
    ],
)

# --- Original 1: "Le Rebond" -- bouncy, talkative eighths, in G. -----------
_REBOND = (
    "r:.5 D4:.5 G4:.5 G4:.5 G4:.5 A4:.5 B4:1 | B4:.5 A4:.5 G#4:.5 A4:.5 B4:1 G#4:1 |"
    " A4:.5 C5:.5 C5:.5 C5:.5 B4:.5 A4:.5 E5:1 | D5:1.5 C5:.5 A4:1 F#4:1 |"
    " r:.5 D4:.5 G4:.5 G4:.5 B4:.5 B4:.5 D5:.5 F5:.5 | E5:1 E5:.5 D5:.5 Eb5:1.5 C5:.5 |"
    " B4:1 D5:.5 B4:.5 G#4:1 B4:1 | A4:.5 B4:.5 C#5:.5 E5:.5 D5:.5 C5:.5 A4:1 |"
    " E5:.5 E5:.5 E5:.5 D5:.5 C5:1 G4:1 | B4:.5 B4:.5 B4:.5 A4:.5 G4:2 |"
    " r:.5 E4:.5 A4:.5 C#5:.5 E5:1 D5:.5 C#5:.5 | D5:1.5 A4:.5 F#4:1 r:1 |"
    " r:.5 B4:.5 B4:.5 C5:.5 D5:1 B4:1 | r:.5 G#4:.5 B4:.5 D5:.5 E5:1.5 D5:.5 |"
    " C#5:1 A4:1 A4:.5 B4:.5 C5:1 | B4:1.5 G4:.5 F#4:.5 G4:.5 A4:1"
)
LE_REBOND = Tune(
    name="original_le_rebond",
    tempo=126,
    meter=4,
    chords=(
        "G | E7 | Am | D7 | G G7 | C Cm6 | G E7 | A7 D7 |"
        " C | G | A7 | D7 | G | E7 | A7 D7 | G D7"
    ),
    passes=[
        Pass(_REBOND, lead=NYLON_GUITAR, bass="two"),
        Pass(_REBOND, lead=CLARINET, bass="walk", lead_velocity=78),
    ],
    seed=3,
)

# --- Original 2: "La Grille" -- a minor musette waltz, for thinking. -------
_GRILLE = (
    "A4:1 D5:1.5 C#5:.5 | D5:1 A4:1 F4:1 | E4:.5 F4:.5 G4:1 Bb4:1 | A4:3 |"
    " G4:1 C#5:1.5 E5:.5 | E5:1 D5:1 C#5:1 | F4:1 A4:1 D5:1 | D5:2 r:1 |"
    " F#4:1 A4:1 C5:1 | Bb4:1.5 A4:.5 G4:1 | E4:.5 G4:.5 Bb4:1 C5:1 | A4:2 C5:1 |"
    " D5:1 Bb4:1 G4:1 | F4:1 A4:1 D5:1 | D5:1 B4:1 G#4:1 | A4:1.5 G4:.5 E4:1"
)
LA_GRILLE = Tune(
    name="original_la_grille",
    tempo=150,
    meter=3,
    swing=0.5,  # a waltz is played straight
    chords="Dm | Dm | A7 | A7 | A7 | A7 | Dm | Dm | D7 | Gm | C7 | F | Gm | Dm | E7 | A7",
    passes=[
        Pass(_GRILLE, lead=NYLON_GUITAR, bass="two"),
        Pass(_GRILLE, lead=CLARINET, bass="two", lead_velocity=78),
    ],
    seed=4,
)

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for tune in (AU_CLAIR_DE_LA_LUNE, AH_VOUS_DIRAI_JE_MAMAN, LE_REBOND, LA_GRILLE):
        seconds = build_loop(tune, os.path.join(OUT, tune.name + ".ogg"))
        print(f"{tune.name}.ogg  {seconds:.1f} s loop")
