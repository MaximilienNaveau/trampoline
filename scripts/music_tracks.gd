# Which tune plays where: one per mode (made by
# tools/audio/make_music.py, see tools/audio/README.md). main.gd only asks
# for the tune of the mode it starts.
# A `class_name` script holding only constants and static functions is
# used like a Python module: `MusicTracks.MULTIPLAYER`, no instance
# needed.
class_name MusicTracks
extends RefCounted

# `preload` loads a file once, as soon as this script is (like an import
# at the top of a Python module): these two are always ready to play.
const MULTIPLAYER := preload("res://assets/audio/music/multi_le_bouquiniste.ogg")
const FREE_PLAY := preload("res://assets/audio/music/libre_flanerie.ogg")
