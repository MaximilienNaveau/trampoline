# Ties every other piece together into an actual playable turn loop:
# Board + TileBag + TurnManager + Scoring + FrenchDictionary +
# GrowthRules + WordMove + WordChallenge. The equivalent of the Unity
# project's GameControllerMultiplayer, but for the current design (see
# doc/RULES.md and this project's other scripts for the individual rules).
class_name GameSession
extends RefCounted

var board: Board
var pool: TilePool
var bag: TileBag
var turns: TurnManager
var scoring: Scoring
var dictionary: FrenchDictionary

# Tiles the current player has placed since their turn began — these are
# still "in progress" and can be freely taken back or relocated (see
# `_can_take_back`/`move_own_tile` below), unlike a tile placed in an
# earlier, already-ended turn, which is frozen in place (only a whole word
# can be relocated afterward, via `move_word`). Reset every `_start_turn()`.
var _placed_this_turn: Array[Tile] = []


# `dictionary` can be injected (e.g. a test reusing one already-loaded
# instance across several sessions, since loading it re-reads and
# normalizes ~79000 lines every time) — defaults to loading a fresh one.
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

	# Everyone is dealt their initial hand up front, like a real board
	# game — not just whoever happens to go first. draw_for_player tracks
	# each player's draw count independently, so calling it once per
	# player here correctly gives each of them their OWN initial batch
	# (see draw_plan.gd), regardless of turn order.
	for player_id in range(number_of_players):
		turns.draw_for_player(bag, player_id)
	scoring.begin_turn(board)


# Whether the game has ended, either way (see turn_manager.gd's header
# comment): every player exhausted, or the board itself fully settled.
func is_game_over() -> bool:
	return turns.is_game_complete() or board.is_complete()


# The player with the highest score once the game is over, or -1 while
# it's still in progress. Doesn't reuse TurnManager.winner_id() — that one
# only looks at TurnManager's OWN "everyone exhausted" flag, which stays
# false if the game instead ended because the board filled up.
func winner_id() -> int:
	if not is_game_over():
		return -1
	var scores := turns.all_player_scores()
	var best := 0
	for player_id in range(1, scores.size()):
		if scores[player_id] > scores[best]:
			best = player_id
	return best


# Places one of the current player's own hand tiles at (row, col), if
# GrowthRules allows it there. Returns false and does nothing if `tile`
# isn't currently in the current player's hand, or if the placement
# itself isn't legal.
func place_tile(row: int, col: int, tile: Tile) -> bool:
	var player_id := turns.current_player_id()
	if not bag.hand(player_id).has(tile):
		return false
	if not GrowthRules.place(board, row, col, tile):
		return false
	bag.consume(player_id, tile)
	scoring.record_placement(row, col, player_id)
	_placed_this_turn.append(tile)
	return true


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
# above for why an older, frozen tile can't move this way (use `move_word`
# for those instead). Returns false and does nothing if picking the tile up
# isn't legal, or if GrowthRules refuses the destination.
func move_own_tile(from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	var tile := board.tile_at(from_row, from_col)
	if tile == null or not _can_take_back(from_row, from_col, tile):
		return false

	board.remove(from_row, from_col)
	if not GrowthRules.place(board, to_row, to_col, tile):
		board.place(from_row, from_col, tile)  # rollback: destination refused
		return false

	scoring.forget(from_row, from_col)
	scoring.record_placement(to_row, to_col, turns.current_player_id())
	return true


# Relocates the word at (from_row, from_col) so its first letter lands at
# (to_row, to_col) — see word_move.gd. Any player may move any word; its
# earned score and ownership travel with it. Returns false and does
# nothing if the move isn't legal.
func move_word(from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	return WordMove.move(board, scoring, from_row, from_col, to_row, to_col)


# Same as `move_word`, but for dragging a word by whichever one of its
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
	return move_word(from_row, from_col, to_row, to_col - grabbed_offset)


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


# Whether the current player may end their turn right now (see
# word_challenge.gd — blocked while any word on the board is invalid).
func can_end_turn() -> bool:
	return WordChallenge.can_end_turn(board, dictionary)


# The word(s) currently blocking `can_end_turn()` above, as a list of
# `{row, col, word}` dictionaries (see word_challenge.gd's
# `find_invalid_words`) — lets the UI explain WHY `end_turn()` just
# refused, rather than only reporting that it did.
func invalid_words() -> Array:
	return WordChallenge.find_invalid_words(board, dictionary)


# Ends the current player's turn: tallies every player's current score,
# pays out any bonuses just earned, marks the player finished if they're
# out of tiles for good, and starts the next turn if the game isn't over.
# Returns false and does nothing if `can_end_turn()` refuses.
func end_turn() -> bool:
	if not can_end_turn():
		return false

	var player_id := turns.current_player_id()
	_recompute_all_scores()
	for row in range(Board.ROWS):
		turns.add_to_player_score(player_id, scoring.claim_line_bonus(board, dictionary, row))
	turns.add_to_player_score(player_id, scoring.claim_board_bonus(board))

	if bag.hand(player_id).is_empty() and not turns.has_more_scheduled_draws(player_id):
		turns.set_player_finished(player_id, true)  # advances the turn on its own
	else:
		turns.end_turn()

	if not is_game_over():
		_start_turn()
	return true


func _start_turn() -> void:
	turns.draw_for_player(bag, turns.current_player_id())
	scoring.begin_turn(board)
	# Whatever the PREVIOUS player placed is now a done deal — frozen in
	# place for anyone else, movable only as a whole word (see
	# `_can_take_back`). The new current player starts with a clean slate.
	_placed_this_turn.clear()


# Recomputes every player's score from scratch — the sum of their own
# share of every word currently on the board (see
# Scoring.player_word_score), across every row and every segment within
# it. Simpler and less error-prone than incrementally patching scores as
# the board changes, at the cost of redoing the sum each time.
func _recompute_all_scores() -> void:
	for player_id in range(turns.number_of_players):
		var total := 0
		for row in range(Board.ROWS):
			for segment in board.segments_in_row(row):
				var col: int = segment[0]
				total += scoring.player_word_score(board, dictionary, row, col, player_id)
		turns.set_player_score(player_id, total)
