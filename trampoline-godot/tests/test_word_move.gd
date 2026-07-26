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
# letter, recording each placement as `player_id`'s.
func _place_word(
	board: Board, scoring: Scoring, row: int, col: int, word: String, player_id: int
) -> void:
	for i in range(word.length()):
		board.place(row, col + i, Tile.new(word[i], "Z", true))
		scoring.record_placement(row, col + i, player_id)


func _test_shift_within_row() -> void:
	var board := Board.new()
	var scoring := Scoring.new()
	_place_word(board, scoring, 0, 2, "CAT", 0)  # scores 1, 2, 3 at cols 2, 3, 4

	_check(WordMove.can_move(board, 0, 2, 0, 0), "shifting left within the row is legal")
	_check(WordMove.move(board, scoring, 0, 2, 0, 0), "the shift succeeds")

	_check(board.word_at(0, 0) == "CAT", "the word reads correctly at its new position")
	_check(board.is_empty(0, 3) and board.is_empty(0, 4), "the vacated cells are empty again")

	_check(scoring.score_at(0, 0) == 1, "the first letter's score traveled with it")
	_check(scoring.score_at(0, 1) == 2, "...the second letter's too")
	_check(scoring.score_at(0, 2) == 3, "...and the third")
	_check(scoring.owner_at(0, 0) == 0, "ownership traveled with the tiles too")
	_check(scoring.score_at(0, 3) == 0, "the old columns forgot their scores")
	_check(scoring.owner_at(0, 3) == -1, "...and their ownership")


func _test_move_to_an_empty_row() -> void:
	var board := Board.new()
	var scoring := Scoring.new()
	_place_word(board, scoring, 1, 0, "DOG", 0)  # scores 1, 2, 3 at cols 0, 1, 2

	_check(WordMove.move(board, scoring, 1, 0, 5, 4), "moving to a different, empty row succeeds")
	_check(board.word_at(5, 4) == "DOG", "the word reads correctly in its new row")
	_check(board.word_at(1, 0) == "", "the original row is empty again")
	_check(scoring.score_at(5, 4) == 1, "scores moved to the new row's own tracker")
	_check(scoring.score_at(1, 0) == 0, "...and are gone from the old row's tracker")


func _test_move_alongside_another_word() -> void:
	var board := Board.new()
	var scoring := Scoring.new()
	_place_word(board, scoring, 2, 0, "FOX", 1)  # already there, player 1
	_place_word(board, scoring, 3, 0, "CAT", 0)  # about to be relocated, player 0

	_check(
		not WordMove.can_move(board, 3, 0, 2, 2),
		"a destination overlapping FOX's own cells is refused"
	)
	_check(WordMove.can_move(board, 3, 0, 2, 5), "a non-overlapping destination is legal")
	_check(WordMove.move(board, scoring, 3, 0, 2, 5), "the move succeeds")

	_check(
		board.segments_in_row(2) == [[0, 2], [5, 7]],
		"the row now holds two separate, coexisting words"
	)
	_check(board.word_at(2, 0) == "FOX", "the original word is untouched")
	_check(board.word_at(2, 6) == "CAT", "the relocated word reads correctly alongside it")
	_check(scoring.owner_at(2, 0) == 1, "FOX's ownership is unaffected by the unrelated move")
	_check(scoring.owner_at(2, 6) == 0, "CAT keeps its own ownership after moving")


func _test_bounds_and_missing_source() -> void:
	var board := Board.new()
	var scoring := Scoring.new()
	_place_word(board, scoring, 4, 6, "CAT", 0)  # cols 6, 7, 8 — flush against the edge

	_check(
		not WordMove.can_move(board, 4, 6, 4, 7),
		"shifting right would push a letter past the last column"
	)
	_check(
		not WordMove.can_move(board, 4, 0, 4, 3),
		"there's no word anchored at an empty column to move"
	)
