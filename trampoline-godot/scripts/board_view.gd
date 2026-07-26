# Visualizes a Board's current state as a 13x9 grid of colored cells, and
# accepts tiles dropped onto it from a HandView (see board_cell.gd/
# tile_panel.gd for the actual drag-and-drop mechanics). No custom art yet,
# just enough color to tell yellow letters, green letters, and empty cells
# apart at a glance.
#
# Deliberately knows nothing about GameSession, TurnManager, scoring, or
# any other game rule: it only ever reads a `Board` (see board.gd), draws
# whatever's there, and asks GrowthRules (a stateless rule-checker, not a
# mutator) whether a drop would be legal. It never places a tile itself —
# that's main.gd's job, listening for the `tile_dropped` signal below. It
# also never watches the board for changes on its own — whoever owns this
# node calls `refresh(board)` after the board changes (placing/removing/
# flipping/moving a tile).
class_name BoardView
extends Control

# Re-broadcasts whichever cell just received a dropped tile — see
# board_cell.gd's own `tile_dropped` signal for what a "signal" is. Having
# ONE signal here, instead of main.gd connecting to all 117 cells
# individually, keeps BoardView's grid an implementation detail its owner
# doesn't need to know about.
signal tile_dropped(row: int, col: int, tile: Tile)

# One Panel + Label per board cell, indexed [row][col] — built once in
# _ready() and reused by refresh(), rather than destroying and recreating
# 117 nodes every time the board changes.
var _cell_panels: Array = []
var _cell_labels: Array = []

# `@onready` runs this line once this node's children actually exist
# (right before `_ready()` — a plain `var _grid := $Grid` at the top of
# the class would run too early, before the scene tree below this node
# has been built). `$Grid` is shorthand for "find my child node named
# Grid", declared in board_view.tscn.
@onready var _grid: GridContainer = $Grid


func _ready() -> void:
	_build_grid()


# Creates all 117 cell nodes once, empty. GridContainer lays out its
# children left-to-right then wraps after `columns` of them (set to 9 in
# board_view.tscn), so simply adding them row by row, column by column,
# naturally produces the right grid shape with no manual positioning.
func _build_grid() -> void:
	for row in range(Board.ROWS):
		var panel_row: Array = []
		var label_row: Array = []
		for col in range(Board.COLUMNS):
			var panel := BoardCell.new()
			panel.row = row
			panel.col = col
			panel.custom_minimum_size = Vector2(32, 32)
			panel.add_theme_stylebox_override("panel", TileView.empty_style())
			# Connects this cell's own signal to a method on BoardView (see
			# `_on_cell_tile_dropped` below) — the standard Godot way of
			# reacting to a signal, same idea as a Qt slot or a C callback
			# registered against a function pointer.
			panel.tile_dropped.connect(_on_cell_tile_dropped)

			var label := Label.new()
			# PRESET_FULL_RECT stretches the label to fill its parent
			# panel entirely, rather than sizing itself to just the text.
			label.set_anchors_preset(Control.PRESET_FULL_RECT)
			label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
			panel.add_child(label)

			_grid.add_child(panel)
			panel_row.append(panel)
			label_row.append(label)
		_cell_panels.append(panel_row)
		_cell_labels.append(label_row)


# Redraws every cell from `board`'s current state.
func refresh(board: Board) -> void:
	for row in range(Board.ROWS):
		for col in range(Board.COLUMNS):
			var tile := board.tile_at(row, col)
			var label: Label = _cell_labels[row][col]
			var panel: BoardCell = _cell_panels[row][col]
			# Kept current every refresh, not just at creation, in case a
			# future save/load ever swaps in a whole new Board instance.
			panel.board = board
			if tile == null:
				label.text = ""
				panel.add_theme_stylebox_override("panel", TileView.empty_style())
			else:
				label.text = tile.current_letter()
				panel.add_theme_stylebox_override("panel", TileView.style_for_tile(tile))


# Forwards one cell's drop straight to this view's own single signal (see
# the header comment above `tile_dropped`'s declaration).
func _on_cell_tile_dropped(row: int, col: int, tile: Tile) -> void:
	tile_dropped.emit(row, col, tile)
