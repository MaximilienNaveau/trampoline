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
	_test_per_player_attribution()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


# Places `word` starting at column 0 of `row`, one plain-yellow tile per
# letter, recording each placement as `player_id`'s in left-to-right order.
func _place_word(board: Board, scoring: Scoring, row: int, word: String, player_id: int) -> void:
	for col in range(word.length()):
		board.place(row, col, Tile.new(word[col], "Z", true))
		scoring.record_placement(row, col, player_id)


func _test_word_score() -> void:
	var board := Board.new()
	var scoring := Scoring.new()

	_place_word(board, scoring, 0, "QSXWZY", 0)
	_check(
		scoring.player_word_score(board, _dictionary, 0, 0, 0) == 0,
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
		scoring.record_placement(1, col, 0)
	_check(
		board.word_at(1, 0) == "maison",
		"sanity: the word still reads correctly with a green letter"
	)
	_check(
		scoring.player_word_score(board, _dictionary, 1, 0, 0) == 16,
		"position sum minus the green-letter penalty"
	)


func _test_line_bonus() -> void:
	var board := Board.new()
	var scoring := Scoring.new()

	# A whole 9-letter word written in one turn, starting from an empty
	# row — a Scrabble-style "bingo" — entirely yellow: +50.
	_place_word(board, scoring, 2, "etudiante", 0)
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
	_place_word(board, scoring, 3, "etudiante", 0)
	var green_letter := Tile.new("Z", "i", true)
	board.remove(3, 4)
	board.place(3, 4, green_letter)
	green_letter.flip()
	_check(
		scoring.claim_line_bonus(board, _dictionary, 3) == 30,
		"a from-scratch 9-letter mixed-color word: scrabble bonus +30"
	)

	# 9 letters, but not a real word: no bonus, ever.
	_place_word(board, scoring, 4, "ZZZZZZZZZ", 0)
	_check(
		scoring.claim_line_bonus(board, _dictionary, 4) == 0,
		"9 letters of gibberish earns no bonus"
	)

	# A valid word, but shorter than 9 letters: no bonus yet.
	_place_word(board, scoring, 5, "maison", 0)
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
		scoring.record_placement(6, col, 0)
	scoring.begin_turn(board)
	for col in range(5, 9):
		board.place(6, col, Tile.new(word9[col], "Z", true))
		scoring.record_placement(6, col, 0)
	_check(board.word_at(6, 0) == word9, "sanity: the word completed correctly")
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
	_place_word(board, scoring, 12, "ZZZZZZZZZ", 0)

	_check(
		scoring.claim_board_bonus(board) == 30, "whole-board bonus once all 13 rows are finished"
	)
	_check(scoring.claim_board_bonus(board) == 0, "the whole-board bonus can't be claimed twice")


# Reproduces Maximilien's own worked example exactly: player 0 lays a
# 6-letter word ("etudia", scoring 1+2+3+4+5+6=21), and player 1 later
# completes it to 9 letters ("etudiante") with a 3-letter suffix ("nte"),
# scoring 7+8+9=24 for their own 3 letters — each player only sees their
# own contribution, and completing an already-started word earns the +10
# bonus rather than the from-scratch scrabble bonus. "etudia" happens to
# ALSO be a genuinely valid word on its own (the passé simple of
# "étudier" — "[il/elle] étudia", "he/she studied"), so player 0 already
# scores for it before player 1 ever touches the row.
func _test_per_player_attribution() -> void:
	var board := Board.new()
	var scoring := Scoring.new()

	var word9 := "etudiante"
	for col in range(6):
		board.place(7, col, Tile.new(word9[col], "Z", true))
		scoring.record_placement(7, col, 0)
	_check(
		scoring.player_word_score(board, _dictionary, 7, 0, 0) == 21,
		'sanity: "etudia" is already a valid 6-letter word, scoring for its author immediately'
	)

	scoring.begin_turn(board)  # row already has 6 letters when player 1's turn starts
	for col in range(6, 9):
		board.place(7, col, Tile.new(word9[col], "Z", true))
		scoring.record_placement(7, col, 1)
	_check(board.word_at(7, 0) == word9, "sanity: the word completed correctly")

	_check(
		scoring.player_word_score(board, _dictionary, 7, 0, 0) == 21,
		"player 0 is credited only for their own 6 letters: 1+2+3+4+5+6"
	)
	_check(
		scoring.player_word_score(board, _dictionary, 7, 0, 1) == 24,
		"player 1 is credited only for their own 3 letters: 7+8+9"
	)
	_check(
		scoring.claim_line_bonus(board, _dictionary, 7) == 10,
		"completing player 0's word earns the +10 bonus, not a scrabble bonus"
	)
