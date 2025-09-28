using System.ComponentModel.DataAnnotations;

namespace AirlineSimulationApi.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool EmailConfirmed { get; set; } = false;
    
    // Navigation properties
    public NotificationPreferences? NotificationPreferences { get; set; }
    public LoyaltyAccount? LoyaltyAccount { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
