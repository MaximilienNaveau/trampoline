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
# NameEntryMenu needs to know how many name fields to show in between.
var _pending_player_count: int = 0

# The names entered in NameEntryMenu for the CURRENT turn-based session,
# index 0 = player 0, etc. — used by `_player_name` below instead of the
# plain "Joueur N" fallback. Not saved/loaded (see save_game.gd — a
# loaded session has no names of its own), and irrelevant to free
# play/crossword mode, which never shows per-player names at all.
var _player_names: Array[String] = []

# `ConfirmationDialog` only comes with two buttons (OK/Cancel) built in —
# this third one is added in code below (see `_ready()`), not declared in
# the scene, so it needs a plain `var` here instead of `@onready`.
var _save_and_restart_button: Button

# `$Layout/Content/...` look up the child nodes declared in scenes/main.tscn —
# the same shorthand board_view.gd itself uses for its own "Grid" child.
# `@onready` runs this once those children actually exist, rather than too
# early (see board_view.gd for the same pattern). Everything visible lives
# inside `Content`, a VBoxContainer that stacks top-to-bottom instead of
# each node anchoring itself to a hand-picked pixel offset, so the whole
# screen reflows on a narrower/taller window (a phone in portrait) instead
# of clipping or running off-screen. `Layout`, the actual direct child of
# Main, is a plain ScrollContainer wrapping `Content` — a safety net in
# case some screen is too short to fit everything (the whole page scrolls
# rather than cropping), not part of the reflow itself.
@onready var _title: Label = $Layout/Content/Title
@onready var _tagline: Label = $Layout/Content/Tagline
@onready var _board_view: BoardView = $Layout/Content/PlayArea/BoardView
@onready var _hand_view: HandView = $Layout/Content/PlayArea/HandView
@onready var _end_turn_button: Button = $Layout/Content/TopBar/EndTurnButton
@onready var _scoreboard: Label = $Layout/Content/Scoreboard
@onready var _game_over_banner: Label = $Layout/Content/GameOverBanner
@onready var _end_turn_message: Label = $Layout/Content/EndTurnMessage
# Save/Load/New Game live inside the "☰" dropdown menu (OptionsPanel)
# instead of a permanently-visible row, to leave more of a small phone
# screen for the board itself — see `_on_options_pressed`.
@onready var _save_button: Button = $Layout/Content/OptionsPanel/MenuItems/SaveButton
@onready var _load_button: Button = $Layout/Content/OptionsPanel/MenuItems/LoadButton
@onready var _new_game_button: Button = $Layout/Content/OptionsPanel/MenuItems/NewGameButton
@onready var _options_button: Button = $Layout/Content/TopBar/OptionsButton
@onready var _options_panel: PanelContainer = $Layout/Content/OptionsPanel
@onready var _timer_toggle: CheckButton = $Layout/Content/OptionsPanel/MenuItems/TimerToggle
@onready var _turn_timer_label: Label = $Layout/Content/TopBar/TurnTimerLabel
@onready var _turn_timer: Timer = $TurnTimer
@onready var _save_file_dialog: FileDialog = $SaveFileDialog
@onready var _load_file_dialog: FileDialog = $LoadFileDialog
@onready var _new_game_confirm_dialog: ConfirmationDialog = $NewGameConfirmDialog
@onready var _start_menu: Control = $Layout/Content/StartMenu
@onready var _one_player_button: Button = $Layout/Content/StartMenu/Players/OnePlayerButton
@onready var _two_players_button: Button = $Layout/Content/StartMenu/Players/TwoPlayersButton
@onready var _three_players_button: Button = $Layout/Content/StartMenu/Players/ThreePlayersButton
@onready var _four_players_button: Button = $Layout/Content/StartMenu/Players/FourPlayersButton
@onready var _name_entry_menu: Control = $Layout/Content/NameEntryMenu
# The 4 name fields, indexed 0..3 to match player_id — see
# `_on_player_count_chosen` for how only the first `count` of them are
# actually shown.
@onready var _name_edits: Array[LineEdit] = [
	$Layout/Content/NameEntryMenu/Name1Edit,
	$Layout/Content/NameEntryMenu/Name2Edit,
	$Layout/Content/NameEntryMenu/Name3Edit,
	$Layout/Content/NameEntryMenu/Name4Edit,
]
@onready var _start_with_names_button: Button = $Layout/Content/NameEntryMenu/Buttons/StartButton
@onready var _back_to_start_menu_button: Button = $Layout/Content/NameEntryMenu/Buttons/BackButton
@onready var _free_play_button: Button = $Layout/Content/StartMenu/FreePlayButton
@onready var _palette_view: HandView = $Layout/Content/PlayArea/PaletteView
# The 10 crossword puzzle-picker buttons, indexed 0..9 to match
# CrosswordPuzzles.PUZZLES — see `_ready()`'s loop below for how each one
# reports its own index, and crossword_puzzles.gd for the data itself.
@onready var _crossword_puzzle_buttons: Array[Button] = [
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle1Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle2Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle3Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle4Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle5Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle6Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle7Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle8Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle9Button,
	$Layout/Content/StartMenu/CrosswordGrid/CrosswordPuzzle10Button,
]
@onready var _clue_list_view: ClueListView = $Layout/Content/PlayArea/ClueListView


func _ready() -> void:
	# Godot batches mouse-motion events into one per rendered frame by
	# default (meant to save work for games that don't need per-event
	# precision). For a dragged tile that means it visibly lags behind the
	# real cursor whenever the frame rate dips below the mouse's own
	# polling rate. Turning accumulation off delivers every motion event
	# immediately instead, which is the standard fix for sluggish-feeling
	# drags in Godot.
	Input.set_use_accumulated_input(false)

	# Connects BoardView's/HandView's signals, and every button's built-in
	# `pressed` signal (every Button has one), to the methods below — see
	# board_cell.gd's header comment for what a signal/connection is.
	_board_view.tile_dropped.connect(_on_board_tile_dropped)
	_board_view.tile_moved.connect(_on_board_tile_moved)
	_board_view.word_moved.connect(_on_board_word_moved)
	_board_view.tile_flipped.connect(_on_board_tile_flipped)
	_hand_view.tile_returned.connect(_on_hand_tile_returned)
	_end_turn_button.pressed.connect(_on_end_turn_pressed)
	_save_button.pressed.connect(_on_save_pressed)
	_load_button.pressed.connect(_on_load_pressed)
	_new_game_button.pressed.connect(_on_new_game_pressed)
	_options_button.pressed.connect(_on_options_pressed)
	_timer_toggle.toggled.connect(_on_timer_toggle_toggled)
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
	_back_to_start_menu_button.pressed.connect(_on_back_to_start_menu_pressed)
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
# create the GameSession yet — shows NameEntryMenu first (see
# `_on_names_confirmed`, which actually creates it), with exactly `count`
# of its 4 name fields visible.
func _on_player_count_chosen(count: int) -> void:
	_pending_player_count = count
	_start_menu.visible = false
	_name_entry_menu.visible = true
	for i in range(_name_edits.size()):
		_name_edits[i].visible = i < count
		_name_edits[i].text = ""


# Called when NameEntryMenu's "Commencer" button is pressed. Only NOW
# does the actual GameSession get created — nothing before this point
# could show real game state anyway, since GameSession would build its
# own French dictionary and starting hands. A blank name field falls
# back to the plain "Joueur N" (see `_player_name`).
func _on_names_confirmed() -> void:
	_player_names = []
	for i in range(_pending_player_count):
		var entered := _name_edits[i].text.strip_edges()
		_player_names.append(entered if entered != "" else "Joueur %d" % (i + 1))
	_session = GameSession.new(_pending_player_count)
	_name_entry_menu.visible = false
	_title.visible = false
	_tagline.visible = false
	_board_view.visible = true
	_hand_view.visible = true
	_end_turn_button.visible = true
	_save_button.visible = true
	_load_button.visible = true
	_new_game_button.visible = true
	_refresh_views()
	_restart_turn_timer()


# Called when NameEntryMenu's "Retour" button is pressed — no session
# exists yet at this point, so there's nothing to tear down, just go
# back to picking a player count.
func _on_back_to_start_menu_pressed() -> void:
	_name_entry_menu.visible = false
	_start_menu.visible = true


# Whether the free-play sandbox is the one currently active, rather than
# a turn-based `_session` (or nothing at all, at the start menu).
func _is_free_play() -> bool:
	return _free_play_session != null


# Called when StartMenu's "Jeu libre" button is pressed. Unlike
# `_on_player_count_chosen`, there's no turn to end and no timer to run
# (EndTurnButton stays hidden), but Save/Load do work here — see
# save_game.gd's `save_free_play`/`load_free_play`.
func _on_free_play_chosen() -> void:
	_free_play_session = FreePlaySession.new()
	_start_menu.visible = false
	_title.visible = false
	_tagline.visible = false
	_board_view.visible = true
	_palette_view.visible = true
	_new_game_button.visible = true
	_save_button.visible = true
	_load_button.visible = true
	_refresh_views()


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
	_free_play_session = FreePlaySession.new()
	_crossword_puzzle = CrosswordPuzzle.new(puzzle_index)
	_start_menu.visible = false
	_title.visible = false
	_tagline.visible = false
	_board_view.visible = true
	_palette_view.visible = true
	_clue_list_view.visible = true
	_new_game_button.visible = true
	_save_button.visible = true
	_load_button.visible = true
	_refresh_views()


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
# rather than paying to reload the ~79000-word list again. Does nothing
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
			_clue_list_view.visible = _is_crossword()
			_refresh_views()
		return
	var loaded_session := SaveGame.load(path, _session.dictionary)
	if loaded_session != null:
		_session = loaded_session
		_player_names = []  # a loaded session has no names of its own
		_end_turn_message.text = ""
		_refresh_views()
		_restart_turn_timer()


# Called when the End Turn button is pressed. `end_turn()` itself refuses
# and returns false if an invalid word is currently on the board (see
# word_challenge.gd) — in which case `_end_turn_message` explains why,
# listing the offending word(s) (see `_format_end_turn_error` below).
# Cleared on success, since the turn actually ended.
func _on_end_turn_pressed() -> void:
	if _session.end_turn():
		_end_turn_message.text = ""
		_restart_turn_timer()
	else:
		_end_turn_message.text = _format_end_turn_error()
	_refresh_views()


# Redraws both views from the session's current state — the board, and
# whichever player's turn it currently is. `_session.can_take_back` is
# passed straight through as a Callable (see board_view.gd's `refresh` for
# what that means) rather than BoardView asking GameSession itself. Also
# checks whether the game has just ended (see `_update_game_over_banner`).
func _refresh_views() -> void:
	if _is_free_play():
		_board_view.refresh(
			_free_play_session.board,
			_free_play_session.can_take_back,
			_free_play_session.can_place_here
		)
		_palette_view.refresh(_free_play_session.available_tiles())
		_scoreboard.text = "Score : %d" % _free_play_session.current_score()
		if _is_crossword():
			_clue_list_view.refresh(
				_crossword_puzzle.clues, _crossword_puzzle.solved_rows(_free_play_session.board)
			)
			if _crossword_puzzle.is_solved(_free_play_session.board):
				_game_over_banner.text = "Mots croisés résolus !"
			else:
				_game_over_banner.text = ""
		return
	_board_view.refresh(_session.board, _session.can_take_back, _session.can_place_here)
	_hand_view.refresh(_session.bag.hand(_session.turns.current_player_id()))
	_scoreboard.text = _format_scoreboard()
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
		text += "%s : %d pts" % [_player_name(player_id), scores[player_id]]
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


# Hides everything that depends on `_session` and shows `StartMenu`
# again — used both by `_ready()` (before any game has started yet) and
# by `_start_new_game()` (after abandoning one).
func _return_to_start_menu() -> void:
	_start_menu.visible = true
	_title.visible = true
	_tagline.visible = true
	_board_view.visible = false
	_hand_view.visible = false
	_end_turn_button.visible = false
	_save_button.visible = false
	_load_button.visible = false
	_new_game_button.visible = false
	_palette_view.visible = false
	_clue_list_view.visible = false
	_name_entry_menu.visible = false
	_options_panel.visible = false
	_player_names = []
	_end_turn_message.text = ""
	_game_over_banner.text = ""
	_scoreboard.text = ""
	_turn_timer.stop()
	_turn_timer_label.visible = false


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
		"Partie terminée ! %s gagne avec %d points." % [_player_name(winner_id), winner_score]
	)
	_end_turn_button.disabled = true
