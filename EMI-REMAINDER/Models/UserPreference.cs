using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMI_REMAINDER.Models;

[Table("UserPreferences")]
public class UserPreference
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "tinyint(1)")]
    public bool PushEnabled { get; set; } = true;

    [Column(TypeName = "tinyint(1)")]
    public bool SmsEnabled { get; set; } = false;

    [Column(TypeName = "tinyint(1)")]
    public bool WhatsAppEnabled { get; set; } = false;

    [MaxLength(50)]
    public string ReminderDays { get; set; } = "7,3,0";

    [MaxLength(10)]
    public string Language { get; set; } = "en";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
