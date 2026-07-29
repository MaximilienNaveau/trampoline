# How a `Tile` gets drawn, shared by every view that shows one (BoardView,
# HandView, ...) — just the yellow/green/empty color mapping and a helper to
# turn a color into a drawable style, so each view doesn't redefine its own
# copy of these three colors. Deliberately only about *drawing*, never
# touching board/scoring/turn logic, per this project's "separate logic from
# graphics" rule — this file is graphics code, but it's graphics code that
# knows about `Tile`, not the other way around.
class_name TileView
extends RefCounted

const YELLOW_COLOR := Color(0.96, 0.87, 0.4)
const GREEN_COLOR := Color(0.6, 0.85, 0.55)
const EMPTY_COLOR := Color(0.88, 0.88, 0.88)


# The style to draw an occupied cell/tile with, based on which face it's
# currently showing.
static func style_for_tile(tile: Tile) -> StyleBoxFlat:
	var color := GREEN_COLOR if tile.current_is_green() else YELLOW_COLOR
	return _make_style(color)


# The style to draw an empty cell with (no tile there at all).
static func empty_style() -> StyleBoxFlat:
	return _make_style(EMPTY_COLOR)


# Builds a floating preview of `tile` for Control.set_drag_preview(),
# shared by every draggable tile view (TilePanel, BoardCell) — already
# centered on the cursor. Godot forcibly re-centers whatever ROOT control
# it's given on the mouse position every frame, overwriting any position we
# set on it ourselves, but it never touches a CHILD's position — so this
# returns an otherwise-empty root Control with the actual visible tile as
# an offset child, which is what keeps the tile looking centered no matter
# where Godot moves the root.
static func build_drag_preview(tile: Tile, tile_size: Vector2) -> Control:
	var root := Control.new()

	var visual := Panel.new()
	# This visual is never added to the real scene tree, so nothing ever
	# runs a layout pass on it — setting only `custom_minimum_size` (a mere
	# size *request* for a parent container to honor) would leave its
	# actual `size` at (0, 0). Setting `size` directly gives it real,
	# immediate dimensions to render and to center against below.
	visual.size = tile_size
	visual.position = -tile_size / 2
	visual.add_theme_stylebox_override("panel", style_for_tile(tile))
	var label := Label.new()
	label.set_anchors_preset(Control.PRESET_FULL_RECT)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.text = tile.current_letter()
	visual.add_child(label)

	root.add_child(visual)
	return root


# A StyleBoxFlat is Godot's simplest drawable: a solid-colored (optionally
# rounded/bordered) rectangle.
static func _make_style(color: Color) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = color
	return style
