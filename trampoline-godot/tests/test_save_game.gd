# Standalone checks for save_game.gd, runnable via:
#   godot4 --headless --path . --script tests/test_save_game.gd
extends SceneTree

# "user://" is Godot's writable per-user data directory — safe to write
# to from any machine this test runs on, unlike a path hardcoded into one
# particular session's scratch directory.
const _SAVE_PATH := "user://test_save_game_output.json"

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

	_test_round_trip_preserves_state()
	_test_load_missing_file_returns_null()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


# Moves a real tile whose YELLOW face is `letter` out of the undrawn pile
# and into player_id's hand — used instead of TileBag.draw() so these
# tests can build exact words deterministically. Has to be a tile that's
# actually part of GameSession.pool (not a freshly-constructed one),
# since save/load identifies tiles by their fixed position in that pool,
# not by value.
func _draw_specific_front(session: GameSession, player_id: int, letter: String) -> Tile:
	var undrawn := session.bag.undrawn_tiles()
	for tile in undrawn:
		if tile.front_letter() == letter:
			undrawn.erase(tile)
			session.bag.hand(player_id).append(tile)
			return tile
	return null


# Same idea, but finds a tile by its GREEN back face instead — used to
# test that flip state round-trips, not just which letter a tile shows.
func _draw_specific_green_back(session: GameSession, player_id: int, letter: String) -> Tile:
	var undrawn := session.bag.undrawn_tiles()
	for tile in undrawn:
		if tile.back_letter() == letter and tile.back_is_green():
			undrawn.erase(tile)
			session.bag.hand(player_id).append(tile)
			return tile
	return null


func _test_round_trip_preserves_state() -> void:
	var session := GameSession.new(2, _dictionary)

	var word := "MAISON"
	for i in range(word.length()):
		var tile := _draw_specific_front(session, 0, word[i])
		session.place_tile(4, i, tile)
	session.end_turn()  # player 0 scores 21; now player 1's turn

	# A flipped (green-showing) tile elsewhere, so flip state specifically
	# is checked to round-trip, not just letters.
	var green_tile := _draw_specific_green_back(session, 1, "I")
	session.place_tile(5, 0, green_tile)
	green_tile.flip()

	_check(SaveGame.save(session, _SAVE_PATH), "saving succeeds")

	var loaded := SaveGame.load(_SAVE_PATH, _dictionary)
	_check(loaded != null, "loading succeeds")

	_check(loaded.turns.number_of_players == 2, "player count round-trips")
	_check(loaded.board.word_at(4, 0) == "MAISON", "a placed word round-trips onto the board")
	_check(loaded.turns.player_score(0) == 21, "player 0's score round-trips")
	_check(loaded.turns.current_player_id() == 1, "whose turn it is round-trips")

	var loaded_green_tile := loaded.board.tile_at(5, 0)
	_check(loaded_green_tile.current_letter() == "I", "a flipped tile's shown face round-trips")
	_check(loaded_green_tile.current_is_green(), "...including that it counts as green")

	_check(
		loaded.scoring.player_word_score(loaded.board, _dictionary, 4, 0, 0) == 21,
		"scoring state round-trips well enough to recompute the same score"
	)

	DirAccess.remove_absolute(_SAVE_PATH)


func _test_load_missing_file_returns_null() -> void:
	var loaded := SaveGame.load("user://this_file_does_not_exist.json", _dictionary)
	_check(loaded == null, "loading a missing file returns null instead of crashing")
