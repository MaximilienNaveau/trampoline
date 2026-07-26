# Saving and loading a whole GameSession to/from a JSON file.
#
# The one real trick: all 117 tiles are shared references — the same
# Tile might currently sit on the board, in a hand, or in the undrawn
# pile, and its only mutable state is which face is showing. So rather
# than serializing tiles independently (risking the same tile ending up
# duplicated or inconsistent across the file), everything else — board
# cells, hands, the undrawn pile — references tiles by their fixed INDEX
# into GameSession.pool.tiles, the one place that list is built once,
# deterministically (see tile_pool.gd), and never reordered.
#
# JSON itself adds two gotchas handled throughout this file and the
# export_state/restore_state methods it calls: every number round-trips
# as a float (so anything going into a typed Array[int]/Dictionary needs
# an explicit int(...) cast back), and Dictionary keys round-trip as
# strings (so int keys like column or player IDs need explicit str()/
# int() conversions on the way out and back in).
class_name SaveGame
extends RefCounted

const SAVE_VERSION := 1


# Writes `session`'s entire state to `path` — e.g. "user://save1.json".
# "user://" is Godot's own writable, per-user data directory (the
# equivalent of a config/save directory a C++ or Python app would look up
# via the OS rather than hardcoding); a plain absolute path works too.
# Returns false if the file couldn't be opened for writing.
static func save(session: GameSession, path: String) -> bool:
	var tile_index := _build_tile_index(session.pool)

	var tile_states := []
	for tile in session.pool.tiles:
		tile_states.append(tile.is_showing_front())

	var board_data := []
	for row in range(Board.ROWS):
		var row_data := []
		for col in range(Board.COLUMNS):
			var tile := session.board.tile_at(row, col)
			row_data.append(tile_index[tile] if tile != null else -1)
		board_data.append(row_data)

	var undrawn_data := []
	for tile in session.bag.undrawn_tiles():
		undrawn_data.append(tile_index[tile])

	var hands_data := {}
	for player_id in range(session.turns.number_of_players):
		var hand_indices := []
		for tile in session.bag.hand(player_id):
			hand_indices.append(tile_index[tile])
		hands_data[str(player_id)] = hand_indices

	var data := {
		"version": SAVE_VERSION,
		"number_of_players": session.turns.number_of_players,
		"tile_states": tile_states,
		"board": board_data,
		"undrawn": undrawn_data,
		"hands": hands_data,
		"turns": session.turns.export_state(),
		"scoring": session.scoring.export_state(),
	}

	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(data))
	return true


# Reconstructs a GameSession from a file written by `save`. `dictionary`
# can be injected the same way GameSession's own constructor allows —
# loading the ~79000-word dictionary fresh every time would be wasteful
# when a caller already has one loaded. Returns null if the file is
# missing or not valid JSON.
static func load(path: String, dictionary: FrenchDictionary = null) -> GameSession:
	if not FileAccess.file_exists(path):
		return null
	var file := FileAccess.open(path, FileAccess.READ)
	if file == null:
		return null

	var data = JSON.parse_string(file.get_as_text())
	if data == null:
		return null

	# GameSession's normal constructor deals a fresh hand to everyone and
	# shuffles a new bag — every bit of that gets overwritten below.
	# Slightly wasteful, but far simpler than giving GameSession a second,
	# uninitialized-on-purpose constructor path just for loading.
	var session := GameSession.new(int(data["number_of_players"]), dictionary)

	for i in range(session.pool.tiles.size()):
		session.pool.tiles[i].set_showing_front(data["tile_states"][i])

	for row in range(Board.ROWS):
		for col in range(Board.COLUMNS):
			var index: int = int(data["board"][row][col])
			if index != -1:
				session.board.place(row, col, session.pool.tiles[index])

	var undrawn: Array[Tile] = []
	for index in data["undrawn"]:
		undrawn.append(session.pool.tiles[int(index)])

	var hands := {}
	for key in data["hands"]:
		var hand: Array[Tile] = []
		for index in data["hands"][key]:
			hand.append(session.pool.tiles[int(index)])
		hands[int(key)] = hand
	session.bag.restore_state(undrawn, hands)

	session.turns.restore_state(data["turns"])
	session.scoring.restore_state(data["scoring"])

	return session


# Maps each Tile object in `pool` to its fixed position in `pool.tiles` —
# the shared "which physical tile is this" identity used throughout
# `save` above.
static func _build_tile_index(pool: TilePool) -> Dictionary:
	var index := {}
	for i in range(pool.tiles.size()):
		index[pool.tiles[i]] = i
	return index
