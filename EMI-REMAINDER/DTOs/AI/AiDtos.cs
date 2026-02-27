namespace EMI_REMAINDER.DTOs.AI;

public class ExplainEmiRequest
{
    public int BillId { get; set; }
}

public class ExplainEmiResponse
{
    public string Explanation { get; set; } = string.Empty;
}

public class ReminderMessageRequest
{
    public int BillId { get; set; }
    public string Channel { get; set; } = "push";
}

public class ReminderMessageResponse
{
    public string Message { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
}

public class MonthlyInsightsRequest
{
    public int Month { get; set; }
    public int Year { get; set; }
}

public class MonthlyInsightsResponse
{
    public string Insights { get; set; } = string.Empty;
}
