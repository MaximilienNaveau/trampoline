# One droppable (and, sometimes, draggable) cell in BoardView's grid (see
# board_view.gd). Knows the Board only well enough to ask GrowthRules
# whether a drop would be legal here — the same read-only relationship
# BoardView itself has with Board, never mutating anything directly.
# Actually placing/moving a tile is main.gd's job: this cell just reports
# what happened via the signals below.
class_name BoardCell
extends Panel

# `signal` declares a named event this node can broadcast — Godot's way of
# saying "something happened" without the emitter needing to know who (if
# anyone) is listening, similar in spirit to a Qt signal if you've seen
# one, or a plain callback list in C++. BoardView connects to both of these
# once per cell (see board_view.gd) and re-emits its own single signals, so
# main.gd only has to listen in one place instead of 117.
signal tile_dropped(row: int, col: int, tile: Tile)
signal tile_moved(from_row: int, from_col: int, to_row: int, to_col: int)

# This cell's position in the board grid — set once by BoardView right
# after creating this node.
var row: int
var col: int

# The Board this cell belongs to — kept up to date by BoardView.refresh()
# so `_can_drop_data` below always checks legality against how the board
# looks *right now*, not just at creation time.
var board: Board

# Whether the tile currently sitting here (if any) is still eligible to be
# picked back up — see GameSession.can_take_back. Kept up to date by
# BoardView.refresh(), same as `board` above. BoardCell never decides this
# itself: it has no notion of turns, only Board does.
var draggable: bool = false


# A "virtual" function: Godot calls this itself (we never call it
# directly) once per frame while something is being dragged over this
# Control, to ask "would you accept a drop here?" Returning false is what
# shows the player a "no" cursor over illegal cells, and also stops
# `_drop_data` below from ever being called for this drop.
func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	return data is Dictionary and data.has("tile") and GrowthRules.can_place(board, row, col)


# Virtual function Godot calls once, only if `_can_drop_data` just returned
# true and the player actually released the mouse here. `data` is exactly
# whatever the drag started with: a TilePanel from the hand (see
# tile_panel.gd) only ever has "tile", while another BoardCell (see
# `_get_drag_data` below) also carries "from_row"/"from_col" — that's how
# we tell "placing from hand" and "moving on the board" apart here.
func _drop_data(_at_position: Vector2, data: Variant) -> void:
	if data.has("from_row"):
		tile_moved.emit(data["from_row"], data["from_col"], row, col)
	else:
		tile_dropped.emit(row, col, data["tile"])


# Virtual function: Godot calls this the moment the player starts dragging
# this Control (see tile_panel.gd's own `_get_drag_data` for the general
# idea). Returning `null` — which happens whenever `draggable` is false —
# is how a Control tells Godot "no drag starts here at all".
func _get_drag_data(_at_position: Vector2) -> Variant:
	if not draggable:
		return null
	var tile := board.tile_at(row, col)
	set_drag_preview(TileView.build_drag_preview(tile, size))
	return {"tile": tile, "from_row": row, "from_col": col}
