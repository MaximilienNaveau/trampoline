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
	_test_free_play_round_trip_preserves_state()
	_test_free_play_round_trip_with_no_puzzle()
	_test_load_free_play_rejects_a_turn_based_save()

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


# Free play has no bag/hand — every one of its 117 tiles is already
# reachable straight off `pool.tiles`, so this finds one by its front
# letter directly, rather than drawing/removing from anywhere first (no
# equivalent of _draw_specific_front's bag bookkeeping needed).
func _find_free_play_tile(session: FreePlaySession, letter: String) -> Tile:
	for tile in session.pool.tiles:
		if tile.front_letter() == letter:
			return tile
	return null


func _test_free_play_round_trip_preserves_state() -> void:
	var session := FreePlaySession.new(_dictionary)
	var word := "MAISON"
	for i in range(word.length()):
		session.place_tile(4, i, _find_free_play_tile(session, word[i]))

	_check(SaveGame.save_free_play(session, _SAVE_PATH, 3), "saving succeeds")

	var loaded = SaveGame.load_free_play(_SAVE_PATH, _dictionary)
	_check(loaded != null, "loading succeeds")
	_check(loaded["session"].board.word_at(4, 0) == "MAISON", "the board round-trips")
	_check(loaded["puzzle_index"] == 3, "the active puzzle index round-trips")
	_check(loaded["session"].current_score() == 21, "scoring state round-trips into the same score")

	DirAccess.remove_absolute(_SAVE_PATH)


# -1 means "plain free play, no crossword puzzle active" (see
# free_play_session.gd/crossword_puzzle.gd) — confirmed separately from
# the puzzle-index-3 case above since 0 would also be a valid puzzle
# index, unlike -1.
func _test_free_play_round_trip_with_no_puzzle() -> void:
	var session := FreePlaySession.new(_dictionary)
	_check(
		SaveGame.save_free_play(session, _SAVE_PATH),
		"saving with the default puzzle_index succeeds"
	)

	var loaded = SaveGame.load_free_play(_SAVE_PATH, _dictionary)
	_check(loaded["puzzle_index"] == -1, "no active puzzle round-trips as -1")

	DirAccess.remove_absolute(_SAVE_PATH)


# A turn-based save has no "puzzle_index" key at all (see `save` above) —
# load_free_play must recognize that and refuse rather than crash trying
# to read fields (like "number_of_players") that only the OTHER format has.
func _test_load_free_play_rejects_a_turn_based_save() -> void:
	var session := GameSession.new(1, _dictionary)
	SaveGame.save(session, _SAVE_PATH)

	var loaded = SaveGame.load_free_play(_SAVE_PATH, _dictionary)
	_check(loaded == null, "a turn-based save is refused, not mistaken for a free-play one")

	DirAccess.remove_absolute(_SAVE_PATH)
