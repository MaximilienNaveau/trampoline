# Multiplayer Trampoline - Implementation Guide

This document explains the multiplayer modifications made to your Trampoline game.

## New Scripts Created

### 1. TurnManager.cs
**Purpose:** Controls turn-based gameplay flow

**Key Features:**
- Tracks current player (0-3)
- Manages turn progression
- Detects when players complete their 13 words
- Fires events when turns change or game completes

**Events:**
- `OnTurnChanged(int playerId)` - Fired when turn switches
- `OnGameCompleted()` - Fired when all players finish

**Public Methods:**
```csharp
int GetCurrentPlayerId()                // Get active player
bool IsPlayerTurn(int playerId)         // Check if it's a player's turn
void EndCurrentTurn()                   // Move to next player
void SetPlayerFinished(int id, bool)    // Mark player as done
bool IsGameComplete()                   // Check if game ended
```

---

### 2. TokenDistributor.cs
**Purpose:** Manages token distribution from shared pool

**Key Features:**
- Shuffles all 117 tokens at start
- Draws tokens for players (typically 9 at a time)
- Tracks which tokens belong to which player
- Returns unused tokens back to pool

**Public Methods:**
```csharp
List<BasicToken> DrawTokensForPlayer(int playerId, int count)
void ReturnTokensFromPlayer(int playerId)
int GetAvailableTokenCount()
bool CanDrawTokens(int count)
```

---

### 3. StoreMultiplayer.cs
**Purpose:** Single dynamic storage area that changes for each player

**Key Features:**
- **ONE store for ALL players** - changes color and content dynamically
- Displays current player's tokens (9 max)
- Color and label update when turn changes
- Tracks each player's unplayed tokens separately
- Automatically draws tokens when player's turn starts

**Inspector Settings:**
- `maxTokens_` - How many tokens to display (default 9)
- `backgroundImage_` - UI Image that changes color per player
- `playerLabel_` - TextMeshPro showing "Player 1", "Player 2", etc.
- `playerColors_` - Array of 4 colors for players

**Public Methods:**
```csharp
int GetCurrentPlayerId()
bool OwnsToken(BasicToken token)
int GetTokenCount()
bool CanInteract()
List<BasicToken> GetPlayerTokens(int playerId)
```

**Setup in Unity:**
1. Use your existing Store GameObject
2. Remove `Store` component, add `StoreMultiplayer` component
3. Assign a background `Image` component
4. Player label is auto-created (optionally customize in Inspector)
5. Configure to show 9 tiles (1 row of 9, or 3 rows of 3)

---

### 4. BoardMultiplayer.cs
**Purpose:** Shared board with row ownership tracking

**Key Features:**
- Tracks which player owns which row
- Visual indicators (colored tile backgrounds)
- Automatically assigns ownership when tokens placed
- Counts complete words per player

**Public Methods:**
```csharp
void SetRowOwner(int rowIndex, int playerId)
int GetRowOwner(int rowIndex)
List<WordWithOwner> GetPlayerWords(int playerId)
int GetPlayerCompleteWordCount(int playerId)
```

**New Data Structure:**
```csharp
public class WordWithOwner {
    public string word_;
    public int nb_green_letters_;
    public int playerId_;
    public int rowIndex_;
}
```

---

### 5. GameControllerMultiplayer.cs (Updated)
**Purpose:** Manages multiplayer game logic and scoring

**Key Features:**
- Tracks scores for all players separately
- Validates words per player
- Detects when player completes 13 words
- Shows final results and winner

**Public Methods:**
```csharp
int GetPlayerScore(int playerId)
int[] GetAllPlayerScores()
int GetPlayerCompleteWordCount(int playerId)
void EndCurrentPlayerTurn()
int GetWinnerId()
```

---

### 6. DragAndDrop.cs (Updated)
**Purpose:** Prevents dragging other players' tokens

**Key Changes:**
- Checks if TurnManager exists (for multiplayer detection)
- Verifies token belongs to current player
- Prevents drag if not current player's turn
- Still works in solo mode (no TurnManager present)

---

## Scene Setup Instructions

### Required GameObject Hierarchy

```
Canvas
├── GameCanvas (tag: "GameCanvas")
├── Header
│   └── (Your existing header elements)
├── BoardMultiplayer (with BoardMultiplayer component)
└── Store (SINGLE dynamic store with StoreMultiplayer component)
    ├── Background Image
    └── Player Label (TextMeshProUGUI showing "Player 1")

GameControllers (Empty GameObject)
├── GameControllerMultiplayer
├── TurnManager
├── TokenDistributor
├── TokenPool (existing)
└── FrenchDictionary (existing)
```

---

## Step-by-Step Scene Configuration

### Step 1: Replace Board Component
1. Select your Board GameObject
2. Remove `Board` component
3. Add `BoardMultiplayer` component
4. Configure rows/columns as before

### Step 2: Setup Single Dynamic Store
1. Select your existing Store GameObject
2. Remove `Store` component
3. Add `StoreMultiplayer` component
4. Set `Max Tokens` to 9
5. **Add Background Image:**
   - Add an `Image` component to the Store (or a child object)
   - Assign this Image to `Background Image` field
6. **Player Label (Auto-created):**
   - A TextMeshProUGUI "PlayerLabel" is automatically created at runtime
   - If you want custom styling, you can assign your own in the Inspector
7. Configure grid: 1 row × 9 cols OR 3 rows × 3 cols

**Result:** The store will automatically:
- Change color based on current player
- Display "Player 1", "Player 2", etc.
- Show only the current player's 9 tokens
- Remember each player's unplayed tokens

### Step 3: Add New Manager Components
1. Create empty GameObject named "GameManagers"
2. Add `TurnManager` component
3. Add `TokenDistributor` component
4. These components have no required Inspector settings

### Step 4: Replace GameController
1. Find your GameController GameObject
2. Remove `GameController` component
3. Add `GameControllerMultiplayer` component

### Step 5: Position Store
Position the store prominently on screen:
```
┌─────────────────────┐
│   Store             │
│   [Player 1]        │
│   [9 tokens]        │
├─────────────────────┤
│                     │
│       Board         │
│                     │
└─────────────────────┘
```

The store dynamically updates for each player's turn.

---

## Testing Checklist

### Initial Setup Test
- [ ] Scene loads without errors
- [ ] Console shows "TurnManager: Game started with X players"
- [ ] Console shows "TokenDistributor: Initialized with 117 tokens"
- [ ] Each store shows as player's color

### Gameplay Test
- [ ] Player 1's store shows "Player 1" and has red/first color
- [ ] Player 1 can drag their tokens
- [ ] Player 2 cannot drag tokens (not their turn)
- [ ] Tokens can be placed on board
- [ ] Board tiles show subtle player color when word placed
- [ ] Score updates for current player

### Turn Progression Test
- [ ] Call `turnManager.EndCurrentTurn()` (via script or button)
- [ ] Store background color changes to Player 2's color
- [ ] Label updates to "Player 2"
- [ ] Player 2's tokens appear in store
- [ ] Player 1's tokens are remembered (not lost)
- [ ] Player 2 can now drag their tokens
- [ ] Player 1 cannot drag tokens (not visible)

### Completion Test
- [ ] When player completes 13 words of 9 letters
- [ ] Console shows "Player X has completed 13 words!"
- [ ] Turn automatically advances to next player
- [ ] When all players finish, "Game completed!" appears
- [ ] Winner is announced in console

---

## Common Issues & Solutions

### Issue: "TurnManager not found"
**Solution:** Make sure you added TurnManager component to a GameObject in the scene.

### Issue: "Store for Player X not found"
**Solution:** This error no longer applies - you only need ONE store with StoreMultiplayer component.

### Issue: Can't drag any tokens
**Solution:** 
1. Check that TurnManager.GetCurrentPlayerId() matches a store's playerId
2. Verify tokens are in the correct store's `myTokens_` list
3. Make sure canvas has "GameCanvas" tag

### Issue: No tokens in stores
**Solution:**
1. Verify TokenDistributor is in scene
2. Check TokenPool is initialized
3. Store auto-draws tokens on turn start - wait for first turn
4. Check console for "Player X drew Y tokens" message

### Issue: Store doesn't change color/player
**Solution:**
1. Verify TurnManager exists and is firing OnTurnChanged event
2. Check `Background Image` field is assigned in StoreMultiplayer
3. Check `Player Label` field is assigned (TextMeshProUGUI)
4. Try manually calling `turnManager.EndCurrentTurn()` to test

### Issue: Board doesn't show player colors
**Solution:**
1. Ensure Board uses `BoardMultiplayer` component (not `Board`)
2. Check that tiles have Image components
3. Colors are subtle (20% alpha) - they should be faint tints

---

## Configuration Options

### Player Colors
In `StoreMultiplayer` and `BoardMultiplayer`, you can customize:
```csharp
private Color[] playerColors_ = new Color[] {
    new Color(1f, 0.7f, 0.7f, 0.3f),  // Player 1: Light Red
    new Color(0.7f, 0.7f, 1f, 0.3f),  // Player 2: Light Blue
    new Color(0.7f, 1f, 0.7f, 0.3f),  // Player 3: Light Green
    new Color(1f, 1f, 0.7f, 0.3f)     // Player 4: Light Yellow
};
```

### Tokens Per Draw
In `StoreMultiplayer`:
```csharp
private int maxTokens_ = 9;  // Change to any number
```

### Win Condition
In `GameControllerMultiplayer`:
```csharp
if (completeWords >= 13)  // Change required number of words
```

---

## Adding UI Elements

### Turn Indicator
```csharp
// In a UI script:
void Update() {
    TurnManager tm = FindAnyObjectByType<TurnManager>();
    int current = tm.GetCurrentPlayerId();
    turnText.text = $"Player {current + 1}'s Turn";
}
```

### Score Display
```csharp
// In a UI script:
void Update() {
    GameControllerMultiplayer gc = FindAnyObjectByType<GameControllerMultiplayer>();
    for (int i = 0; i < numberOfPlayers; i++) {
        playerScoreTexts[i].text = $"P{i+1}: {gc.GetPlayerScore(i)}";
    }
}
```

### End Turn Button
```csharp
// Attach to button onClick:
public void OnEndTurnClicked() {
    GameControllerMultiplayer gc = FindAnyObjectByType<GameControllerMultiplayer>();
    gc.EndCurrentPlayerTurn();
}
```

---

## Differences from Solo Mode

| Feature | Solo Mode | Multiplayer Mode |
|---------|-----------|------------------|
| Storage | 1 Store with 117 tokens | 1 Dynamic Store showing 9 tokens per player |
| Store Display | All tokens visible | Current player's tokens only |
| Store Color | Static | Changes per player turn |
| Board | Shared, no ownership | Shared with row ownership |
| Scoring | Single score | Separate scores per player |
| Turn Flow | No turns | Turn-based with restrictions |
| Token Distribution | All available | Drawn from shared pool (9 per turn) |
| Token Memory | N/A | Each player's tokens remembered separately |
| Win Condition | Complete 13 words | Complete 13 words per player |

---

## Next Steps

1. **Test with 2 players first** - Set PlayerPrefs "NumberOfPlayers" to 2
2. **Create UI for turn indication** - Show whose turn it is
3. **Add "End Turn" button** - Call `EndCurrentPlayerTurn()`
4. **Create score display** - Show all player scores
5. **Add final results screen** - Show winner when game completes
6. **Test with 3-4 players** - Add more stores and test

---

## Debug Commands

Useful for testing in Unity Console or debug scripts:

```csharp
// Get current state
TurnManager tm = FindAnyObjectByType<TurnManager>();
Debug.Log($"Current Player: {tm.GetCurrentPlayerId() + 1}");
Debug.Log($"Game Complete: {tm.IsGameComplete()}");

TokenDistributor td = FindAnyObjectByType<TokenDistributor>();
Debug.Log($"Available Tokens: {td.GetAvailableTokenCount()}");

GameControllerMultiplayer gc = FindAnyObjectByType<GameControllerMultiplayer>();
int[] scores = gc.GetAllPlayerScores();
for (int i = 0; i < scores.Length; i++) {
    Debug.Log($"Player {i+1}: {scores[i]} points");
}
```

---

## Files Summary

**New Files Created:**
- `TurnManager.cs` - Turn management
- `TokenDistributor.cs` - Token distribution
- `StoreMultiplayer.cs` - Player-specific storage
- `BoardMultiplayer.cs` - Board with ownership

**Modified Files:**
- `GameControllerMultiplayer.cs` - Completely rewritten for multiplayer
- `DragAndDrop.cs` - Added turn restrictions

**Unchanged Files (still used):**
- `TokenPool.cs` - Token definitions
- `FrenchDictionary.cs` - Word validation
- `BasicToken.cs` - Token behavior
- `Tile.cs` - Tile behavior
- `ScrollableGrid.cs` - Base grid class

---

Good luck with your multiplayer implementation! Let me know if you need help with any specific part.
