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
	_test_time_limit_finishes_the_round()
	_test_time_limit_solo()
	_test_tie_break_on_tiles_left()
	_test_refill_at_turn_start()
	_test_discard()
	_test_discard_then_place()
	_test_place_tile_from_hand()
	_test_place_tile_not_in_hand_fails()
	_test_place_tile_anywhere_empty_succeeds()
	_test_invalid_cells_empty_for_a_normal_word()
	_test_invalid_cells_flags_a_disconnected_placement()
	_test_invalid_cells_flags_two_different_rows()
	_test_invalid_cells_clears_once_the_offending_tile_is_taken_back()
	_test_can_end_turn_blocks_bad_word()
	_test_end_turn_scores_and_advances()
	_test_bonus_is_kept_on_later_turns()
	_test_game_over_via_exhaustion()
	_test_game_over_via_board_completion()
	_test_remove_tile()
	_test_remove_tile_frozen_after_turn_ends()
	_test_remove_tile_allows_middle_of_this_turns_word()
	_test_move_own_tile()
	_test_move_own_tile_refuses_frozen_tile()
	_test_move_own_tile_to_a_different_row_is_flagged_invalid()
	_test_move_word()
	_test_move_word_by_grabbed_tile_from_the_start()
	_test_move_word_by_grabbed_tile_from_the_middle()
	_test_move_word_by_grabbed_tile_refuses_illegal_destination()
	_test_flip_tile()
	_test_flip_tile_frozen_tile_still_flips()
	_test_flip_tile_refuses_empty_cell()

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
	_check(session.bag.hand(0).size() == 13, "the starter is dealt a full 13-tile hand")
	_check(session.bag.hand(1).size() == 13, "so is the other player")
	_check(session.bag.remaining_count() == 117 - 26, "...both drawn from the shared bag")


# Once the hour is up, the game only ends when the round does: the
# players after the current one still get their turn.
func _test_time_limit_finishes_the_round() -> void:
	var session := GameSession.new(3, _dictionary)
	session.end_turn()  # player 0 -> 1
	session.add_play_time(GameSession.TIME_LIMIT_SECONDS)
	_check(session.is_last_round(), "the hour being up starts the last round")
	_check(not session.is_game_over(), "...but the game isn't over yet")
	session.end_turn()  # player 1 -> 2
	_check(not session.is_game_over(), "player 2 still gets their turn")
	session.end_turn()  # player 2 -> back to 0
	_check(session.is_game_over(), "the game ends once the round is over")
	_check(session.ended_by_time, "...because time ran out")
	session.add_play_time(10.0)
	_check(
		session.play_time == GameSession.TIME_LIMIT_SECONDS,
		"no more play time is counted after the end"
	)


# Solo, a round is a single turn: it ends with the turn time ran out in.
func _test_time_limit_solo() -> void:
	var session := GameSession.new(1, _dictionary)
	session.add_play_time(GameSession.TIME_LIMIT_SECONDS + 5.0)
	_check(not session.is_game_over(), "solo: the current turn can still be finished")
	session.end_turn()
	_check(session.is_game_over(), "solo: the game ends with that turn")


# Equal scores: fewest tiles left in hand wins; equal again, both win.
func _test_tie_break_on_tiles_left() -> void:
	var session := GameSession.new(2, _dictionary)
	session.turns.set_player_score(0, 40)
	session.turns.set_player_score(1, 40)
	session.ended_by_time = true
	_check(session.winner_ids() == ([0, 1] as Array[int]), "same score, same hand size: both win")
	session.bag.consume(1, session.bag.hand(1)[0])
	_check(session.winner_ids() == ([1] as Array[int]), "same score: fewer tiles left wins")


# Each turn starts by topping the new current player's hand back up to the
# limit — never beyond it, however many turns they've had.
func _test_refill_at_turn_start() -> void:
	var session := GameSession.new(2, _dictionary)
	session.end_turn()
	_check(session.bag.hand(1).size() == 13, "player 1's first turn starts with exactly 13 tiles")
	session.end_turn()
	_check(session.bag.hand(0).size() == 13, "a player who placed nothing still holds 13, no more")

	var word := "maison"
	for i in range(word.length()):
		var tile := Tile.new(word[i], "Z", true)
		_give_hand_tile(session, 0, tile)
		session.place_tile(4, i, tile)
	session.end_turn()
	session.end_turn()
	_check(session.bag.hand(0).size() == 13, "after placing a word, the next turn refills to 13")


# Tiles marked during a turn go back to the bag when it ends, and the next
# turn's refill replaces them.
func _test_discard() -> void:
	var session := GameSession.new(2, _dictionary)
	var kept: Tile = session.bag.hand(0)[0]
	var given_back: Tile = session.bag.hand(0)[1]
	var bag_before := session.bag.remaining_count()

	_check(session.set_marked_for_discard(given_back, true), "marking a hand tile succeeds")
	_check(session.is_marked_for_discard(given_back), "...and it's marked")
	session.set_marked_for_discard(kept, true)
	session.set_marked_for_discard(kept, false)
	_check(not session.is_marked_for_discard(kept), "a tile can be unmarked again")
	_check(
		session.marked_for_discard().size() == 1 and session.marked_for_discard()[0] == given_back,
		"the marked list holds just that tile"
	)
	_check(
		not session.set_marked_for_discard(session.bag.hand(1)[0], true),
		"the other player's tile can't be marked"
	)

	session.end_turn()
	_check(not session.bag.hand(0).has(given_back), "the marked tile left the hand at turn end")
	_check(session.bag.hand(0).has(kept), "...the unmarked one stayed")
	_check(session.bag.hand(0).size() == 12, "...and the hand isn't refilled until its next turn")
	_check(session.bag.undrawn_tiles().has(given_back), "...the tile is back in the bag")
	_check(session.bag.remaining_count() == bag_before + 1, "...one more tile in the bag")

	session.end_turn()
	_check(session.bag.hand(0).size() == 13, "the next turn refills the hand to 13")


# A tile marked and then placed isn't given back: it's on the board now.
func _test_discard_then_place() -> void:
	var session := GameSession.new(2, _dictionary)
	var tile := Tile.new("A", "Z", true)
	_give_hand_tile(session, 0, tile)
	session.set_marked_for_discard(tile, true)
	session.place_tile(4, 0, tile)
	_check(not session.is_marked_for_discard(tile), "placing a marked tile unmarks it")


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


# place_tile() no longer enforces GrowthRules/one-row-per-turn itself (see
# game_session.gd's own comment) — a player can drop a tile on any empty
# cell, anywhere, and rearrange all turn; `invalid_cells()`/`can_end_turn()`
# (see the `_test_invalid_cells_*` tests below) are what actually catch an
# illegal result, the same way a bad dictionary word already works.
func _test_place_tile_anywhere_empty_succeeds() -> void:
	var session := GameSession.new(2, _dictionary)
	var first: Tile = session.bag.hand(0)[0]
	var second: Tile = session.bag.hand(0)[1]
	session.place_tile(2, 4, first)
	_check(
		session.place_tile(2, 2, second), "a non-adjacent placement in the same row succeeds now"
	)
	_check(not session.bag.hand(0).has(second), "...and is consumed like any other placement")


# The common case: letters placed one after another, all touching, all in
# the same row — this should never trip the single-row/single-word check.
# Uses a real French word (unlike "CAT" elsewhere in this file) since this
# one actually calls can_end_turn(), which also checks the dictionary.
func _test_invalid_cells_empty_for_a_normal_word() -> void:
	var session := GameSession.new(1, _dictionary)
	var word := "CHAT"
	var tiles: Array[Tile] = []
	for letter in word:
		var tile := Tile.new(letter, "Z", true)
		_give_hand_tile(session, 0, tile)
		tiles.append(tile)
	for i in range(tiles.size()):
		session.place_tile(2, 2 + i, tiles[i])
	_check(session.invalid_cells().is_empty(), "one contiguous word this turn is entirely legal")
	_check(session.can_end_turn(), "...so ending the turn is allowed")


# Two separate groups of letters in the SAME row, placed the same turn —
# doc/RULES.md's "extend the word, or start fresh in an empty row" doesn't
# describe planting a second, disconnected clump, even though each cell
# individually landed on an empty spot.
func _test_invalid_cells_flags_a_disconnected_placement() -> void:
	var session := GameSession.new(2, _dictionary)
	var first: Tile = session.bag.hand(0)[0]
	var second: Tile = session.bag.hand(0)[1]
	session.place_tile(2, 0, first)
	session.place_tile(2, 4, second)

	var invalid := session.invalid_cells()
	_check(invalid.size() == 2, "both disconnected cells are reported, not just one")
	_check(not session.can_end_turn(), "a disconnected placement blocks ending the turn")


# Same idea as `_test_invalid_cells_flags_a_disconnected_placement`, but
# spread across two different rows instead of one — see doc/RULES.md's
# "Turns and draws" section: a turn plays onto a single horizontal line.
func _test_invalid_cells_flags_two_different_rows() -> void:
	var session := GameSession.new(2, _dictionary)
	var first: Tile = session.bag.hand(0)[0]
	var second: Tile = session.bag.hand(0)[1]
	session.place_tile(2, 4, first)
	session.place_tile(5, 4, second)

	var invalid := session.invalid_cells()
	_check(invalid.size() == 2, "both rows' cells are reported")
	_check(not session.can_end_turn(), "placements on two different rows block ending the turn")


# invalid_cells() is recomputed fresh from the board each time, not a flag
# set once and forgotten — so taking the offending tile back should be
# enough to clear it, no separate "unlock" bookkeeping needed.
func _test_invalid_cells_clears_once_the_offending_tile_is_taken_back() -> void:
	var session := GameSession.new(1, _dictionary)
	var first: Tile = session.bag.hand(0)[0]
	var second: Tile = session.bag.hand(0)[1]
	session.place_tile(2, 4, first)
	session.place_tile(6, 0, second)
	session.remove_tile(6, 0)

	_check(
		session.invalid_cells().is_empty(),
		"with the stray second-row tile taken back, only row 2 remains touched"
	)
	_check(session.can_end_turn(), "...so ending the turn is allowed again")


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


# A bonus is paid once, then must stay in the score: later turns
# recompute the words' points, not the bonuses already won.
func _test_bonus_is_kept_on_later_turns() -> void:
	var session := GameSession.new(2, _dictionary)
	var word := "personnes"
	for i in range(word.length()):
		var tile := Tile.new(word[i], "Z", true)
		_give_hand_tile(session, 0, tile)
		session.place_tile(4, i, tile)
	session.end_turn()
	# 1+2+…+9 for the letters, +50 for a full yellow word in one turn.
	_check(session.turns.player_score(0) == 95, "a full yellow word scores 45 + 50")
	session.end_turn()  # player 1 passes
	_check(session.turns.player_score(0) == 95, "...and keeps its bonus on the next turn")


func _test_game_over_via_exhaustion() -> void:
	var session := GameSession.new(2, _dictionary)
	session.turns.set_player_score(0, 50)
	session.turns.set_player_score(1, 30)
	session.turns.set_player_finished(0, true)
	session.turns.set_player_finished(1, true)
	_check(session.is_game_over(), "the game is over once every player is exhausted")
	_check(session.winner_ids() == ([0] as Array[int]), "the higher scorer wins")


func _test_game_over_via_board_completion() -> void:
	var session := GameSession.new(2, _dictionary)
	_check(not session.is_game_over(), "sanity: a fresh session isn't over")

	# Fill the whole board directly, skipping GameSession.place_tile
	# entirely — this test is about GameSession's aggregation (is_game_over,
	# winner_id), not placement, and this way avoids needing 117 tiles
	# individually dealt into a hand first.
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


# move_own_tile() doesn't enforce the single-row rule either — moving this
# turn's own tile to a different row succeeds, same as a fresh placement
# there would, and shows up via invalid_cells() instead.
func _test_move_own_tile_to_a_different_row_is_flagged_invalid() -> void:
	var session := GameSession.new(1, _dictionary)
	var tiles: Array[Tile] = []
	for letter in ["C", "A"]:
		var tile := Tile.new(letter, "Z", true)
		_give_hand_tile(session, 0, tile)
		tiles.append(tile)
	session.place_tile(10, 2, tiles[0])
	session.place_tile(10, 3, tiles[1])

	_check(session.move_own_tile(10, 2, 6, 0), "moving to a different row succeeds")
	_check(session.board.tile_at(6, 0) == tiles[0], "the tile is now at its new position")
	_check(not session.can_end_turn(), "...but leaves the turn spread across two rows, blocked")


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

	# Placed directly on the board (same shortcut as
	# _test_game_over_via_board_completion, no hand bookkeeping needed) —
	# this test is about WordMove's destination-occupied check, not
	# placement itself.
	session.board.place(12, 7, Tile.new("X", "Z", true))  # occupies where "T" would land

	_check(
		not session.move_word_by_grabbed_tile(12, 2, 12, 5),
		"refused because the destination overlaps an unrelated tile"
	)
	_check(session.board.word_at(12, 2) == "CAT", "the word stays exactly where it was")


func _test_flip_tile() -> void:
	var session := GameSession.new(1, _dictionary)
	var tile := Tile.new("E", "P", true)
	_give_hand_tile(session, 0, tile)
	session.place_tile(0, 0, tile)
	_check(
		session.board.tile_at(0, 0).current_letter() == "E",
		"sanity: showing its front face at first"
	)

	_check(session.flip_tile(0, 0), "flip_tile succeeds on an occupied cell")
	_check(
		session.board.tile_at(0, 0).current_letter() == "P", "the tile now shows its back letter"
	)
	_check(session.board.tile_at(0, 0).current_is_green(), "...and counts as green now")

	_check(session.flip_tile(0, 0), "flipping back succeeds too")
	_check(
		session.board.tile_at(0, 0).current_letter() == "E", "the tile is back to showing its front"
	)


# Flipping never empties a cell (see doc/RULES.md's "trampoline"
# mechanic), so — unlike remove_tile/move_own_tile — it isn't restricted to
# the current turn's own tiles; a tile from an already-ended turn flips
# just as freely.
func _test_flip_tile_frozen_tile_still_flips() -> void:
	var session := GameSession.new(2, _dictionary)
	var word := "MAISON"
	for i in range(word.length()):
		var tile := Tile.new(word[i], "Z", true)
		_give_hand_tile(session, 0, tile)
		session.place_tile(1, i, tile)
	session.end_turn()  # player 0's turn is over; player 1 is now current

	_check(session.flip_tile(1, 0), "a tile from an already-ended turn can still be flipped")
	_check(session.board.tile_at(1, 0).current_letter() == "Z", "it now shows its back letter")


func _test_flip_tile_refuses_empty_cell() -> void:
	var session := GameSession.new(1, _dictionary)
	_check(not session.flip_tile(5, 5), "flipping an empty cell is refused")
