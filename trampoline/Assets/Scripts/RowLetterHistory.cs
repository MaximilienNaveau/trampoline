using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks the placement history of letters in a single row for progressive scoring.
/// Each letter gets a position score (1, 2, 3...) based on when it was added.
/// </summary>
public class RowLetterHistory
{
    private int rowIndex_;
    private int nextPositionScore_ = 1;  // Next score to assign (starts at 1)
    private Dictionary<BasicToken, int> tokenPositionScores_;  // Track each token's position score
    private HashSet<BasicToken> frozenTokens_;  // Tokens that are part of validated words
    private int frozenWordIdCounter_ = 0;  // Counter for assigning unique frozen word IDs
    
    public RowLetterHistory(int rowIndex)
    {
        rowIndex_ = rowIndex;
        tokenPositionScores_ = new Dictionary<BasicToken, int>();
        frozenTokens_ = new HashSet<BasicToken>();
    }
    
    /// <summary>
    /// Assign position scores to new tokens that don't have them yet.
    /// Returns the number of new tokens scored.
    /// </summary>
    public int AssignPositionScoresToNewTokens(List<BasicToken> currentTokensInRow, int playerId, Color playerColor)
    {
        int newTokensScored = 0;
        
        // First, remove any tokens that are no longer in the row
        List<BasicToken> tokensToRemove = new List<BasicToken>();
        foreach (var token in tokenPositionScores_.Keys)
        {
            if (!currentTokensInRow.Contains(token))
            {
                tokensToRemove.Add(token);
            }
        }
        foreach (var token in tokensToRemove)
        {
            if (!frozenTokens_.Contains(token))
            {
                tokenPositionScores_.Remove(token);
                token.ClearOwnership();
                Debug.Log($"RowLetterHistory: Removed token '{token.GetLetters()}' from row {rowIndex_} (moved away)");
            }
        }
        
        // Now assign scores to new tokens
        foreach (BasicToken token in currentTokensInRow)
        {
            // Skip if token already has a position score
            if (tokenPositionScores_.ContainsKey(token))
            {
                continue;
            }
            
            // Assign the next position score
            tokenPositionScores_[token] = nextPositionScore_;
            token.SetOwnership(playerId, nextPositionScore_, playerColor);
            
            nextPositionScore_++;
            newTokensScored++;
            
            Debug.Log($"RowLetterHistory: Assigned position score {nextPositionScore_ - 1} to token '{token.GetLetters()}' in row {rowIndex_}");
        }
        
        return newTokensScored;
    }
    
    /// <summary>
    /// Get the total score for all tokens in this row belonging to a specific player.
    /// Score is the sum of position scores: 1+2+3+4...
    /// </summary>
    public int GetPlayerScore(int playerId)
    {
        int score = 0;
        
        foreach (var kvp in tokenPositionScores_)
        {
            BasicToken token = kvp.Key;
            int positionScore = kvp.Value;
            
            // Only count tokens owned by this player
            if (token.GetOwnerId() == playerId)
            {
                score += positionScore;
            }
        }
        
        return score;
    }
    
    /// <summary>
    /// Get the score for a player's frozen tokens only (from previous turns).
    /// </summary>
    public int GetPlayerFrozenScore(int playerId)
    {
        int totalScore = 0;
        
        foreach (var kvp in tokenPositionScores_)
        {
            BasicToken token = kvp.Key;
            int positionScore = kvp.Value;
            
            if (token.GetOwnerId() == playerId && frozenTokens_.Contains(token))
            {
                totalScore += positionScore;
            }
        }
        
        return totalScore;
    }
    
    /// <summary>
    /// Get the score for a player's unfrozen tokens only (current turn).
    /// </summary>
    public int GetPlayerCurrentTurnScore(int playerId)
    {
        int totalScore = 0;
        
        foreach (var kvp in tokenPositionScores_)
        {
            BasicToken token = kvp.Key;
            int positionScore = kvp.Value;
            
            if (token.GetOwnerId() == playerId && !frozenTokens_.Contains(token))
            {
                totalScore += positionScore;
            }
        }
        
        return totalScore;
    }
    
    /// <summary>
    /// Freeze tokens as part of a validated word.
    /// Frozen tokens cannot be moved individually.
    /// </summary>
    public void FreezeWord(List<BasicToken> tokensInWord)
    {
        int wordId = frozenWordIdCounter_++;
        
        foreach (BasicToken token in tokensInWord)
        {
            token.SetFrozen(true, wordId);
            frozenTokens_.Add(token);
        }
        
        Debug.Log($"RowLetterHistory: Froze {tokensInWord.Count} tokens as word ID {wordId} in row {rowIndex_}");
    }
    
    /// <summary>
    /// Check if a token is frozen (part of a validated word).
    /// </summary>
    public bool IsTokenFrozen(BasicToken token)
    {
        return frozenTokens_.Contains(token);
    }
    
    /// <summary>
    /// Remove a token from tracking (when it's moved to store).
    /// Frozen tokens cannot be removed.
    /// </summary>
    public bool RemoveToken(BasicToken token)
    {
        if (frozenTokens_.Contains(token))
        {
            Debug.LogWarning($"RowLetterHistory: Cannot remove frozen token '{token.GetLetters()}' from row {rowIndex_}");
            return false;
        }
        
        if (tokenPositionScores_.ContainsKey(token))
        {
            tokenPositionScores_.Remove(token);
            token.ClearOwnership();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the current highest position score assigned.
    /// </summary>
    public int GetNextPositionScore()
    {
        return nextPositionScore_;
    }
    
    /// <summary>
    /// Reset the row (for testing or clearing the board).
    /// </summary>
    public void Clear()
    {
        tokenPositionScores_.Clear();
        frozenTokens_.Clear();
        nextPositionScore_ = 1;
        frozenWordIdCounter_ = 0;
    }
}
