# Entry point for the game's main scene: creates a GameSession, shows its
# board/hand via BoardView/HandView, turns a dropped tile (see
# board_view.gd's `tile_dropped` signal) into an actual placement, and ends
# the current turn on a button press. This is the one place allowed to know
# about both the game rules (GameSession) and the views — BoardView/
# HandView never talk to GameSession directly, per this project's
# "separate logic from graphics" rule. Currently just a fixed 2-player game
# started fresh every time the app runs — no menu, no save/load wiring yet.
extends Control

# Kept as a field (not a local var in _ready()) now that other methods
# below — `_on_board_tile_dropped`, `_on_end_turn_pressed` — need to reach
# it too.
var _session: GameSession

# `$BoardView`/`$HandView`/`$EndTurnButton`/`$Scoreboard`/`$GameOverBanner`/
# `$EndTurnMessage` look up the child nodes of those names declared in
# scenes/main.tscn — the same shorthand board_view.gd itself uses for its
# own "Grid" child. `@onready` runs this once those children actually
# exist, rather than too early (see board_view.gd for the same pattern).
@onready var _board_view: BoardView = $BoardView
@onready var _hand_view: HandView = $HandView
@onready var _end_turn_button: Button = $EndTurnButton
@onready var _scoreboard: Label = $Scoreboard
@onready var _game_over_banner: Label = $GameOverBanner
@onready var _end_turn_message: Label = $EndTurnMessage


func _ready() -> void:
	# Godot batches mouse-motion events into one per rendered frame by
	# default (meant to save work for games that don't need per-event
	# precision). For a dragged tile that means it visibly lags behind the
	# real cursor whenever the frame rate dips below the mouse's own
	# polling rate. Turning accumulation off delivers every motion event
	# immediately instead, which is the standard fix for sluggish-feeling
	# drags in Godot.
	Input.set_use_accumulated_input(false)

	# GameSession would build its own French dictionary if not given one,
	# but loading those ~79000 words is the slowest part of starting a
	# session — there's nothing else here yet that needs a shared
	# instance, so this just keeps things simple rather than plumbing one
	# through for no current benefit.
	_session = GameSession.new(2)
	# Connects BoardView's/HandView's signals, and the button's built-in
	# `pressed` signal (every Button has one), to the methods below — see
	# board_cell.gd's header comment for what a signal/connection is.
	_board_view.tile_dropped.connect(_on_board_tile_dropped)
	_board_view.tile_moved.connect(_on_board_tile_moved)
	_board_view.word_moved.connect(_on_board_word_moved)
	_board_view.tile_flipped.connect(_on_board_tile_flipped)
	_hand_view.tile_returned.connect(_on_hand_tile_returned)
	_end_turn_button.pressed.connect(_on_end_turn_pressed)
	_refresh_views()


# Called whenever a TilePanel gets dropped onto a BoardCell (see
# board_cell.gd/tile_panel.gd). GrowthRules legality was already checked
# before Godot even allowed the drop (`BoardCell._can_drop_data`), but
# `place_tile` also checks the tile is actually in the current player's
# hand — belt and braces, and the only check that actually matters here
# since the view layer already filtered out illegal board positions.
func _on_board_tile_dropped(row: int, col: int, tile: Tile) -> void:
	_session.place_tile(row, col, tile)
	_refresh_views()


# Called whenever a BoardCell gets dragged onto another BoardCell (see
# board_cell.gd) — relocating a tile the current player placed earlier this
# same turn. `move_own_tile` re-checks eligibility itself (the view layer
# only used it to decide whether to even start the drag), so an already-
# frozen tile that somehow got dragged anyway simply won't move.
func _on_board_tile_moved(from_row: int, from_col: int, to_row: int, to_col: int) -> void:
	_session.move_own_tile(from_row, from_col, to_row, to_col)
	_refresh_views()


# Called whenever a BoardCell is dragged onto another BoardCell while
# carrying a whole WORD instead of a single tile (see board_cell.gd) —
# relocating an already-played word from an earlier turn. Any player may
# do this, not just whoever originally placed it (see word_move.gd).
func _on_board_word_moved(from_row: int, from_col: int, to_row: int, to_col: int) -> void:
	_session.move_word_by_grabbed_tile(from_row, from_col, to_row, to_col)
	_refresh_views()


# Called whenever a BoardCell is clicked (not dragged) — flipping that
# tile in place (see game_session.gd's `flip_tile`). Any board tile may be
# flipped, any time, by any player (see doc/RULES.md's "trampoline"
# mechanic), so there's no eligibility check to make here first.
func _on_board_tile_flipped(row: int, col: int) -> void:
	_session.flip_tile(row, col)
	_refresh_views()


# Called whenever a BoardCell gets dragged onto the hand row (see
# hand_row.gd) — giving a tile back to the current player's storage.
func _on_hand_tile_returned(from_row: int, from_col: int) -> void:
	_session.remove_tile(from_row, from_col)
	_refresh_views()


# Called when the End Turn button is pressed. `end_turn()` itself refuses
# and returns false if an invalid word is currently on the board (see
# word_challenge.gd) — in which case `_end_turn_message` explains why,
# listing the offending word(s) (see `_format_end_turn_error` below).
# Cleared on success, since the turn actually ended.
func _on_end_turn_pressed() -> void:
	if _session.end_turn():
		_end_turn_message.text = ""
	else:
		_end_turn_message.text = _format_end_turn_error()
	_refresh_views()


# Redraws both views from the session's current state — the board, and
# whichever player's turn it currently is. `_session.can_take_back` is
# passed straight through as a Callable (see board_view.gd's `refresh` for
# what that means) rather than BoardView asking GameSession itself. Also
# checks whether the game has just ended (see `_update_game_over_banner`).
func _refresh_views() -> void:
	_board_view.refresh(_session.board, _session.can_take_back)
	_hand_view.refresh(_session.bag.hand(_session.turns.current_player_id()))
	_scoreboard.text = _format_scoreboard()
	_update_game_over_banner()


# Builds a one-line summary like "Joueur 1 : 12 pts   Joueur 2 : 5 pts (à
# vous de jouer)" — plain string formatting, not worth its own view class
# the way BoardView/HandView are (those turn game state into colors,
# positions, and drag-and-drop; this just turns numbers into text). Once
# the game is over, nobody's "up next" anymore, so that last bit is
# dropped (see `_update_game_over_banner` for the actual game-over
# announcement).
func _format_scoreboard() -> String:
	var scores := _session.turns.all_player_scores()
	var current_player_id := _session.turns.current_player_id()
	var is_over := _session.is_game_over()
	var text := ""
	for player_id in range(scores.size()):
		if player_id > 0:
			text += "   "
		# `%d` inside a string, followed by `% [...]`, is GDScript's
		# printf-style formatting — like Python's `"%d" % value`.
		text += "Joueur %d : %d pts" % [player_id + 1, scores[player_id]]
		if not is_over and player_id == current_player_id:
			text += " (à vous de jouer)"
	return text


# Builds the message shown by `_end_turn_message` after a refused
# `end_turn()` — names the actual invalid word(s) (see
# game_session.gd's `invalid_words`) rather than just saying "no".
func _format_end_turn_error() -> String:
	var words: Array[String] = []
	for entry in _session.invalid_words():
		words.append(entry["word"])
	if words.size() == 1:
		return "Mot invalide : %s" % words[0]
	return "Mots invalides : %s" % ", ".join(words)


# Shows a winner announcement once the game is over (see
# game_session.gd's `is_game_over`/`winner_id` — either every player is
# exhausted, or the board itself filled up), and disables the End Turn
# button since there's no longer a turn to end. Blank/enabled otherwise.
func _update_game_over_banner() -> void:
	if not _session.is_game_over():
		_game_over_banner.text = ""
		_end_turn_button.disabled = false
		return
	var winner_id := _session.winner_id()
	var winner_score := _session.turns.player_score(winner_id)
	_game_over_banner.text = (
		"Partie terminée ! Joueur %d gagne avec %d points." % [winner_id + 1, winner_score]
	)
	_end_turn_button.disabled = true
