# Tracks the placement-order position score AND owning player for every
# cell in one row (see doc/RULES.md: score is placement ORDER, not column
# index — whichever letter is placed next in the row gets the next
# counter value, regardless of which column it lands in). Ownership
# matters because a word's score is attributed per player: whoever placed
# a given letter gets that letter's position score, not the row as a
# whole. A full game holds 13 of these, one per board row — see
# scoring.gd.
class_name RowScore
extends RefCounted

var _next_score := 1
var _scores: Dictionary = {}  # col (int) -> position score (int)
var _owners: Dictionary = {}  # col (int) -> player_id (int)


# Records that `player_id` just placed a tile at `col`, assigning it the
# next sequential position score for this row (1, 2, 3, ...). If `col`
# already had a score from an earlier placement — possible after its tile
# was removed (only ever legal for a blank tile, see growth_rules.gd) and
# something new placed there — this overwrites both the score and the
# owner, since it's a genuinely new placement event.
func record_placement(col: int, player_id: int) -> int:
	var score := _next_score
	_scores[col] = score
	_owners[col] = player_id
	_next_score += 1
	return score


# The position score assigned to `col`, or 0 if nothing has been recorded
# there (yet).
func score_at(col: int) -> int:
	return _scores.get(col, 0)


# The player who placed the tile at `col`, or -1 if nothing has been
# recorded there (yet).
func owner_at(col: int) -> int:
	return _owners.get(col, -1)


# The sum of position scores across `columns`, regardless of who placed
# them — typically a row's current word, from `Board.word_columns()`.
func total_score(columns: Array) -> int:
	var total := 0
	for col in columns:
		total += score_at(col)
	return total


# The sum of position scores across `columns`, counting only the ones
# `player_id` placed.
func total_score_for_player(columns: Array, player_id: int) -> int:
	var total := 0
	for col in columns:
		if owner_at(col) == player_id:
			total += score_at(col)
	return total
