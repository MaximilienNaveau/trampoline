# Manages drawing tiles from a shared, shuffled pool and tracking which
# tiles each player currently holds. This mirrors
# `../trampoline/Assets/Scripts/TokenDistributor.cs` in the Unity project:
# same shuffle-once-then-draw-in-order behavior, same per-player hand
# tracking, just in GDScript.
class_name TileBag
extends RefCounted

# Tiles not yet drawn by any player, next draw first. Godot arrays have no
# dedicated queue type, so a plain Array used via `pop_front()` plays that
# role — the GDScript equivalent of Unity's `Queue<BasicToken>`.
var _undrawn: Array[Tile] = []

# Each player's currently-held, not-yet-placed tiles, keyed by player index
# (0-based). A Dictionary here plays the role of C#'s
# `Dictionary<int, List<BasicToken>>` in `TokenDistributor.cs`.
var _hands: Dictionary = {}

# Takes every tile from `pool` (see tile_pool.gd), shuffles the draw order,
# and starts with every player's hand empty.
func _init(pool: TilePool) -> void:
	# `duplicate()` copies the array itself (so shuffling this bag's copy
	# doesn't reorder `pool.tiles`); the `Tile` objects inside are shared,
	# not cloned, which is fine since there's exactly one physical tile per
	# `Tile` instance for the whole game.
	_undrawn = pool.tiles.duplicate()
	_undrawn.shuffle()

# Draws up to `count` tiles for `player_id`, adding them to that player's
# hand. Returns the tiles actually drawn — fewer than `count` if the bag
# runs out first.
func draw(player_id: int, count: int) -> Array[Tile]:
	if not _hands.has(player_id):
		_hands[player_id] = [] as Array[Tile]
	var drawn: Array[Tile] = []
	for i in range(min(count, _undrawn.size())):
		var tile: Tile = _undrawn.pop_front()
		drawn.append(tile)
		_hands[player_id].append(tile)
	return drawn

# Returns one tile from a player's hand back to the bag (e.g. a player
# decides not to play a tile they drew). Does nothing if that player isn't
# actually holding it.
func return_tile(player_id: int, tile: Tile) -> void:
	if not _hands.has(player_id):
		return
	var hand: Array = _hands[player_id]
	var index := hand.find(tile)
	if index == -1:
		return
	hand.remove_at(index)
	_undrawn.append(tile)

# Removes a tile from a player's hand without returning it to the bag —
# call this once the tile has actually been placed on the board.
func consume(player_id: int, tile: Tile) -> void:
	if _hands.has(player_id):
		_hands[player_id].erase(tile)

# The tiles a player currently holds, unplaced.
func hand(player_id: int) -> Array[Tile]:
	return _hands.get(player_id, [])

# How many tiles are left to draw from the bag.
func remaining_count() -> int:
	return _undrawn.size()
