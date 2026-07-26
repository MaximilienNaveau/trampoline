# French dictionary word-validity check, ported from the Unity project's
# `FrenchDictionary.cs`. Wraps the "classique" Dicollecte word list
# (data/dictionaries/fr-classique.dic, MPL 2.0 — see the accompanying
# README_dict_fr.txt) as a set of normalized words, checked against
# whatever word a row currently spells (see doc/RULES.md: any dictionary
# word is legal, including all grammatical forms — the word list already
# contains every inflected form as its own line, there's no on-the-fly
# conjugation happening here).
#
# Simplified from the C# version: that one supported both a synchronous
# and an async load, but every actual caller in the Unity project passed
# `async: false` anyway, so only the synchronous path is ported here.
class_name FrenchDictionary
extends RefCounted

const _DICTIONARY_PATH := "res://data/dictionaries/fr-classique.dic"

# Every accented/ligature/superscript character that appears in the source
# word list, mapped to the plain uppercase letter(s) it should be treated
# as. The physical game tiles only ever show plain A-Z letters (see
# doc/RULES.md), so a dictionary word can only ever match a placed word
# once accents are stripped this way — same idea as the C# version's
# Unicode NFD-normalize-then-strip-combining-marks approach, just spelled
# out directly since GDScript has no built-in equivalent.
const _ACCENT_MAP := {
	"à": "A", "â": "A", "ä": "A", "å": "A", "ã": "A", "á": "A",
	"À": "A", "Â": "A", "Ä": "A", "Å": "A", "Ã": "A", "Á": "A",
	"æ": "AE", "Æ": "AE",
	"ç": "C", "ć": "C", "Ç": "C", "Ć": "C",
	"é": "E", "è": "E", "ê": "E", "ë": "E",
	"É": "E", "È": "E", "Ê": "E", "Ë": "E",
	"í": "I", "î": "I", "ï": "I", "Í": "I", "Î": "I", "Ï": "I",
	"ñ": "N", "ń": "N", "Ñ": "N", "Ń": "N",
	"ó": "O", "ô": "O", "ö": "O", "Ó": "O", "Ô": "O", "Ö": "O",
	"œ": "OE", "Œ": "OE",
	"ś": "S", "š": "S", "Ś": "S", "Š": "S",
	"ù": "U", "û": "U", "ü": "U", "Ù": "U", "Û": "U", "Ü": "U",
	"ÿ": "Y", "Ÿ": "Y",
	# Superscript/subscript letters (used in footnote-style ordinal
	# abbreviations like "1ᵉʳ") that the C# version also replaced one by
	# one rather than via general Unicode normalization.
	"ᵃ": "A", "ᵇ": "B", "ᶜ": "C", "ᵈ": "D", "ᵉ": "E", "ᶠ": "F",
	"ᵍ": "G", "ʰ": "H", "ⁱ": "I", "ʲ": "J", "ᵏ": "K", "ˡ": "L",
	"ᵐ": "M", "ⁿ": "N", "ᵒ": "O", "ᵖ": "P", "ʳ": "R", "ˢ": "S",
	"ᵗ": "T", "ᵘ": "U", "ᵛ": "V", "ʷ": "W", "ˣ": "X", "ʸ": "Y",
	"ᶻ": "Z",
	"ₐ": "A", "ₑ": "E", "ₕ": "H", "ᵢ": "I", "ⱼ": "J", "ₖ": "K",
	"ₗ": "L", "ₘ": "M", "ₙ": "N", "ₒ": "O", "ₚ": "P", "ᵣ": "R",
	"ₛ": "S", "ₜ": "T", "ᵤ": "U", "ᵥ": "V", "ₓ": "X",
}

# The set of every valid (already-normalized) word. GDScript has no
# built-in Set type, so — same trick as in `tile_bag.gd`'s hands — a
# Dictionary stands in for one: only the keys matter, values are unused.
var _words: Dictionary = {}

func _init() -> void:
	_load()

# Whether `word` is valid, case- and accent-insensitively — matching
# however the game's plain-letter tiles actually spell it out.
func is_word_valid(word: String) -> bool:
	return _words.has(_normalize(word))

# How many words loaded — mainly useful for sanity-checking the load
# itself (tests, or a debug screen), not needed for gameplay.
func word_count() -> int:
	return _words.size()

func _load() -> void:
	var file := FileAccess.open(_DICTIONARY_PATH, FileAccess.READ)
	if file == null:
		push_error("FrenchDictionary: could not open %s" % _DICTIONARY_PATH)
		return
	while not file.eof_reached():
		_add_line(file.get_line())

# Parses one raw Hunspell .dic line (e.g. "1er/23\t154" or "abaisser/12")
# and adds the resulting word to the set, unless it should be skipped.
# Mirrors `FrenchDictionary.cs`'s `LoadDictionary` exactly, including how
# the file's leading word-count line and any comment line end up
# harmlessly filtered out on their own (see the digit check below): split
# on '/' then '\t' then ' ' to isolate the bare word, normalize it, then
# reject anything left containing a dash or a digit.
func _add_line(line: String) -> void:
	if line.strip_edges() == "" or line.begins_with("#"):
		return
	var word := line.split("/")[0].split("\t")[0].split(" ")[0]
	var normalized := _normalize(word)
	if normalized.contains("-") or _contains_digit(normalized):
		return
	_words[normalized] = true

func _normalize(word: String) -> String:
	for accented in _ACCENT_MAP:
		word = word.replace(accented, _ACCENT_MAP[accented])
	return word.to_upper()

func _contains_digit(text: String) -> bool:
	for i in range(text.length()):
		if text[i] in "0123456789":
			return true
	return false
