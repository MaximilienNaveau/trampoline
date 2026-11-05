# How the board and the current player's hand stood when a turn began —
# what GameSession measures the turn against: what it earned (the
# board's change in worth), which cells must be filled again, and what
# "Recommencer" puts back (see GameSession.reset_turn).
class_name TurnStart
extends RefCounted

# What the board was worth (see scoring.gd's `board_value`).
var value := 0
# The cells holding a letter, as `Vector2i(col, row)`: during a turn any
# tile may move, but these must be filled again before it ends (see
# GameSession.emptied_cells). A blank ("-") tile's cell isn't among them:
# taking a blank off shortens a word on purpose (see doc/RULES.md).
var cells_to_keep_filled: Array[Vector2i] = []

# The tile in every cell (null if empty), row by row.
var _board: Array = []
# The hand, in order — a copy, not the live array.
var _hand: Array[Tile] = []
# Which face every one of those tiles showed: tile -> is_showing_front().
var _faces: Dictionary = {}


# Notes down how `board` and `hand` stand right now.
func _init(board: Board, hand: Array[Tile], dictionary: FrenchDictionary) -> void:
	value = Scoring.board_value(board, dictionary)
	for row in range(Board.ROWS):
		var row_tiles: Array[Tile] = []
		for col in range(Board.COLUMNS):
			var tile := board.tile_at(row, col)
			row_tiles.append(tile)
			if tile != null:
				_faces[tile] = tile.is_showing_front()
				if not tile.is_blank():
					cells_to_keep_filled.append(Vector2i(col, row))
		_board.append(row_tiles)
	_hand = hand.duplicate()
	for tile in _hand:
		_faces[tile] = tile.is_showing_front()


# Puts `board` and `hand` (the live array) back as they were noted down,
# every tile showing the same face.
func restore(board: Board, hand: Array[Tile]) -> void:
	for row in range(Board.ROWS):
		for col in range(Board.COLUMNS):
			board.remove(row, col)
	for row in range(Board.ROWS):
		for col in range(Board.COLUMNS):
			var tile: Tile = _board[row][col]
			if tile != null:
				board.place(row, col, tile)
	hand.clear()
	hand.append_array(_hand)
	for tile in _faces:
		tile.set_showing_front(_faces[tile])
