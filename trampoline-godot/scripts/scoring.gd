# Combines position scores (row_score.gd), the green-letter penalty, and
# the completion bonuses into the actual points a row/board is worth (see
# doc/RULES.md). Only dictionary-valid words score anything at all.
class_name Scoring
extends RefCounted

const LINE_COMPLETION_BONUS := 10
const SINGLE_COLOR_WORD_BONUS := 50
const MIXED_COLOR_WORD_BONUS := 30
const BOARD_COMPLETION_BONUS := 30
const GREEN_LETTER_PENALTY := 5

# One placement-order tracker per row, plus one "already paid?" flag per
# row for the line bonus, and a single flag for the whole-board bonus.
var _row_scores: Array[RowScore] = []
var _line_bonus_claimed: Array[bool] = []
var _board_bonus_claimed := false

# Snapshot of each row's word length, taken by `begin_turn`, so
# `claim_line_bonus` can tell apart the two different ways a row can reach
# 9 letters (confirmed by Maximilien — these are NOT the same bonus):
#   - Completing a word that already had letters on the board before the
#     current turn began: worth +10, regardless of color.
#   - Writing an entire 9-letter word within a single turn, starting from
#     a completely empty row — the equivalent of a Scrabble "bingo" —
#     earns more instead: +50 if entirely yellow, +30 if mixed color.
# Defaults to all zeros, so code that never calls `begin_turn` (tests,
# tools) treats every row as if it started the (single, implicit) turn
# empty — i.e. eligible for the bigger bonus.
var _word_length_at_turn_start: Array[int] = []


func _init() -> void:
	for row in range(Board.ROWS):
		_row_scores.append(RowScore.new())
		_line_bonus_claimed.append(false)
		_word_length_at_turn_start.append(0)


# Snapshots every row's current word length. Call this once, at the start
# of each player's turn, before they place or remove anything, so
# `claim_line_bonus` can later tell a from-scratch "scrabble" apart from
# completing a word that already existed.
func begin_turn(board: Board) -> void:
	for row in range(Board.ROWS):
		_word_length_at_turn_start[row] = board.word_columns(row).size()


# Records that a tile was just placed at `(row, col)` — see
# `RowScore.record_placement`. Call this once per successful placement,
# alongside `GrowthRules.place`.
func record_placement(row: int, col: int) -> int:
	return _row_scores[row].record_placement(col)


# The current score for a row's word: the sum of its letters' position
# scores, minus 5 points per green letter used. Scores 0 for an in-progress
# or wrong word — only a valid dictionary word contributes anything.
func word_score(board: Board, dictionary: FrenchDictionary, row: int) -> int:
	if not dictionary.is_word_valid(board.word_in_row(row)):
		return 0
	var columns := board.word_columns(row)
	var total := _row_scores[row].total_score(columns)
	total -= GREEN_LETTER_PENALTY * _green_letter_count(board, row, columns)
	return total


# Whether to award a line-completion bonus for `row` right now: only once
# per row, ever, and only once its word is a dictionary-valid 9 letters.
# Returns the bonus amount, or 0 if it isn't earned yet (or was already
# claimed). See `_word_length_at_turn_start` above for which of the two
# distinct bonuses this pays out.
func claim_line_bonus(board: Board, dictionary: FrenchDictionary, row: int) -> int:
	if _line_bonus_claimed[row]:
		return 0
	var columns := board.word_columns(row)
	if columns.size() != Board.COLUMNS:
		return 0
	if not dictionary.is_word_valid(board.word_in_row(row)):
		return 0
	_line_bonus_claimed[row] = true

	if _word_length_at_turn_start[row] > 0:
		return LINE_COMPLETION_BONUS

	if _green_letter_count(board, row, columns) == 0:
		return SINGLE_COLOR_WORD_BONUS
	return MIXED_COLOR_WORD_BONUS


# Whether to award the whole-board completion bonus right now: only once,
# ever, and only once every row is finished (see `Board.is_row_finished` —
# a row counts even if it was deliberately shortened, not just at 9
# letters). Returns the bonus amount, or 0 if not earned yet (or already
# claimed).
func claim_board_bonus(board: Board) -> int:
	if _board_bonus_claimed:
		return 0
	for row in range(Board.ROWS):
		if not board.is_row_finished(row):
			return 0
	_board_bonus_claimed = true
	return BOARD_COMPLETION_BONUS


func _green_letter_count(board: Board, row: int, columns: Array) -> int:
	var count := 0
	for col in columns:
		if board.tile_at(row, col).current_is_green():
			count += 1
	return count
