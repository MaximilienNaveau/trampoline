# The solo "free play" sandbox from doc/RULES.md's single-player modes:
# all 117 tiles available at once, no turns, no draw restrictions — place,
# move, remove, or flip any tile anywhere, any time. Standard scoring
# still applies (a word's score is the sum of its letters' placement-order
# position scores, minus 5 per green letter — see scoring.gd), just with a
# single implicit player instead of GameSession's turn-based ones.
#
# Deliberately NOT a GameSession subclass: GameSession carries turns/bag/
# draw-scheduling that free play has no use for at all, so inheriting
# would either buy nothing (an empty base) or force free-play-skip
# branches into GameSession itself. main.gd instead treats whichever
# session is active (GameSession or this) the same way BoardView already
# treats `is_takeable`/`can_place_here` — as plain Callables, not a shared
# type.
class_name FreePlaySession
extends RefCounted

# Scoring is written per-player (see scoring.gd), but free play only ever
# has the one — every placement is attributed to this fixed id.
const PLAYER_ID := 0

var board: Board
var pool: TilePool
var scoring: Scoring
var dictionary: FrenchDictionary


# `dictionary` can be injected the same way GameSession's constructor
# allows (e.g. a test reusing one already-loaded instance, since loading
# it re-reads and normalizes ~79000 lines every time) — defaults to
# loading a fresh one.
func _init(a_dictionary: FrenchDictionary = null) -> void:
	board = Board.new()
	pool = TilePool.new()
	scoring = Scoring.new()
	dictionary = a_dictionary if a_dictionary != null else FrenchDictionary.new()


# Every tile NOT currently sitting somewhere on the board — there's no
# hand/bag bookkeeping to maintain at all in free play, so "what's
# available" is simply recomputed from the board's current state each
# time this is called, rather than tracked incrementally.
func available_tiles() -> Array[Tile]:
	# A Dictionary used purely as a hash set (its values never read, only
	# `.has()` checked below) — the standard GDScript way to test "is this
	# tile somewhere on the board?" in O(1) per tile instead of scanning
	# the whole board again for each of the 117.
	var on_board := {}
	for row in range(Board.ROWS):
		for col in range(Board.COLUMNS):
			var tile := board.tile_at(row, col)
			if tile != null:
				on_board[tile] = true

	var available: Array[Tile] = []
	for tile in pool.tiles:
		if not on_board.has(tile):
			available.append(tile)
	return available


# Places `tile` at (row, col) — no GrowthRules restriction at all (see
# board.gd's own header comment: free play calls straight into Board).
# Returns false and does nothing if the cell is already occupied.
func place_tile(row: int, col: int, tile: Tile) -> bool:
	if not board.place(row, col, tile):
		return false
	scoring.record_placement(row, col, PLAYER_ID)
	return true


# Whether the tile currently at (row, col) can be picked back up — always
# true in free play, regardless of whose turn it is or when it was placed
# (there's no such thing as either here). False only because the cell
# itself is empty is handled by BoardCell before this is even asked (see
# board_cell.gd's `_get_drag_data`).
func can_take_back(_row: int, _col: int) -> bool:
	return true


# Whether (row, col) is currently a legal spot to drop a NEW tile — the
# one universal restriction that's true in every mode (see board.gd): a
# cell can't hold two tiles at once. Skipping this check would let a
# board-to-board drag onto an occupied cell silently destroy the tile
# already there (removed from its source, then discarded because the
# destination refused it with nothing to roll back to).
func can_place_here(row: int, col: int) -> bool:
	return board.is_empty(row, col)


# Picks the tile at (row, col) back up, giving it back to `available_tiles()`
# (nothing else to update — see that function's own comment). Returns
# null if the cell is empty.
func remove_tile(row: int, col: int) -> Tile:
	var tile := board.tile_at(row, col)
	if tile == null:
		return null
	board.remove(row, col)
	scoring.forget(row, col)
	return tile


# Relocates a tile from (from_row, from_col) to (to_row, to_col). Needed
# because `can_take_back` is unconditionally true, so a drag can start
# from an occupied board cell, not just from the tile palette. Mirrors
# GameSession.move_own_tile's rollback exactly: if the destination turns
# out occupied, the tile goes right back where it came from instead of
# vanishing. Returns false and does nothing if the source is empty.
func move_tile(from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	var tile := board.tile_at(from_row, from_col)
	if tile == null:
		return false

	board.remove(from_row, from_col)
	if not board.place(to_row, to_col, tile):
		board.place(from_row, from_col, tile)  # rollback: destination occupied
		return false

	scoring.forget(from_row, from_col)
	scoring.record_placement(to_row, to_col, PLAYER_ID)
	return true


# Flips the tile at (row, col) in place, revealing its other face — same
# as GameSession.flip_tile, and for the same reason scoring needs no
# separate bookkeeping here either: Scoring always reads a tile's CURRENT
# face live (see scoring.gd). Returns false and does nothing if the cell
# is empty.
func flip_tile(row: int, col: int) -> bool:
	var tile := board.tile_at(row, col)
	if tile == null:
		return false
	tile.flip()
	return true


# The current total score: the sum of every currently-valid word's
# position scores on the board, minus the green-letter penalty (see
# Scoring.player_word_score) — recomputed from scratch each call, the
# same approach GameSession._recompute_all_scores uses, just for the
# single PLAYER_ID instead of once per turn-based player. There's no
# separate "commit" step in free play — the score is just always whatever
# it currently is.
func current_score() -> int:
	var total := 0
	for row in range(Board.ROWS):
		for segment in board.segments_in_row(row):
			var col: int = segment[0]
			total += scoring.player_word_score(board, dictionary, row, col, PLAYER_ID)
	return total
