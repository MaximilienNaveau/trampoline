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


# Clears whatever score/owner is recorded for `col`, without touching the
# running counter. Used when a whole word is relocated (see
# word_move.gd): the tile's score travels WITH it to its new cell (see
# `restore` below) rather than being earned again, so the old cell just
# needs to forget it ever held anything.
func forget(col: int) -> void:
	_scores.erase(col)
	_owners.erase(col)


# Directly sets `col`'s score and owner to specific values, bypassing the
# usual "next sequential score" auto-increment — used only when relocating
# an already-scored tile (see word_move.gd), never for an actual new
# placement (use `record_placement` for that).
func restore(col: int, score: int, player_id: int) -> void:
	_scores[col] = score
	_owners[col] = player_id


# Exports this row's state — see save_game.gd. `_scores`/`_owners` are
# keyed by column (an int), but JSON only supports string dictionary
# keys, so the keys are converted to strings explicitly here rather than
# leaving it to chance.
func export_state() -> Dictionary:
	var scores := {}
	for col in _scores:
		scores[str(col)] = _scores[col]
	var owners := {}
	for col in _owners:
		owners[str(col)] = _owners[col]
	return {"next_score": _next_score, "scores": scores, "owners": owners}


# Overwrites this row's entire state from `data` (see `export_state`),
# converting column keys back from strings to ints and every number back
# from JSON's float to int — see turn_manager.gd's restore_state for why.
func restore_state(data: Dictionary) -> void:
	_next_score = int(data["next_score"])
	_scores.clear()
	for col in data["scores"]:
		_scores[int(col)] = int(data["scores"][col])
	_owners.clear()
	for col in data["owners"]:
		_owners[int(col)] = int(data["owners"][col])
