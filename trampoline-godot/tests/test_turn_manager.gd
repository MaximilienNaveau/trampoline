# Standalone checks for turn_manager.gd, runnable via:
#   godot4 --headless --path . --script tests/test_turn_manager.gd
extends SceneTree

var _failures := 0


func _check(condition: bool, message: String) -> void:
	if condition:
		print("PASS: ", message)
	else:
		print("FAIL: ", message)
		_failures += 1


func _initialize() -> void:
	_test_initial_state()
	_test_scores()
	_test_turn_advancement()
	_test_skips_finished_players()
	_test_lone_player_cycles_back_to_themselves()
	_test_game_completion_and_winner()
	_test_solo_play()
	_test_draw_for_player()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


func _test_initial_state() -> void:
	var turns := TurnManager.new(3)
	_check(turns.current_player_id() == 0, "play starts with player 0")
	_check(turns.player_score(0) == 0, "scores start at 0")
	_check(not turns.is_player_finished(0), "nobody starts finished")
	_check(not turns.is_game_complete(), "the game isn't complete at the start")
	_check(turns.winner_id() == -1, "no winner before the game is complete")


func _test_scores() -> void:
	var turns := TurnManager.new(2)
	turns.set_player_score(0, 21)
	turns.add_to_player_score(0, 4)
	_check(turns.player_score(0) == 25, "set then add composes correctly")
	_check(turns.all_player_scores() == [25, 0], "all_player_scores reflects both players")


func _test_turn_advancement() -> void:
	var turns := TurnManager.new(3)
	turns.end_turn()
	_check(turns.current_player_id() == 1, "turn advances to player 1")
	turns.end_turn()
	_check(turns.current_player_id() == 2, "then to player 2")
	turns.end_turn()
	_check(turns.current_player_id() == 0, "then wraps back around to player 0")


func _test_skips_finished_players() -> void:
	var turns := TurnManager.new(3)
	turns.set_player_finished(1, true)
	_check(
		turns.current_player_id() == 0,
		"marking a non-current player finished doesn't end anyone's turn"
	)
	turns.end_turn()
	_check(turns.current_player_id() == 2, "ending player 0's turn skips the finished player 1")


func _test_lone_player_cycles_back_to_themselves() -> void:
	var turns := TurnManager.new(3)
	# Finishing player 0 (the current player) auto-advances to player 1.
	turns.set_player_finished(0, true)
	_check(turns.current_player_id() == 1, "sanity: finishing player 0 hands the turn to player 1")
	# Finishing player 2 (NOT the current player) doesn't move anyone.
	turns.set_player_finished(2, true)
	_check(
		turns.current_player_id() == 1,
		"sanity: finishing a non-current player doesn't end anyone's turn"
	)

	# Player 1 is now the only one left; ending their turn should hand
	# play right back to them, not complete the game.
	turns.end_turn()
	_check(turns.current_player_id() == 1, "the lone unfinished player keeps getting the turn")
	_check(
		not turns.is_game_complete(), "...and the game isn't complete while they're still playing"
	)


func _test_game_completion_and_winner() -> void:
	var turns := TurnManager.new(2)
	turns.set_player_score(0, 10)
	turns.set_player_score(1, 99)
	turns.set_player_finished(0, true)
	_check(not turns.is_game_complete(), "one player finishing isn't enough to complete the game")
	turns.set_player_finished(1, true)
	_check(turns.is_game_complete(), "the game completes once every player has finished")
	_check(turns.winner_id() == 1, "the higher scorer wins")


func _test_solo_play() -> void:
	var turns := TurnManager.new(1)
	turns.end_turn()
	_check(
		turns.current_player_id() == 0, "solo play always hands the turn back to the only player"
	)
	_check(not turns.is_game_complete(), "not complete until that player finishes")
	turns.set_player_finished(0, true)
	_check(turns.is_game_complete(), "finishing the only player completes the game")
	_check(turns.winner_id() == 0, "the only player is the winner")


func _test_draw_for_player() -> void:
	var pool := TilePool.new()

	# 2 players: player 0 (the starter) draws 13+1=14 initially, player 1
	# draws 13. Both draw 9 every time after.
	var bag2 := TileBag.new(pool)
	var turns2 := TurnManager.new(2)
	_check(
		turns2.draw_for_player(bag2, 0).size() == 14,
		"2-player starter's initial draw includes the bonus tile"
	)
	_check(
		turns2.draw_for_player(bag2, 1).size() == 13,
		"2-player non-starter's initial draw has no bonus"
	)
	_check(
		turns2.draw_for_player(bag2, 0).size() == 9, "subsequent draws are 9, even for the starter"
	)

	# 3 players: nobody gets a bonus tile, everyone draws 12 initially.
	var bag3 := TileBag.new(pool)
	var turns3 := TurnManager.new(3)
	_check(turns3.draw_for_player(bag3, 0).size() == 12, "3-player initial draw has no bonus tile")
