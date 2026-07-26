# The 13x9 board: 117 cells, one 9-letter word per row (see doc/RULES.md).
#
# Pure grid mechanics only — place/remove/flip a tile in a cell, and read
# back what's there. On purpose, this does NOT enforce the turn-based
# "grow only, one contiguous word per row, blanks are the one exception"
# ruleset — that's mode-specific: free-play mode needs to place and remove
# tiles anywhere, with no restrictions at all, while turn-based/scored play
# needs those restrictions. See growth_rules.gd for that ruleset; it calls
# into this file rather than the other way around, so this file has no
# knowledge of which mode is using it, or of players/turns/scoring/drawing.
class_name Board
extends RefCounted

# 13 rows, 9 columns — the game's board is always exactly this size (no
# per-instance configuration needed, so these are constants rather than
# constructor parameters).
const ROWS := 13
const COLUMNS := 9

# The grid itself: one Array per row, each holding either `null` (empty
# cell) or a `Tile` (occupied cell). `Array[Tile]` still accepts `null` in
# GDScript — a typed array only constrains what class non-null elements
# must be, the same way a `Tile*` in C++ can be `nullptr`.
var _cells: Array = []

func _init() -> void:
	for row in range(ROWS):
		var empty_row: Array[Tile] = []
		empty_row.resize(COLUMNS)  # `resize` fills the new slots with `null`.
		_cells.append(empty_row)

# Whether a cell has no tile in it yet.
func is_empty(row: int, col: int) -> bool:
	return _cells[row][col] == null

# The tile currently in a cell, or `null` if it's empty.
func tile_at(row: int, col: int) -> Tile:
	return _cells[row][col]

# Index of the leftmost filled cell in a row, or -1 if the whole row is
# empty. In turn-based play this is always one contiguous run (enforced by
# growth_rules.gd); in free-play mode a row could have gaps, in which case
# this is just "the first filled cell reading left to right", nothing more.
func leftmost_filled(row: int) -> int:
	for col in range(COLUMNS):
		if not is_empty(row, col):
			return col
	return -1

# Index of the rightmost filled cell in a row, or -1 if the whole row is
# empty.
func rightmost_filled(row: int) -> int:
	for col in range(COLUMNS - 1, -1, -1):
		if not is_empty(row, col):
			return col
	return -1

# Places `tile` at `(row, col)`. The only universal constraint — true in
# every mode — is that a cell can't hold two tiles at once. Returns false
# and does nothing if the cell is already occupied.
func place(row: int, col: int, tile: Tile) -> bool:
	if not is_empty(row, col):
		return false
	_cells[row][col] = tile
	return true

# Removes and returns whatever tile is at `(row, col)`, leaving it empty.
# Unconditional — free-play mode relies on being able to pick up and move
# any tile at will. Turn-based play must NOT call this directly; it should
# go through `GrowthRules.remove()`, which only allows removing a
# currently-blank-showing tile. Returns `null` if the cell is empty.
func remove(row: int, col: int) -> Tile:
	var tile: Tile = _cells[row][col]
	_cells[row][col] = null
	return tile

# The column indices that make up a row's current word, reading from its
# leftmost filled cell. Stops at the first blank-showing tile (the
# "shorten" mechanic — see doc/RULES.md) or the first empty cell, whichever
# comes first, WITHOUT including a blank tile's column, since a blank marks
# "the word ends here" even though the row might still have empty cells
# left to grow into later. Empty for a completely empty row.
func word_columns(row: int) -> Array[int]:
	var left := leftmost_filled(row)
	var columns: Array[int] = []
	if left == -1:
		return columns
	for col in range(left, COLUMNS):
		var tile: Tile = _cells[row][col]
		if tile == null or tile.is_blank():
			break
		columns.append(col)
	return columns

# The word currently spelled out by a row — see `word_columns` above for
# exactly which cells that covers. Returns "" for a completely empty row.
func word_in_row(row: int) -> String:
	var word := ""
	for col in word_columns(row):
		word += _cells[row][col].current_letter()
	return word

# Whether a row needs no further growth: either its word has reached the
# full 9 letters, or it was deliberately cut short by a blank-showing tile
# (the "shorten" mechanic) — in both cases nothing more is expected to be
# added to this row. A word merely jammed against the board's edge without
# reaching 9 letters and without a blank cap does NOT count — it could
# still grow further as a prefix.
func is_row_finished(row: int) -> bool:
	var columns := word_columns(row)
	if columns.size() == COLUMNS:
		return true
	if columns.is_empty():
		return false
	var col_after_word: int = columns[-1] + 1
	if col_after_word >= COLUMNS:
		return false
	var tile: Tile = _cells[row][col_after_word]
	return tile != null and tile.is_blank()
