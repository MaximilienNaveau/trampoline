# Per-player tile-draw schedule, keyed by number of players (see
# doc/RULES.md). Every player draws `initial` tiles first, then `draws`
# more batches of `draw_size` tiles each. For 1 and 3 players this uses
# all 117 tiles exactly; for 2 and 4, it uses 116, with the 117th tile
# going to whoever starts (player 0 in this codebase — see
# `has_starter_bonus_tile`).
#
# Solo play (1) has no real "initial vs. later" distinction to make (no
# opponents to be ahead of or behind), so its schedule is simply 13 draws
# of 9, using the whole bag evenly.
class_name DrawPlan
extends RefCounted

const _SCHEDULE := {
	1: {"initial": 9, "draws": 12, "draw_size": 9},
	2: {"initial": 13, "draws": 5, "draw_size": 9},
	3: {"initial": 12, "draws": 3, "draw_size": 9},
	4: {"initial": 11, "draws": 2, "draw_size": 9},
}


static func initial_draw_size(number_of_players: int) -> int:
	return _SCHEDULE[number_of_players]["initial"]


static func subsequent_draw_count(number_of_players: int) -> int:
	return _SCHEDULE[number_of_players]["draws"]


static func subsequent_draw_size(number_of_players: int) -> int:
	return _SCHEDULE[number_of_players]["draw_size"]


# Whether this player count leaves a 117th tile over for whoever starts
# (true for 2 and 4 players; 1 and 3 divide the bag exactly evenly).
static func has_starter_bonus_tile(number_of_players: int) -> bool:
	return number_of_players == 2 or number_of_players == 4
