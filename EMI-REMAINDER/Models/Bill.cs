using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMI_REMAINDER.Models;

[Table("Bills")]
public class Bill
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Column(TypeName = "decimal(12,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    [MaxLength(20)]
    public string Frequency { get; set; } = "Monthly";

    [Column(TypeName = "tinyint(1)")]
    public bool IsRecurring { get; set; } = true;

    [MaxLength(20)]
    public string Status { get; set; } = "due";

    [Column(TypeName = "text")]
    public string? Notes { get; set; }

    [MaxLength(200)]
    public string? Institution { get; set; }

    [MaxLength(100)]
    public string? AccountInfo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
