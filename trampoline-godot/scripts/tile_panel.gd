# One draggable tile in a HandView row (see hand_view.gd). Knows nothing
# about GameSession/turns/scoring — it only carries a reference to the
# `Tile` it displays, and implements Godot's drag-and-drop hook so a player
# can pick it up and drop it onto a BoardCell (see board_cell.gd).
class_name TilePanel
extends Panel

# The tile this panel represents — set once by HandView.refresh() right
# after creating this node.
var tile: Tile


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

	return {"tile": tile}
