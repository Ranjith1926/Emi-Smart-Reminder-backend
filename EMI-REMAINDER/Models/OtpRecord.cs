using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMI_REMAINDER.Models;

[Table("OtpRecords")]
public class OtpRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(15)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OtpHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(36)]
    public string RequestId { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int FailedAttempts { get; set; } = 0;
}
