# The crossword-reconstruction puzzle data for doc/RULES.md's third
# single-player mode: each puzzle is 13 (clue, 9-letter answer) pairs,
# one per board row. Extracted from doc/GRILLE 02&01r.xls — every
# `GRILLE nn&mmr.xls` sheet prints both puzzle nn's own (unanswered)
# clues AND puzzle mm's answer key (its clues WITH their solutions), so
# `02&01r.xls`'s answer-key half is puzzle 1's complete, solved data.
#
# Only puzzle 1 is filled in for now — puzzles 2-10 extract the exact
# same way from `03&02r.xls` through `11&10r.xls` (puzzle 11 has clues
# but no file ever supplies its answers, so it can't be used this way).
# `PUZZLES` is already shaped to hold more than one, so adding them later
# is a plain data addition, not a redesign.
class_name CrosswordPuzzles
extends RefCounted

const PUZZLES := [
	[
		["Oiseau pastis.", "PERROQUET"],
		["S'occupe des légumes", "JARDINIER"],
		["Véhicule spécialisé.", "AMBULANCE"],
		["Boirez goulûment.", "SIFFLEREZ"],
		["POETE.", "KARLFELDT"],
		["Femme d'envahisseur venu de l'est.", "WISIGOTHE"],
		["Périlleux.", "DANGEREUX"],
		["Concert.", "SYMPHONIE"],
		["Vaudrais cher.", "COUTERAIS"],
		["Dames-jeannes.", "BOMBONNES"],
		["Subsistent.", "SURVIVENT"],
		["Admises", "ACCEPTEES"],
		["Font Briller.", "ASTIQUENT"],
	],
]
