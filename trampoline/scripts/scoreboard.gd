# The scores under the header: "Score : 12" in free play; in a turn-based
# game, every player's score, and beside the current player's, what their
# turn would earn if it ended now ("+21", in orange, or "-5" in red), plus
# any bonus it would win. When the turn ends, that "+N" floats into the
# score, then each bonus pops up in its place and follows (see
# `play_turn_end`).
#
# Purely visual: it only reads the game session it's given.
class_name Scoreboard
extends HFlowContainer

const _FONT_SIZE := 20
# `ribbon` orange for points won, `error` red for points lost, faint
# `cream_text` for "+0" (see doc/ART_DIRECTION.md's palette).
const _GAIN_COLOR := Color("e4733a")
const _LOSS_COLOR := Color("c8463c")
const _FAINT_COLOR := Color(0.953, 0.937, 0.847, 0.6)
# How long the "+N" takes to float into the score, then how long each
# bonus pops (see doc/ART_DIRECTION.md's "Motion": ≤ 250 ms each).
const _FLOAT_SECONDS := 0.25
const _POP_SECONDS := 0.2
# How long the new score stays in view once counted, before the turn ends
# (and, with 2+ players, the handoff screen covers it).
const _HOLD_SECONDS := 0.5

# The current player's entry, kept to animate it (see `play_turn_end`):
# their "Alice : 12 pts" label, its name and score, and the "+N" label.
var _score_label: Label
var _name := ""
var _score := 0
var _points_label: Label
# Its "bonus +50" labels.
var _bonus_labels: Array[Label] = []
# Whether `play_turn_end` is running: the scores shown are then its own,
# ahead of the session's, so refreshes wait for it to finish.
var _animating := false


func _ready() -> void:
	# HFlowContainer lays its children out left to right, wrapping onto
	# a new line when they don't fit — like words in a paragraph — so 3-4
	# players on a phone take two lines instead of overflowing.
	alignment = FlowContainer.ALIGNMENT_CENTER
	add_theme_constant_override("h_separation", 24)


# Free play: a single "Score : 12".
func show_score(score: int) -> void:
	if _animating:
		return
	_clear()
	add_child(_make_label("Score : %d" % score, Color.TRANSPARENT))
	visible = true


# A turn-based game: every player (`names[i]`) with their score; the
# current player's underlined in orange, with what their turn would change
# right now beside it. Once the game is over, nobody's turn is shown.
func show_players(session: GameSession, names: Array[String]) -> void:
	if _animating:
		return
	_clear()
	var scores := session.turns.all_player_scores()
	var current := session.turns.current_player_id()
	for player_id in range(names.size()):
		# An HBoxContainer keeps one player's labels together on one line.
		var entry := HBoxContainer.new()
		entry.add_theme_constant_override("separation", 6)
		add_child(entry)
		var label := _make_label(
			_score_text(names[player_id], scores[player_id]), Color.TRANSPARENT
		)
		var is_current := player_id == current and not session.is_game_over()
		entry.add_child(_underlined(label, is_current))
		if not is_current:
			continue
		_score_label = label
		_name = names[player_id]
		_score = scores[player_id]
		var points := session.turn_points()
		var color := _FAINT_COLOR
		if points != 0:
			color = _GAIN_COLOR if points > 0 else _LOSS_COLOR
		# `%+d` always writes the sign: "+0", "+12", "-5".
		_points_label = _make_label("%+d" % points, color)
		entry.add_child(_points_label)
		for bonus in session.turn_bonuses():
			_bonus_labels.append(_make_label("bonus +%d" % bonus, _GAIN_COLOR))
			entry.add_child(_bonus_labels[-1])
	visible = true


# Hides the scoreboard (back on the start screen).
func hide_scores() -> void:
	_clear()
	visible = false


# The end of a turn worth `points` plus `bonuses` (see GameSession's
# `turn_points`/`turn_bonuses`, read BEFORE ending the turn): the "+N"
# floats into the score, which then counts it; then each bonus pops up
# where the "+N" was and floats in too. A coroutine: `await` it to wait
# for the animation to end (like a Python `async def`).
func play_turn_end(points: int, bonuses: Array[int]) -> void:
	if _score_label == null:
		return
	_animating = true
	# Made see-through rather than emptied: a label changing size would
	# re-lay the page out, under the animation.
	for label in _bonus_labels:
		label.modulate.a = 0.0
	if points != 0:
		var floater := _make_floater(_points_label.text, _points_label)
		_points_label.modulate.a = 0.0
		await _float_into_score(floater)
		_add_to_score(points)
	for bonus in bonuses:
		var popup := _make_floater("+%d" % bonus, _points_label)
		await _pop(popup)
		await _float_into_score(popup)
		_add_to_score(bonus)
	if points != 0 or not bonuses.is_empty():
		# `create_timer` makes a one-shot timer; awaiting its `timeout`
		# signal is GDScript's `sleep`, without freezing the game.
		await get_tree().create_timer(_HOLD_SECONDS).timeout
	_animating = false


func _clear() -> void:
	for child in get_children():
		# `queue_free` only frees at the end of the frame: removed first,
		# so two refreshes in one frame don't show both sets of labels.
		remove_child(child)
		child.queue_free()
	_score_label = null
	_points_label = null
	_bonus_labels.clear()


# A label in the scoreboard's font size; `color` TRANSPARENT keeps the
# theme's text color.
func _make_label(text: String, color: Color) -> Label:
	var label := Label.new()
	label.text = text
	label.add_theme_font_size_override("font_size", _FONT_SIZE)
	if color != Color.TRANSPARENT:
		label.add_theme_color_override("font_color", color)
	return label


# `label` over a thin line, orange if `shown` (the current player), else
# invisible, so every entry is the same height.
func _underlined(label: Label, shown: bool) -> VBoxContainer:
	var box := VBoxContainer.new()
	box.add_theme_constant_override("separation", 0)
	box.add_child(label)
	# A ColorRect is a plain filled rectangle.
	var line := ColorRect.new()
	line.custom_minimum_size.y = 3
	line.color = _GAIN_COLOR if shown else Color.TRANSPARENT
	box.add_child(line)
	return box


func _score_text(player_name: String, score: int) -> String:
	# `%d` inside a string, followed by `% [...]`, is GDScript's
	# printf-style formatting — like Python's `"%d" % value`. Non-breaking
	# spaces (U+00A0) keep "Joueur 2 : 5 pts" from wrapping mid-entry.
	return ("%s : %d pts" % [player_name, score]).replace(" ", " ")


# An orange copy of `text` lying over `over`, free to move: a `top_level`
# Control isn't placed by its parent container (it's positioned in
# screen coordinates, like a floating window), so tweening its position
# isn't undone by the flow layout.
func _make_floater(text: String, over: Control) -> Label:
	var floater := _make_label(text, _GAIN_COLOR if not text.begins_with("-") else _LOSS_COLOR)
	floater.top_level = true
	add_child(floater)
	floater.global_position = over.global_position
	return floater


# Slides `floater` onto the score's number (the label's right end) while
# fading it out, then frees it.
func _float_into_score(floater: Label) -> void:
	var target := _score_label.global_position + Vector2(_score_label.size.x - floater.size.x, 0)
	target.x -= (
		_score_label
		. get_theme_font("font")
		. get_string_size(" pts", HORIZONTAL_ALIGNMENT_LEFT, -1, _FONT_SIZE)
		. x
	)
	# `create_tween()` animates properties over time; `set_parallel` runs
	# the next `tween_property` calls together rather than one after the
	# other.
	var tween := create_tween().set_parallel()
	tween.tween_property(floater, "global_position", target, _FLOAT_SECONDS)
	tween.tween_property(floater, "modulate:a", 0.0, _FLOAT_SECONDS)
	# `await` pauses this function until the tween emits `finished`
	# (a signal), without freezing the game.
	await tween.finished
	floater.queue_free()


# Pops `popup` in where it lies, growing from small.
func _pop(popup: Label) -> void:
	popup.pivot_offset = popup.size / 2
	popup.scale = Vector2(0.4, 0.4)
	# TRANS_BACK + EASE_OUT overshoots a little before settling: a "pop".
	var tween := create_tween().set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	tween.tween_property(popup, "scale", Vector2.ONE, _POP_SECONDS)
	await tween.finished


# Adds `points` to the current player's shown score, with a little
# bounce, ahead of the real refresh once the turn has ended.
func _add_to_score(points: int) -> void:
	_score += points
	_score_label.text = _score_text(_name, _score)
	_score_label.pivot_offset = _score_label.size / 2
	_score_label.scale = Vector2(1.15, 1.15)
	create_tween().tween_property(_score_label, "scale", Vector2.ONE, _POP_SECONDS)
