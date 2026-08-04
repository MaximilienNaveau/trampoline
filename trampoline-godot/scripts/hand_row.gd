# The actual drop target for returning a tile from the board back to
# wherever HandView's tiles came from (see hand_view.gd/board_cell.gd for
# the rest of the drag-and-drop mechanics) — the current player's hand in
# turn-based play, or the free-play palette (see scenes/palette_view.tscn)
# in free play. This lives on the Row container itself, rather than on
# HandView's own root Control, because a Container's rect grows to fit its
# children — exactly the area the tiles are visible in — while HandView's
# root Control has no size of its own to speak of.
#
# `extends Container` rather than a specific box/grid layout: the drop
# logic below cares nothing about HOW the tiles are arranged, only about
# accepting a tile dragged off the board, so the identical script backs
# both an HFlowContainer (the turn-based hand, wrapping onto more rows
# rather than cropping a full hand) and a GridContainer (the free-play
# palette, which needs to wrap 117 tiles across many rows).
class_name HandRow
extends Container

signal tile_returned(from_row: int, from_col: int)


# Only accepts drops that came from a BoardCell (see board_cell.gd's
# `_get_drag_data`, which is the only source that adds "from_row" to the
# dragged data) — a drag already starting from within the hand doesn't need
# handling here yet (see hand_view.gd's header comment).
func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	return data is Dictionary and data.has("from_row")


func _drop_data(_at_position: Vector2, data: Variant) -> void:
	tile_returned.emit(data["from_row"], data["from_col"])
