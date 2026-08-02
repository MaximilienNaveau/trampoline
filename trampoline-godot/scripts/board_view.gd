# Visualizes a Board's current state as a 13x9 grid of colored cells, and
# accepts tiles dropped onto it from a HandView (see board_cell.gd/
# tile_panel.gd for the actual drag-and-drop mechanics). No custom art yet,
# just enough color to tell yellow letters, green letters, and empty cells
# apart at a glance.
#
# Deliberately knows nothing about GameSession, TurnManager, scoring, or
# any other game rule: it only ever reads a `Board` (see board.gd), draws
# whatever's there, and asks GrowthRules (a stateless rule-checker, not a
# mutator) whether a drop would be legal. It never places/moves a tile
# itself — that's main.gd's job, listening for the signals below. It also
# never watches the board for changes on its own — whoever owns this node
# calls `refresh(board, is_takeable)` after the board changes (placing/
# removing/flipping/moving a tile).
class_name BoardView
extends Control

# Re-broadcasts whichever cell just received a dropped/moved tile — see
# board_cell.gd's own same-named signals for what a "signal" is, and for
# why one carries "from"/"to" positions while the other doesn't. Having
# these here, instead of main.gd connecting to all 117 cells individually,
# keeps BoardView's grid an implementation detail its owner doesn't need to
# know about.
signal tile_dropped(row: int, col: int, tile: Tile)
signal tile_moved(from_row: int, from_col: int, to_row: int, to_col: int)
signal word_moved(from_row: int, from_col: int, to_row: int, to_col: int)
signal tile_flipped(row: int, col: int)

# One cell per board square, indexed [row][col] — built once in _ready()
# and reused by refresh(), rather than destroying and recreating 117 nodes
# every time the board changes.
var _cell_panels: Array = []

# `@onready` runs this line once this node's children actually exist
# (right before `_ready()` — a plain `var _grid := $Grid` at the top of
# the class would run too early, before the scene tree below this node
# has been built). `$Grid` is shorthand for "find my child node named
# Grid", declared in board_view.tscn.
@onready var _grid: GridContainer = $Grid


func _ready() -> void:
	_build_grid()


# Creates all 117 cell nodes once, empty. GridContainer lays out its
# children left-to-right then wraps after `columns` of them (set to 9 in
# board_view.tscn), so simply adding them row by row, column by column,
# naturally produces the right grid shape with no manual positioning.
func _build_grid() -> void:
	for row in range(Board.ROWS):
		var panel_row: Array = []
		for col in range(Board.COLUMNS):
			var panel := BoardCell.new()
			panel.row = row
			panel.col = col
			panel.board_view = self
			panel.custom_minimum_size = Vector2(32, 32)
			panel.add_theme_stylebox_override("panel", TileView.empty_style())
			# Connects this cell's own signals to methods on BoardView (see
			# `_on_cell_tile_dropped`/`_on_cell_tile_moved` below) — the
			# standard Godot way of reacting to a signal, same idea as a Qt
			# slot or a C callback registered against a function pointer.
			panel.tile_dropped.connect(_on_cell_tile_dropped)
			panel.tile_moved.connect(_on_cell_tile_moved)
			panel.word_moved.connect(_on_cell_word_moved)
			panel.tile_flipped.connect(_on_cell_tile_flipped)

			var label := Label.new()
			# PRESET_FULL_RECT stretches the label to fill its parent
			# panel entirely, rather than sizing itself to just the text.
			label.set_anchors_preset(Control.PRESET_FULL_RECT)
			label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
			label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
			panel.add_child(label)
			# Handed to the cell itself (not just kept here) so it can
			# redraw its own text/color — needed both by `refresh()` below
			# and by the cell itself once a drag that started there ends
			# (see board_cell.gd's `refresh()`/`_notification`).
			panel.label = label

			# Same idea for the small "other face" hint badge (see
			# tile_view.gd's build_corner_badge/update_corner_badge).
			var corner_badge := TileView.build_corner_badge()
			panel.add_child(corner_badge)
			panel.corner_badge = corner_badge

			_grid.add_child(panel)
			panel_row.append(panel)
		_cell_panels.append(panel_row)


# Redraws every cell from `board`'s current state. `is_takeable` is a
# Callable — GDScript's way of passing a function itself around as a value
# (like a C++ std::function or a Python function reference), taking
# `(row, col)` and answering whether that cell's tile can currently be
# picked back up. BoardView calls it once per cell instead of knowing the
# actual rule itself (see this file's header comment) — main.gd passes
# `GameSession.can_take_back` here.
func refresh(board: Board, is_takeable: Callable) -> void:
	for row in range(Board.ROWS):
		for col in range(Board.COLUMNS):
			var panel: BoardCell = _cell_panels[row][col]
			# Kept current every refresh, not just at creation, in case a
			# future save/load ever swaps in a whole new Board instance.
			panel.board = board
			panel.draggable = is_takeable.call(row, col)
			panel.refresh()


# Looks up the single cell at (row, col) — used by BoardCell itself (see
# board_cell.gd's `_get_drag_data`) to reach and hide sibling cells that
# belong to the same word being dragged, not just the one actually grabbed.
func get_cell(row: int, col: int) -> BoardCell:
	return _cell_panels[row][col]


# Forwards one cell's drop straight to this view's own single signal (see
# the header comment above `tile_dropped`'s declaration).
func _on_cell_tile_dropped(row: int, col: int, tile: Tile) -> void:
	tile_dropped.emit(row, col, tile)


# Same idea, for a tile moved from one board cell to another.
func _on_cell_tile_moved(from_row: int, from_col: int, to_row: int, to_col: int) -> void:
	tile_moved.emit(from_row, from_col, to_row, to_col)


# Same idea, for a whole word dragged by one of its (frozen) tiles.
func _on_cell_word_moved(from_row: int, from_col: int, to_row: int, to_col: int) -> void:
	word_moved.emit(from_row, from_col, to_row, to_col)


# Same idea, for a cell clicked (not dragged) to flip its tile in place.
func _on_cell_tile_flipped(row: int, col: int) -> void:
	tile_flipped.emit(row, col)
