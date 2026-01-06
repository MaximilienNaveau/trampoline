# Quick Reference - Multiplayer Components

## Files Created ✓

### Core Multiplayer Logic
1. **TurnManager.cs** - Controls whose turn it is
2. **TokenDistributor.cs** - Distributes tokens from shared pool
3. **StoreMultiplayer.cs** - Player-specific storage (9 tokens)
4. **BoardMultiplayer.cs** - Board with row ownership tracking
5. **GameControllerMultiplayer.cs** - Manages scores and game flow

### UI Helper Components
6. **MultiplayerScoreDisplay.cs** - Shows all player scores
7. **EndTurnButton.cs** - Button to end current player's turn

### Modified Files
8. **DragAndDrop.cs** - Prevents dragging other players' tokens

---

## Unity Scene Setup (Quick Steps)

### 1. Board Setup
- Replace `Board` component → `BoardMultiplayer` component

### 2. Store Setup (Create 2-4 stores)
For each player:
- Duplicate existing Store GameObject
- Remove `Store` component → Add `StoreMultiplayer` component  
- Set `Player Id`: 0, 1, 2, or 3
- Set `Max Tokens`: 9
- Add background `Image` component
- Configure grid: 9 tiles (1×9 or 3×3)

### 3. Add Managers
Create empty GameObject "GameManagers" with:
- `TurnManager` component
- `TokenDistributor` component

### 4. Replace Controller
- Remove `GameController` → Add `GameControllerMultiplayer`

### 5. Optional UI
**Score Display:**
- Create UI Text elements (4x TextMeshPro)
- Add `MultiplayerScoreDisplay` component
- Assign text elements to array

**End Turn Button:**
- Create UI Button
- Add `EndTurnButton` component

---

## Component Inspector Settings

### StoreMultiplayer
```
Player Id: 0, 1, 2, or 3
Max Tokens: 9
Background Image: [Assign Image component]
Player Colors: [4 colors in array]
```

### MultiplayerScoreDisplay
```
Player Score Texts: [Array of 4 TextMeshProUGUI]
Player Highlights: [Optional: Array of Images]
Score Format: "Player {0}: {1} pts ({2}/13)"
Current Player Color: Yellow
Normal Player Color: White
```

### EndTurnButton
```
Button Text: [Optional: TextMeshProUGUI]
Button Text Format: "End Turn (Player {0})"
Update Button Text: ✓
```

---

## Testing Commands (Unity Console)

```csharp
// Get references
var tm = FindAnyObjectByType<TurnManager>();
var td = FindAnyObjectByType<TokenDistributor>();
var gc = FindAnyObjectByType<GameControllerMultiplayer>();

// Check current state
Debug.Log($"Current Player: {tm.GetCurrentPlayerId() + 1}");
Debug.Log($"Available Tokens: {td.GetAvailableTokenCount()}");

// View scores
for (int i = 0; i < gc.GetNumberOfPlayers(); i++) {
    Debug.Log($"Player {i+1}: {gc.GetPlayerScore(i)} pts, " +
              $"{gc.GetPlayerCompleteWordCount(i)} words");
}

// End turn manually
gc.EndCurrentPlayerTurn();

// Check game state
Debug.Log($"Game Complete: {tm.IsGameComplete()}");
if (tm.IsGameComplete()) {
    Debug.Log($"Winner: Player {gc.GetWinnerId() + 1}");
}
```

---

## Gameplay Flow

```
1. Game Start
   └─> Player 1's turn begins
       └─> Player 1's store draws 9 tokens
       
2. Player 1's Turn
   ├─> Drag tokens to board
   ├─> Form words (validated)
   ├─> Score updates
   └─> Click "End Turn"
   
3. Turn Changes
   └─> Player 2's turn begins
       └─> Player 2's store draws 9 tokens
       
4. Repeat steps 2-3
   
5. Player Completes
   └─> When 13 words of 9 letters reached
       └─> Auto-advance to next player
       
6. All Players Complete
   └─> Game ends
       └─> Winner announced
```

---

## Key Behaviors

### Token Ownership
- Tokens belong to the player who drew them
- Only current player can drag their tokens
- Tokens on board are "consumed"

### Row Ownership  
- When player places first token in a row → they own that row
- Row gets subtle color tint
- Cannot modify other players' rows

### Turn Restrictions
- Can only drag tokens during your turn
- Store is highlighted when it's your turn
- Other stores are dimmed

### Scoring
- Each player scored independently
- Score = Σ(n×(n+1)/2) - (green_letters×5)
- Only valid words count

---

## Common Workflows

### Starting Game with 2 Players
```csharp
PlayerPrefs.SetInt("NumberOfPlayers", 2);
SceneManager.LoadScene("multi_game_scene");
```

### Forcing Turn Switch (Debug)
```csharp
TurnManager tm = FindAnyObjectByType<TurnManager>();
tm.ForcePlayerTurn(1); // Switch to Player 2
```

### Checking Token Distribution
```csharp
TokenDistributor td = FindAnyObjectByType<TokenDistributor>();
Debug.Log(td.GetDistributionSummary());
```

### Resetting Game
```csharp
TurnManager tm = FindAnyObjectByType<TurnManager>();
TokenDistributor td = FindAnyObjectByType<TokenDistributor>();
tm.ResetGame();
td.Reset();
```

---

## Visual Indicators

| Element | Not Your Turn | Your Turn | Finished |
|---------|---------------|-----------|----------|
| Store Background | Dim (α=0.2) | Bright (α=0.5) | Dim |
| Board Rows | Player color tint | Player color tint | Player color tint |
| Score Text | White | Yellow | White + "✓" |
| Can Drag | ✗ | ✓ | ✗ |

---

## Player Colors (Default)

- **Player 1:** Light Red (1.0, 0.7, 0.7)
- **Player 2:** Light Blue (0.7, 0.7, 1.0)
- **Player 3:** Light Green (0.7, 1.0, 0.7)
- **Player 4:** Light Yellow (1.0, 1.0, 0.7)

Customize in `StoreMultiplayer.playerColors_` and `BoardMultiplayer.playerColors_`

---

## Troubleshooting Quick Fixes

| Problem | Solution |
|---------|----------|
| Can't drag anything | Check TurnManager exists and current player ID matches store |
| No tokens appear | Verify TokenDistributor exists and initialized |
| Wrong player highlighted | Check StoreMultiplayer playerId matches index |
| Rows not colored | Use BoardMultiplayer (not Board) |
| Game won't end turns | Verify EndTurnButton has button.onClick connected |

---

## File Locations

All new scripts are in:
```
Assets/Scripts/
├── TurnManager.cs
├── TokenDistributor.cs
├── StoreMultiplayer.cs
├── BoardMultiplayer.cs
├── GameControllerMultiplayer.cs
├── MultiplayerScoreDisplay.cs
└── EndTurnButton.cs
```

Documentation:
```
trampoline/
└── MULTIPLAYER_SETUP.md (full guide)
└── QUICK_REFERENCE.md (this file)
```

---

## Next Implementation Steps

1. ✓ Core scripts created
2. ⚠ Setup Unity scene (follow MULTIPLAYER_SETUP.md)
3. ⚠ Test with 2 players
4. ⚠ Add UI score display
5. ⚠ Add "End Turn" button
6. ⚠ Test with 3-4 players
7. ⚠ Add final results screen
8. ⚠ Polish and playtest

---

Good luck! Reference MULTIPLAYER_SETUP.md for detailed instructions.
