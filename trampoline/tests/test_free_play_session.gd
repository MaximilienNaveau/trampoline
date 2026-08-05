# Standalone checks for free_play_session.gd, runnable via:
#   godot4 --headless --path . --script tests/test_free_play_session.gd
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

	_test_available_tiles_starts_full()
	_test_available_tiles_shrinks_after_placement()
	_test_available_tiles_grows_after_removal()
	_test_place_tile_refuses_occupied_cell()
	_test_can_take_back_is_always_true()
	_test_can_place_here_true_for_empty_false_for_occupied()
	_test_move_tile_into_empty_cell()
	_test_move_tile_rolls_back_when_destination_occupied()
	_test_flip_tile()
	_test_flip_tile_refuses_empty_cell()
	_test_current_score_zero_initially()
	_test_current_score_matches_word_sum()
	_test_current_score_drops_by_green_penalty()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


func _test_available_tiles_starts_full() -> void:
	var session := FreePlaySession.new(_dictionary)
	_check(
		session.available_tiles().size() == session.pool.tiles.size(),
		"every one of the 117 tiles starts available"
	)


func _test_available_tiles_shrinks_after_placement() -> void:
	var session := FreePlaySession.new(_dictionary)
	var tile: Tile = session.pool.tiles[0]
	session.place_tile(0, 0, tile)
	_check(
		session.available_tiles().size() == session.pool.tiles.size() - 1,
		"placing a tile removes it from the available pool"
	)
	_check(not session.available_tiles().has(tile), "...specifically that exact tile")


func _test_available_tiles_grows_after_removal() -> void:
	var session := FreePlaySession.new(_dictionary)
	var tile: Tile = session.pool.tiles[0]
	session.place_tile(0, 0, tile)
	session.remove_tile(0, 0)
	_check(
		session.available_tiles().size() == session.pool.tiles.size(),
		"removing the tile returns it to the available pool"
	)
	_check(session.available_tiles().has(tile), "...specifically that exact tile again")


func _test_place_tile_refuses_occupied_cell() -> void:
	var session := FreePlaySession.new(_dictionary)
	var first: Tile = session.pool.tiles[0]
	var second: Tile = session.pool.tiles[1]
	session.place_tile(0, 0, first)
	_check(not session.place_tile(0, 0, second), "a second tile can't land on an occupied cell")
	_check(session.board.tile_at(0, 0) == first, "the original tile is undisturbed")


func _test_can_take_back_is_always_true() -> void:
	var session := FreePlaySession.new(_dictionary)
	_check(session.can_take_back(0, 0), "true even for an empty cell (the caller checks occupancy)")
	session.place_tile(0, 0, session.pool.tiles[0])
	_check(session.can_take_back(0, 0), "true for an occupied cell too — no turn/ownership concept")


func _test_can_place_here_true_for_empty_false_for_occupied() -> void:
	var session := FreePlaySession.new(_dictionary)
	session.place_tile(0, 0, session.pool.tiles[0])
	_check(not session.can_place_here(0, 0), "refused: the cell already holds a tile")
	_check(session.can_place_here(0, 1), "allowed: any empty cell, no adjacency restriction")


func _test_move_tile_into_empty_cell() -> void:
	var session := FreePlaySession.new(_dictionary)
	var tile: Tile = session.pool.tiles[0]
	session.place_tile(0, 4, tile)
	_check(session.move_tile(0, 4, 0, 0), "relocating to an empty cell succeeds")
	_check(session.board.tile_at(0, 0) == tile, "the tile is now at its new position")
	_check(session.board.is_empty(0, 4), "the old cell is empty")


func _test_move_tile_rolls_back_when_destination_occupied() -> void:
	var session := FreePlaySession.new(_dictionary)
	var moving: Tile = session.pool.tiles[0]
	var blocker: Tile = session.pool.tiles[1]
	session.place_tile(0, 4, moving)
	session.place_tile(0, 0, blocker)

	_check(not session.move_tile(0, 4, 0, 0), "refused: the destination is already occupied")
	_check(session.board.tile_at(0, 4) == moving, "the moved tile lands back at its origin")
	_check(session.board.tile_at(0, 0) == blocker, "the blocker is untouched")


func _test_flip_tile() -> void:
	var session := FreePlaySession.new(_dictionary)
	var tile := Tile.new("E", "P", true)
	session.place_tile(0, 0, tile)
	_check(session.flip_tile(0, 0), "flip_tile succeeds on an occupied cell")
	_check(
		session.board.tile_at(0, 0).current_letter() == "P", "the tile now shows its back letter"
	)


func _test_flip_tile_refuses_empty_cell() -> void:
	var session := FreePlaySession.new(_dictionary)
	_check(not session.flip_tile(5, 5), "flipping an empty cell is refused")


func _test_current_score_zero_initially() -> void:
	var session := FreePlaySession.new(_dictionary)
	_check(session.current_score() == 0, "a fresh, empty board scores 0")


# Same word/expected-sum pair as GameSession's own
# _test_end_turn_scores_and_advances, confirming free play's live
# current_score() agrees with the turn-based end_turn() tally for
# identical input — placement-order position scores, summed.
func _test_current_score_matches_word_sum() -> void:
	var session := FreePlaySession.new(_dictionary)
	var word := "maison"
	for i in range(word.length()):
		session.place_tile(4, i, Tile.new(word[i], "Z", true))
	_check(session.current_score() == 21, "1+2+3+4+5+6 for a freshly spelled valid word")


func _test_current_score_drops_by_green_penalty() -> void:
	var session := FreePlaySession.new(_dictionary)
	var word := "maison"
	var tiles: Array[Tile] = []
	for i in range(word.length()):
		# Same letter on both faces, on purpose: flipping must turn this
		# letter green WITHOUT changing what the row spells (a "Z" back, as
		# used elsewhere, would turn "maison" into gibberish and drop the
		# whole word's score to 0 instead of just docking 5 points).
		var tile := Tile.new(word[i], word[i], true)
		session.place_tile(4, i, tile)
		tiles.append(tile)

	tiles[0].flip()  # still reads "m" — just showing its green back now
	_check(
		session.current_score() == 21 - 5, "flipping one letter green costs 5 points off the total"
	)
