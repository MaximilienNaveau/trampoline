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
	_test_free_play_round_trip()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


# Moves a real tile matching `wanted` (a function taking a Tile, returning
# whether it fits) into player_id's hand, from the undrawn pile or another
# hand — used instead of TileBag.draw() so these tests can build exact
# words deterministically. Has to be a tile that's actually part of
# GameSession.pool (not a freshly-constructed one), since save/load
# identifies tiles by their fixed position in that pool, not by value.
# It searches the hands too: after the shuffle, every tile with a given
# letter may already have been dealt.
func _take_tile(session: GameSession, player_id: int, wanted: Callable) -> Tile:
	var places: Array = [session.bag.undrawn_tiles()]
	for other in range(session.turns.number_of_players):
		if other != player_id:
			places.append(session.bag.hand(other))
	for place in places:
		for tile in place:
			if wanted.call(tile):
				place.erase(tile)
				session.bag.hand(player_id).append(tile)
				return tile
	return null


func _test_round_trip_preserves_state() -> void:
	var session := GameSession.new(2, _dictionary)
	session.player_names = ["Alice", ""]

	var word := "MAISON"
	for i in range(word.length()):
		var letter := word[i]
		var tile := _take_tile(session, 0, func(t: Tile) -> bool: return t.front_letter() == letter)
		session.place_tile(4, i, tile)
	session.end_turn()  # player 0 scores 21; now player 1's turn

	# A tile in player 1's hand turned to its green face, so flip state
	# specifically is checked to round-trip, not just letters.
	var green_tile := _take_tile(session, 1, func(t: Tile) -> bool: return t.back_is_green())
	green_tile.flip()

	_check(SaveGame.save(session, _SAVE_PATH), "saving succeeds")

	var loaded := SaveGame.load(_SAVE_PATH, _dictionary)
	_check(loaded != null, "loading succeeds")

	_check(loaded.turns.number_of_players == 2, "player count round-trips")
	_check(loaded.board.word_at(4, 0) == "MAISON", "a placed word round-trips onto the board")
	_check(loaded.turns.player_score(0) == 21, "player 0's score round-trips")
	_check(loaded.turns.current_player_id() == 1, "whose turn it is round-trips")
	_check(loaded.bag.hand(1).size() == session.bag.hand(1).size(), "the hands round-trip")

	var index := session.pool.tiles.find(green_tile)
	_check(loaded.pool.tiles[index].current_is_green(), "a flipped tile's shown face round-trips")
	_check(loaded.bag.hand(1).has(loaded.pool.tiles[index]), "...and it's still in that hand")

	_check(loaded.player_names == ["Alice", ""], "the entered names round-trip")

	_check(loaded.turn_points() == 0, "the loaded turn starts from the board as saved")
	_check(
		loaded.turn_start.cells_to_keep_filled.size() == 6, "...with MAISON's cells to keep filled"
	)

	DirAccess.remove_absolute(_SAVE_PATH)


func _test_load_missing_file_returns_null() -> void:
	var loaded := SaveGame.load("user://this_file_does_not_exist.json", _dictionary)
	_check(loaded == null, "loading a missing file returns null instead of crashing")


# Free play's board round-trips, flip state included (see
# SaveGame.save_free_play); each crossword has a save file of its own.
func _test_free_play_round_trip() -> void:
	var session := FreePlaySession.new(_dictionary)
	var word := "MAISON"
	for i in range(word.length()):
		for tile in session.available_tiles():
			if tile.front_letter() == word[i]:
				session.place_tile(4, i, tile)
				break
	session.flip_tile(4, 0)
	_check(SaveGame.save_free_play(session, _SAVE_PATH), "free play: saving succeeds")
	var loaded := SaveGame.load_free_play(_SAVE_PATH, _dictionary)
	_check(loaded != null, "free play: loading succeeds")
	_check(
		loaded.board.word_at(4, 0) == session.board.word_at(4, 0),
		"free play: the board round-trips, the flipped tile included"
	)
	_check(loaded.current_score() == session.current_score(), "...into the same score")
	_check(
		(
			SaveGame.free_play_path(-1) != SaveGame.free_play_path(0)
			and SaveGame.free_play_path(0) != SaveGame.free_play_path(1)
		),
		"free play and each crossword save to their own file"
	)
	DirAccess.remove_absolute(_SAVE_PATH)
