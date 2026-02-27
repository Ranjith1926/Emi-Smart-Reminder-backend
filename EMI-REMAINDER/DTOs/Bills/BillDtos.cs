namespace EMI_REMAINDER.DTOs.Bills;

public class CreateBillRequest
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public bool IsRecurring { get; set; } = true;
    public string? Notes { get; set; }
    public string? Institution { get; set; }
    public string? AccountInfo { get; set; }
}

public class UpdateBillRequest
{
    public string? Title { get; set; }
    public string? Category { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Frequency { get; set; }
    public bool? IsRecurring { get; set; }
    public string? Notes { get; set; }
    public string? Institution { get; set; }
    public string? AccountInfo { get; set; }
}

public class BillResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public string Frequency { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ComputedStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Institution { get; set; }
    public string? AccountInfo { get; set; }
    public int OverdueDays { get; set; }
    public bool IsDueWithin7Days { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BillQueryParams
{
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string Sort { get; set; } = "dueDate";
    public string Order { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class MarkPaidResponse
{
    public BillResponse Bill { get; set; } = null!;
    public BillResponse? NextBill { get; set; }
}
