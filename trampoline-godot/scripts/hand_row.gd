# The actual drop target for returning a tile from the board back to the
# current player's hand (see hand_view.gd/board_cell.gd for the rest of the
# drag-and-drop mechanics). This lives on the Row HBoxContainer itself,
# rather than on HandView's own root Control, because a Container's rect
# grows to fit its children — exactly the area the tiles are visible in —
# while HandView's root Control has no size of its own to speak of.
class_name HandRow
extends HBoxContainer

signal tile_returned(from_row: int, from_col: int)


# Only accepts drops that came from a BoardCell (see board_cell.gd's
# `_get_drag_data`, which is the only source that adds "from_row" to the
# dragged data) — a drag already starting from within the hand doesn't need
# handling here yet (see hand_view.gd's header comment).
func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	return data is Dictionary and data.has("from_row")


func _drop_data(_at_position: Vector2, data: Variant) -> void:
	tile_returned.emit(data["from_row"], data["from_col"])
