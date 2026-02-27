using EMI_REMAINDER.Data;
using EMI_REMAINDER.DTOs.AI;
using EMI_REMAINDER.Models;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Services;

public class AiService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AiService> _logger;

    public AiService(AppDbContext db, ILogger<AiService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(ExplainEmiResponse? Response, string? Error)> ExplainEmiAsync(int userId, int billId)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == billId && b.UserId == userId);
        if (bill is null) return (null, "Bill not found.");

        var explanation = GenerateEmiExplanation(bill);
        return (new ExplainEmiResponse { Explanation = explanation }, null);
    }

    public async Task<(ReminderMessageResponse? Response, string? Error)> GenerateReminderMessageAsync(
        int userId, int billId, string channel)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == billId && b.UserId == userId);
        if (bill is null) return (null, "Bill not found.");

        var message = GenerateChannelMessage(bill, channel);
        return (new ReminderMessageResponse { Message = message, Channel = channel }, null);
    }

    public async Task<MonthlyInsightsResponse> GenerateMonthlyInsightsAsync(int userId, int month, int year)
    {
        var bills = await _db.Bills.Where(b => b.UserId == userId).ToListAsync();
        var insights = GenerateMonthlyInsights(bills, month, year);
        return new MonthlyInsightsResponse { Insights = insights };
    }

    private static string GenerateMonthlyInsights(List<Bill> bills, int month, int year)
    {
        var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
        var monthBills = bills.Where(b => b.DueDate.Month == month && b.DueDate.Year == year).ToList();
        var totalAmount = monthBills.Sum(b => b.Amount);
        var paidBills = monthBills.Where(b => b.Status == "paid").ToList();
        var pendingBills = monthBills.Where(b => b.Status != "paid").ToList();
        var paidAmount = paidBills.Sum(b => b.Amount);
        var pendingAmount = pendingBills.Sum(b => b.Amount);

        var categoryBreakdown = monthBills
            .GroupBy(b => b.Category)
            .Select(g => $"- **{g.Key}:** ₹{g.Sum(b => b.Amount):N0} ({g.Count()} bills)")
            .ToList();

        var overdueBills = pendingBills.Where(b => b.DueDate.Date < DateTime.UtcNow.Date).ToList();

        return $"""
            ## 📊 Monthly Summary: {monthName}

            **Total Bills:** {monthBills.Count}
            **Total Amount:** ₹{totalAmount:N0}
            **Paid:** ₹{paidAmount:N0} ({paidBills.Count} bills)
            **Pending:** ₹{pendingAmount:N0} ({pendingBills.Count} bills)
            {(overdueBills.Count > 0 ? $"**⚠️ Overdue:** {overdueBills.Count} bills totalling ₹{overdueBills.Sum(b => b.Amount):N0}" : "")}

            ### 📁 Category Breakdown
            {(categoryBreakdown.Count > 0 ? string.Join("\n", categoryBreakdown) : "No bills this month.")}

            ### 💡 Tips
            - {(pendingAmount > 0 ? $"You have ₹{pendingAmount:N0} in pending payments. Prioritize overdue bills first." : "All bills are paid! Great job staying on top of your finances.")}
            - Track your spending trends month-over-month to identify savings opportunities.
            - Consider setting up auto-pay for recurring bills to never miss a due date.
            """;
    }

    private static string GenerateEmiExplanation(Bill bill)
    {
        var today = DateTime.UtcNow.Date;
        var dueDate = bill.DueDate.Date;
        var daysUntilDue = (dueDate - today).Days;
        var statusText = daysUntilDue > 0 ? $"due in **{daysUntilDue} days**" : daysUntilDue == 0 ? "**due today**" : $"**{Math.Abs(daysUntilDue)} days overdue**";

        var categoryInsights = bill.Category switch
        {
            "EMI" => $"""
                ## 🏦 EMI Analysis: {bill.Title}

                **Amount:** ₹{bill.Amount:N0}
                **Due Date:** {bill.DueDate:dd MMMM yyyy} ({statusText})
                **Frequency:** {bill.Frequency}
                {(bill.Institution is not null ? $"**Institution:** {bill.Institution}" : "")}

                ### 💡 EMI Tips
                - Set up **auto-debit** to avoid missed payments and late fees.
                - **Part-prepayment** can significantly reduce your loan tenure.
                - Maintain a **buffer of 2–3x your EMI** in your savings account.
                - Missing EMI payments impacts your **CIBIL score** negatively.

                ### 📊 Annual Commitment
                - **Monthly:** ₹{bill.Amount:N0}
                - **Quarterly:** ₹{bill.Amount * 3:N0}
                - **Yearly:** ₹{bill.Amount * 12:N0}
                """,

            "Credit Card" => $"""
                ## 💳 Credit Card Bill: {bill.Title}

                **Amount:** ₹{bill.Amount:N0}
                **Due Date:** {bill.DueDate:dd MMMM yyyy} ({statusText})

                ### ⚠️ Important
                - Pay the **full amount** to avoid interest charges (36–48% p.a.).
                - Minimum payment protects your credit score but incurs heavy interest.
                - Set up **auto-pay** for the minimum amount as a safety net.
                - Keep utilization below **30%** for a healthy credit score.

                ### 💰 Interest Impact (if only minimum paid)
                Assuming 42% p.a. interest: carrying ₹{bill.Amount:N0} costs ~₹{bill.Amount * 0.035m:N0}/month.
                """,

            "Utilities" => $"""
                ## ⚡ Utility Bill: {bill.Title}

                **Amount:** ₹{bill.Amount:N0}
                **Due Date:** {bill.DueDate:dd MMMM yyyy} ({statusText})

                ### 💡 Saving Tips
                - Pay before the due date to avoid **late payment surcharges**.
                - Switch to **online payment** for instant confirmation and cashback.
                - Track monthly trends to identify unusual spikes in consumption.
                """,

            "Subscriptions" => $"""
                ## 📱 Subscription: {bill.Title}

                **Amount:** ₹{bill.Amount:N0}
                **Due Date:** {bill.DueDate:dd MMMM yyyy} ({statusText})
                **Frequency:** {bill.Frequency}

                ### 📋 Subscription Audit
                - Annual cost: ₹{(bill.Frequency == "Monthly" ? bill.Amount * 12 : bill.Amount):N0}
                - Review if you actively use this subscription.
                - Consider **annual plans** — usually 20–40% cheaper than monthly.
                """,

            _ => $"""
                ## 📌 {bill.Title}

                **Amount:** ₹{bill.Amount:N0}
                **Due Date:** {bill.DueDate:dd MMMM yyyy} ({statusText})
                **Category:** {bill.Category}

                Pay on time to maintain a good financial record.
                """
        };

        return categoryInsights;
    }

    private static string GenerateChannelMessage(Bill bill, string channel)
    {
        var today = DateTime.UtcNow.Date;
        var daysUntilDue = (bill.DueDate.Date - today).Days;
        var daysText = daysUntilDue switch
        {
            0 => "today",
            1 => "tomorrow",
            > 1 => $"in {daysUntilDue} days",
            _ => $"{Math.Abs(daysUntilDue)} days ago (OVERDUE)"
        };

        return channel.ToLower() switch
        {
            "whatsapp" => $"""
                🔔 *Payment Reminder*

                Hello! This is a friendly reminder about your upcoming payment.

                📌 *Bill:* {bill.Title}
                💰 *Amount:* ₹{bill.Amount:N0}
                📅 *Due Date:* {bill.DueDate:dd MMM yyyy} ({daysText})
                🏦 *Provider:* {bill.Institution ?? "N/A"}

                Please ensure timely payment to avoid penalties.

                _Sent by EMI Reminder App_ ✅
                """,

            "sms" => $"Reminder: {bill.Title} Rs.{bill.Amount:N0} due {daysText} ({bill.DueDate:dd/MM/yyyy}). Pay on time. -EmiReminder",

            _ => // push
                $"Payment Due: {bill.Title} — ₹{bill.Amount:N0} is due {daysText}. Tap to view details."
        };
    }
}
