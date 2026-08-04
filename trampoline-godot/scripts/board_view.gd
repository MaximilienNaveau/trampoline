# Visualizes a Board's current state as a 13x9 grid of colored cells, and
# accepts tiles dropped onto it from a HandView (see board_cell.gd/
# tile_panel.gd for the actual drag-and-drop mechanics). BoardView's own
# root IS the wood-textured frame around the grid (see board_view.tscn's
# `theme_override_styles/panel`) — a `PanelContainer` instead of a plain
# `Control` so it reports the grid's real size to whatever contains it
# (see main.tscn's container-based layout), not just a zero-size point.
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
extends PanelContainer

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

# Mirrors the fixed border main.tscn's own `Margin` node keeps around the
# whole page — used by `_update_cell_sizes` below, which has no direct
# way to ask that node for its own margin, so this just needs to be kept
# in sync with it by hand if it ever changes.
const _PAGE_MARGIN := 24.0

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
	# The viewport's own `size_changed` signal fires whenever the actual
	# window/screen size changes — e.g. the player resizing the window,
	# or an Android device simply having a different screen size than
	# this one. Keeping cells sized off that, rather than a fixed 32px,
	# is what makes the board fill the available width instead of always
	# drawing the same small fixed-size grid regardless of screen size.
	# (Listening on `_grid.resized` instead, as an earlier version of
	# this did, doesn't work: once cells claim some size, nothing ever
	# asks for less than that again, so GridContainer itself would only
	# ever grow, never actually shrink back down to even NOTICE a
	# smaller window — see `_update_cell_sizes`'s own comment.)
	get_viewport().size_changed.connect(_update_cell_sizes)
	_update_cell_sizes()


# Creates all 117 cell nodes once, empty. GridContainer lays out its
# children left-to-right then wraps after `columns` of them (set to 9 in
# board_view.tscn), so simply adding them row by row, column by column,
# naturally produces the right grid shape with no manual positioning. Each
# cell's actual size is set separately, once real space is available (see
# `_update_cell_sizes`) — not here, since nothing has been laid out yet.
func _build_grid() -> void:
	for row in range(Board.ROWS):
		var panel_row: Array = []
		for col in range(Board.COLUMNS):
			var panel := BoardCell.new()
			panel.row = row
			panel.col = col
			panel.board_view = self
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


# Resizes every cell to be as big as it can while (a) staying square and
# (b) fitting exactly `Board.COLUMNS` of them across the viewport's
# current width — instead of a fixed pixel size that would leave empty
# space beside the board on a wide screen, or run off the edge on a
# narrow one. Deliberately reads the VIEWPORT's width rather than
# `_grid.size.x` (GridContainer's own current width) — a cell's own
# `custom_minimum_size` is exactly what feeds into how wide GridContainer
# (and everything containing it, up to BoardView itself) reports
# NEEDING to be, so measuring that directly has a feedback problem: once
# cells have claimed some size, nothing ever asks a *container* for less
# than that claim again, even once the window shrinks and there's
# genuinely less room. That was a real, one-way-ratchet bug this used to
# have. The viewport's own width is the one number in this whole chain
# that's never influenced by how big the cells last were.
func _update_cell_sizes() -> void:
	# The wood frame's own left/right content margin (see
	# board_view.tscn's StyleBoxTexture) already eats into how much of
	# BoardView's own width the grid actually gets — read directly off
	# the stylebox rather than hardcoding it a second time here.
	var frame_style: StyleBox = get_theme_stylebox("panel")
	var frame_margin: float = frame_style.content_margin_left + frame_style.content_margin_right
	var separation: int = _grid.get_theme_constant("h_separation")
	var available_width := (
		get_viewport_rect().size.x - frame_margin - _PAGE_MARGIN - separation * (Board.COLUMNS - 1)
	)
	if available_width <= 0:
		return
	var cell_side: float = floor(available_width / Board.COLUMNS)
	# The letter's font size scales with the cell itself, rather than
	# staying fixed — a fixed size would look tiny on a wide screen's
	# huge cells, or overflow a narrow screen's small ones. This ratio
	# was picked by eye against the tile art.
	var font_size := int(cell_side * 0.5)
	for panel_row in _cell_panels:
		for panel: BoardCell in panel_row:
			panel.custom_minimum_size = Vector2(cell_side, cell_side)
			panel.label.add_theme_font_size_override("font_size", font_size)


# Redraws every cell from `board`'s current state. `is_takeable`/
# `can_place_here` are Callables — GDScript's way of passing a function
# itself around as a value (like a C++ std::function or a Python function
# reference), each taking `(row, col)` and answering one question: whether
# that cell's tile can currently be picked back up, and whether a NEW tile
# could legally be dropped there. BoardView calls them once per cell
# instead of knowing either rule itself (see this file's header comment)
# — main.gd passes `GameSession.can_take_back`/`GameSession.can_place_here`
# (or FreePlaySession's own versions) here.
func refresh(board: Board, is_takeable: Callable, can_place_here: Callable) -> void:
	for row in range(Board.ROWS):
		for col in range(Board.COLUMNS):
			var panel: BoardCell = _cell_panels[row][col]
			# Kept current every refresh, not just at creation, in case a
			# future save/load ever swaps in a whole new Board instance.
			panel.board = board
			panel.draggable = is_takeable.call(row, col)
			panel.placeable = can_place_here.call(row, col)
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
