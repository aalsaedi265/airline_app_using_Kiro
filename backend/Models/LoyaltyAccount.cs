using System.ComponentModel.DataAnnotations;

namespace AirlineSimulationApi.Models;

public class LoyaltyAccount
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string MembershipNumber { get; set; } = string.Empty;
    
    [Required]
    public LoyaltyTier Tier { get; set; }
    
    [Range(0, int.MaxValue)]
    public int Points { get; set; }
    
    [Range(0, int.MaxValue)]
    public int MilesFlown { get; set; }
    
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
}

public enum LoyaltyTier
{
    Basic,
    Silver,
    Gold,
    Platinum,
    Diamond
}