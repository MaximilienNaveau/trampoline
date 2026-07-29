# Entry point for the game's main scene: creates a GameSession, shows its
# board/hand via BoardView/HandView, turns a dropped tile (see
# board_view.gd's `tile_dropped` signal) into an actual placement, and ends
# the current turn on a button press. This is the one place allowed to know
# about both the game rules (GameSession) and the views — BoardView/
# HandView never talk to GameSession directly, per this project's
# "separate logic from graphics" rule. Currently just a fixed 2-player game
# started fresh every time the app runs — no menu, no save/load wiring, no
# turn/score display yet.
extends Control

# Kept as a field (not a local var in _ready()) now that other methods
# below — `_on_board_tile_dropped`, `_on_end_turn_pressed` — need to reach
# it too.
var _session: GameSession

# `$BoardView`/`$HandView`/`$EndTurnButton` look up the child nodes of
# those names declared in scenes/main.tscn — the same shorthand
# board_view.gd itself uses for its own "Grid" child. `@onready` runs this
# once those children actually exist, rather than too early (see
# board_view.gd for the same pattern).
@onready var _board_view: BoardView = $BoardView
@onready var _hand_view: HandView = $HandView
@onready var _end_turn_button: Button = $EndTurnButton


func _ready() -> void:
	# GameSession would build its own French dictionary if not given one,
	# but loading those ~79000 words is the slowest part of starting a
	# session — there's nothing else here yet that needs a shared
	# instance, so this just keeps things simple rather than plumbing one
	# through for no current benefit.
	_session = GameSession.new(2)
	# Connects BoardView's signal, and the button's built-in `pressed`
	# signal (every Button has one), to the methods below — see
	# board_cell.gd's header comment for what a signal/connection is.
	_board_view.tile_dropped.connect(_on_board_tile_dropped)
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


# Called when the End Turn button is pressed. `end_turn()` itself refuses
# and returns false if an invalid word is currently on the board (see
# word_challenge.gd) — printing to the console is a placeholder for real
# on-screen feedback, not built yet.
func _on_end_turn_pressed() -> void:
	if not _session.end_turn():
		print("Cannot end turn: an invalid word is on the board.")
	_refresh_views()


# Redraws both views from the session's current state — the board, and
# whichever player's turn it currently is.
func _refresh_views() -> void:
	_board_view.refresh(_session.board)
	_hand_view.refresh(_session.bag.hand(_session.turns.current_player_id()))
