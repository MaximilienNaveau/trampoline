# Visualizes one player's hand as a row of colored tiles, using the same
# yellow/green/blank coloring as BoardView (via TileView). Each tile is a
# TilePanel (see tile_panel.gd), draggable onto a BoardView cell — this
# view itself doesn't need to know that's possible, it just builds them.
#
# Unlike BoardView's grid (13x9, always the same 117 cells), a hand's size
# actually changes turn to turn — tiles leave when placed, more arrive on a
# draw — so there's no fixed pool of nodes to reuse here. `refresh()` just
# throws away last turn's tile nodes and builds fresh ones matching however
# many tiles are in hand right now; hands are small (at most 13 tiles), so
# rebuilding every refresh is simple and cheap enough to not need pooling.
class_name HandView
extends Control

# `$Row` looks up the child node named "Row" declared in hand_view.tscn,
# same `@onready`/`$Name` pattern as BoardView's `_grid`.
@onready var _row: HBoxContainer = $Row


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
		panel.custom_minimum_size = Vector2(32, 32)
		panel.add_theme_stylebox_override("panel", TileView.style_for_tile(tile))

		var label := Label.new()
		label.set_anchors_preset(Control.PRESET_FULL_RECT)
		label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
		label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		label.text = tile.current_letter()
		panel.add_child(label)

		_row.add_child(panel)
