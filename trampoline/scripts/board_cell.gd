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
# one, or a plain callback list in C++. BoardView connects to all three of
# these once per cell (see board_view.gd) and re-emits its own single
# signals, so main.gd only has to listen in one place instead of 117.
signal tile_dropped(row: int, col: int, tile: Tile)
signal tile_moved(from_row: int, from_col: int, to_row: int, to_col: int)
signal word_moved(from_row: int, from_col: int, to_row: int, to_col: int)
signal tile_flipped(row: int, col: int)

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

# Whether a NEW tile could legally be dropped here right now — see
# GameSession.can_place_here (turn-based play: must extend an existing
# word) or FreePlaySession.can_place_here (free play: any empty cell).
# Same reasoning as `draggable` above: BoardCell has no rules of its own,
# it only asks whichever session is running through a Callable BoardView
# hands it (see board_view.gd's `refresh`).
var placeable: bool = false

# This cell's own Label child, handed over once by BoardView right after
# creating it (see board_view.gd's `_build_grid`) — kept here, not just
# there, so this cell can redraw its own text/color itself (see `refresh`
# below).
var label: Label

# The small top-right badge showing what this tile's OTHER face is (see
# tile_view.gd's `build_corner_badge`/`update_corner_badge`) — also handed
# over by BoardView at creation time, same reasoning as `label` above.
var corner_badge: Panel

# The BoardView that owns this cell, also handed over at creation time —
# needed so a word-drag (see `_get_drag_data` below) can reach and hide
# every OTHER cell in the same word, not just this one (see board_view.gd's
# `get_cell`).
var board_view: BoardView

# Remembers where a left-click started, so `_gui_input` below can tell a
# plain click (releasing close to where it was pressed) from a drag that
# happened to end and release back over this same cell.
var _press_position: Vector2 = Vector2.ZERO


# A "virtual" function: Godot calls this itself (we never call it
# directly) once per frame while something is being dragged over this
# Control, to ask "would you accept a drop here?" Returning false is what
# shows the player a "no" cursor over illegal cells, and also stops
# `_drop_data` below from ever being called for this drop. `data` is
# whatever the drag started with (see `_get_drag_data` below and
# tile_panel.gd's own version) — a Dictionary shaped one of three ways,
# which is how a drop target tells them apart.
func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	if not data is Dictionary:
		return false
	if data.has("word_from_row"):
		# Mirrors GameSession.move_word_by_grabbed_tile's own offset math
		# (see game_session.gd) — WordMove.can_move checks legality for the
		# word's FIRST letter, not necessarily the tile actually grabbed.
		var source_columns := board.word_columns_at(data["word_from_row"], data["word_from_col"])
		if source_columns.is_empty():
			return false
		var grabbed_offset: int = source_columns.find(data["word_from_col"])
		return WordMove.can_move(
			board, data["word_from_row"], data["word_from_col"], row, col - grabbed_offset
		)
	return data.has("tile") and placeable


# Virtual function Godot calls once, only if `_can_drop_data` just returned
# true and the player actually released the mouse here.
func _drop_data(_at_position: Vector2, data: Variant) -> void:
	if data.has("word_from_row"):
		word_moved.emit(data["word_from_row"], data["word_from_col"], row, col)
	elif data.has("from_row"):
		tile_moved.emit(data["from_row"], data["from_col"], row, col)
	else:
		tile_dropped.emit(row, col, data["tile"])


# Virtual function: Godot calls this the moment the player starts dragging
# this Control (see tile_panel.gd's own `_get_drag_data` for the general
# idea). Returning `null` — which happens whenever the cell is empty — is
# how a Control tells Godot "no drag starts here at all".
func _get_drag_data(_at_position: Vector2) -> Variant:
	var tile := board.tile_at(row, col)
	if tile == null:
		return null

	if draggable:
		# Hide this cell's own copy while it's being dragged, so there
		# aren't two of it on screen at once — one here, one following the
		# cursor. `refresh()` puts it back (see `_notification` below) once
		# the drag ends, whether or not it actually moved the tile away.
		hide_tile()
		set_drag_preview(TileView.build_drag_preview(tile, size))
		return {"tile": tile, "from_row": row, "from_col": col}

	# Not individually takeable — this tile is frozen from an earlier turn
	# (see GameSession.can_take_back) — so dragging it instead offers up
	# the WHOLE WORD it belongs to, the same way any player may relocate
	# any already-played word (see word_move.gd). The preview shows every
	# tile in that word, not just the one grabbed, so the player can see
	# what they're actually about to move — and every cell in the word
	# (not just this one) hides for the same reason as above, via
	# board_view.get_cell to reach the sibling cells that hold them.
	var word_columns := board.word_columns_at(row, col)
	var grabbed_index: int = word_columns.find(col)
	var word_tiles: Array[Tile] = []
	for c in word_columns:
		word_tiles.append(board.tile_at(row, c))
		board_view.get_cell(row, c).hide_tile()
	set_drag_preview(TileView.build_word_drag_preview(word_tiles, grabbed_index, size))
	return {"word_from_row": row, "word_from_col": col}


# Blanks this cell out, regardless of what's actually on the board there —
# used while its tile is off being dragged (see `_get_drag_data` above),
# when the board itself hasn't changed yet so `refresh()` below would just
# redraw the tile right back.
func hide_tile() -> void:
	label.text = ""
	TileView.hide_corner_badge(corner_badge)
	add_theme_stylebox_override("panel", TileView.empty_style())


# Redraws this single cell from `board`'s current state — used by
# BoardView.refresh() for every cell (see board_view.gd), and by this cell
# itself once a drag that started here ends (see `_notification` below),
# to restore its look if the drag didn't actually carry its tile away.
func refresh() -> void:
	var tile := board.tile_at(row, col)
	if tile == null:
		label.text = ""
		TileView.hide_corner_badge(corner_badge)
		add_theme_stylebox_override("panel", TileView.empty_style())
	else:
		label.text = tile.current_letter()
		add_theme_stylebox_override("panel", TileView.style_for_tile(tile))
		TileView.update_corner_badge(corner_badge, tile)


# Virtual function: Godot calls this on EVERY Control (not just the one
# being dragged) whenever something relevant happens, `what` saying which
# kind of event. NOTIFICATION_DRAG_END fires once a drag ends, successful
# or not — a successful drop already gets this cell redrawn correctly via
# BoardView.refresh() (see main.gd's `_refresh_views`), so this only ever
# has real work to do when the drag was cancelled or dropped somewhere
# illegal, putting this cell's hidden tile back.
func _notification(what: int) -> void:
	if what == NOTIFICATION_DRAG_END:
		refresh()


# Virtual function: Godot calls this for mouse/keyboard events over this
# Control that the drag-and-drop system above doesn't already claim. Used
# here to detect a plain click — releasing the mouse close to where it was
# pressed — and treat it as "flip this tile in place": doc/RULES.md's
# "trampoline" mechanic says flipping never empties a cell, so (unlike
# dragging) any occupied cell may always be flipped, no `draggable` check
# needed.
func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed:
			_press_position = event.position
		elif board.tile_at(row, col) != null and event.position.distance_to(_press_position) < 6:
			tile_flipped.emit(row, col)
