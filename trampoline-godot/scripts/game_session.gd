# Ties every other piece together into an actual playable turn loop:
# Board + TileBag + TurnManager + Scoring + FrenchDictionary +
# GrowthRules + WordMove + WordChallenge. The equivalent of the Unity
# project's GameControllerMultiplayer, but for the current design (see
# doc/RULES.md and this project's other scripts for the individual rules).
class_name GameSession
extends RefCounted

var board: Board
var bag: TileBag
var turns: TurnManager
var scoring: Scoring
var dictionary: FrenchDictionary


# `dictionary` can be injected (e.g. a test reusing one already-loaded
# instance across several sessions, since loading it re-reads and
# normalizes ~79000 lines every time) — defaults to loading a fresh one.
func _init(number_of_players: int, a_dictionary: FrenchDictionary = null) -> void:
	board = Board.new()
	bag = TileBag.new(TilePool.new())
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
	return true


# Picks a currently-blank-showing tile at (row, col) back up (see
# growth_rules.gd's `can_remove` — a real letter can never be removed this
# way, only a blank one, and only from its own word's edge). The tile goes
# into the CURRENT player's hand — whoever performs the removal receives
# it, regardless of who originally placed it (assumption worth flagging:
# doc/RULES.md doesn't say where a picked-up tile should go; giving it to
# the remover, rather than back into the shared bag, was the simplest
# reasonable default). Returns null and does nothing if the removal isn't
# legal.
func remove_tile(row: int, col: int) -> Tile:
	var tile := GrowthRules.remove(board, row, col)
	if tile == null:
		return null
	scoring.forget(row, col)
	bag.give_to_hand(turns.current_player_id(), tile)
	return tile


# Relocates the word at (from_row, from_col) so its first letter lands at
# (to_row, to_col) — see word_move.gd. Any player may move any word; its
# earned score and ownership travel with it. Returns false and does
# nothing if the move isn't legal.
func move_word(from_row: int, from_col: int, to_row: int, to_col: int) -> bool:
	return WordMove.move(board, scoring, from_row, from_col, to_row, to_col)


# Whether the current player may end their turn right now (see
# word_challenge.gd — blocked while any word on the board is invalid).
func can_end_turn() -> bool:
	return WordChallenge.can_end_turn(board, dictionary)


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
