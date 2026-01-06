using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the distribution of tokens to players in multiplayer mode.
/// Handles drawing tokens from the shared pool and returning unused tokens.
/// </summary>
public class TokenDistributor : MonoBehaviour
{
    private TokenPool tokenPool_;
    private Queue<BasicToken> availableTokens_;
    private bool initialized_ = false;
    private System.Random random_;
    
    // Track which tokens are currently drawn by which player
    private Dictionary<int, List<BasicToken>> playerDrawnTokens_;

    void Start()
    {
        tokenPool_ = FindAnyObjectByType<TokenPool>();
        if (tokenPool_ == null)
        {
            Debug.LogError("TokenDistributor: TokenPool not found!");
            throw new System.Exception("TokenDistributor: TokenPool not found!");
        }
        
        random_ = new System.Random();
        playerDrawnTokens_ = new Dictionary<int, List<BasicToken>>();
        
        InitializeTokenQueue();
    }

    /// <summary>
    /// Initialize the token queue by shuffling all available tokens.
    /// </summary>
    private void InitializeTokenQueue()
    {
        availableTokens_ = new Queue<BasicToken>();
        
        // Get all tokens from the pool
        List<BasicToken> allTokens = tokenPool_.GetPool();
        
        // Create a shuffled list of tokens
        List<BasicToken> shuffledTokens = allTokens.OrderBy(x => random_.Next()).ToList();
        
        // Add all tokens to the queue
        foreach (BasicToken token in shuffledTokens)
        {
            // Make sure token is not on board and not being dragged
            token.SetInBoard(false);
            token.gameObject.SetActive(false);
            availableTokens_.Enqueue(token);
        }
        
        initialized_ = true;
        Debug.Log($"TokenDistributor: Initialized with {availableTokens_.Count} tokens.");
    }

    /// <summary>
    /// Draw a specified number of tokens for a player.
    /// </summary>
    /// <param name="playerId">The player requesting tokens</param>
    /// <param name="count">Number of tokens to draw (typically 9)</param>
    /// <returns>List of drawn tokens</returns>
    public List<BasicToken> DrawTokensForPlayer(int playerId, int count)
    {
        if (!initialized_)
        {
            Debug.LogError("TokenDistributor: Not initialized yet!");
            return new List<BasicToken>();
        }
        
        List<BasicToken> drawnTokens = new List<BasicToken>();
        
        // Return tokens that were previously drawn by this player
        if (playerDrawnTokens_.ContainsKey(playerId))
        {
            ReturnTokensFromPlayer(playerId);
        }
        
        // Draw new tokens
        int tokensToDraw = Mathf.Min(count, availableTokens_.Count);
        for (int i = 0; i < tokensToDraw; i++)
        {
            if (availableTokens_.Count > 0)
            {
                BasicToken token = availableTokens_.Dequeue();
                token.gameObject.SetActive(true);
                token.SetInBoard(false);
                drawnTokens.Add(token);
            }
        }
        
        // Track which tokens this player has
        playerDrawnTokens_[playerId] = new List<BasicToken>(drawnTokens);
        
        Debug.Log($"TokenDistributor: Drew {drawnTokens.Count} tokens for Player {playerId + 1}. {availableTokens_.Count} tokens remaining.");
        
        if (drawnTokens.Count < count)
        {
            Debug.LogWarning($"TokenDistributor: Could only draw {drawnTokens.Count} tokens out of requested {count}.");
        }
        
        return drawnTokens;
    }

    /// <summary>
    /// Return unused tokens from a player back to the pool.
    /// This is called when tokens are not placed and need to go back to the queue.
    /// </summary>
    /// <param name="playerId">The player returning tokens</param>
    public void ReturnTokensFromPlayer(int playerId)
    {
        if (!playerDrawnTokens_.ContainsKey(playerId))
        {
            return;
        }
        
        List<BasicToken> tokensToReturn = playerDrawnTokens_[playerId];
        int returnedCount = 0;
        
        foreach (BasicToken token in tokensToReturn)
        {
            // Only return tokens that are not on the board
            if (!token.GetInBoard())
            {
                token.gameObject.SetActive(false);
                availableTokens_.Enqueue(token);
                returnedCount++;
            }
        }
        
        playerDrawnTokens_[playerId].Clear();
        Debug.Log($"TokenDistributor: Returned {returnedCount} tokens from Player {playerId + 1}. {availableTokens_.Count} tokens now available.");
    }

    /// <summary>
    /// Return specific tokens back to the pool (for when player discards without using).
    /// </summary>
    public void ReturnSpecificTokens(List<BasicToken> tokens)
    {
        foreach (BasicToken token in tokens)
        {
            if (!token.GetInBoard())
            {
                token.gameObject.SetActive(false);
                availableTokens_.Enqueue(token);
            }
        }
        
        Debug.Log($"TokenDistributor: Returned {tokens.Count} specific tokens. {availableTokens_.Count} tokens now available.");
    }

    /// <summary>
    /// Get the number of tokens still available in the pool.
    /// </summary>
    public int GetAvailableTokenCount()
    {
        return availableTokens_.Count;
    }

    /// <summary>
    /// Get tokens currently held by a specific player.
    /// </summary>
    public List<BasicToken> GetPlayerTokens(int playerId)
    {
        if (playerDrawnTokens_.ContainsKey(playerId))
        {
            return new List<BasicToken>(playerDrawnTokens_[playerId]);
        }
        return new List<BasicToken>();
    }

    /// <summary>
    /// Remove tokens from a player's hand when they place them on the board.
    /// This doesn't return them to the pool - they're consumed.
    /// </summary>
    public void ConsumeTokensFromPlayer(int playerId, List<BasicToken> consumedTokens)
    {
        if (!playerDrawnTokens_.ContainsKey(playerId))
        {
            return;
        }
        
        foreach (BasicToken token in consumedTokens)
        {
            playerDrawnTokens_[playerId].Remove(token);
        }
        
        Debug.Log($"TokenDistributor: Player {playerId + 1} consumed {consumedTokens.Count} tokens.");
    }

    /// <summary>
    /// Check if there are enough tokens available for a draw.
    /// </summary>
    public bool CanDrawTokens(int count)
    {
        return availableTokens_.Count >= count;
    }

    /// <summary>
    /// Reset the distributor (useful for restarting the game).
    /// </summary>
    public void Reset()
    {
        playerDrawnTokens_.Clear();
        InitializeTokenQueue();
        Debug.Log("TokenDistributor: Reset complete.");
    }

    /// <summary>
    /// Get a summary of token distribution state (for debugging).
    /// </summary>
    public string GetDistributionSummary()
    {
        string summary = $"Available: {availableTokens_.Count}\n";
        foreach (var kvp in playerDrawnTokens_)
        {
            int activeTokens = kvp.Value.Count(t => !t.GetInBoard());
            int boardTokens = kvp.Value.Count(t => t.GetInBoard());
            summary += $"Player {kvp.Key + 1}: {activeTokens} in hand, {boardTokens} on board\n";
        }
        return summary;
    }
}
