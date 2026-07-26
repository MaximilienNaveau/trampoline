# Entry point for the game's main scene: creates a GameSession and shows
# its board via BoardView (see scenes/main.tscn). Currently just a fixed
# 2-player game started fresh every time the app runs — no menu, no
# save/load wiring, no interaction yet (see board_view.gd's own header
# comment) — this exists purely to get a real GameSession rendering on
# screen for the first time.
extends Control

# `$BoardView` looks up the child node named "BoardView" declared in
# scenes/main.tscn — the same shorthand board_view.gd itself uses for its
# own "Grid" child. `@onready` runs this once that child actually exists,
# rather than too early (see board_view.gd for the same pattern).
@onready var _board_view: BoardView = $BoardView


func _ready() -> void:
	# GameSession would build its own French dictionary if not given one,
	# but loading those ~79000 words is the slowest part of starting a
	# session — there's nothing else here yet that needs a shared
	# instance, so this just keeps things simple rather than plumbing one
	# through for no current benefit.
	var session := GameSession.new(2)
	_board_view.refresh(session.board)
