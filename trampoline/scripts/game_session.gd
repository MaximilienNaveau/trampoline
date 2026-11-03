# Ties every other piece together into an actual playable turn loop:
# Board + TileBag + TurnManager + Scoring + FrenchDictionary +
# GrowthRules + WordMove + WordChallenge. The equivalent of the Unity
# project's GameControllerMultiplayer, but for the current design (see
# doc/RULES.md and this project's other scripts for the individual rules).
class_name GameSession
extends RefCounted

# How long a game lasts (see doc/RULES.md's "End of the game"): one hour
# of actual play. Once it's up, the current round is finished — every
# player gets the same number of turns — and the game ends.
const TIME_LIMIT_SECONDS := 3600.0

var board: Board
var pool: TilePool
var bag: TileBag
var turns: TurnManager
var scoring: Scoring
var dictionary: FrenchDictionary

# Seconds of actual play so far — only counted while someone is really
# playing (the caller decides, see main.gd's `_process`), and saved with
# the game (see save_game.gd).
var play_time := 0.0
# Whether the game ended because the hour ran out (see `end_turn`) —
# saved with the game too.
var ended_by_time := false
# Bonus points each player has won so far (line, full word, full board —
# see scoring.gd), indexed by player id. Kept apart from the words'
# points, which are recomputed from the board after every turn (see
# `_recompute_all_scores`), so a bonus won once stays won. Saved with the
# game (see save_game.gd).
var bonus_points: Array[int] = []

# Tiles the current player has placed since their turn began — these are
# still "in progress" and can be freely taken back or relocated (see
# `_can_take_back`/`move_own_tile` below), unlike a tile placed in an
# earlier, already-ended turn, which is frozen in place (only a whole word
# can be relocated afterward, via `move_word_by_grabbed_tile`). Also what `invalid_cells()`
# below checks against: a turn confined to one row and one connected group
# of letters (see doc/RULES.md) is a property of ALL of this turn's tiles
# together, not any single placement. Reset every `_start_turn()`.
var _placed_this_turn: Array[Tile] = []

# Tiles in the current player's hand they've chosen to give back to the
# bag when their turn ends, to draw others instead (see
# `set_marked_for_discard`), in the order they were marked.
# Reset every `_start_turn()`.
var _marked_for_discard: Array[Tile] = []


# `dictionary` can be injected (e.g. a test reusing one already-loaded
# instance across several sessions, since loading it re-reads and
# normalizes ~438,000 lines every time) — defaults to loading a fresh one.
func _init(number_of_players: int, a_dictionary: FrenchDictionary = null) -> void:
	board = Board.new()
	# Kept as its own field (not just handed to TileBag and forgotten) so
	# save_game.gd has one canonical, indexable list of all 117 tiles to
	# save/restore flip-state against, regardless of which of them are
	# currently on the board, in a hand, or still undrawn.
	pool = TilePool.new()
	bag = TileBag.new(pool)
	turns = TurnManager.new(number_of_players)
	scoring = Scoring.new()
	dictionary = a_dictionary if a_dictionary != null else FrenchDictionary.new()

	# Everyone is dealt a full hand up front, like a real board game — not
	# just whoever happens to go first. Their first turn's refill then
	# draws nothing, the hand being full already (see `_start_turn`).
	for player_id in range(number_of_players):
		turns.refill_hand(bag, player_id)
		bonus_points.append(0)
	scoring.begin_turn(board)


# Whether the game has ended, either way (see turn_manager.gd's header
# comment): every player exhausted, or the board itself fully settled.
func is_game_over() -> bool:
	return turns.is_game_complete() or board.is_complete() or ended_by_time


# Counts `seconds` more of actual play towards the hour (see
# TIME_LIMIT_SECONDS). Does nothing once the game is over.
func add_play_time(seconds: float) -> void:
	if not is_game_over():
		play_time += seconds


# Seconds of play left before the last round starts (0 once it has).
func remaining_time() -> float:
	return maxf(TIME_LIMIT_SECONDS - play_time, 0.0)


# Whether the hour is up and the current round is the last one.
func is_last_round() -> bool:
	return play_time >= TIME_LIMIT_SECONDS and not is_game_over()


# Whoever won once the game is over (empty while it's still in progress),
# in player order: the highest score; on equal scores, whoever has the
# fewest tiles left in hand; if that's equal too, they all win (see
# doc/RULES.md's "End of the game"). Doesn't reuse TurnManager.winner_id()
# — that one only looks at TurnManager's OWN "everyone exhausted" flag,
# which stays false if the game ended another way.
func winner_ids() -> Array[int]:
	var winners: Array[int] = []
	if not is_game_over():
		return winners
	for player_id in range(turns.number_of_players):
		var order := 1 if winners.is_empty() else _compare_players(player_id, winners[0])
		if order > 0:
			winners.clear()
		if order >= 0:
			winners.append(player_id)
	return winners


# Whether `a` finished ahead of `b` (1), behind (-1), or level (0): by
# score, then by fewest tiles left in hand.
func _compare_players(a: int, b: int) -> int:
	var score_a := turns.player_score(a)
	var score_b := turns.player_score(b)
	if score_a != score_b:
		return 1 if score_a > score_b else -1
	var tiles_a := bag.hand(a).size()
	var tiles_b := bag.hand(b).size()
	if tiles_a != tiles_b:
		return 1 if tiles_a < tiles_b else -1
	return 0


# Places one of the current player's own hand tiles at (row, col), as long
# as it's actually empty. Returns false and does nothing if `tile` isn't
# currently in the current player's hand, or the cell is already occupied.
# Deliberately does NOT check GrowthRules/TurnValidity here — a player is
# free to place tiles anywhere and rearrange all turn; `can_end_turn()`
# below is what actually catches an illegal result, the same way an
# invalid dictionary word already works (see word_challenge.gd). This lets
# the UI show the player what's wrong (see `invalid_cells()`) rather than
# just silently refusing to let a tile land somewhere.
func place_tile(row: int, col: int, tile: Tile) -> bool:
	var player_id := turns.current_player_id()
	if not bag.hand(player_id).has(tile):
		return false
	if not board.place(row, col, tile):
		return false
	bag.consume(player_id, tile)
	scoring.record_placement(row, col, player_id)
	_placed_this_turn.append(tile)
	# A tile on the board is no longer in hand to give back.
	_marked_for_discard.erase(tile)
	return true


# Marks `tile`, in the current player's hand, to be given back to the bag
# when their turn ends (`marked` true), or changes their mind (false).
# Their next turn then refills their hand with other tiles (see
# `_start_turn`). Returns false and does nothing if the tile isn't in the
# current player's hand.
func set_marked_for_discard(tile: Tile, marked: bool) -> bool:
	if not bag.hand(turns.current_player_id()).has(tile):
		return false
	_marked_for_discard.erase(tile)
	if marked:
		_marked_for_discard.append(tile)
	return true


# Every tile currently marked to be given back, in the order marked — a
# copy, so the caller can't change the marks behind this class's back.
func marked_for_discard() -> Array[Tile]:
	return _marked_for_discard.duplicate()


# Whether the tile at (row, col) can currently be picked back up — either
# because it's a blank sitting at its word's edge (the always-available
# "shorten" mechanic, see growth_rules.gd's `can_remove`), or because the
# CURRENT player placed it earlier in this very turn. Nothing is committed
# until the turn actually ends (see `_start_turn`), so a this-turn tile can
# be taken back from anywhere in its word, edge or middle — freely
# rearranging/rebuilding a word while it's still being composed is exactly
# what lets a player be more inventive, and `can_end_turn` is what actually
# guards the board once they're done. A real letter placed in an EARLIER
# turn matches neither case: it's frozen in place.
func _can_take_back(row: int, col: int, tile: Tile) -> bool:
	return GrowthRules.can_remove(board, row, col) or _placed_this_turn.has(tile)


# Whether the tile currently at (row, col) can be picked back up right now
# (see `_can_take_back` above) — false if the cell is empty.
func can_take_back(row: int, col: int) -> bool:
	var tile := board.tile_at(row, col)
	return tile != null and _can_take_back(row, col, tile)


# Picks the tile at (row, col) back up, if `can_take_back` allows it. The
# tile goes into the CURRENT player's hand — whoever performs the removal
# receives it, regardless of who originally placed it (assumption worth
# flagging: doc/RULES.md doesn't say where a picked-up tile should go;
# giving it to the remover, rather than back into the shared bag, was the
# simplest reasonable default). Returns null and does nothing if the
# removal isn't legal.
func remove_tile(row: int, col: int) -> Tile:
	var tile := board.tile_at(row, col)
	if tile == null or not _can_take_back(row, col, tile):
		return null
	board.remove(row, col)
	scoring.forget(row, col)
	bag.give_to_hand(turns.current_player_id(), tile)
	_placed_this_turn.erase(tile)
	return tile


# Relocates a single tile the current player placed earlier this same turn
# from (from_row, from_col) to (to_row, to_col) — see `_can_take_back`
# above for why an older, frozen tile can't move this way (use `move_word_by_grabbed_tile`
# for those instead). Returns false and does nothing if picking the tile up
# isn't legal, or the destination is already occupied — like `place_tile`,
# doesn't otherwise restrict WHERE it can land; `can_end_turn()` is what
# catches an illegal result.
func move_own_tile(from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	var tile := board.tile_at(from_row, from_col)
	if tile == null or not _can_take_back(from_row, from_col, tile):
		return false

	board.remove(from_row, from_col)
	if not board.place(to_row, to_col, tile):
		board.place(from_row, from_col, tile)  # rollback: destination occupied
		return false

	scoring.forget(from_row, from_col)
	scoring.record_placement(to_row, to_col, turns.current_player_id())
	return true


# Relocates the word at (from_row, from_col) so its first letter lands at
# (to_row, to_col) — see word_move.gd. Any player may move any word; its
# earned score and ownership travel with it. Returns false and does
# nothing if the move isn't legal.
func _move_word(from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	return WordMove.move(board, scoring, from_row, from_col, to_row, to_col)


# Same as `_move_word`, but for dragging a word by whichever one of its
# tiles the player actually grabbed, at (from_row, from_col) — (to_row,
# to_col) is where THAT tile should land, not necessarily the word's first
# letter. Without this adjustment, dragging the middle of a word would
# yank its first letter to the drop point instead of following the
# cursor. Returns false and does nothing if the source isn't part of a
# word, or if the resulting move isn't legal.
func move_word_by_grabbed_tile(from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	var source_columns := board.word_columns_at(from_row, from_col)
	if source_columns.is_empty():
		return false
	var grabbed_offset: int = source_columns.find(from_col)
	return _move_word(from_row, from_col, to_row, to_col - grabbed_offset)


# Flips the tile at (row, col) in place, revealing its other face — see
# doc/RULES.md's "trampoline" mechanic: a cell is never emptied by this,
# only changed, so (unlike `_can_take_back`) any player may flip any tile
# on the board at any time, regardless of whose turn it is or who placed
# it. Scoring doesn't need telling about this separately: `Scoring` always
# reads a tile's CURRENT face live (see scoring.gd), so a flipped tile's
# new letter/color is simply picked up the next time scores are
# recomputed. Returns false and does nothing if the cell is empty.
func flip_tile(row: int, col: int) -> bool:
	var tile := board.tile_at(row, col)
	if tile == null:
		return false
	tile.flip()
	return true


# Whether (row, col) is currently a legal spot to drop a new tile: simply
# whether it's empty. Placement itself no longer enforces GrowthRules (see
# `place_tile`'s own comment) — exposed so BoardView can ask through a
# Callable (see board_view.gd's `refresh`) instead of BoardCell hardcoding
# this itself.
func can_place_here(row: int, col: int) -> bool:
	return board.is_empty(row, col)


# Whether the current player may end their turn right now: blocked while
# any word on the board is invalid (see word_challenge.gd) OR this turn's
# own placements don't form one legal single-row, single-word turn (see
# turn_validity.gd).
func can_end_turn() -> bool:
	return WordChallenge.can_end_turn(board, dictionary) and invalid_cells().is_empty()


# The word(s) currently blocking `can_end_turn()` above, as a list of
# `{row, col, word}` dictionaries (see word_challenge.gd's
# `find_invalid_words`) — lets the UI explain WHY `end_turn()` just
# refused, rather than only reporting that it did.
func invalid_words() -> Array:
	return WordChallenge.find_invalid_words(board, dictionary)


# The cell(s) currently blocking `can_end_turn()` for the OTHER reason (see
# turn_validity.gd) — this turn's own placements spread across more than
# one row, or split into more than one disconnected group of letters.
# Empty if this turn, so far, is legal on this front (independent of
# whether a word elsewhere on the board also happens to be invalid).
func invalid_cells() -> Array:
	return TurnValidity.invalid_cells(board, _placed_this_turn)


# What the current player's score would change by, from their words
# alone, if their turn ended right now — negative if, say, they flipped
# one of their own letters to green (see scoring.gd). Bonuses come
# separately, see `turn_bonuses`. Changes nothing.
func turn_points() -> int:
	var player_id := turns.current_player_id()
	return _word_points(player_id) + bonus_points[player_id] - turns.player_score(player_id)


# The bonuses the current player would win if their turn ended right now
# (see scoring.gd: +10 a line, +30/+50 a full word, +30 the board), one
# entry each, rows top to bottom then the board's. Changes nothing.
func turn_bonuses() -> Array[int]:
	var bonuses: Array[int] = []
	for row in range(Board.ROWS):
		var bonus := scoring.line_bonus(board, dictionary, row)
		if bonus > 0:
			bonuses.append(bonus)
	if scoring.board_bonus(board) > 0:
		bonuses.append(scoring.board_bonus(board))
	return bonuses


# Ends the current player's turn: tallies every player's current score,
# pays out any bonuses just earned, marks the player finished if they're
# out of tiles for good, and starts the next turn if the game isn't over.
# Returns false and does nothing if `can_end_turn()` refuses.
func end_turn() -> bool:
	if not can_end_turn():
		return false

	var player_id := turns.current_player_id()
	for row in range(Board.ROWS):
		bonus_points[player_id] += scoring.claim_line_bonus(board, dictionary, row)
	bonus_points[player_id] += scoring.claim_board_bonus(board)
	_recompute_all_scores()

	# The tiles this player chose to give back go into the bag now; their
	# hand is refilled at the start of their next turn, so the next player
	# never sees the new tiles.
	for tile in _marked_for_discard:
		bag.return_tile(player_id, tile)
	_marked_for_discard.clear()

	if bag.hand(player_id).is_empty() and bag.remaining_count() == 0:
		turns.set_player_finished(player_id, true)  # advances the turn on its own
	else:
		turns.end_turn()

	# Once the hour is up, the game ends when the round does: when play
	# wraps back around (the next player comes before, or is, this one).
	if play_time >= TIME_LIMIT_SECONDS and turns.current_player_id() <= player_id:
		ended_by_time = true

	if not is_game_over():
		_start_turn()
	return true


func _start_turn() -> void:
	turns.refill_hand(bag, turns.current_player_id())
	scoring.begin_turn(board)
	# Whatever the PREVIOUS player placed is now a done deal — frozen in
	# place for anyone else, movable only as a whole word (see
	# `_can_take_back`). The new current player starts with a clean slate.
	_placed_this_turn.clear()
	_marked_for_discard.clear()


# Recomputes every player's score from scratch — the sum of their own
# share of every word currently on the board (see
# Scoring.player_word_score), across every row and every segment within
# it, plus the bonuses they've won (see `bonus_points`). Simpler and less
# error-prone than incrementally patching scores as the board changes, at
# the cost of redoing the sum each time.
func _recompute_all_scores() -> void:
	for player_id in range(turns.number_of_players):
		turns.set_player_score(player_id, _word_points(player_id) + bonus_points[player_id])


# `player_id`'s share of every valid word on the board right now.
func _word_points(player_id: int) -> int:
	var total := 0
	for row in range(Board.ROWS):
		for segment in board.segments_in_row(row):
			var col: int = segment[0]
			total += scoring.player_word_score(board, dictionary, row, col, player_id)
	return total
