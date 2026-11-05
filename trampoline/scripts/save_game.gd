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
#
# A game is saved on its own, between turns, to `AUTOSAVE_PATH` (see
# main.gd), and picked up again from the start card's "Reprendre la
# partie" — always at the start of a turn.
class_name SaveGame
extends RefCounted

const SAVE_VERSION := 1
# Where the game in progress is saved. "user://" is Godot's own writable,
# per-user data folder (on Android, the app's private storage).
const AUTOSAVE_PATH := "user://autosave.json"
# Where the games saved by hand (the ☰ menu's "Sauvegarder la partie") are
# kept, one file each, named after when they were saved.
const SAVES_DIR := "user://saves/"


# Keeps a copy of the game saved at `autosave` — the game in progress, as
# its current turn began (see main.gd's `_begin_turn`) — in the folder
# `dir`, named after the date and time. Returns false if there's no game
# to copy, or the copy failed.
static func keep_copy(autosave: String, dir: String) -> bool:
	if not FileAccess.file_exists(autosave):
		return false
	DirAccess.make_dir_recursive_absolute(dir)
	# "2026-11-05T14:30:12" — `:` isn't allowed in a file name everywhere.
	var stamp := Time.get_datetime_string_from_system().replace(":", "-")
	var path := dir + stamp + ".json"
	var extra := 2
	while FileAccess.file_exists(path):  # two saves in the same second
		path = dir + "%s_%d.json" % [stamp, extra]
		extra += 1
	return DirAccess.copy_absolute(autosave, path) == OK


# The games kept in the folder `dir` (see `keep_copy`), newest first, each
# a `{path, time, names, scores}` dictionary: its file, when it was saved
# (seconds since 1970, UTC), and each player's entered name (maybe empty)
# and score. Files that can't be read are left out.
static func saved_games(dir: String) -> Array[Dictionary]:
	var games: Array[Dictionary] = []
	if not DirAccess.dir_exists_absolute(dir):  # nothing saved yet
		return games
	for file in DirAccess.get_files_at(dir):
		if not file.ends_with(".json"):
			continue
		var path := dir + file
		var data = JSON.parse_string(FileAccess.get_file_as_string(path))
		if data == null or not data.has("turns"):
			continue
		var names: Array[String] = []
		var scores: Array[int] = []
		for i in range(int(data["number_of_players"])):
			var entered: Array = data.get("player_names", [])
			names.append(entered[i] if i < entered.size() else "")
			scores.append(int(data["turns"]["scores"][i]))
		var time := FileAccess.get_modified_time(path)
		games.append({"path": path, "time": time, "names": names, "scores": scores})
	games.sort_custom(func(a: Dictionary, b: Dictionary) -> bool: return a["time"] > b["time"])
	return games


# Where free play is saved (`puzzle_index` -1), or crossword number
# `puzzle_index + 1` (see crossword_puzzle.gd): each its own file, so
# every crossword keeps its own progress. Saved on every change, picked
# up again when that mode is chosen (see main.gd's `_start_free_play`).
static func free_play_path(puzzle_index: int) -> String:
	if puzzle_index < 0:
		return "user://free_play.json"
	return "user://crossword_%d.json" % (puzzle_index + 1)


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
		"play_time": session.play_time,
		"ended_by_time": session.ended_by_time,
		"player_names": session.player_names,
	}

	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(data))
	return true


# Reconstructs a GameSession from a file written by `save`. `dictionary`
# can be injected the same way GameSession's own constructor allows —
# loading the ~421,000-word dictionary fresh every time would be wasteful
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
	# Saves from before the one-hour limit have neither: no time played.
	session.play_time = data.get("play_time", 0.0)
	session.ended_by_time = data.get("ended_by_time", false)
	for player_name in data.get("player_names", []):
		session.player_names.append(player_name)
	# The saved turn starts now, from the board and hand as loaded (see
	# GameSession.turn_start); the scoring's own note of it was restored
	# above.
	var hand := session.bag.hand(session.turns.current_player_id())
	session.turn_start = TurnStart.new(session.board, hand, session.dictionary)

	return session


# Writes free play's board (see free_play_session.gd) to `path` (see
# `free_play_path`). Free play has no bag/hands/turns to save at all
# (every tile not on the board is simply "available" — see
# FreePlaySession.available_tiles), so this is a smaller version of
# `save` above: just tile flip-state and the board (the score is read off
# the board). Returns false if the file couldn't be opened for writing.
static func save_free_play(session: FreePlaySession, path: String) -> bool:
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
	var data := {"version": SAVE_VERSION, "tile_states": tile_states, "board": board_data}
	var file := FileAccess.open(path, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(data))
	return true


# The free play written to `path` by `save_free_play`, or null if the
# file is missing or isn't valid JSON.
static func load_free_play(path: String, dictionary: FrenchDictionary = null) -> FreePlaySession:
	if not FileAccess.file_exists(path):
		return null
	var data = JSON.parse_string(FileAccess.get_file_as_string(path))
	if data == null or not data.has("board"):
		return null
	var session := FreePlaySession.new(dictionary)
	for i in range(session.pool.tiles.size()):
		session.pool.tiles[i].set_showing_front(data["tile_states"][i])
	for row in range(Board.ROWS):
		for col in range(Board.COLUMNS):
			var index: int = int(data["board"][row][col])
			if index != -1:
				session.board.place(row, col, session.pool.tiles[index])
	return session


# Maps each Tile object in `pool` to its fixed position in `pool.tiles` —
# the shared "which physical tile is this" identity used throughout
# `save` above.
static func _build_tile_index(pool: TilePool) -> Dictionary:
	var index := {}
	for i in range(pool.tiles.size()):
		index[pool.tiles[i]] = i
	return index
