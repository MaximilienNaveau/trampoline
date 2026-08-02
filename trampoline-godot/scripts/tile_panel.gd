# One draggable tile in a HandView row (see hand_view.gd). Knows nothing
# about GameSession/turns/scoring — it only carries a reference to the
# `Tile` it displays, and implements Godot's drag-and-drop hook so a player
# can pick it up and drop it onto a BoardCell (see board_cell.gd).
class_name TilePanel
extends Panel

# The tile this panel represents — set once by HandView.refresh() right
# after creating this node.
var tile: Tile

# This panel's own main Label and corner badge, set once by
# HandView.refresh() right after creating them (see tile_view.gd's
# `build_corner_badge`/`update_corner_badge`) — kept here so `refresh()`
# below can redraw them in place after a flip, without HandView needing to
# rebuild this whole panel from scratch.
var label: Label
var corner_badge: Panel

# Remembers where a left-click started, so `_gui_input` below can tell a
# plain click (releasing close to where it was pressed) from a drag that
# happened to end and release back over this same tile.
var _press_position: Vector2 = Vector2.ZERO


# Virtual function: Godot calls this itself the moment the player starts
# dragging this Control with the mouse (see board_cell.gd's header comment
# for what "virtual function" means here). Its return value becomes the
# payload every drop target receives in `_can_drop_data`/`_drop_data` — a
# plain Dictionary is the simplest way to carry the dragged Tile along.
func _get_drag_data(_at_position: Vector2) -> Variant:
	# Shows a copy of this tile following the cursor while it's dragged, so
	# the player can see what they're holding. Godot destroys this preview
	# automatically once the drag ends — we never free it ourselves. See
	# TileView.build_drag_preview for why it needs its own wrapper Control
	# to stay centered on the cursor (BoardCell's own drag source, for
	# moving a tile already on the board, reuses the exact same helper).
	set_drag_preview(TileView.build_drag_preview(tile, size))

	# Fades this panel out while it's being dragged, so it doesn't look
	# like there are two of this tile at once (one here, one following the
	# cursor). `modulate` tints/fades a Control without resizing it (unlike
	# `visible = false`, which would also pull it out of HBoxContainer's
	# layout and make the rest of the hand jump sideways to fill the gap).
	# `_notification` below fades it back in once the drag ends — if it
	# actually got placed, HandView.refresh() will have already queued this
	# whole panel for deletion anyway, making this a harmless no-op.
	modulate = Color(1, 1, 1, 0)

	return {"tile": tile}


# See board_cell.gd's own `_notification` for what NOTIFICATION_DRAG_END
# means and why every Control receives it once a drag ends, successful or
# not.
func _notification(what: int) -> void:
	if what == NOTIFICATION_DRAG_END:
		modulate = Color(1, 1, 1, 1)


# Redraws this tile's own text/color and other-face hint from `tile`'s
# current state — needed after a flip (see `_gui_input` below). Unlike a
# board tile, flipping a tile still in hand has no game-rule consequence
# at all (see game_session.gd's `flip_tile`, which only ever touches board
# tiles) — it's just choosing which face to place, so this panel handles
# it entirely on its own, no signal to main.gd needed.
func refresh() -> void:
	label.text = tile.current_letter()
	add_theme_stylebox_override("panel", TileView.style_for_tile(tile))
	TileView.update_corner_badge(corner_badge, tile)


# Virtual function: Godot calls this for mouse/keyboard events over this
# Control that the drag-and-drop system above doesn't already claim (see
# board_cell.gd's own version for the fuller explanation of the click-vs-
# drag distinction this makes).
func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_press_position = event.position
		elif event.position.distance_to(_press_position) < 6:
			tile.flip()
			refresh()
