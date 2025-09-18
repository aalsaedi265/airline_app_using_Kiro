namespace AirlineSimulationApi.Models;

public class LoyaltyAccount
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string MembershipNumber { get; set; } = string.Empty;
    public LoyaltyTier Tier { get; set; }
    public int Points { get; set; }
    public int MilesFlown { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ApplicationUser User { get; set; } = null!;
}

public enum LoyaltyTier
{
    Basic,
    Silver,
    Gold,
    Platinum,
    Diamond
}