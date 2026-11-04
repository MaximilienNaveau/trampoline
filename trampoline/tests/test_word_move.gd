# Standalone checks for word_move.gd, runnable via:
#   godot4 --headless --path . --script tests/test_word_move.gd
extends SceneTree

var _failures := 0


func _check(condition: bool, message: String) -> void:
	if condition:
		print("PASS: ", message)
	else:
		print("FAIL: ", message)
		_failures += 1


func _initialize() -> void:
	_test_shift_within_row()
	_test_move_to_an_empty_row()
	_test_move_alongside_another_word()
	_test_bounds_and_missing_source()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


# Places `word` starting at `col` of `row`, one plain-yellow tile per
# letter.
func _place_word(board: Board, row: int, col: int, word: String) -> void:
	for i in range(word.length()):
		board.place(row, col + i, Tile.new(word[i], "Z", true))


func _test_shift_within_row() -> void:
	var board := Board.new()
	_place_word(board, 0, 2, "CAT")

	_check(WordMove.can_move(board, 0, 2, 0, 0), "shifting left within the row is legal")
	_check(WordMove.move(board, 0, 2, 0, 0), "the shift succeeds")
	_check(board.word_at(0, 0) == "CAT", "the word reads correctly at its new position")
	_check(board.is_empty(0, 3) and board.is_empty(0, 4), "the vacated cells are empty again")


func _test_move_to_an_empty_row() -> void:
	var board := Board.new()
	_place_word(board, 1, 0, "DOG")

	_check(WordMove.move(board, 1, 0, 5, 4), "moving to a different, empty row succeeds")
	_check(board.word_at(5, 4) == "DOG", "the word reads correctly in its new row")
	_check(board.word_at(1, 0) == "", "the original row is empty again")


func _test_move_alongside_another_word() -> void:
	var board := Board.new()
	_place_word(board, 2, 0, "FOX")
	_place_word(board, 3, 0, "CAT")

	_check(
		not WordMove.can_move(board, 3, 0, 2, 2),
		"a destination overlapping FOX's own cells is refused"
	)
	_check(WordMove.can_move(board, 3, 0, 2, 5), "a non-overlapping destination is legal")
	_check(WordMove.move(board, 3, 0, 2, 5), "the move succeeds")

	_check(
		board.segments_in_row(2) == [[0, 2], [5, 7]],
		"the row now holds two separate, coexisting words"
	)
	_check(board.word_at(2, 0) == "FOX", "the original word is untouched")
	_check(board.word_at(2, 6) == "CAT", "the relocated word reads correctly alongside it")


func _test_bounds_and_missing_source() -> void:
	var board := Board.new()
	_place_word(board, 4, 6, "CAT")

	_check(
		not WordMove.can_move(board, 4, 6, 4, 7),
		"shifting right would push a letter past the last column"
	)
	_check(
		not WordMove.can_move(board, 4, 0, 4, 3),
		"there's no word anchored at an empty column to move"
	)
