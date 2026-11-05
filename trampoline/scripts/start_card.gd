# The cream card on the box lid (see box_lid.gd): the title, the tagline,
# and the start menu — "Reprendre la partie", how many players (then
# their names), "Jeu libre", the crosswords, "Comment jouer". It only
# reports what the player chose, through its signals; main.gd starts it.
#
# Purely a menu: it creates no game itself.
extends VBoxContainer

# A game for `names.size()` players, `names[i]` what player i entered
# (maybe empty: see main.gd's `_player_name`).
signal game_chosen(names: Array[String])
# "Reprendre la partie": the game saved as its last turn began.
signal resume_chosen
# Free play (`puzzle_index` -1), or crossword number `puzzle_index + 1`.
signal free_play_chosen(puzzle_index: int)
signal how_to_play_chosen

# How many players the count buttons picked, kept until the names are
# confirmed (see `_on_names_confirmed`).
var _player_count := 0

@onready var _start_menu: Control = $StartMenu
@onready var _name_entry: Control = $NameEntry
@onready var _resume_button: Button = $StartMenu/ResumeButton
# The 4 name fields, indexed 0..3 to match player_id — only the first
# `_player_count` of them are shown (see `_on_player_count_chosen`).
@onready var _name_edits: Array[LineEdit] = [
	$NameEntry/Name1Edit,
	$NameEntry/Name2Edit,
	$NameEntry/Name3Edit,
	$NameEntry/Name4Edit,
]


func _ready() -> void:
	# `Callable.bind(n)` hands the same method one extra fixed argument per
	# button (n itself), rather than four near-identical handlers.
	for count in range(1, 5):
		get_node("StartMenu/Players/Button%d" % count).pressed.connect(
			_on_player_count_chosen.bind(count)
		)
	$NameEntry/Buttons/StartButton.pressed.connect(_on_names_confirmed)
	$NameEntry/Buttons/BackButton.pressed.connect(show_menu)
	_resume_button.pressed.connect(resume_chosen.emit)
	$StartMenu/HowToPlayButton.pressed.connect(how_to_play_chosen.emit)
	$StartMenu/FreePlayButton.pressed.connect(free_play_chosen.emit.bind(-1))
	# The 10 crossword buttons, each reporting its index in
	# CrosswordPuzzles.PUZZLES (see crossword_puzzles.gd).
	for i in range(CrosswordPuzzles.PUZZLES.size()):
		get_node("StartMenu/CrosswordGrid/CrosswordPuzzle%dButton" % (i + 1)).pressed.connect(
			free_play_chosen.emit.bind(i)
		)


# Shows the title and the start menu — with "Reprendre la partie" only
# while a game is saved (see SaveGame.AUTOSAVE_PATH).
func show_menu() -> void:
	for node: Control in [$Title, $Tagline, _start_menu]:
		node.visible = true
	_name_entry.visible = false
	_resume_button.visible = FileAccess.file_exists(SaveGame.AUTOSAVE_PATH)


# Hides it all, as the lid opens on a game.
func close() -> void:
	for node: Control in [$Title, $Tagline, _start_menu, _name_entry]:
		node.visible = false


# A player-count button: shows the name fields, exactly `count` of them.
func _on_player_count_chosen(count: int) -> void:
	_player_count = count
	_start_menu.visible = false
	_name_entry.visible = true
	for i in range(_name_edits.size()):
		_name_edits[i].visible = i < count
		_name_edits[i].text = ""


# "Commencer": the names entered, trimmed of spaces.
func _on_names_confirmed() -> void:
	var names: Array[String] = []
	for i in range(_player_count):
		names.append(_name_edits[i].text.strip_edges())
	game_chosen.emit(names)
