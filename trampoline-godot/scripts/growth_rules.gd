# The turn-based/scored-play ruleset for growing a row's word: a new tile
# must extend the row's existing contiguous run (prefix or suffix, never a
# gap), and only a currently-blank-showing tile can be taken back — a real
# letter, once placed, never can (see doc/RULES.md).
#
# This is NOT how free-play mode works — free play calls straight into
# Board with no restrictions at all (see board.gd). Only turn-based/scored
# modes should go through this file.
#
# Every function here is `static`: this class holds no state of its own,
# it only asks questions about (or acts on) a `Board` passed in as an
# argument — the same way a free function taking a pointer/reference would
# in C++, rather than a method on an object with its own fields.
class_name GrowthRules
extends RefCounted

# Whether `col` is a legal spot to place a new tile in `row` of `board`:
# the cell must be empty, and it must sit immediately next to the row's
# current contiguous run of tiles (or the row can be entirely empty, in
# which case any of its 9 cells is a valid first placement).
static func can_place(board: Board, row: int, col: int) -> bool:
	if not board.is_empty(row, col):
		return false
	var left := board.leftmost_filled(row)
	if left == -1:
		return true  # row is empty: anywhere is a legal first tile
	var right := board.rightmost_filled(row)
	return col == left - 1 or col == right + 1

# Places `tile` at `(row, col)` in `board` if `can_place` allows it.
# Returns false and does nothing otherwise.
static func place(board: Board, row: int, col: int, tile: Tile) -> bool:
	if not can_place(board, row, col):
		return false
	return board.place(row, col, tile)

# Whether the tile at `(row, col)` in `board` can be taken back: only true
# if a tile is there, it's currently showing its blank face (a real letter
# can never be removed this way — only flipped, or grown around), AND it
# sits at one end of the row's contiguous run rather than in the middle.
# That last condition matters even though a blank can only ever be PLACED
# at an edge (see `can_place`): further growth can happen on the other
# side afterward, leaving an edge blank stranded in the middle — removing
# it from there would tear a hole in an otherwise-contiguous word.
static func can_remove(board: Board, row: int, col: int) -> bool:
	var tile := board.tile_at(row, col)
	if tile == null or not tile.is_blank():
		return false
	return col == board.leftmost_filled(row) or col == board.rightmost_filled(row)

# Removes the tile at `(row, col)` in `board` if `can_remove` allows it.
# Returns the removed tile, or `null` if the removal isn't legal.
static func remove(board: Board, row: int, col: int) -> Tile:
	if not can_remove(board, row, col):
		return null
	return board.remove(row, col)
