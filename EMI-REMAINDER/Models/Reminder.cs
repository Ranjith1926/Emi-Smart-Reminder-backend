using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMI_REMAINDER.Models;

[Table("Reminders")]
public class Reminder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int BillId { get; set; }
    public int UserId { get; set; }

    public DateTime ReminderDate { get; set; }
    public int DaysBefore { get; set; }

    [Column(TypeName = "text")]
    public string Message { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Channel { get; set; } = "push";

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(BillId))]
    public Bill Bill { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
