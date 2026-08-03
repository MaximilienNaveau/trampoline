# Visualizes a collection of colored tiles, using the same yellow/green/
# blank coloring as BoardView (via TileView). Each tile is a TilePanel (see
# tile_panel.gd), draggable onto a BoardView cell — this view itself
# doesn't need to know that's possible, it just builds them. The row
# itself (see hand_row.gd) accepts a tile dragged back FROM the board,
# re-broadcast here as `tile_returned` the same way BoardView re-broadcasts
# its own cells' signals.
#
# Instanced twice: as one player's turn-based hand (hand_view.tscn, a
# single-row HBoxContainer), and as the free-play palette of every
# currently-unplaced tile (scenes/palette_view.tscn, a scrollable
# multi-row GridContainer) — this script itself doesn't care which, since
# `refresh()` only ever deals in a plain list of tiles (see hand_row.gd's
# own comment for why the SAME `Row` script backs both container shapes).
#
# Unlike BoardView's grid (13x9, always the same 117 cells), the tiles
# shown here change turn to turn — tiles leave when placed, more arrive on
# a draw (or, in free play, whenever anything moves on/off the board) — so
# there's no fixed pool of nodes to reuse here. `refresh()` just throws
# away the previous tile nodes and builds fresh ones matching whatever's
# passed in; even the free-play palette's worst case (117 tiles) is cheap
# enough to rebuild from scratch every refresh, so this needs no pooling.
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
		panel.label = label

		# The small "other face" hint badge (see tile_view.gd's
		# build_corner_badge/update_corner_badge) — lets a player see what
		# flipping this tile would reveal before they commit to it.
		var corner_badge := TileView.build_corner_badge()
		TileView.update_corner_badge(corner_badge, tile)
		panel.add_child(corner_badge)
		panel.corner_badge = corner_badge

		_row.add_child(panel)
