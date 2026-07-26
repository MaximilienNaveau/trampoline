# Standalone checks for scoring.gd, runnable via:
#   godot4 --headless --path . --script tests/test_scoring.gd
extends SceneTree

var _failures := 0
var _dictionary: FrenchDictionary


func _check(condition: bool, message: String) -> void:
	if condition:
		print("PASS: ", message)
	else:
		print("FAIL: ", message)
		_failures += 1


func _initialize() -> void:
	_dictionary = FrenchDictionary.new()

	_test_word_score()
	_test_line_bonus()
	_test_board_bonus()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


# Places `word` starting at column 0 of `row`, one plain-yellow tile per
# letter, recording each placement with `scoring` in left-to-right order.
func _place_word(board: Board, scoring: Scoring, row: int, word: String) -> void:
	for col in range(word.length()):
		board.place(row, col, Tile.new(word[col], "Z", true))
		scoring.record_placement(row, col)


func _test_word_score() -> void:
	var board := Board.new()
	var scoring := Scoring.new()

	_place_word(board, scoring, 0, "QSXWZY")
	_check(
		scoring.word_score(board, _dictionary, 0) == 0,
		"gibberish scores 0, even with position scores recorded"
	)

	# "maison" (6 letters, confirmed valid) with its 4th letter ('s') on a
	# green face: score = (1+2+3+4+5+6) - 5*1 = 16. Built one column at a
	# time (rather than via `_place_word`) so col 3's tile can be
	# constructed with its green face already showing "s" from the start —
	# recording a placement twice for the same column would give it a
	# fresh, later position score instead of preserving this one.
	var word := "maison"
	for col in range(word.length()):
		var tile: Tile
		if col == 3:
			tile = Tile.new("Z", "s", true)
			tile.flip()  # show the green "s" face instead of the dummy front
		else:
			tile = Tile.new(word[col], "Z", true)
		board.place(1, col, tile)
		scoring.record_placement(1, col)
	_check(
		board.word_in_row(1) == "maison",
		"sanity: the word still reads correctly with a green letter"
	)
	_check(
		scoring.word_score(board, _dictionary, 1) == 16,
		"position sum minus the green-letter penalty"
	)


func _test_line_bonus() -> void:
	var board := Board.new()
	var scoring := Scoring.new()

	# A whole 9-letter word written in one turn, starting from an empty
	# row — a Scrabble-style "bingo" — entirely yellow: +50.
	_place_word(board, scoring, 2, "etudiante")
	_check(
		scoring.claim_line_bonus(board, _dictionary, 2) == 50,
		"a from-scratch 9-letter all-yellow word: scrabble bonus +50"
	)
	_check(
		scoring.claim_line_bonus(board, _dictionary, 2) == 0,
		"the same row's bonus can't be claimed twice"
	)

	# Same again, but with one letter forced green: the mixed-color
	# scrabble bonus, +30.
	_place_word(board, scoring, 3, "etudiante")
	var green_letter := Tile.new("Z", "i", true)
	board.remove(3, 4)
	board.place(3, 4, green_letter)
	green_letter.flip()
	_check(
		scoring.claim_line_bonus(board, _dictionary, 3) == 30,
		"a from-scratch 9-letter mixed-color word: scrabble bonus +30"
	)

	# 9 letters, but not a real word: no bonus, ever.
	_place_word(board, scoring, 4, "ZZZZZZZZZ")
	_check(
		scoring.claim_line_bonus(board, _dictionary, 4) == 0,
		"9 letters of gibberish earns no bonus"
	)

	# A valid word, but shorter than 9 letters: no bonus yet.
	_place_word(board, scoring, 5, "maison")
	_check(
		scoring.claim_line_bonus(board, _dictionary, 5) == 0,
		"a valid but not-yet-9-letter word earns no bonus"
	)

	# Completing a word that already had letters on the board before this
	# turn began: +10 only, never the bigger scrabble bonus, regardless of
	# color. First 5 letters of "etudiante" go down, THEN begin_turn snapshots
	# the row as already non-empty, THEN the remaining 4 letters complete it.
	var word9 := "etudiante"
	for col in range(5):
		board.place(6, col, Tile.new(word9[col], "Z", true))
		scoring.record_placement(6, col)
	scoring.begin_turn(board)
	for col in range(5, 9):
		board.place(6, col, Tile.new(word9[col], "Z", true))
		scoring.record_placement(6, col)
	_check(board.word_in_row(6) == word9, "sanity: the word completed correctly")
	_check(
		scoring.claim_line_bonus(board, _dictionary, 6) == 10,
		"completing a word that already existed before this turn: +10 only"
	)


func _test_board_bonus() -> void:
	var board := Board.new()
	var scoring := Scoring.new()

	# Rows 0-11: a single real letter capped by a blank tile right after
	# it — "finished" per Board.is_row_finished without needing 9 letters.
	for row in range(12):
		board.place(row, 0, Tile.new("X", "Z", true))
		var shorten_tile := Tile.new("Y", "", true)
		board.place(row, 1, shorten_tile)
		shorten_tile.flip()

	_check(
		scoring.claim_board_bonus(board) == 0,
		"bonus not earned until every one of the 13 rows is finished"
	)

	# Row 12: a full 9 letters (doesn't need to be a real word for the
	# board itself to count as physically complete).
	_place_word(board, scoring, 12, "ZZZZZZZZZ")

	_check(
		scoring.claim_board_bonus(board) == 30, "whole-board bonus once all 13 rows are finished"
	)
	_check(scoring.claim_board_bonus(board) == 0, "the whole-board bonus can't be claimed twice")
