# Redraws the game's icon: one tile, its yellow M face up, with a green C
# on its back (shown in its corner), drawn by the game's own tile code
# (see tile_view.gd) so it matches the tiles in play. Needs a window to
# draw in (not --headless):
#   godot4 --path . --script tools/build_icon.gd
#
# Writes, in assets/icon/:
#   app_icon.png — the whole tile: the project's icon (project.godot), and
#     Android's before 8.0.
#   icon_background.png, icon_foreground.png — Android's two layers (8.0
#     and later, see export_presets.cfg): the launcher crops them to its
#     own shape (a circle, a rounded square…) keeping only the middle two
#     thirds, so the background is the tile's yellow and the foreground
#     just its M and C, small enough to stay inside any crop.
#   icon_monochrome.png — Android 13's themed icon: only the shapes count
#     (the launcher colors them), so it's just the letters M and C.
extends SceneTree

const _DIR := "res://assets/icon/"
const _SIZE := 512
# Transparent room left round the whole tile, in pixels.
const _MARGIN := 16
# Android's adaptive layers: 432 px square, the tile's letters drawn on a
# 200 px tile centred in it — its C badge's outer corner then stays inside
# the 288 px wide middle the launcher shows.
const _LAYER_SIZE := 432
const _LAYER_TILE := 200


func _initialize() -> void:
	_draw_all()


func _draw_all() -> void:
	await _draw("app_icon.png", _SIZE, _SIZE - 2 * _MARGIN, true, true)
	await _draw("icon_foreground.png", _LAYER_SIZE, _LAYER_TILE, false, true)
	await _draw("icon_monochrome.png", _LAYER_SIZE, _LAYER_TILE, false, false)
	# The background: the tile's yellow, nothing drawn on it.
	var background := Image.create(_LAYER_SIZE, _LAYER_SIZE, false, Image.FORMAT_RGBA8)
	background.fill(TileView.YELLOW_COLOR)
	background.save_png(_DIR + "icon_background.png")
	print("Wrote ", _DIR, "icon_background.png")
	quit()


# Draws the M/C tile, `tile_side` px wide, centred on a transparent
# `canvas` px square, and saves it as `file_name`. Without `face`, the
# tile's yellow face isn't drawn, only what's on it; without `badge`, the
# corner's green square isn't either, only its C.
func _draw(file_name: String, canvas: int, tile_side: int, face: bool, badge: bool) -> void:
	# A SubViewport is an off-screen drawing surface, like a render target
	# in OpenGL; with a transparent background, only what's drawn shows.
	var viewport := SubViewport.new()
	viewport.size = Vector2i.ONE * canvas
	viewport.transparent_bg = true
	viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	root.add_child(viewport)
	# `Tile.new(front, back, back_is_green)`: a yellow M, a green C.
	var tile := TileView.build_tile_visual(Tile.new("M", "C", true), Vector2.ONE * tile_side)
	tile.position = Vector2.ONE * (canvas - tile_side) / 2
	# Flat, without the cork base a tile off the board shows (see
	# tile_view.gd's `add_cork`).
	tile.get_node("Cork").free()
	if not face:
		# StyleBoxEmpty draws nothing: the Panel itself becomes invisible.
		tile.add_theme_stylebox_override("panel", StyleBoxEmpty.new())
	if not badge:
		# The corner badge, the tile's second child (see tile_view.gd's
		# `build_tile_visual`).
		tile.get_child(1).add_theme_stylebox_override("panel", StyleBoxEmpty.new())
	viewport.add_child(tile)
	# Waits until that frame has actually been drawn before reading it.
	await RenderingServer.frame_post_draw
	viewport.get_texture().get_image().save_png(_DIR + file_name)
	print("Wrote ", _DIR, file_name)
	viewport.queue_free()
