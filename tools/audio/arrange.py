"""Turn a lead sheet (melody + chord symbols) into a small acoustic band,
"a la Brassens": nylon guitar playing the pompe (soft bass-beat strum, short
accented strum on the backbeat), upright bass in two-feel or walking, and a
lead instrument carrying the tune. Writes MIDI with mido, renders it with
fluidsynth and a free General MIDI soundfont, then cuts a seamless loop.

Run from `nix develop .#audio` (needs $SF2_GENERALUSER, fluidsynth, sox, ffmpeg).
"""

import os
import random
import subprocess
import tempfile
from dataclasses import dataclass

import mido

SAMPLE_RATE = 44100
TICKS_PER_BEAT = 960

# General MIDI programs (0-based, as mido counts them).
NYLON_GUITAR = 24
ACOUSTIC_BASS = 32
CLARINET = 71
ACCORDION = 21
HARMONICA = 22
WHISTLE = 78

_PITCH_CLASSES = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}

# Chord qualities -> intervals above the root, in semitones.
_QUALITIES = {
    "": (0, 4, 7),
    "m": (0, 3, 7),
    "7": (0, 4, 7, 10),
    "m7": (0, 3, 7, 10),
    "maj7": (0, 4, 7, 11),
    "6": (0, 4, 7, 9),
    "m6": (0, 3, 7, 9),
    "dim7": (0, 3, 6, 9),
}


def _pitch_class(name):
    """'C' -> 0, 'F#' -> 6, 'Bb' -> 10."""
    pc = _PITCH_CLASSES[name[0]]
    for accidental in name[1:]:
        pc += {"#": 1, "b": -1}[accidental]
    return pc % 12


def note_number(name):
    """'C5' -> 72 (MIDI), sharps '#' and flats 'b' allowed: 'F#4', 'Bb4'."""
    return 12 * (int(name[-1]) + 1) + _PITCH_CLASSES[name[0]] + sum(
        {"#": 1, "b": -1}[a] for a in name[1:-1]
    )


def parse_chord(symbol):
    """'A7' -> (9, (0, 4, 7, 10)); 'Bbm6' -> (10, (0, 3, 7, 9))."""
    root_len = 2 if len(symbol) > 1 and symbol[1] in "#b" else 1
    return _pitch_class(symbol[:root_len]), _QUALITIES[symbol[root_len:]]


def parse_bars(text):
    """Split a '|'-separated string into one list of tokens per bar."""
    return [bar.split() for bar in text.split("|")]


def parse_melody(text):
    """'C5:1 r:.5 D5:.5 | E5:2 D5:2' -> per-bar lists of (midi or None, beats)."""
    bars = []
    for tokens in parse_bars(text):
        notes = []
        for token in tokens:
            name, beats = token.split(":")
            notes.append((None if name == "r" else note_number(name), float(beats)))
        bars.append(notes)
    return bars


@dataclass
class Pass:
    """One time through the tune: its melody, who plays it, how the bass walks."""

    melody: str
    lead: int = NYLON_GUITAR
    bass: str = "two"  # "two" (half notes) or "walk" (quarter notes)
    lead_velocity: int = 88


@dataclass
class Tune:
    name: str
    tempo: float  # quarter notes per minute
    meter: int  # beats per bar: 4 or 3
    chords: str  # one '|'-separated bar per melody bar, chords split the bar evenly
    passes: list
    transpose: int = 0
    swing: float = 0.62  # where the off-beat eighth lands (0.5 = straight)
    seed: int = 1


@dataclass
class Note:
    beat: float
    length: float
    pitch: int
    velocity: int
    channel: int
    delay_ms: float = 0.0  # added after swing: strum roll + human looseness


def _swing(beat, ratio):
    """Push off-beat eighths late: maps [0, .5] -> [0, ratio], [.5, 1] -> [ratio, 1]."""
    whole = int(beat // 1)
    frac = beat - whole
    if frac <= 0.5:
        return whole + frac / 0.5 * ratio
    return whole + ratio + (frac - 0.5) / 0.5 * (1 - ratio)


def _voicing(root, intervals, low=52, high=67):
    """Guitar voicing: each chord tone placed in its octave nearest the middle."""
    centre = (low + high) / 2
    pitches = []
    for interval in intervals:
        pc = (root + interval) % 12
        candidates = [p for p in range(low, high + 1) if p % 12 == pc]
        pitches.append(min(candidates, key=lambda p: abs(p - centre)))
    return sorted(set(pitches))


def _bass_note(pc, low=36):
    """The chord root in the bass register, C2..B2."""
    return low + (pc - low) % 12


def _chord_segments(tune):
    """[(start_beat, length, root_pc, intervals)] for one pass of the tune."""
    segments = []
    for bar_index, symbols in enumerate(parse_bars(tune.chords)):
        length = tune.meter / len(symbols)
        for i, symbol in enumerate(symbols):
            root, intervals = parse_chord(symbol)
            segments.append(
                (bar_index * tune.meter + i * length, length, (root + tune.transpose) % 12, intervals)
            )
    return segments


def _comp(segments, meter, rng):
    """Rhythm guitar on channel 1: the pompe."""
    notes = []
    for start, length, root, intervals in segments:
        chord = _voicing(root, intervals)
        beat = start
        while beat < start + length - 1e-6:
            in_bar = beat % meter
            if meter == 4:
                # Downstroke: soft on 1 and 3, short and accented on 2 and 4.
                accent = in_bar in (1, 3)
                velocity = 72 if accent else 42
                hold = 0.22 if accent else 0.35
            else:  # waltz: bass alone on 1, strums on 2 and 3
                if in_bar == 0:
                    beat += 1
                    continue
                velocity, hold = 62, 0.4
            for i, pitch in enumerate(chord):
                notes.append(Note(beat, hold, pitch, velocity + rng.randint(-5, 5), 1, i * 11))
            # Swung upstroke on the "and" of 4, every other bar: the bounce.
            if meter == 4 and in_bar == 3 and int(beat // meter) % 2 == 1:
                for i, pitch in enumerate(reversed(chord[-3:])):
                    notes.append(Note(beat + 0.5, 0.15, pitch, 34, 1, i * 9))
            beat += 1
    return notes


def _bass(segments, meter, style):
    """Upright bass on channel 2."""
    notes = []
    for index, (start, length, root, intervals) in enumerate(segments):
        next_root = segments[(index + 1) % len(segments)][2]
        low = _bass_note(root)
        fifth = low + 7 if low + 7 <= 50 else low - 5
        third = low + intervals[1]
        if meter == 3:
            # One note per bar, alternating root and fifth bar by bar.
            pitch = fifth if int(start // meter) % 2 else low
            notes.append(Note(start, 2.6, pitch, 92, 2))
        elif style == "two":
            for k, beat in enumerate(_frange(start, start + length, 2)):
                notes.append(Note(beat, 1.8, low if k % 2 == 0 else fifth, 92, 2))
        else:  # walk: chord tones, then a chromatic step into the next root
            target = _bass_note(next_root)
            beats = list(_frange(start, start + length, 1))
            line = [low, third, fifth, fifth + 2][: len(beats)]
            if len(beats) > 1:
                line[-1] = target - 1 if target > line[-2] else target + 1
            for beat, pitch in zip(beats, line):
                notes.append(Note(beat, 0.9, pitch, 88, 2))
    return notes


def _frange(start, stop, step):
    beat = start
    while beat < stop - 1e-6:
        yield beat
        beat += step


def arrange(tune):
    """All the notes of every pass of the tune, and its length in beats."""
    rng = random.Random(tune.seed)
    segments = _chord_segments(tune)
    pass_beats = len(parse_bars(tune.chords)) * tune.meter
    notes = []
    for number, a_pass in enumerate(tune.passes):
        offset = number * pass_beats
        bars = parse_melody(a_pass.melody)
        assert len(bars) == len(parse_bars(tune.chords)), f"{tune.name}: bar count"
        # The lead gets its own channel per instrument so programs never clash.
        channel = 0 if a_pass.lead == NYLON_GUITAR else 3
        for bar_index, bar in enumerate(bars):
            total = sum(beats for _, beats in bar)
            assert abs(total - tune.meter) < 1e-6, f"{tune.name} pass {number} bar {bar_index + 1}: {total} beats"
            beat = offset + bar_index * tune.meter
            for pitch, beats in bar:
                if pitch is not None:
                    notes.append(
                        Note(beat, beats * 0.92, pitch + tune.transpose,
                             a_pass.lead_velocity + rng.randint(-6, 6), channel)
                    )
                beat += beats
        shifted = [(s + offset, l, r, i) for s, l, r, i in segments]
        notes += _comp(shifted, tune.meter, rng)
        notes += _bass(shifted, tune.meter, a_pass.bass)
    for note in notes:
        note.delay_ms += rng.uniform(0, 9)  # never early, so loops cut cleanly
    return notes, pass_beats * len(tune.passes)


# Per channel: (program, pan 0..127, reverb send 0..127).
def _channel_setup(leads):
    setup = {1: (NYLON_GUITAR, 48, 40), 2: (ACOUSTIC_BASS, 64, 25)}
    for lead in leads:
        setup[0 if lead == NYLON_GUITAR else 3] = (lead, 76, 55)
    return setup


def write_midi(notes, tempo, swing, leads, path, repeats=1, loop_beats=0, tail_beats=0):
    """Write the notes (repeated `repeats` times, `loop_beats` apart) as MIDI."""
    seconds_per_beat = 60 / tempo
    events = []  # (tick, order, message): note-offs (order 0) before note-ons
    for repeat in range(repeats):
        for note in notes:
            start = _swing(note.beat, swing) + note.delay_ms / 1000 / seconds_per_beat
            end = _swing(note.beat + note.length, swing) + note.delay_ms / 1000 / seconds_per_beat
            base = repeat * loop_beats
            on = round((base + start) * TICKS_PER_BEAT)
            off = max(on + 1, round((base + end) * TICKS_PER_BEAT))
            events.append((on, 1, mido.Message("note_on", note=note.pitch, velocity=max(1, min(127, note.velocity)), channel=note.channel)))
            events.append((off, 0, mido.Message("note_off", note=note.pitch, velocity=0, channel=note.channel)))
    end_tick = round((repeats * loop_beats + tail_beats) * TICKS_PER_BEAT)
    if tail_beats:
        # A harmless controller message keeps fluidsynth rendering the reverb tail.
        events.append((end_tick, 0, mido.Message("control_change", control=110, value=0, channel=9)))
    track = mido.MidiTrack()
    track.append(mido.MetaMessage("set_tempo", tempo=mido.bpm2tempo(tempo), time=0))
    for channel, (program, pan, reverb) in _channel_setup(leads).items():
        track.append(mido.Message("program_change", program=program, channel=channel, time=0))
        track.append(mido.Message("control_change", control=10, value=pan, channel=channel, time=0))
        track.append(mido.Message("control_change", control=91, value=reverb, channel=channel, time=0))
    now = 0
    for tick, _, message in sorted(events, key=lambda e: (e[0], e[1])):
        track.append(message.copy(time=tick - now))
        now = tick
    midi = mido.MidiFile(ticks_per_beat=TICKS_PER_BEAT)
    midi.tracks.append(track)
    midi.save(path)


def render(midi_path, wav_path, gain=0.5):
    """MIDI -> 44.1 kHz stereo wav through fluidsynth (no audio device used)."""
    subprocess.run(
        ["fluidsynth", "-ni", "-q", "-g", str(gain), "-r", str(SAMPLE_RATE),
         "-F", wav_path, os.environ["SF2_GENERALUSER"], midi_path],
        check=True, stderr=subprocess.DEVNULL,
    )


def build_loop(tune, ogg_path):
    """Render the tune three times in a row and keep the middle one: it starts
    with the reverb tail of its predecessor, so it loops without a seam."""
    notes, beats = arrange(tune)
    leads = {p.lead for p in tune.passes}
    with tempfile.TemporaryDirectory() as tmp:
        midi, wav, cut = (os.path.join(tmp, n) for n in ("t.mid", "t.wav", "cut.wav"))
        write_midi(notes, tune.tempo, tune.swing, leads, midi, repeats=3, loop_beats=beats)
        render(midi, wav)
        start = round(beats * 60 / tune.tempo * SAMPLE_RATE)
        subprocess.run(["sox", wav, cut, "trim", f"{start}s", f"{start}s", "gain", "-n", "-1"], check=True)
        _encode_ogg(cut, ogg_path)
    return beats * 60 / tune.tempo


def build_stinger(notes, tempo, leads, wav_path, tail_beats=2.0, swing=0.5):
    """A one-shot musical sound effect (no loop), mono wav for Godot."""
    beats = max(n.beat + n.length for n in notes)
    with tempfile.TemporaryDirectory() as tmp:
        midi, wav, trimmed = (os.path.join(tmp, n) for n in ("s.mid", "s.wav", "trim.wav"))
        write_midi(notes, tempo, swing, leads, midi, loop_beats=beats, tail_beats=tail_beats)
        render(midi, wav, gain=0.7)
        # Mono, drop the silent tail (below -50 dB); then normalise and fade
        # the last 0.3 s (a second pass: fade needs the trimmed length).
        subprocess.run(["sox", wav, "-c", "1", trimmed, "reverse", "silence", "1", "0.02", "-50d", "reverse"], check=True)
        subprocess.run(["sox", trimmed, wav_path, "gain", "-n", "-1", "fade", "0", "-0", "0.3"], check=True)


def _encode_ogg(wav, ogg):
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", wav, "-c:a", "libvorbis", "-q:a", "5", ogg], check=True)
