# One droppable cell in BoardView's grid (see board_view.gd). Knows the
# Board only well enough to ask GrowthRules whether a drop would be legal
# here — the same read-only relationship BoardView itself has with Board,
# never mutating anything directly. Actually placing the tile is main.gd's
# job: this cell just reports where a drop landed via the `tile_dropped`
# signal below.
class_name BoardCell
extends Panel

# `signal` declares a named event this node can broadcast — Godot's way of
# saying "something happened" without the emitter needing to know who (if
# anyone) is listening, similar in spirit to a Qt signal if you've seen
# one, or a plain callback list in C++. BoardView connects to this once per
# cell (see board_view.gd) and re-emits its own single `tile_dropped`
# signal, so main.gd only has to listen in one place instead of 117.
signal tile_dropped(row: int, col: int, tile: Tile)

# This cell's position in the board grid — set once by BoardView right
# after creating this node.
var row: int
var col: int

# The Board this cell belongs to — kept up to date by BoardView.refresh()
# so `_can_drop_data` below always checks legality against how the board
# looks *right now*, not just at creation time.
var board: Board


# A "virtual" function: Godot calls this itself (we never call it
# directly) once per frame while something is being dragged over this
# Control, to ask "would you accept a drop here?" Returning false is what
# shows the player a "no" cursor over illegal cells, and also stops
# `_drop_data` below from ever being called for this drop.
func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	return data is Dictionary and data.has("tile") and GrowthRules.can_place(board, row, col)


# Virtual function Godot calls once, only if `_can_drop_data` just returned
# true and the player actually released the mouse here. `data` is exactly
# whatever the dragged TilePanel's `_get_drag_data` returned (see
# tile_panel.gd) — a Dictionary holding the Tile being placed.
func _drop_data(_at_position: Vector2, data: Variant) -> void:
	tile_dropped.emit(row, col, data["tile"])
