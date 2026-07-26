# Standalone checks for row_score.gd, runnable via:
#   godot4 --headless --path . --script tests/test_row_score.gd
extends SceneTree

var _failures := 0


func _check(condition: bool, message: String) -> void:
	if condition:
		print("PASS: ", message)
	else:
		print("FAIL: ", message)
		_failures += 1


func _initialize() -> void:
	_test_placement_order()
	_test_re_placement_gets_a_fresh_score()
	_test_total_score()

	if _failures == 0:
		print("\nAll checks passed.")
	else:
		print("\n", _failures, " check(s) failed.")
	quit(1 if _failures > 0 else 0)


func _test_placement_order() -> void:
	var scores := RowScore.new()
	_check(scores.score_at(4) == 0, "an untouched column has no score yet")

	# Placement order, not column order: col 4 first, then col 3 (a
	# prefix), then col 5 (a suffix) — scores should be 1, 2, 3 in THAT
	# order, not sorted by column.
	_check(scores.record_placement(4) == 1, "first placement anywhere gets score 1")
	_check(
		scores.record_placement(3) == 2,
		"second placement gets score 2, even though its column is smaller"
	)
	_check(scores.record_placement(5) == 3, "third placement gets score 3")
	_check(scores.score_at(4) == 1, "col 4 keeps its score 1")
	_check(scores.score_at(3) == 2, "col 3 keeps its score 2")
	_check(scores.score_at(5) == 3, "col 5 keeps its score 3")


func _test_re_placement_gets_a_fresh_score() -> void:
	var scores := RowScore.new()
	scores.record_placement(0)
	scores.record_placement(1)
	var fresh := scores.record_placement(0)
	_check(fresh == 3, "re-recording a column overwrites with the next fresh score")
	_check(scores.score_at(0) == 3, "score_at reflects the overwritten value")


func _test_total_score() -> void:
	var scores := RowScore.new()
	scores.record_placement(2)  # 1
	scores.record_placement(3)  # 2
	scores.record_placement(4)  # 3
	_check(scores.total_score([2, 3, 4]) == 6, "total_score sums the given columns")
	_check(scores.total_score([3]) == 2, "total_score works for a single column")
	_check(scores.total_score([]) == 0, "total_score of no columns is 0")
