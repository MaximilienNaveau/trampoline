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

# Built once, on first use, and reused by every tile/cell afterwards (see
# `add_bevel_highlight` below) — a `Texture2D` (what `Gradient`Texture2D`
# actually is) can be handed to any number of nodes at once, the same
# image drawn stretched to fit each one, so there's no need for a fresh
# copy per tile. `static var` (unlike a plain `var` on a `RefCounted`
# class like this one, which never gets instantiated) is GDScript's way
# of sharing one value across every caller of these `static func`s.
static var _bevel_texture: GradientTexture2D


# The diagonal light-to-dark sheen `add_bevel_highlight` draws over a
# tile — built lazily (only the first time anything asks for it) since
# most of a session's tiles are drawn well after this file first loads.
static func _get_bevel_texture() -> GradientTexture2D:
	if _bevel_texture == null:
		# A `Gradient` is just a list of colors at positions (0 to 1) along
		# a line; `set_color`/`add_point` below give it exactly two, one at
		# each end — semi-transparent white fading to semi-transparent
		# black is what reads as "light hitting the top-left corner of a
		# raised surface, shadow pooling in the opposite corner."
		var gradient := Gradient.new()
		gradient.set_color(0, Color(1, 1, 1, 0.35))
		gradient.set_color(1, Color(0, 0, 0, 0.2))
		_bevel_texture = GradientTexture2D.new()
		_bevel_texture.gradient = gradient
		# `fill_from`/`fill_to` are in UV space (0,0 = top-left corner of
		# whatever this texture is stretched across, 1,1 = bottom-right) —
		# a straight line between those two corners is the actual diagonal.
		_bevel_texture.fill_from = Vector2(0, 0)
		_bevel_texture.fill_to = Vector2(1, 1)
	return _bevel_texture


# Lays a subtle diagonal light/shadow sheen over `panel` — the cheap,
# shader-free way to make a flat-colored StyleBoxFlat rectangle read as a
# small raised cube instead, echoing the real tiles' chunky wood/cork
# look (see doc/real-game/). Every tile/cell view (BoardCell in
# board_view.gd, TilePanel in hand_view.gd, and this file's own drag
# preview below) calls this once, right after setting its base color —
# never inside `refresh()`, since the sheen itself never changes.
static func add_bevel_highlight(panel: Panel) -> void:
	var sheen := TextureRect.new()
	sheen.texture = _get_bevel_texture()
	sheen.set_anchors_preset(Control.PRESET_FULL_RECT)
	# `stretch_mode` makes the texture's own *pixels* fill whatever size
	# this tile ends up at (which varies — see board_view.gd's
	# `_update_cell_sizes`) instead of tiling. That alone isn't enough,
	# though: a `TextureRect`'s default `expand_mode` keeps the CONTROL
	# itself sized to the texture's own natural pixel dimensions
	# (`GradientTexture2D`'s default width/height), no matter what its
	# anchors say — so without this, every sheen was actually rendering
	# as a fixed ~100x100 square regardless of tile size, spilling out
	# over neighboring tiles and the bare background beyond them.
	# IGNORE_SIZE is what makes it truly follow the anchors instead.
	sheen.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	sheen.stretch_mode = TextureRect.STRETCH_SCALE
	# Otherwise this full-rect overlay would swallow every click/drag
	# meant for the tile underneath it (see board_cell.gd's `_gui_input`
	# for why MOUSE_FILTER_IGNORE is the right one to reach for here).
	sheen.mouse_filter = Control.MOUSE_FILTER_IGNORE
	panel.add_child(sheen)


# The style to draw an occupied cell/tile with, based on which face it's
# currently showing.
static func style_for_tile(tile: Tile) -> StyleBoxFlat:
	var color := GREEN_COLOR if tile.current_is_green() else YELLOW_COLOR
	return _make_style(color)


# The style to draw an empty cell with (no tile there at all).
static func empty_style() -> StyleBoxFlat:
	return _make_style(EMPTY_COLOR)


# The letter a tile would show if flipped right now — whichever face ISN'T
# currently showing. Lets a player see what's "behind" a tile before
# committing to flip it (see doc/RULES.md's "trampoline" mechanic). A
# blank back face (the "shorten" mechanic) reads as "-", matching the
# physical tile's own printed face.
static func other_face_letter(tile: Tile) -> String:
	if tile.is_showing_front():
		return tile.back_letter() if tile.back_letter() != "" else "-"
	return tile.front_letter()


# The color other_face_letter() above would be shown in — the front face
# is always yellow (see tile.gd); the back is green unless it's one of the
# two special all-yellow tiles.
static func other_face_color(tile: Tile) -> Color:
	if tile.is_showing_front():
		return GREEN_COLOR if tile.back_is_green() else YELLOW_COLOR
	return YELLOW_COLOR


# Creates the small top-right badge every tile view (BoardCell, TilePanel,
# and this file's own drag-preview builder below) uses to show
# other_face_letter/other_face_color — matching the physical tiles, which
# print this hint on its own small colored patch rather than as plain
# colored text (which would be unreadable whenever it happened to match
# the tile's own CURRENT color, e.g. a tile whose two faces are both
# yellow). Starts empty/invisible; call update_corner_badge() once there's
# an actual tile to describe.
static func build_corner_badge() -> Panel:
	var badge := Panel.new()
	badge.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	# A control anchored to a corner still needs to know which way to grow
	# from that zero-size point to fit its actual size — left/down here, so
	# the badge stays inside ITS OWN tile instead of spilling into the next
	# one. Setting this explicitly rather than trusting the preset's own
	# default avoids exactly that overflow.
	badge.grow_horizontal = Control.GROW_DIRECTION_BEGIN
	badge.grow_vertical = Control.GROW_DIRECTION_END
	# `custom_minimum_size` (a genuine, persistent size *request*), not a
	# one-time `size` assignment — a plain Panel has no content of its own
	# to otherwise justify a non-zero size, so an anchored-but-content-less
	# Panel like this one would just get resolved back down to 0x0 the next
	# time anything re-runs layout on it (unlike a Label, whose own text
	# already gives it a real size to grow from).
	badge.custom_minimum_size = Vector2(14, 12)
	badge.visible = false

	var label := Label.new()
	# A node's `name` lets a later lookup find it again via get_node() (see
	# update_corner_badge below) — like a dictionary key, except it's how
	# Godot's scene tree itself is addressed.
	label.name = "Label"
	label.set_anchors_preset(Control.PRESET_FULL_RECT)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.add_theme_font_size_override("font_size", 8)
	badge.add_child(label)

	return badge


# Refreshes a badge built by build_corner_badge() above to match `tile`'s
# current state — needed after a flip, since which face counts as "the
# other one" swaps too.
static func update_corner_badge(badge: Panel, tile: Tile) -> void:
	badge.visible = true
	badge.add_theme_stylebox_override("panel", _make_style(other_face_color(tile)))
	var label: Label = badge.get_node("Label")
	label.text = other_face_letter(tile)


# Hides a badge built by build_corner_badge() above — used wherever a cell
# has no tile at all (nothing to hint at).
static func hide_corner_badge(badge: Panel) -> void:
	badge.visible = false


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
	var visual := _build_tile_visual(tile, tile_size)
	visual.position = -tile_size / 2
	root.add_child(visual)
	return root


# Same idea, but for a whole word (see BoardCell's word-drag) — draws every
# tile in `tiles` left to right, positioned so the one at `grabbed_index`
# (the tile actually under the cursor) is centered on it, with the rest of
# the word following at its sides, same as it would look on the board.
static func build_word_drag_preview(
	tiles: Array[Tile], grabbed_index: int, tile_size: Vector2
) -> Control:
	var root := Control.new()
	for i in range(tiles.size()):
		var visual := _build_tile_visual(tiles[i], tile_size)
		# How many tile-widths this tile sits from the grabbed one, plus
		# the same "center on the cursor" offset build_drag_preview uses.
		visual.position = Vector2((i - grabbed_index) * tile_size.x, 0) - tile_size / 2
		root.add_child(visual)
	return root


# The actual colored, lettered rectangle both preview builders above
# position — never added to the real scene tree, so nothing ever runs a
# layout pass on it: setting only `custom_minimum_size` (a mere size
# *request* for a parent container to honor) would leave its actual `size`
# at (0, 0). Setting `size` directly gives it real, immediate dimensions to
# render and to position against.
static func _build_tile_visual(tile: Tile, tile_size: Vector2) -> Control:
	var visual := Panel.new()
	visual.size = tile_size
	visual.add_theme_stylebox_override("panel", style_for_tile(tile))
	add_bevel_highlight(visual)
	var label := Label.new()
	label.set_anchors_preset(Control.PRESET_FULL_RECT)
	label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.text = tile.current_letter()
	visual.add_child(label)
	var corner := build_corner_badge()
	update_corner_badge(corner, tile)
	visual.add_child(corner)
	return visual


# A StyleBoxFlat is Godot's simplest drawable: a solid-colored (optionally
# rounded/bordered) rectangle. The border/corner/shadow here are meant to
# echo the real tiles' look (see doc/real-game/): a small cork/wood cube
# with a visibly darker edge, not a flat rectangle of pure color.
static func _make_style(color: Color) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = color
	# `darkened()` returns the same hue at a lower brightness — cheaper
	# than a second color constant per caller, and always stays in sync
	# with whichever color this style was actually built for.
	style.border_color = color.darkened(0.35)
	style.set_border_width_all(2)
	style.set_corner_radius_all(4)
	style.shadow_color = Color(0, 0, 0, 0.25)
	style.shadow_size = 2
	style.shadow_offset = Vector2(0, 1)
	return style
