# Visualizes one player's hand as a row of colored tiles, using the same
# yellow/green/blank coloring as BoardView (via TileView). Each tile is a
# TilePanel (see tile_panel.gd), draggable onto a BoardView cell — this
# view itself doesn't need to know that's possible, it just builds them.
# The row itself (see hand_row.gd) accepts a tile dragged back FROM the
# board, re-broadcast here as `tile_returned` the same way BoardView
# re-broadcasts its own cells' signals.
#
# Unlike BoardView's grid (13x9, always the same 117 cells), a hand's size
# actually changes turn to turn — tiles leave when placed, more arrive on a
# draw — so there's no fixed pool of nodes to reuse here. `refresh()` just
# throws away last turn's tile nodes and builds fresh ones matching however
# many tiles are in hand right now; hands are small (at most 13 tiles), so
# rebuilding every refresh is simple and cheap enough to not need pooling.
class_name HandView
extends Control

signal tile_returned(from_row: int, from_col: int)

# `$Row` looks up the child node named "Row" declared in hand_view.tscn,
# same `@onready`/`$Name` pattern as BoardView's `_grid`.
@onready var _row: HandRow = $Row


func _ready() -> void:
	_row.tile_returned.connect(_on_row_tile_returned)


func _on_row_tile_returned(from_row: int, from_col: int) -> void:
	tile_returned.emit(from_row, from_col)


# Redraws the whole row from `hand`'s current tiles, left to right.
func refresh(hand: Array[Tile]) -> void:
	# `queue_free()` marks a node for deletion at the end of this frame,
	# rather than destroying it immediately (Godot's usual way to remove a
	# node safely) — `free()` would work too here since none of these
	# panels are still being interacted with mid-signal, but queue_free is
	# the standard habit throughout Godot code.
	for child in _row.get_children():
		child.queue_free()

	for tile in hand:
		var panel := TilePanel.new()
		panel.tile = tile
		# Every Control can pick its own mouse cursor shape for whenever the
		# pointer is hovering over it — a hint that this tile can be picked
		# up, shown before the player even starts dragging. CURSOR_DRAG
		# would be the conceptually closest match, but on this system (X11)
		# Godot maps it to the 4-way "move" cross, not a hand; the actual
		# hand-shaped cursor Godot ships is CURSOR_POINTING_HAND.
		panel.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND
		# A Control's default MOUSE_FILTER_STOP means it swallows mouse
		# events entirely, including a drag-and-drop drop — since a
		# TilePanel doesn't handle drops itself, an unhandled drop landing
		# on one (rather than the gap between tiles) would otherwise never
		# reach HandRow, the actual drop target (see hand_row.gd). PASS lets
		# a TilePanel still handle its OWN events (starting a drag out of
		# it still works) while letting anything it doesn't handle continue
		# on to its parent.
		panel.mouse_filter = Control.MOUSE_FILTER_PASS
		panel.custom_minimum_size = Vector2(32, 32)
		panel.add_theme_stylebox_override("panel", TileView.style_for_tile(tile))

		var label := Label.new()
		label.set_anchors_preset(Control.PRESET_FULL_RECT)
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		label.text = tile.current_letter()
		panel.add_child(label)

		_row.add_child(panel)
