# Standalone checks for board.gd, runnable via:
#   godot4 --headless --path . --script tests/test_board.gd
# See tests/test_tiles.gd for why this isn't a test-framework addon.
#
# Board is deliberately unrestricted (see board.gd's header comment) — the
# turn-based growth/blank-removal rules are tested separately, in
# tests/test_growth_rules.gd.
extends SceneTree

var _failures := 0

func _check(condition: bool, message: String) -> void:
	if condition:
		print("PASS: ", message)
	else:
		print("FAIL: ", message)
		_failures += 1

func _initialize() -> void:
	_test_empty_board()
	_test_place_and_remove()
	_test_word_in_row()
	_test_rows_are_independent()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)

func _test_empty_board() -> void:
	var board := Board.new()
	_check(board.leftmost_filled(0) == -1, "empty row has no leftmost filled cell")
	_check(board.rightmost_filled(0) == -1, "empty row has no rightmost filled cell")
	_check(board.word_in_row(0) == "", "empty row spells the empty string")
	_check(board.is_empty(0, 4), "an untouched cell is empty")

func _test_place_and_remove() -> void:
	var board := Board.new()
	_check(board.place(0, 4, Tile.new("C", "Z", true)), "placing into an empty cell succeeds")
	_check(not board.place(0, 4, Tile.new("A", "Z", true)), "placing into an occupied cell fails")

	# Board itself has no adjacency rule — that's growth_rules.gd's job.
	# Placing anywhere else empty, even leaving a gap, is allowed here.
	var far_away_ok := board.place(0, 7, Tile.new("A", "Z", true))
	_check(far_away_ok, "placing anywhere else empty succeeds (no adjacency rule here)")

	var removed := board.remove(0, 4)
	var removed_was_c := removed != null and removed.current_letter() == "C"
	_check(removed_was_c, "remove returns the tile that was there")
	_check(board.is_empty(0, 4), "cell is empty again after removal")
	_check(board.remove(0, 4) == null, "removing an already-empty cell returns null")

	# Board places no restriction on what can be removed either — a
	# real-letter tile comes right back out. That restriction belongs to
	# growth_rules.gd, not the raw grid.
	board.place(0, 4, Tile.new("H", "Z", true))
	var removed_letter := board.remove(0, 4)
	var real_letter_removable := removed_letter != null and removed_letter.current_letter() == "H"
	_check(real_letter_removable, "a real-letter tile can be removed from the raw board")

func _test_word_in_row() -> void:
	var board := Board.new()
	board.place(2, 2, Tile.new("C", "Z", true))
	board.place(2, 3, Tile.new("A", "Z", true))
	board.place(2, 4, Tile.new("T", "Z", true))
	_check(board.word_in_row(2) == "CAT", "word reads correctly from a mid-row start")

	# Flipping is the Tile's own job (see tile.gd), not the board's — call
	# it directly on the tile, not through Board.
	var shorten_tile := Tile.new("E", "", true)
	board.place(2, 5, shorten_tile)
	shorten_tile.flip()  # show the blank back face
	_check(board.word_in_row(2) == "CAT", "a blank-showing tile truncates the word, not included")
	_check(board.rightmost_filled(2) == 5, "but the blank tile's cell still counts as occupied")

func _test_rows_are_independent() -> void:
	var board := Board.new()
	board.place(4, 0, Tile.new("X", "Z", true))
	_check(board.word_in_row(5) == "", "placing in one row leaves another row untouched")
	_check(board.leftmost_filled(5) == -1, "an untouched row still reports no filled cells")
