# A ScrollContainer that holds still while a tile is dragged out of it.
# Used for the page (main.tscn's Layout, scrollable when it doesn't fit
# the screen, e.g. free play's palette in portrait) and the "À rendre"
# tray (discard_tray.gd), which scrolls sideways.
#
# On a touchscreen, a finger dragged across a ScrollContainer scrolls it,
# including the finger dragging a tile: the page slid under the tile. So
# a tile drag starting puts this back where it was when the finger went
# down, which also stops the scrolling there.
extends ScrollContainer

# The scroll position when the last finger/button went down.
var _scroll_at_press := Vector2i.ZERO


# Called with every input event that reaches this container, including
# those a child (a tile, a cell) lets through (`mouse_filter` PASS). A
# touch arrives here as a left mouse button.
func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		_scroll_at_press = Vector2i(scroll_horizontal, scroll_vertical)


# Godot sends NOTIFICATION_DRAG_BEGIN to every node when a drag-and-drop
# starts (a tile picked up, see tile_panel.gd's `_get_drag_data`).
func _notification(what: int) -> void:
	if what == NOTIFICATION_DRAG_BEGIN:
		# Setting the scroll position also ends the touch scroll under way,
		# and the glide it would have had on release (see Godot's
		# ScrollContainer.set_v_scroll).
		scroll_horizontal = _scroll_at_press.x
		scroll_vertical = _scroll_at_press.y
