# Standalone checks for draw_plan.gd, runnable via:
#   godot4 --headless --path . --script tests/test_draw_plan.gd
extends SceneTree

var _failures := 0


func _check(condition: bool, message: String) -> void:
	if condition:
		print("PASS: ", message)
	else:
		print("FAIL: ", message)
		_failures += 1


func _initialize() -> void:
	_test_known_values()
	_test_schedules_total_117_tiles()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


func _test_known_values() -> void:
	_check(DrawPlan.initial_draw_size(1) == 9, "solo initial draw is 9")
	_check(DrawPlan.initial_draw_size(2) == 13, "2-player initial draw is 13")
	_check(DrawPlan.initial_draw_size(3) == 12, "3-player initial draw is 12")
	_check(DrawPlan.initial_draw_size(4) == 11, "4-player initial draw is 11")

	_check(not DrawPlan.has_starter_bonus_tile(1), "solo play has no leftover starter tile")
	_check(DrawPlan.has_starter_bonus_tile(2), "2-player play leaves a 117th tile for the starter")
	_check(not DrawPlan.has_starter_bonus_tile(3), "3-player play divides the bag exactly evenly")
	_check(DrawPlan.has_starter_bonus_tile(4), "4-player play leaves a 117th tile for the starter")


func _test_schedules_total_117_tiles() -> void:
	for players in range(1, 5):
		var initial := DrawPlan.initial_draw_size(players)
		var draws := DrawPlan.subsequent_draw_count(players)
		var draw_size := DrawPlan.subsequent_draw_size(players)
		var per_player := initial + draws * draw_size
		var total := players * per_player
		if DrawPlan.has_starter_bonus_tile(players):
			total += 1
		_check(total == 117, "%d-player schedule adds up to exactly 117 tiles" % players)
