# Entry point for the game's main scene: creates a GameSession, shows its
# board/hand via BoardView/HandView, turns a dropped tile (see
# board_view.gd's `tile_dropped` signal) into an actual placement, and ends
# the current turn on a button press. This is the one place allowed to know
# about both the game rules (GameSession) and the views — BoardView/
# HandView never talk to GameSession directly, per this project's
# "separate logic from graphics" rule. `StartMenu` lets the player pick how
# many people are playing before `_session` even exists — or, instead,
# start the free-play sandbox (see `_on_free_play_chosen`/
# free_play_session.gd), which uses `_free_play_session` in `_session`'s
# place. Everything that needs one or the other stays hidden until then
# (see `_on_player_count_chosen`/`_return_to_start_menu`).
extends Control

# How long the end-turn notification takes to fade in/out (see
# `_set_end_turn_message` below) — snapping straight to full opacity read
# as too abrupt for something popping up over the header.
const _END_TURN_MESSAGE_FADE_SECONDS := 0.25

# Kept as a field (not a local var in _ready()) now that other methods
# below — `_on_board_tile_dropped`, `_on_end_turn_pressed` — need to reach
# it too.
var _session: GameSession

# The free-play sandbox session (see free_play_session.gd), used INSTEAD
# of `_session` when the player picks "Jeu libre" — at most one of the
# two is ever non-null at a time (see `_is_free_play`).
var _free_play_session: FreePlaySession

# The active crossword puzzle (see crossword_puzzle.gd), used ALONGSIDE
# `_free_play_session` when the player picks "Mots croisés" — crossword
# mode is free play's exact same sandbox mechanics, plus this overlay
# checking the board against a hidden target (see `_is_crossword`).
var _crossword_puzzle: CrosswordPuzzle

# Loaded once and reused for every session this app run creates (see
# `_dictionary()` below) — every one of GameSession/FreePlaySession's own
# constructors would otherwise happily load its own fresh copy, but
# reading and normalizing the ~438,000-line word list takes a couple of
# real seconds, noticeable every single time a mode is (re)started.
var _shared_dictionary: FrenchDictionary
# Loads that dictionary in the background, started as soon as the start
# menu shows (see `_ready()`), so it's usually done by the time a mode is
# picked instead of freezing the screen for those seconds then. A Thread
# is a real OS thread, like std::thread / Python's threading.Thread.
var _dictionary_thread := Thread.new()

# Whether the turn timer (see doc/RULES.md's "3-minute soft/hard time
# limit") is switched on right now — a player preference toggled via
# OptionsPanel's `_timer_toggle`, independent of any particular
# GameSession (it survives a New Game).
var _timer_enabled: bool = false

# Set while waiting for a save (triggered from the New Game confirmation
# dialog's "Sauvegarder d'abord" button, see
# `_on_save_and_restart_pressed`) to actually finish, so
# `_on_save_file_selected` knows to follow it up with `_start_new_game`
# — but only THAT save, not an ordinary standalone one from SaveButton.
var _pending_new_game_after_save: bool = false

# How many players StartMenu's count buttons picked, remembered between
# that click and `_on_names_confirmed` actually creating the session —
# NameEntry needs to know how many name fields to show in between.
var _pending_player_count: int = 0

# The names entered in NameEntry for the CURRENT turn-based session,
# index 0 = player 0, etc. — used by `_player_name` below instead of the
# plain "Joueur N" fallback. Not saved/loaded (see save_game.gd — a
# loaded session has no names of its own), and irrelevant to free
# play/crossword mode, which never shows per-player names at all.
var _player_names: Array[String] = []

# `ConfirmationDialog` only comes with two buttons (OK/Cancel) built in —
# this third one is added in code below (see `_ready()`), not declared in
# the scene, so it needs a plain `var` here instead of `@onready`.
var _save_and_restart_button: Button

# `$Layout/Margin/Content/...` look up the child nodes declared in scenes/main.tscn —
# the same shorthand board_view.gd itself uses for its own "Grid" child.
# `@onready` runs this once those children actually exist, rather than too
# early (see board_view.gd for the same pattern). Everything visible lives
# inside `Content`, a VBoxContainer that stacks top-to-bottom instead of
# each node anchoring itself to a hand-picked pixel offset, so the whole
# screen reflows on a narrower/taller window (a phone in portrait) instead
# of clipping or running off-screen. `Layout`, the actual direct child of
# Main, is a plain ScrollContainer around it all — a safety net in case
# some screen is too short to fit everything (the whole page scrolls
# rather than cropping), not part of the reflow itself. `Margin`, right
# below it, just adds a fixed border of breathing room around `Content`
# on all four sides (see main.tscn) so nothing sits flush against the
# screen edge.
@onready var _title: Label = $BoxLid/Box/Hinge/Lid/CardArea/Card/Items/Title
@onready var _tagline: Label = $BoxLid/Box/Hinge/Lid/CardArea/Card/Items/Tagline
# Stacks BoardView above HandArea (the hand/palette/clue list, grouped so
# they move as one) in portrait, or arranges them side by side in
# landscape (see `_update_play_area_orientation` below). Declared as a
# plain `BoxContainer` in main.tscn rather than `VBoxContainer` — Godot
# specifically refuses to change `vertical` at runtime on VBoxContainer/
# HBoxContainer (its own two fixed-orientation convenience subclasses),
# only on their shared base class.
@onready var _play_area: BoxContainer = $Layout/Margin/Content/PlayArea
# Their margin/position are what `_update_board_height_budget` measures.
@onready var _margin: MarginContainer = $Layout/Margin
@onready var _content: VBoxContainer = $Layout/Margin/Content
@onready var _board_view: BoardView = $Layout/Margin/Content/PlayArea/BoardView
@onready var _hand_view: HandView = $Layout/Margin/Content/PlayArea/HandArea/HandBar/HandView
# Beside the hand: where the player drags tiles to give them back to the
# bag at the end of their turn (see discard_tray.gd).
@onready
var _discard_tray: DiscardTray = $Layout/Margin/Content/PlayArea/HandArea/HandBar/DiscardTray
@onready var _end_turn_button: Button = $Layout/Margin/Content/TopBar/EndTurnButton
@onready var _scoreboard: Label = $Layout/Margin/Content/Scoreboard
@onready var _game_over_banner: Label = $Layout/Margin/Content/GameOverBanner
# A floating notification, not part of the page's normal top-to-bottom
# flow — declared as a direct child of Main in main.tscn (a sibling of
# Layout, not nested inside it) so it draws OVER the header instead of
# pushing the board down, and disappears on its own after
# `_end_turn_message_timer` runs out (see `_set_end_turn_message` below).
# `_end_turn_banner` is the whole colored box, `_end_turn_message` is just
# its text.
@onready var _end_turn_banner: PanelContainer = $EndTurnMessage
@onready var _end_turn_message: Label = $EndTurnMessage/Text
@onready var _end_turn_message_timer: Timer = $EndTurnMessageTimer
# Save/Load/New Game live inside the "☰" dropdown menu (OptionsPanel)
# instead of a permanently-visible row, to leave more of a small phone
# screen for the board itself — see `_on_options_pressed`.
@onready var _save_button: Button = $Layout/Margin/Content/OptionsPanel/MenuItems/SaveButton
@onready var _load_button: Button = $Layout/Margin/Content/OptionsPanel/MenuItems/LoadButton
@onready var _new_game_button: Button = $Layout/Margin/Content/OptionsPanel/MenuItems/NewGameButton
# "Comment jouer": the animated rules (see how_to_play_view.gd), opened
# from the start menu or the ☰ menu, drawn over everything else.
@onready var _how_to_play: HowToPlayView = $HowToPlay
@onready var _how_to_play_button: Button = %StartMenu/HowToPlayButton
@onready var _how_to_play_menu_button: Button = %HowToPlayMenuButton
@onready var _options_button: Button = $Layout/Margin/Content/TopBar/OptionsButton
@onready var _options_panel: PanelContainer = $Layout/Margin/Content/OptionsPanel
@onready var _timer_toggle: CheckButton = $Layout/Margin/Content/OptionsPanel/MenuItems/TimerToggle
@onready var _turn_timer_label: Label = $Layout/Margin/Content/TopBar/TurnTimerLabel
# The game's one-hour clock in the header (see game_clock.gd).
@onready var _game_clock: GameClock = $Layout/Margin/Content/TopBar/GameClock
@onready var _turn_timer: Timer = $TurnTimer
@onready var _save_file_dialog: FileDialog = $SaveFileDialog
@onready var _load_file_dialog: FileDialog = $LoadFileDialog
@onready var _new_game_confirm_dialog: ConfirmationDialog = $NewGameConfirmDialog
# `%Name` finds a node marked "unique name in owner" in main.tscn (shown
# with a % in the editor) wherever it sits in the scene — shorter than
# the full `$Path/To/Node`, and survives moving the node around.
@onready var _start_menu: Control = %StartMenu
@onready var _one_player_button: Button = %StartMenu/Players/Button1
@onready var _two_players_button: Button = %StartMenu/Players/Button2
@onready var _three_players_button: Button = %StartMenu/Players/Button3
@onready var _four_players_button: Button = %StartMenu/Players/Button4
@onready var _name_entry_menu: Control = %NameEntry
# The 4 name fields, indexed 0..3 to match player_id — see
# `_on_player_count_chosen` for how only the first `count` of them are
# actually shown.
@onready var _name_edits: Array[LineEdit] = [
	%NameEntry/Name1Edit,
	%NameEntry/Name2Edit,
	%NameEntry/Name3Edit,
	%NameEntry/Name4Edit,
]
@onready var _start_with_names_button: Button = %NameEntry/Buttons/StartButton
@onready var _back_button: Button = %NameEntry/Buttons/BackButton
@onready var _free_play_button: Button = %StartMenu/FreePlayButton
@onready var _palette_view: HandView = $Layout/Margin/Content/PlayArea/HandArea/PaletteView
# The 10 crossword puzzle-picker buttons, indexed 0..9 to match
# CrosswordPuzzles.PUZZLES — see `_ready()`'s loop below for how each one
# reports its own index, and crossword_puzzles.gd for the data itself.
@onready var _crossword_puzzle_buttons: Array[Button] = [
	%StartMenu/CrosswordGrid/CrosswordPuzzle1Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle2Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle3Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle4Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle5Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle6Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle7Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle8Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle9Button,
	%StartMenu/CrosswordGrid/CrosswordPuzzle10Button,
]
@onready var _clue_list_view: ClueListView = $Layout/Margin/Content/PlayArea/HandArea/ClueListView
# Portrait's replacement for `_clue_list_view`: a "?" per board row that
# pops up its clue in a speech bubble (see clue_markers_view.gd). A child
# of BoardView in main.tscn, so it sits over the board and scrolls with it.
@onready var _clue_markers: ClueMarkersView = $Layout/Margin/Content/PlayArea/BoardView/ClueMarkers
# The start screen: the closed game box, with the menu on a card on its
# lid (see box_lid.gd). Choosing a mode sets the game up underneath
# FIRST, then slides the lid away to reveal it — in multiplayer, that
# reveals the handoff screen ("Au tour de …") (see `_begin_turn`), drawn under the
# lid in main.tscn, so player 1's tiles still stay hidden until "C'est moi !".
@onready var _box_lid: BoxLid = $BoxLid
# The handoff screen ("Au tour de Alice", "C'est moi !") shown between two players' turns (see
# `_begin_turn` below). Like EndTurnMessage, it's a direct child of Main,
# declared AFTER Layout in main.tscn so it draws over the whole page —
# board and hand included — and, being a fully opaque `Panel` (same theme
# background as `Background`), hides the next player's tiles until they
# press "C'est moi !". A `Panel` also swallows every click/touch that lands on
# it, so nothing underneath can be dragged while it's up.
@onready var _handoff_screen: Panel = $HandoffScreen
@onready var _handoff_player_name: Label = $HandoffScreen/Center/Items/PlayerName
@onready var _handoff_play_button: Button = $HandoffScreen/Center/Items/PlayButton
# Everyone's score, and what the last turn changed (see score_recap.gd).
@onready var _score_recap: ScoreRecap = $HandoffScreen/Center/Items/ScoreRecap


func _ready() -> void:
	# Godot batches mouse-motion events into one per rendered frame by
	# default (meant to save work for games that don't need per-event
	# precision). For a dragged tile that means it visibly lags behind the
	# real cursor whenever the frame rate dips below the mouse's own
	# polling rate. Turning accumulation off delivers every motion event
	# immediately instead, which is the standard fix for sluggish-feeling
	# drags in Godot.
	Input.set_use_accumulated_input(false)

	# `start()` runs the given function on the thread; its return value is
	# handed back by `wait_to_finish()` (see `_dictionary()`). Safe here:
	# loading only reads a file and fills a brand-new object nothing else
	# touches until then.
	_dictionary_thread.start(func() -> FrenchDictionary: return FrenchDictionary.new())

	# Connects BoardView's/HandView's signals, and every button's built-in
	# `pressed` signal (every Button has one), to the methods below — see
	# board_cell.gd's header comment for what a signal/connection is.
	_board_view.tile_dropped.connect(_on_board_tile_dropped)
	_board_view.tile_moved.connect(_on_board_tile_moved)
	_board_view.word_moved.connect(_on_board_word_moved)
	_board_view.tile_flipped.connect(_on_board_tile_flipped)
	# Keeps the free-play palette's (and the turn-based hand's) own tile
	# size in sync with the board's, which BoardView already recomputes on
	# every viewport resize (see board_view.gd's `_update_cell_sizes`) —
	# without this, resizing the window would resize the board but leave
	# the hand/palette at whatever size they were last drawn at.
	_board_view.cell_size_changed.connect(_on_board_cell_size_changed)
	# A wide-but-short window (a computer) fits the board and hand/palette
	# comfortably side by side; a narrow-but-tall one (a phone) doesn't —
	# see `_update_play_area_orientation` below.
	get_viewport().size_changed.connect(_update_play_area_orientation)
	_update_play_area_orientation()
	# `item_rect_changed` fires whenever a Control's position or size
	# changes — here, whenever something above PlayArea (the scoreboard
	# wrapping onto a second line, the game-over banner appearing) pushes
	# the board down, so its height budget can follow.
	_play_area.item_rect_changed.connect(_update_board_height_budget)
	_hand_view.tile_returned.connect(_on_hand_tile_returned)
	_end_turn_button.pressed.connect(_on_end_turn_pressed)
	_discard_tray.tile_marked.connect(_on_tile_marked_for_discard)
	_hand_view.tile_unmarked.connect(_on_tile_unmarked_for_discard)
	_handoff_play_button.pressed.connect(_on_handoff_play_pressed)
	_save_button.pressed.connect(_on_save_pressed)
	_load_button.pressed.connect(_on_load_pressed)
	_new_game_button.pressed.connect(_on_new_game_pressed)
	_how_to_play_button.pressed.connect(_how_to_play.open)
	_game_clock.is_running = _is_play_clock_running
	_how_to_play_menu_button.pressed.connect(_on_how_to_play_menu_pressed)
	_options_button.pressed.connect(_on_options_pressed)
	_timer_toggle.toggled.connect(_on_timer_toggle_toggled)
	_end_turn_message_timer.timeout.connect(_on_end_turn_message_timer_timeout)
	_save_file_dialog.file_selected.connect(_on_save_file_selected)
	_load_file_dialog.file_selected.connect(_on_load_file_selected)
	# Cancelling the save dialog must also cancel a pending "save and
	# restart" (see `_pending_new_game_after_save`'s own comment) — a
	# FileDialog emits `canceled` when closed without picking a file,
	# the same way `file_selected` fires when one WAS picked.
	_save_file_dialog.canceled.connect(_on_save_file_dialog_canceled)
	# `confirmed` fires when the dialog's own built-in OK button (relabeled
	# "Recommencer" — see scenes/main.tscn) is pressed.
	_new_game_confirm_dialog.confirmed.connect(_start_new_game)
	# `add_button` is how a ConfirmationDialog gets a THIRD button beyond
	# its built-in OK/Cancel — it returns the new Button itself, the same
	# as if it had been declared in the scene, so its `pressed` signal
	# connects the usual way.
	_save_and_restart_button = _new_game_confirm_dialog.add_button("Sauvegarder d'abord", true)
	_save_and_restart_button.pressed.connect(_on_save_and_restart_pressed)
	# `Callable.bind(n)` hands the same method one extra fixed argument
	# per button (n itself), rather than writing four near-identical
	# handler functions that only differ by which number they pass on.
	_one_player_button.pressed.connect(_on_player_count_chosen.bind(1))
	_two_players_button.pressed.connect(_on_player_count_chosen.bind(2))
	_three_players_button.pressed.connect(_on_player_count_chosen.bind(3))
	_four_players_button.pressed.connect(_on_player_count_chosen.bind(4))
	_start_with_names_button.pressed.connect(_on_names_confirmed)
	_back_button.pressed.connect(_on_back_to_start_menu_pressed)
	_free_play_button.pressed.connect(_on_free_play_chosen)
	# Same `Callable.bind` idea as the player-count buttons above, but
	# looped since there are 10 of them — each button reports its own
	# index in `_crossword_puzzle_buttons` (and in CrosswordPuzzles.PUZZLES)
	# instead of main.gd needing 10 near-identical connect lines.
	for i in range(_crossword_puzzle_buttons.size()):
		_crossword_puzzle_buttons[i].pressed.connect(_on_crossword_chosen.bind(i))
	# The palette (free play) and the hand (turn-based play) are never both
	# visible at once, so both can safely feed the same handler.
	_palette_view.tile_returned.connect(_on_hand_tile_returned)

	_return_to_start_menu()


# Called whenever a TilePanel gets dropped onto a BoardCell (see
# board_cell.gd/tile_panel.gd). GrowthRules legality was already checked
# before Godot even allowed the drop (`BoardCell._can_drop_data`), but
# `place_tile` also checks the tile is actually in the current player's
# hand — belt and braces, and the only check that actually matters here
# since the view layer already filtered out illegal board positions.
func _on_board_tile_dropped(row: int, col: int, tile: Tile) -> void:
	if _is_free_play():
		_free_play_session.place_tile(row, col, tile)
	else:
		_session.place_tile(row, col, tile)
	_refresh_views()


# Called whenever a BoardCell gets dragged onto another BoardCell (see
# board_cell.gd) — relocating a tile the current player placed earlier this
# same turn. `move_own_tile` re-checks eligibility itself (the view layer
# only used it to decide whether to even start the drag), so an already-
# frozen tile that somehow got dragged anyway simply won't move.
func _on_board_tile_moved(from_row: int, from_col: int, to_row: int, to_col: int) -> void:
	if _is_free_play():
		_free_play_session.move_tile(from_row, from_col, to_row, to_col)
	else:
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
	if _is_free_play():
		_free_play_session.flip_tile(row, col)
	else:
		_session.flip_tile(row, col)
	_refresh_views()


# BoardView itself starts recomputing cell sizes from `_ready()` onward,
# well before either kind of session might exist (e.g. resizing the window
# while still at the start menu) — `_refresh_views()` reaches straight into
# `_session`/`_free_play_session`, so it'd error without this guard.
func _on_board_cell_size_changed(_side: float) -> void:
	if _is_free_play() or _session != null:
		_refresh_views()


# Landscape (wider than tall) arranges PlayArea's two children — BoardView
# and HandArea — side by side instead of stacked, matching BoardView's own
# `_is_landscape` check so the board's own width math (see
# `LANDSCAPE_BOARD_WIDTH_FRACTION`) agrees with how the page is actually
# laid out. `vertical = true` behaves exactly like a VBoxContainer (today's
# portrait layout); `false` behaves exactly like an HBoxContainer — that's
# genuinely the entire difference between those two node types under the
# hood, `BoxContainer.vertical` is just normally locked once you pick one.
func _update_play_area_orientation() -> void:
	_play_area.vertical = _is_portrait()


# Tells BoardView how much of the screen's height isn't its own (see its
# `vertical_overhead`/`reserved_bottom_rows`), so the board, plus the hand
# under it in portrait turn-based play, fits on screen without scrolling.
# The overhead is measured from the real layout — where PlayArea starts,
# plus the page's bottom margin — rather than guessed, since the
# scoreboard's height depends on how many players there are. That
# position never depends on the board's own size, so re-fitting the
# board can't feed back into it. The ☰ menu (OptionsPanel) is left out:
# it's only open briefly, and counting it would shrink the board while
# it's open.
func _update_board_height_budget() -> void:
	var overhead := _content.position.y + _play_area.position.y
	overhead += _margin.get_theme_constant("margin_bottom")
	if _options_panel.visible:
		overhead -= _options_panel.size.y + _content.get_theme_constant("separation")
	_board_view.vertical_overhead = overhead
	# 2 rows hold about 20 tiles on a phone — a whole hand, which starts at
	# 9-14 tiles (see draw_plan.gd). Only in turn-based portrait: free
	# play's palette holds all 117 tiles and scrolls anyway, and in
	# landscape the hand sits beside the board instead of below it.
	var hand_below := _session != null and _is_portrait()
	_board_view.reserved_bottom_rows = 2 if hand_below else 0


# Whether the window is at least as tall as it is wide (a phone held
# upright) — read straight off the viewport rather than `_play_area.
# vertical`, since BoardView's own resize handling can trigger
# `_refresh_views` before `_update_play_area_orientation` has caught up.
func _is_portrait() -> bool:
	var size := get_viewport_rect().size
	return size.x <= size.y


# Called whenever a BoardCell gets dragged onto the hand row OR the
# free-play palette (see hand_row.gd) — giving a tile back to the current
# player's storage, or back to the free-play pool of available tiles.
func _on_hand_tile_returned(from_row: int, from_col: int) -> void:
	if _is_free_play():
		_free_play_session.remove_tile(from_row, from_col)
	else:
		_session.remove_tile(from_row, from_col)
	_refresh_views()


# Called when one of StartMenu's player-count buttons is pressed (see
# `Callable.bind` above for how each one reports its own number). Doesn't
# create the GameSession yet — shows NameEntry first (see
# `_on_names_confirmed`, which actually creates it), with exactly `count`
# of its 4 name fields visible.
func _on_player_count_chosen(count: int) -> void:
	_pending_player_count = count
	_start_menu.visible = false
	_name_entry_menu.visible = true
	for i in range(_name_edits.size()):
		_name_edits[i].visible = i < count
		_name_edits[i].text = ""


# Called when NameEntry's "Commencer" button is pressed. Only NOW
# does the actual GameSession get created — nothing before this point
# could show real game state anyway, since GameSession would build its
# own French dictionary and starting hands. A blank name field falls
# back to the plain "Joueur N" (see `_player_name`).
func _on_names_confirmed() -> void:
	_player_names = []
	for i in range(_pending_player_count):
		var entered := _name_edits[i].text.strip_edges()
		_player_names.append(entered if entered != "" else "Joueur %d" % (i + 1))
	_session = GameSession.new(_pending_player_count, _dictionary())
	_name_entry_menu.visible = false
	_title.visible = false
	_tagline.visible = false
	_board_view.visible = true
	_hand_view.visible = true
	_end_turn_button.visible = true
	_discard_tray.visible = true
	_save_button.visible = true
	_load_button.visible = true
	_new_game_button.visible = true
	_refresh_views()
	_begin_turn()
	_box_lid.open()


# Called when NameEntry's "Retour" button is pressed — no session
# exists yet at this point, so there's nothing to tear down, just go
# back to picking a player count.
func _on_back_to_start_menu_pressed() -> void:
	_name_entry_menu.visible = false
	_start_menu.visible = true


# The one FrenchDictionary this whole app run ever loads (see
# `_shared_dictionary` above) — every GameSession/FreePlaySession this
# script creates should be handed this instead of leaving their own
# constructor default to loading yet another copy.
func _dictionary() -> FrenchDictionary:
	if _shared_dictionary == null:
		# Joins the background load: returns at once if it's finished,
		# otherwise blocks until it is (a mode picked within the first
		# couple of seconds).
		_shared_dictionary = _dictionary_thread.wait_to_finish()
	return _shared_dictionary


# Built-in callback Godot calls when this node leaves the scene tree
# (here: the app quitting). A Thread must be joined before it's freed,
# or Godot warns — e.g. when quitting from the menu before ever playing.
func _exit_tree() -> void:
	if _dictionary_thread.is_started():
		_dictionary_thread.wait_to_finish()


# Whether the free-play sandbox is the one currently active, rather than
# a turn-based `_session` (or nothing at all, at the start menu).
func _is_free_play() -> bool:
	return _free_play_session != null


# Called when StartMenu's "Jeu libre" button is pressed. Unlike
# `_on_player_count_chosen`, there's no turn to end and no timer to run
# (EndTurnButton stays hidden), but Save/Load do work here — see
# save_game.gd's `save_free_play`/`load_free_play`.
func _on_free_play_chosen() -> void:
	_free_play_session = FreePlaySession.new(_dictionary())
	_start_menu.visible = false
	_title.visible = false
	_tagline.visible = false
	_board_view.visible = true
	_palette_view.visible = true
	_new_game_button.visible = true
	_save_button.visible = true
	_load_button.visible = true
	_refresh_views()
	_box_lid.open()


# Whether a crossword puzzle is the one currently active — always
# alongside a `_free_play_session` (see `_on_crossword_chosen`), never on
# its own.
func _is_crossword() -> bool:
	return _crossword_puzzle != null


# Called when one of StartMenu's crossword puzzle-number buttons is
# pressed (see the `Callable.bind` loop in `_ready()` for how each one
# reports its own index). Reuses free play's exact sandbox (all 117
# tiles, unrestricted placement, live scoring — see `_on_free_play_chosen`
# above) and adds the clue list alongside it; `_refresh_views` is what
# actually checks progress against the puzzle's hidden target each time
# something changes.
func _on_crossword_chosen(puzzle_index: int) -> void:
	_free_play_session = FreePlaySession.new(_dictionary())
	_crossword_puzzle = CrosswordPuzzle.new(puzzle_index)
	_start_menu.visible = false
	_title.visible = false
	_tagline.visible = false
	_board_view.visible = true
	_palette_view.visible = true
	_new_game_button.visible = true
	_save_button.visible = true
	_load_button.visible = true
	_refresh_views()
	_box_lid.open()


# Called when the New Game button is pressed — asks first, rather than
# abandoning the current session outright (see `_start_new_game`,
# `_on_save_and_restart_pressed`, and NewGameConfirmDialog in
# scenes/main.tscn for the actual 3-way choice: restart without saving,
# save first then restart, or cancel and keep playing). Closes the "☰"
# dropdown first — otherwise it would stay open, floating over the
# confirmation dialog.
func _on_new_game_pressed() -> void:
	_options_panel.visible = false
	_new_game_confirm_dialog.popup_centered()


# Actually abandons the current session (unsaved progress is lost unless
# the player chose "Sauvegarder d'abord" first — see
# `_on_save_and_restart_pressed`) and goes back to `StartMenu`. Connected
# to NewGameConfirmDialog's `confirmed` signal (its built-in OK button,
# relabeled "Recommencer" in scenes/main.tscn).
func _start_new_game() -> void:
	_session = null
	_free_play_session = null
	_crossword_puzzle = null
	_return_to_start_menu()


# Called when NewGameConfirmDialog's extra "Sauvegarder d'abord"
# button is pressed. Closes this dialog and opens the ordinary save
# FileDialog instead — `_on_save_file_selected` is what actually follows
# through with `_start_new_game` once that save completes, via
# `_pending_new_game_after_save`.
func _on_save_and_restart_pressed() -> void:
	_new_game_confirm_dialog.hide()
	_pending_new_game_after_save = true
	_save_file_dialog.popup_centered()


# Called if the save FileDialog is dismissed without picking a file —
# clears a pending "save and restart" so it doesn't wrongly fire on
# some LATER, unrelated save (see `_pending_new_game_after_save`).
func _on_save_file_dialog_canceled() -> void:
	_pending_new_game_after_save = false


# Called when the options button ("☰") is pressed — just shows/hides
# OptionsPanel, the dropdown menu holding Save/Load/New Game/the timer
# toggle. Showing the button itself doesn't need `_session` to exist, but
# Save/Load/New Game inside the menu are hidden individually until a game
# actually starts, the same as before (see `_on_names_confirmed`/
# `_return_to_start_menu`).
func _on_options_pressed() -> void:
	_options_panel.visible = not _options_panel.visible


# Called when OptionsPanel's timer CheckButton is switched. Turning it on
# starts counting down immediately for whichever turn is already under
# way (see `_restart_turn_timer`); turning it off just hides the
# countdown rather than leaving a stale number on screen.
func _on_timer_toggle_toggled(enabled: bool) -> void:
	_timer_enabled = enabled
	if enabled:
		_restart_turn_timer()
	else:
		_turn_timer.stop()
		_turn_timer_label.visible = false


# Restarts the 3-minute countdown from the top — called whenever a new
# turn actually begins (a fresh session, a successful End Turn, or a
# loaded save) — but only if the timer is switched on AND a game is
# actually in progress; a no-op otherwise (e.g. toggled on while still
# at the start menu, with no `_session` yet to time).
func _restart_turn_timer() -> void:
	if not _timer_enabled or _session == null:
		return
	_turn_timer_label.visible = true
	_turn_timer.start()


# Runs every frame (defining `_process` is all a Node needs to opt in —
# see board_cell.gd's `_gui_input` comment for the same idea applied to
# input instead of per-frame updates) to keep the countdown label
# showing the actual time left on `_turn_timer`. Once it reaches zero,
# `time_left` just reads 0 from then on (Godot stops a one-shot Timer
# there on its own) — this is the RULES.md "soft" limit: a reminder,
# nothing is forced.
func _process(_delta: float) -> void:
	if not _timer_enabled or _session == null:
		return
	var seconds_left := int(ceil(_turn_timer.time_left))
	if seconds_left > 0:
		_turn_timer_label.remove_theme_color_override("font_color")
		_turn_timer_label.text = "%d:%02d" % [seconds_left / 60, seconds_left % 60]
	else:
		_turn_timer_label.add_theme_color_override("font_color", Color(0.96, 0.4, 0.4, 1))
		_turn_timer_label.text = "Temps écoulé !"


# Whether the hour is ticking: only while someone is actually playing a
# turn — not on the handoff screen ("Au tour de …"), in a menu or dialog, in
# "Comment jouer", or with the box closed (see game_clock.gd).
func _is_play_clock_running() -> bool:
	return (
		not _handoff_screen.visible
		and not _options_panel.visible
		and not _how_to_play.visible
		and not _box_lid.visible
		and not _save_file_dialog.visible
		and not _load_file_dialog.visible
		and not _new_game_confirm_dialog.visible
	)


# Called when the Save button is pressed — opens the save FileDialog (see
# scenes/main.tscn) rather than saving straight away, so the player picks
# where. The actual write happens in `_on_save_file_selected` once they
# confirm a path. Closes the "☰" dropdown first, the same reasoning as
# `_on_new_game_pressed`.
func _on_save_pressed() -> void:
	_options_panel.visible = false
	_save_file_dialog.popup_centered()


# Called when the Load button is pressed — same idea as `_on_save_pressed`,
# for the load FileDialog.
func _on_load_pressed() -> void:
	_options_panel.visible = false
	_load_file_dialog.popup_centered()


# Called once the player confirms a path in the save FileDialog. Free
# play/crossword mode save through `_free_play_session` instead of
# `_session` — see save_game.gd's `save_free_play`, which also records
# which crossword puzzle (if any) was active, so it can be shown again
# on load (see `_on_load_file_selected` below).
func _on_save_file_selected(path: String) -> void:
	if _is_free_play():
		var puzzle_index := _crossword_puzzle.puzzle_index if _is_crossword() else -1
		SaveGame.save_free_play(_free_play_session, path, puzzle_index)
	else:
		SaveGame.save(_session, path)
	if _pending_new_game_after_save:
		_pending_new_game_after_save = false
		_start_new_game()


# Called once the player confirms a path in the load FileDialog. Reuses
# whichever session's dictionary is already loaded (see save_game.gd)
# rather than paying to reload the ~421,000-word list again. Does nothing
# if the file turns out missing, unreadable, or the wrong format for the
# CURRENT mode (see `SaveGame.load`/`load_free_play` — a turn-based save
# can't be loaded from within free play/crossword mode, or vice versa;
# start that other mode first, the same way Save/Load themselves are
# only ever available once a mode is already chosen).
func _on_load_file_selected(path: String) -> void:
	if _is_free_play():
		var loaded = SaveGame.load_free_play(path, _free_play_session.dictionary)
		if loaded != null:
			_free_play_session = loaded["session"]
			var puzzle_index: int = loaded["puzzle_index"]
			_crossword_puzzle = (CrosswordPuzzle.new(puzzle_index) if puzzle_index != -1 else null)
			_refresh_views()
		return
	var loaded_session := SaveGame.load(path, _session.dictionary)
	if loaded_session != null:
		_session = loaded_session
		_player_names = []  # a loaded session has no names of its own
		_set_end_turn_message("")
		_refresh_views()
		_begin_turn()


# Called when the End Turn button is pressed. `end_turn()` itself refuses
# and returns false if an invalid word is currently on the board (see
# word_challenge.gd) — in which case `_end_turn_message` explains why,
# listing the offending word(s) (see `_format_end_turn_error` below).
# Cleared on success, since the turn actually ended.
func _on_end_turn_pressed() -> void:
	# So the recap between turns can show what this turn changed.
	_score_recap.remember_scores(_session)
	if _session.end_turn():
		_set_end_turn_message("")
		_begin_turn()
	else:
		_set_end_turn_message(_format_end_turn_error())
	_refresh_views()


# "Comment jouer" from the ☰ menu: closes the menu, then shows the rules.
func _on_how_to_play_menu_pressed() -> void:
	_options_panel.visible = false
	_how_to_play.open()


# A hand tile dragged into the discard tray: it'll go back to the bag
# when the turn ends.
func _on_tile_marked_for_discard(tile: Tile) -> void:
	_session.set_marked_for_discard(tile, true)
	_refresh_views()


# A tile dragged from the discard tray back into the hand: keep it after
# all.
func _on_tile_unmarked_for_discard(tile: Tile) -> void:
	_session.set_marked_for_discard(tile, false)
	_refresh_views()


# Starts whoever's turn it now is. With 2+ players sharing one device, the
# next player's hand must stay hidden from the one who just played, so
# this first covers the screen with HandoffScreen showing who's up next —
# the real turn (and its timer) only starts once they press "C'est moi !" (see
# `_on_handoff_play_pressed`). Solo play, and a game that just ended,
# have nobody to hand over to, so they skip straight to the timer.
func _begin_turn() -> void:
	if _session.turns.number_of_players == 1 or _session.is_game_over():
		_restart_turn_timer()
		return
	_handoff_player_name.text = _player_name(_session.turns.current_player_id())
	var names: Array[String] = []
	for player_id in range(_session.turns.number_of_players):
		names.append(_player_name(player_id))
	_score_recap.show_scores(_session, names)
	_handoff_screen.visible = true
	# Paused while the device changes hands, so passing it around doesn't
	# eat into the next player's 3 minutes.
	_turn_timer.stop()


# Called when HandoffScreen's "C'est moi !" button is pressed — the next player
# is at the screen, so reveal their hand and start their clock.
func _on_handoff_play_pressed() -> void:
	_handoff_screen.visible = false
	_restart_turn_timer()


# Sets a status label's text AND hides it whenever that text is empty —
# used for `_scoreboard`/`_game_over_banner`, which spend most of their
# time with nothing to say. A `Label` still reserves its own line height in
# `Content`'s VBoxContainer even while showing an empty string (only
# `visible=false` actually removes it), so setting `.text` alone left a
# permanent gap between the header and the board.
func _set_status_text(label: Label, text: String) -> void:
	label.text = text
	label.visible = text != ""


# Same idea as `_set_status_text` above, but for the end-turn notification
# specifically: it's a `PanelContainer` (for its colored background, see
# main.tscn's EndTurnBannerStyle), not a plain `Label`, so the text and the
# visibility toggle land on two different nodes (see `_end_turn_banner`/
# `_end_turn_message` above) — and, being a notification rather than a
# persistent status line, it fades in, then fades back out on its own once
# `_end_turn_message_timer` runs out (see `_on_end_turn_message_timer_
# timeout` below) rather than staying up until something else clears it.
func _set_end_turn_message(text: String) -> void:
	_end_turn_message.text = text
	_end_turn_message_timer.stop()
	if text == "":
		_fade_out_end_turn_banner()
		return
	_end_turn_banner.visible = true
	_end_turn_banner.modulate.a = 0.0
	# `create_tween()` hands back a fresh Tween — Godot's way of animating a
	# property smoothly over time instead of jumping straight to the target
	# value; `tween_property` says WHICH property (here, the banner's own
	# alpha/transparency), to WHAT value, over how many seconds.
	create_tween().tween_property(
		_end_turn_banner, "modulate:a", 1.0, _END_TURN_MESSAGE_FADE_SECONDS
	)
	_end_turn_message_timer.start()


# Called once `_end_turn_message_timer` (see main.tscn, 4 seconds, one-shot)
# runs out — fades out whatever notification is currently showing, same as
# if `_set_end_turn_message("")` had been called directly.
func _on_end_turn_message_timer_timeout() -> void:
	_fade_out_end_turn_banner()


# Shared by both ways the notification can go away (an explicit clear, or
# the timer running out) — animates its alpha back down to 0, then actually
# hides it (`visible = false`) only once that finishes, so a still-fading
# banner doesn't just vanish mid-fade.
func _fade_out_end_turn_banner() -> void:
	if not _end_turn_banner.visible:
		return
	var tween := create_tween()
	tween.tween_property(_end_turn_banner, "modulate:a", 0.0, _END_TURN_MESSAGE_FADE_SECONDS)
	tween.tween_callback(func(): _end_turn_banner.visible = false)


# Redraws both views from the session's current state — the board, and
# whichever player's turn it currently is. `_session.can_take_back` is
# passed straight through as a Callable (see board_view.gd's `refresh` for
# what that means) rather than BoardView asking GameSession itself. Also
# checks whether the game has just ended (see `_update_game_over_banner`).
func _refresh_views() -> void:
	# Also re-checked here, not only on PlayArea moving: starting or
	# leaving a turn-based game changes whether the hand needs room.
	_update_board_height_budget()
	# Only a turn-based game is timed; free play has no time limit.
	_game_clock.session = null if _is_free_play() else _session
	if _is_free_play():
		_board_view.refresh(
			_free_play_session.board,
			_free_play_session.can_take_back,
			_free_play_session.can_place_here
		)
		_palette_view.refresh(_free_play_session.available_tiles(), _board_view.cell_side())
		_set_status_text(_scoreboard, "Score : %d" % _free_play_session.current_score())
		# Clues as a list beside the board in landscape, where it fits; as
		# "?" bubbles on the board itself in portrait, where a list would
		# push the palette off-screen.
		_clue_list_view.visible = _is_crossword() and not _is_portrait()
		_clue_markers.visible = _is_crossword() and _is_portrait()
		# Shrinks the cells just enough to fit the "?" column inside the
		# frame. Re-fitting the cells calls back into `_refresh_views`
		# (see `_on_board_cell_size_changed`), but only on an actual
		# change, so this settles after one extra pass.
		_board_view.reserved_right_width = (
			ClueMarkersView.LANE_WIDTH if _clue_markers.visible else 0.0
		)
		if _is_crossword():
			var solved := _crossword_puzzle.solved_rows(_free_play_session.board)
			_clue_list_view.refresh(_crossword_puzzle.clues, solved)
			var row_rects: Array[Rect2] = []
			for row in range(Board.ROWS):
				row_rects.append(_board_view.row_rect(row))
			_clue_markers.refresh(_crossword_puzzle.clues, solved, row_rects)
			if _crossword_puzzle.is_solved(_free_play_session.board):
				_set_status_text(_game_over_banner, "Mots croisés résolus !")
			else:
				_set_status_text(_game_over_banner, "")
		return
	_board_view.refresh(
		_session.board, _session.can_take_back, _session.can_place_here, _session.invalid_cells()
	)
	# The hand shows the tiles the player keeps; the tray beside it, the
	# ones marked to give back.
	var kept: Array[Tile] = []
	for tile in _session.bag.hand(_session.turns.current_player_id()):
		if not _session.is_marked_for_discard(tile):
			kept.append(tile)
	_hand_view.refresh(kept, _board_view.cell_side())
	_discard_tray.refresh(_session.marked_for_discard(), _board_view.cell_side())
	_set_status_text(_scoreboard, _format_scoreboard())
	_update_game_over_banner()


# `_player_names[player_id]` if the player actually entered one (see
# `_on_names_confirmed`), else the plain "Joueur N" default — also what
# every player shows as once a LOADED session has no names of its own.
func _player_name(player_id: int) -> String:
	if player_id < _player_names.size():
		return _player_names[player_id]
	return "Joueur %d" % (player_id + 1)


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
		var entry := "%s : %d pts" % [_player_name(player_id), scores[player_id]]
		if not is_over and player_id == current_player_id:
			entry += " (à vous de jouer)"
		# Scoreboard wraps onto more lines when every player doesn't fit on
		# one (3-4 players on a phone, see main.tscn's `autowrap_mode`) —
		# non-breaking spaces (U+00A0) inside each player's own entry make
		# it break only BETWEEN players, never mid-"Joueur 2 : 5 pts".
		text += entry.replace(" ", " ")
	return text


# Builds the message shown by `_end_turn_banner` after a refused
# `end_turn()` — names the actual invalid word(s) (see game_session.gd's
# `invalid_words`) and/or explains a broken single-row/single-word turn
# (see `invalid_cells`) rather than just saying "no". Both can be true at
# once (an invalid word AND a scattered turn), so each gets its own line
# rather than one crowding the other out.
func _format_end_turn_error() -> String:
	var messages: Array[String] = []

	var words: Array[String] = []
	for entry in _session.invalid_words():
		words.append(entry["word"])
	if words.size() == 1:
		messages.append("Mot invalide : %s" % words[0])
	elif words.size() > 1:
		messages.append("Mots invalides : %s" % ", ".join(words))

	if not _session.invalid_cells().is_empty():
		messages.append(
			"Les lettres jouées ce tour doivent former un seul mot, sur une seule ligne."
		)

	return "\n".join(messages)


# Hides everything that depends on `_session` and shows `StartMenu`
# again — used both by `_ready()` (before any game has started yet) and
# by `_start_new_game()` (after abandoning one).
func _return_to_start_menu() -> void:
	_start_menu.visible = true
	_box_lid.close()
	_title.visible = true
	_tagline.visible = true
	_board_view.visible = false
	_hand_view.visible = false
	_end_turn_button.visible = false
	_discard_tray.visible = false
	_save_button.visible = false
	_load_button.visible = false
	_new_game_button.visible = false
	_palette_view.visible = false
	_clue_list_view.visible = false
	_clue_markers.visible = false
	_board_view.reserved_right_width = 0.0
	_name_entry_menu.visible = false
	_options_panel.visible = false
	_handoff_screen.visible = false
	_player_names = []
	_set_end_turn_message("")
	_set_status_text(_game_over_banner, "")
	_set_status_text(_scoreboard, "")
	_turn_timer.stop()
	_turn_timer_label.visible = false
	_game_clock.session = null


# Shows a winner announcement once the game is over (see
# game_session.gd's `is_game_over`/`winner_id` — either every player is
# exhausted, or the board itself filled up), and disables the End Turn
# button since there's no longer a turn to end. Blank/enabled otherwise.
func _update_game_over_banner() -> void:
	if not _session.is_game_over():
		_set_status_text(_game_over_banner, "")
		_end_turn_button.disabled = false
		return
	var winners := _session.winner_ids()
	var names: Array[String] = []
	for player_id in winners:
		names.append(_player_name(player_id))
	var score := _session.turns.player_score(winners[0])
	var ending := "Temps écoulé ! " if _session.ended_by_time else "Partie terminée ! "
	# One winner: "Alice gagne"; a tie: "Alice et Bruno gagnent" (see
	# GameSession.winner_ids for how ties are broken first).
	var who := (
		"%s gagne" % names[0]
		if names.size() == 1
		else "%s et %s gagnent" % [", ".join(names.slice(0, -1)), names[-1]]
	)
	_set_status_text(_game_over_banner, ending + "%s avec %d points." % [who, score])
	_end_turn_button.disabled = true
