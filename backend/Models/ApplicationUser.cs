using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AirlineSimulationApi.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public NotificationPreferences? NotificationPreferences { get; set; }
    public LoyaltyAccount? LoyaltyAccount { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}