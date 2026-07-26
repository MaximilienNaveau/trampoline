# Visualizes a Board's current state as a 13x9 grid of colored cells —
# read-only for now, no clicking/dragging yet, just a way to actually SEE
# what's on the board. No custom art yet either, just enough color to
# tell yellow letters, green letters, and empty cells apart at a glance.
#
# Deliberately knows nothing about GameSession, TurnManager, scoring, or
# any other game rule: it only ever reads a `Board` (see board.gd) and
# draws whatever's there, per this project's "separate logic from
# graphics" rule (see CLAUDE.md). It also never watches for changes on
# its own — whoever owns this node calls `refresh(board)` after the
# board changes (placing/removing/flipping/moving a tile).
class_name BoardView
extends Control

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
			var panel := Panel.new()
			panel.custom_minimum_size = Vector2(32, 32)
			panel.add_theme_stylebox_override("panel", TileView.empty_style())

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
			var panel: Panel = _cell_panels[row][col]
			if tile == null:
				label.text = ""
				panel.add_theme_stylebox_override("panel", TileView.empty_style())
			else:
				label.text = tile.current_letter()
				panel.add_theme_stylebox_override("panel", TileView.style_for_tile(tile))
