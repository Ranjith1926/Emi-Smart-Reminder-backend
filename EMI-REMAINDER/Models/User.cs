using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMI_REMAINDER.Models;

[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = "User";

    [MaxLength(255)]
    public string? Email { get; set; }

    [Column(TypeName = "tinyint(1)")]
    public bool IsPremium { get; set; } = false;

    public DateTime? PremiumExpiresAt { get; set; }

    [MaxLength(500)]
    public string? FcmToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public UserPreference? Preferences { get; set; }
}
