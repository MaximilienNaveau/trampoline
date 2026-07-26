# Standalone checks for growth_rules.gd, runnable via:
#   godot4 --headless --path . --script tests/test_growth_rules.gd
extends SceneTree

var _failures := 0


func _check(condition: bool, message: String) -> void:
	if condition:
		print("PASS: ", message)
	else:
		print("FAIL: ", message)
		_failures += 1


func _initialize() -> void:
	_test_placement_adjacency()
	_test_blank_removal_exception()
	_test_stranded_blank_cannot_be_removed()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


func _test_placement_adjacency() -> void:
	var board := Board.new()
	for col in range(Board.COLUMNS):
		var msg := "any column is valid on an empty row (col %d)" % col
		_check(GrowthRules.can_place(board, 0, col), msg)

	var first := GrowthRules.place(board, 0, 4, Tile.new("C", "Z", true))
	_check(first, "first placement in a row succeeds")
	_check(not GrowthRules.can_place(board, 0, 4), "an occupied cell is never placeable again")
	_check(GrowthRules.can_place(board, 0, 3), "the cell left of the word is placeable")
	_check(GrowthRules.can_place(board, 0, 5), "the cell right of the word is placeable")
	_check(not GrowthRules.can_place(board, 0, 2), "a cell 2 away would leave a gap, not placeable")
	_check(not GrowthRules.can_place(board, 0, 6), "same, 2 away on the other side")

	_check(
		GrowthRules.place(board, 0, 5, Tile.new("A", "Z", true)), "extending as a suffix succeeds"
	)
	_check(
		GrowthRules.place(board, 0, 3, Tile.new("S", "Z", true)), "extending as a prefix succeeds"
	)
	_check(board.word_in_row(0) == "SCA", "word reads left-to-right after prefix+suffix growth")

	var refused := GrowthRules.place(board, 0, 7, Tile.new("Z", "Z", true))
	_check(not refused, "a non-adjacent placement is refused")


func _test_blank_removal_exception() -> void:
	var board := Board.new()
	GrowthRules.place(board, 1, 0, Tile.new("H", "Z", true))
	_check(
		not GrowthRules.can_remove(board, 1, 0), "a tile showing a real letter cannot be removed"
	)
	_check(GrowthRules.remove(board, 1, 0) == null, "...and remove() refuses it too")
	_check(not board.is_empty(1, 0), "it's still there after the refused removal")

	var shorten_tile := Tile.new("E", "", true)
	GrowthRules.place(board, 1, 1, shorten_tile)
	shorten_tile.flip()  # now showing its blank back face
	_check(GrowthRules.can_remove(board, 1, 1), "a tile currently showing blank CAN be removed")

	var removed := GrowthRules.remove(board, 1, 1)
	_check(removed == shorten_tile, "remove returns the same tile that was taken back")
	_check(board.is_empty(1, 1), "cell is empty again after removing the blank tile")
	_check(GrowthRules.can_place(board, 1, 1), "the cell is placeable again once emptied")


func _test_stranded_blank_cannot_be_removed() -> void:
	var board := Board.new()
	# A blank tile can only ever be PLACED at an edge, but growth on the
	# *other* side afterward can strand it in the middle of the row —
	# removing it from there would tear a hole in an otherwise-contiguous
	# word, so that must be refused even though the tile is still blank.
	var shorten_tile := Tile.new("E", "", true)
	GrowthRules.place(board, 2, 4, shorten_tile)  # row empty: anywhere is legal
	shorten_tile.flip()  # now showing blank
	GrowthRules.place(board, 2, 5, Tile.new("A", "Z", true))  # suffix
	GrowthRules.place(board, 2, 3, Tile.new("S", "Z", true))  # prefix

	_check(board.leftmost_filled(2) == 3, "sanity: the blank is no longer at either edge")
	_check(
		board.rightmost_filled(2) == 5,
		"sanity: the row spans cols 3-5 with the blank in the middle"
	)
	_check(
		not GrowthRules.can_remove(board, 2, 4), "a blank stranded in the middle cannot be removed"
	)
	_check(GrowthRules.remove(board, 2, 4) == null, "...and remove() refuses it too")
