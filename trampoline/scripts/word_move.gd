# Relocating an already-played word: shifting it left/right within its
# own row (to open up room to prepend/append, if it's shorter than 9
# letters), or moving it wholesale to a different row entirely — see
# doc/RULES.md. Confirmed by Maximilien:
#   - Any player may move any word, not just its original contributors.
#   - Moving a word changes the board's worth only if it makes or breaks
#     words — what the turn earns is that change (see scoring.gd).
#   - The destination can be any row, even one that already holds an
#     unrelated word, as long as the exact destination cells are free —
#     this is why board.gd supports more than one word per row.
#
# Every function here is `static`, same reasoning as growth_rules.gd: this
# class holds no state, it only acts on a `Board` passed in.
class_name WordMove
extends RefCounted


# Whether the word at `(from_row, from_col)` can be relocated so its first
# letter lands at `(to_row, to_col)`, keeping its internal letter order
# and length unchanged. Legal as long as every destination cell is either
# empty or already part of the SAME word (relevant when shifting within
# one row — those cells don't need to already be empty, since they're
# being vacated as part of this same move).
static func can_move(board: Board, from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	var source_columns := board.word_columns_at(from_row, from_col)
	if source_columns.is_empty():
		return false
	for offset in range(source_columns.size()):
		var dest_col := to_col + offset
		if dest_col < 0 or dest_col >= Board.COLUMNS:
			return false
		var dest_is_source_cell := to_row == from_row and source_columns.has(dest_col)
		if not dest_is_source_cell and not board.is_empty(to_row, dest_col):
			return false
	return true


# Relocates the word at `(from_row, from_col)` so its first letter lands
# at `(to_row, to_col)`. Returns false and does nothing if `can_move`
# refuses the move.
static func move(board: Board, from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	if not can_move(board, from_row, from_col, to_row, to_col):
		return false
	var source_columns := board.word_columns_at(from_row, from_col)

	# Pull every tile out of its old cell first,
	# before placing anything back down — a move that shifts a word
	# within its own row can have destination cells that overlap the
	# source, so nothing can be placed until the whole word is lifted.
	var tiles: Array[Tile] = []
	for col in source_columns:
		tiles.append(board.remove(from_row, col))

	for i in range(tiles.size()):
		board.place(to_row, to_col + i, tiles[i])

	return true
