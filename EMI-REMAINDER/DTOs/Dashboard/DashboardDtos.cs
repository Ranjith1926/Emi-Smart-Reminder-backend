using EMI_REMAINDER.DTOs.Bills;

namespace EMI_REMAINDER.DTOs.Dashboard;

public class DashboardSummary
{
    public decimal TotalDueAmount { get; set; }
    public decimal TotalOverdueAmount { get; set; }
    public decimal TotalPaidThisMonth { get; set; }
    public int BillsDueNext7Days { get; set; }
    public int BillsOverdue { get; set; }
    public int TotalBills { get; set; }
    public int PaidBills { get; set; }
    public int PendingBills { get; set; }
}

public class OverdueResponse
{
    public List<BillResponse> Data { get; set; } = new();
    public decimal TotalOverdueAmount { get; set; }
}

public class MonthlySummary
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public double PaymentPercentage { get; set; }
    public List<CategoryBreakdown> CategoryBreakdown { get; set; } = new();
}

public class CategoryBreakdown
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}
