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


# A StyleBoxFlat is Godot's simplest drawable: a solid-colored (optionally
# rounded/bordered) rectangle.
static func _make_style(color: Color) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = color
	return style
