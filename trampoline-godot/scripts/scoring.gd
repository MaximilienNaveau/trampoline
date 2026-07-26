# Combines position scores (row_score.gd), the green-letter penalty, and
# the completion bonuses into the actual points a row/board is worth (see
# doc/RULES.md). Only dictionary-valid words score anything at all.
#
# Position scores and the green-letter penalty are attributed PER PLAYER —
# whoever placed a letter gets that letter's score, and pays for it if
# it's green (see `player_word_score`). Bonuses are different: they're a
# lump sum paid to whoever completes the row, not split between everyone
# who contributed to it (see `claim_line_bonus`/`claim_board_bonus`) — so
# unlike position scores, this class doesn't need to know which player is
# claiming a bonus, only the caller does.
#
# A row can hold more than one separate word at once (see board.gd) —
# `player_word_score` is anchored at a specific column so it always scores
# ONE particular word, never accidentally blending two unrelated ones.
# `claim_line_bonus` doesn't need that anchor: reaching 9 letters fills an
# entire row, so at most one word per row can ever be a bonus candidate at
# a time.
class_name Scoring
extends RefCounted

const LINE_COMPLETION_BONUS := 10
const SINGLE_COLOR_WORD_BONUS := 50
const MIXED_COLOR_WORD_BONUS := 30
const BOARD_COMPLETION_BONUS := 30
const GREEN_LETTER_PENALTY := 5

# One placement-order/ownership tracker per row, plus one "already paid?"
# flag per row for the line bonus, and a single flag for the whole-board
# bonus.
var _row_scores: Array[RowScore] = []
var _line_bonus_claimed: Array[bool] = []
var _board_bonus_claimed := false

# Snapshot of how many cells were filled in each row, taken by
# `begin_turn`, so `claim_line_bonus` can tell apart the two different
# ways a row can reach 9 letters (confirmed by Maximilien — these are NOT
# the same bonus):
#   - Completing a word that already had letters on the board before the
#     current turn began: worth +10, regardless of color.
#   - Writing an entire 9-letter word within a single turn, starting from
#     a completely empty row — the equivalent of a Scrabble "bingo" —
#     earns more instead: +50 if entirely yellow, +30 if mixed color.
# Defaults to all zeros, so code that never calls `begin_turn` (tests,
# tools) treats every row as if it started the (single, implicit) turn
# empty — i.e. eligible for the bigger bonus.
var _filled_count_at_turn_start: Array[int] = []


func _init() -> void:
	for row in range(Board.ROWS):
		_row_scores.append(RowScore.new())
		_line_bonus_claimed.append(false)
		_filled_count_at_turn_start.append(0)


# Snapshots how many cells are currently filled in every row. Call this
# once, at the start of each player's turn, before they place or remove
# anything, so `claim_line_bonus` can later tell a from-scratch "scrabble"
# apart from completing a word that already existed.
func begin_turn(board: Board) -> void:
	for row in range(Board.ROWS):
		var filled := 0
		for col in range(Board.COLUMNS):
			if not board.is_empty(row, col):
				filled += 1
		_filled_count_at_turn_start[row] = filled


# Records that `player_id` just placed a tile at `(row, col)` — see
# `RowScore.record_placement`. Call this once per successful placement,
# alongside `GrowthRules.place`.
func record_placement(row: int, col: int, player_id: int) -> int:
	return _row_scores[row].record_placement(col, player_id)


# `player_id`'s share of the score of the word at `(row, col)`: the sum of
# THEIR letters' position scores, minus 5 points per green letter THEY
# placed. Scores 0 for an in-progress or wrong word — only a valid
# dictionary word contributes anything — and 0 for a player who
# contributed nothing to it.
func player_word_score(
	board: Board, dictionary: FrenchDictionary, row: int, col: int, player_id: int
) -> int:
	if not dictionary.is_word_valid(board.word_at(row, col)):
		return 0
	var columns := board.word_columns_at(row, col)
	var total := _row_scores[row].total_score_for_player(columns, player_id)
	total -= GREEN_LETTER_PENALTY * _player_green_letter_count(board, row, columns, player_id)
	return total


# Whether to award a line-completion bonus for `row` right now: only once
# per row, ever, and only once it holds exactly one word (not zero, not
# two — see board.gd) that's a dictionary-valid 9 letters. Returns the
# bonus amount, or 0 if it isn't earned yet (or was already claimed). See
# `_filled_count_at_turn_start` above for which of the two distinct
# bonuses this pays out. Unlike `player_word_score`, this isn't split per
# player — it's a lump sum for whoever completes the row; the caller (not
# this class) knows who that is.
func claim_line_bonus(board: Board, dictionary: FrenchDictionary, row: int) -> int:
	if _line_bonus_claimed[row]:
		return 0

	var segments := board.segments_in_row(row)
	if segments.size() != 1:
		return 0

	var col: int = segments[0][0]
	var columns := board.word_columns_at(row, col)
	var is_finished_valid_word := (
		columns.size() == Board.COLUMNS and dictionary.is_word_valid(board.word_at(row, col))
	)
	if not is_finished_valid_word:
		return 0

	_line_bonus_claimed[row] = true
	if _filled_count_at_turn_start[row] > 0:
		return LINE_COMPLETION_BONUS
	if _all_green_letter_count(board, row, columns) == 0:
		return SINGLE_COLOR_WORD_BONUS
	return MIXED_COLOR_WORD_BONUS


# Whether to award the whole-board completion bonus right now: only once,
# ever, and only once every row is finished (see `Board.is_complete` — a
# row counts even if it was deliberately shortened, not just at 9
# letters). Returns the bonus amount, or 0 if not earned yet (or already
# claimed).
func claim_board_bonus(board: Board) -> int:
	if _board_bonus_claimed or not board.is_complete():
		return 0
	_board_bonus_claimed = true
	return BOARD_COMPLETION_BONUS


# The position score/owner recorded for `(row, col)` — exposed so
# word_move.gd can carry a tile's earned score along when relocating it.
# Prefer `player_word_score`/`claim_line_bonus` for anything else; these
# two (plus `forget`/`restore_placement` below) exist purely to support
# moving a tile without it losing or regaining credit.
func score_at(row: int, col: int) -> int:
	return _row_scores[row].score_at(col)


func owner_at(row: int, col: int) -> int:
	return _row_scores[row].owner_at(col)


# Clears the score/owner recorded at `(row, col)` — call this on a tile's
# OLD position when relocating it (see word_move.gd), right before
# `restore_placement` on its new one.
func forget(row: int, col: int) -> void:
	_row_scores[row].forget(col)


# Restores a previously-recorded score/owner at `(row, col)` — call this
# on a tile's NEW position when relocating it (see word_move.gd), with the
# values read via `score_at`/`owner_at` before it was moved. Unlike
# `record_placement`, this does NOT draw a fresh score from the row's
# counter — the tile keeps whatever it already earned.
func restore_placement(row: int, col: int, score: int, player_id: int) -> void:
	_row_scores[row].restore(col, score, player_id)


# Exports every bit of this class's state (including all 13 RowScores) —
# see save_game.gd.
func export_state() -> Dictionary:
	var row_scores := []
	for row_score in _row_scores:
		row_scores.append(row_score.export_state())
	return {
		"row_scores": row_scores,
		"line_bonus_claimed": _line_bonus_claimed,
		"board_bonus_claimed": _board_bonus_claimed,
		"filled_count_at_turn_start": _filled_count_at_turn_start,
	}


# Overwrites this class's entire state from `data` (see `export_state`).
# See turn_manager.gd's restore_state for why numbers need an explicit
# `int(...)` cast after a JSON round-trip.
func restore_state(data: Dictionary) -> void:
	for row in range(Board.ROWS):
		_row_scores[row].restore_state(data["row_scores"][row])
	_line_bonus_claimed.clear()
	for claimed in data["line_bonus_claimed"]:
		_line_bonus_claimed.append(claimed)
	_board_bonus_claimed = data["board_bonus_claimed"]
	_filled_count_at_turn_start.clear()
	for filled in data["filled_count_at_turn_start"]:
		_filled_count_at_turn_start.append(int(filled))


# How many letters in `columns` are green, regardless of who placed them —
# used for the bonus, which cares about the whole word's color mix.
func _all_green_letter_count(board: Board, row: int, columns: Array) -> int:
	var count := 0
	for col in columns:
		if board.tile_at(row, col).current_is_green():
			count += 1
	return count


# How many of `player_id`'s own letters in `columns` are green — used for
# their personal green-letter penalty, which only charges you for your
# own risky letters, not your row-mates'.
func _player_green_letter_count(board: Board, row: int, columns: Array, player_id: int) -> int:
	var count := 0
	for col in columns:
		var owned_by_player := _row_scores[row].owner_at(col) == player_id
		if owned_by_player and board.tile_at(row, col).current_is_green():
			count += 1
	return count
