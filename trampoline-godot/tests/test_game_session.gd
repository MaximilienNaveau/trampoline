# Standalone checks for game_session.gd, runnable via:
#   godot4 --headless --path . --script tests/test_game_session.gd
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

	_test_initial_draw()
	_test_place_tile_from_hand()
	_test_place_tile_not_in_hand_fails()
	_test_place_tile_illegal_location_fails()
	_test_can_end_turn_blocks_bad_word()
	_test_end_turn_scores_and_advances()
	_test_game_over_via_exhaustion()
	_test_game_over_via_board_completion()
	_test_remove_tile()
	_test_remove_tile_frozen_after_turn_ends()
	_test_remove_tile_allows_middle_of_this_turns_word()
	_test_move_own_tile()
	_test_move_own_tile_refuses_frozen_tile()
	_test_move_word()
	_test_move_word_by_grabbed_tile_from_the_start()
	_test_move_word_by_grabbed_tile_from_the_middle()
	_test_move_word_by_grabbed_tile_refuses_illegal_destination()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


# Injects `tile` directly into `player_id`'s hand. TileBag has no "just
# give me this specific tile" API on purpose (a real player draws
# randomly) — this leans on `hand()` returning the live array by
# reference, purely so these tests can set up deterministic scenarios
# ("place exactly this word") instead of whatever the shuffle happened to
# deal.
func _give_hand_tile(session: GameSession, player_id: int, tile: Tile) -> void:
	session.bag.hand(player_id).append(tile)


func _test_initial_draw() -> void:
	var session := GameSession.new(2, _dictionary)
	_check(
		session.bag.hand(0).size() == 14, "2-player starter's initial hand includes the bonus tile"
	)
	_check(session.bag.hand(1).size() == 13, "the non-starter's initial hand has no bonus")


func _test_place_tile_from_hand() -> void:
	var session := GameSession.new(2, _dictionary)
	var tile: Tile = session.bag.hand(0)[0]
	_check(
		session.place_tile(0, 4, tile), "placing a tile actually in hand, on an empty row, succeeds"
	)
	_check(session.board.tile_at(0, 4) == tile, "the tile is now on the board")
	_check(not session.bag.hand(0).has(tile), "...and gone from the hand")


func _test_place_tile_not_in_hand_fails() -> void:
	var session := GameSession.new(2, _dictionary)
	var foreign_tile := Tile.new("A", "Z", true)
	_check(not session.place_tile(1, 4, foreign_tile), "a tile nobody drew can't be placed")
	_check(session.board.is_empty(1, 4), "the board is untouched by the refused placement")


func _test_place_tile_illegal_location_fails() -> void:
	var session := GameSession.new(2, _dictionary)
	var first: Tile = session.bag.hand(0)[0]
	var second: Tile = session.bag.hand(0)[1]
	session.place_tile(2, 4, first)
	_check(
		not session.place_tile(2, 2, second), "a non-adjacent placement is refused by GrowthRules"
	)
	_check(session.bag.hand(0).has(second), "the refused tile stays in hand, not consumed")


func _test_can_end_turn_blocks_bad_word() -> void:
	var session := GameSession.new(1, _dictionary)
	var word := "QSXWZY"  # confirmed gibberish elsewhere (test_french_dictionary.gd)
	for i in range(word.length()):
		var tile := Tile.new(word[i], "Z", true)
		_give_hand_tile(session, 0, tile)
		session.place_tile(3, i, tile)
	_check(not session.can_end_turn(), "gibberish on the board blocks ending the turn")
	_check(not session.end_turn(), "end_turn() itself refuses too")
	_check(session.turns.current_player_id() == 0, "...and doesn't advance the turn")


func _test_end_turn_scores_and_advances() -> void:
	var session := GameSession.new(2, _dictionary)
	var word := "maison"
	for i in range(word.length()):
		var tile := Tile.new(word[i], "Z", true)
		_give_hand_tile(session, 0, tile)
		session.place_tile(4, i, tile)

	_check(session.can_end_turn(), "a valid word allows ending the turn")
	_check(session.end_turn(), "end_turn() succeeds")
	_check(session.turns.player_score(0) == 21, "player 0 scores 1+2+3+4+5+6 for their own word")
	_check(session.turns.current_player_id() == 1, "play moves on to player 1")


func _test_game_over_via_exhaustion() -> void:
	var session := GameSession.new(2, _dictionary)
	session.turns.set_player_score(0, 50)
	session.turns.set_player_score(1, 30)
	session.turns.set_player_finished(0, true)
	session.turns.set_player_finished(1, true)
	_check(session.is_game_over(), "the game is over once every player is exhausted")
	_check(session.winner_id() == 0, "the higher scorer wins")


func _test_game_over_via_board_completion() -> void:
	var session := GameSession.new(2, _dictionary)
	_check(not session.is_game_over(), "sanity: a fresh session isn't over")

	# Fill the whole board directly (bypassing GrowthRules — this test is
	# about GameSession's aggregation, not placement legality, which has
	# its own tests already).
	for row in range(12):
		session.board.place(row, 0, Tile.new("X", "Z", true))
		var shorten_tile := Tile.new("Y", "", true)
		session.board.place(row, 1, shorten_tile)
		shorten_tile.flip()
	for col in range(Board.COLUMNS):
		session.board.place(12, col, Tile.new("Z", "Z", true))

	_check(
		session.is_game_over(),
		"the game is over once the board fills up, even with nobody marked exhausted"
	)


func _test_remove_tile() -> void:
	var session := GameSession.new(1, _dictionary)
	var real_tile := Tile.new("S", "Z", true)
	_give_hand_tile(session, 0, real_tile)
	session.place_tile(5, 0, real_tile)

	var shorten_tile := Tile.new("E", "", true)
	_give_hand_tile(session, 0, shorten_tile)
	session.place_tile(5, 1, shorten_tile)
	shorten_tile.flip()
	_check(not session.bag.hand(0).has(shorten_tile), "sanity: the tile left the hand when placed")

	_check(
		session.remove_tile(5, 0) == real_tile,
		"a real-letter tile placed THIS turn can be taken back"
	)
	_check(session.board.is_empty(5, 0), "its cell is empty again")
	_check(session.bag.hand(0).has(real_tile), "...and it went back to the current player's hand")

	var removed := session.remove_tile(5, 1)
	_check(removed == shorten_tile, "remove_tile returns the tile that was picked up")
	_check(session.board.is_empty(5, 1), "the cell is empty again")
	_check(
		session.bag.hand(0).has(shorten_tile),
		"the picked-up tile goes to the current player's hand"
	)


# Once a turn ends, whatever it placed is frozen — a real letter can no
# longer be taken back at all, even from its own word's edge, unlike a
# still-in-progress turn's own tiles (see _test_remove_tile above).
func _test_remove_tile_frozen_after_turn_ends() -> void:
	var session := GameSession.new(2, _dictionary)
	var word := "MAISON"
	for i in range(word.length()):
		var tile := Tile.new(word[i], "Z", true)
		_give_hand_tile(session, 0, tile)
		session.place_tile(6, i, tile)
	session.end_turn()  # player 0's turn is over; player 1 is now current

	_check(
		session.remove_tile(6, 0) == null,
		"a real-letter tile from an ALREADY-ENDED turn is frozen, even at its word's edge"
	)
	_check(session.board.word_at(6, 0) == "MAISON", "the word is untouched")


# Unlike the always-available blank "shorten" mechanic (edge-only, see
# growth_rules.gd), nothing about a still-in-progress turn is committed
# yet — so even the MIDDLE tile of a word the current player is still
# building can be taken back, letting them freely rearrange/rebuild it
# before ending their turn.
func _test_remove_tile_allows_middle_of_this_turns_word() -> void:
	var session := GameSession.new(1, _dictionary)
	var tiles: Array[Tile] = []
	for letter in ["C", "A", "T"]:
		var tile := Tile.new(letter, "Z", true)
		_give_hand_tile(session, 0, tile)
		tiles.append(tile)
	session.place_tile(7, 2, tiles[0])
	session.place_tile(7, 3, tiles[1])
	session.place_tile(7, 4, tiles[2])

	_check(
		session.remove_tile(7, 3) == tiles[1],
		"the middle tile of this turn's own word can be taken back too"
	)
	_check(session.board.is_empty(7, 3), "its cell is empty again")
	_check(session.bag.hand(0).has(tiles[1]), "...and it's back in the current player's hand")


func _test_move_own_tile() -> void:
	var session := GameSession.new(1, _dictionary)
	var tile := Tile.new("S", "Z", true)
	_give_hand_tile(session, 0, tile)
	session.place_tile(8, 4, tile)

	_check(session.move_own_tile(8, 4, 8, 0), "relocating this turn's own tile succeeds")
	_check(session.board.tile_at(8, 0) == tile, "the tile is now at its new position")
	_check(session.board.is_empty(8, 4), "the old cell is empty")


func _test_move_own_tile_refuses_frozen_tile() -> void:
	var session := GameSession.new(2, _dictionary)
	var word := "MAISON"  # must be dictionary-valid, or end_turn() below would refuse
	for i in range(word.length()):
		var tile := Tile.new(word[i], "Z", true)
		_give_hand_tile(session, 0, tile)
		session.place_tile(9, i, tile)
	var tile: Tile = session.board.tile_at(9, 0)
	session.end_turn()

	_check(
		not session.move_own_tile(9, 0, 9, 8),
		"a tile from an already-ended turn can't be relocated this way"
	)
	_check(session.board.tile_at(9, 0) == tile, "it stays exactly where it was")


func _test_move_word() -> void:
	var session := GameSession.new(1, _dictionary)
	var tiles: Array[Tile] = []
	for letter in ["C", "A", "T"]:
		var tile := Tile.new(letter, "Z", true)
		_give_hand_tile(session, 0, tile)
		tiles.append(tile)
	session.place_tile(6, 2, tiles[0])
	session.place_tile(6, 3, tiles[1])
	session.place_tile(6, 4, tiles[2])

	_check(session.move_word(6, 2, 6, 0), "relocating the word succeeds")
	_check(session.board.word_at(6, 0) == "CAT", "the word reads correctly at its new position")
	_check(session.board.is_empty(6, 3) and session.board.is_empty(6, 4), "the old cells are empty")


# Dragging by the word's OWN first letter should behave exactly like
# move_word — no offset to correct for.
func _test_move_word_by_grabbed_tile_from_the_start() -> void:
	var session := GameSession.new(1, _dictionary)
	var tiles: Array[Tile] = []
	for letter in ["C", "A", "T"]:
		var tile := Tile.new(letter, "Z", true)
		_give_hand_tile(session, 0, tile)
		tiles.append(tile)
	session.place_tile(10, 2, tiles[0])
	session.place_tile(10, 3, tiles[1])
	session.place_tile(10, 4, tiles[2])

	_check(
		session.move_word_by_grabbed_tile(10, 2, 10, 0),
		"relocating by the word's first letter succeeds"
	)
	_check(session.board.word_at(10, 0) == "CAT", "the word reads correctly at its new position")


# Dragging by a MIDDLE letter should carry the whole word so THAT letter
# (not the word's first letter) ends up at the drop point — otherwise the
# word would visibly jump instead of following the cursor.
func _test_move_word_by_grabbed_tile_from_the_middle() -> void:
	var session := GameSession.new(1, _dictionary)
	var tiles: Array[Tile] = []
	for letter in ["C", "A", "T"]:
		var tile := Tile.new(letter, "Z", true)
		_give_hand_tile(session, 0, tile)
		tiles.append(tile)
	session.place_tile(11, 2, tiles[0])
	session.place_tile(11, 3, tiles[1])  # the "A" — grabbed from here
	session.place_tile(11, 4, tiles[2])

	# Drop the grabbed "A" at column 6: the word's first letter ("C") must
	# then land one column earlier, at column 5.
	_check(
		session.move_word_by_grabbed_tile(11, 3, 11, 6), "relocating by a middle letter succeeds"
	)
	_check(session.board.word_at(11, 5) == "CAT", "the word lands offset by the grab point")
	_check(session.board.tile_at(11, 6) == tiles[1], "the grabbed A ends up exactly where dropped")


func _test_move_word_by_grabbed_tile_refuses_illegal_destination() -> void:
	var session := GameSession.new(1, _dictionary)
	var tiles: Array[Tile] = []
	for letter in ["C", "A", "T"]:
		var tile := Tile.new(letter, "Z", true)
		_give_hand_tile(session, 0, tile)
		tiles.append(tile)
	session.place_tile(12, 2, tiles[0])
	session.place_tile(12, 3, tiles[1])
	session.place_tile(12, 4, tiles[2])

	# Placed directly on the board (bypassing GrowthRules, same as
	# _test_game_over_via_board_completion) since a disconnected tile at
	# col 7 wouldn't be adjacent to anything and GrowthRules would refuse
	# it — this test is about WordMove's destination check, not placement.
	session.board.place(12, 7, Tile.new("X", "Z", true))  # occupies where "T" would land

	_check(
		not session.move_word_by_grabbed_tile(12, 2, 12, 5),
		"refused because the destination overlaps an unrelated tile"
	)
	_check(session.board.word_at(12, 2) == "CAT", "the word stays exactly where it was")
