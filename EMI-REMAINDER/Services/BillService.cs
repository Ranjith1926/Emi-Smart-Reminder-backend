using EMI_REMAINDER.Data;
using EMI_REMAINDER.DTOs;
using EMI_REMAINDER.DTOs.Bills;
using EMI_REMAINDER.Models;
using Microsoft.EntityFrameworkCore;

namespace EMI_REMAINDER.Services;

public class BillService
{
    private readonly AppDbContext _db;
    private readonly ReminderService _reminderService;

    public BillService(AppDbContext db, ReminderService reminderService)
    {
        _db = db;
        _reminderService = reminderService;
    }

    public async Task<PagedResponse<BillResponse>> GetBillsAsync(int userId, BillQueryParams query)
    {
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);
        var q = _db.Bills.Where(b => b.UserId == userId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (query.Status.Equals("overdue", StringComparison.OrdinalIgnoreCase))
                q = q.Where(b => b.DueDate < DateTime.UtcNow.Date && b.Status != "paid");
            else
                q = q.Where(b => b.Status == query.Status.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
            q = q.Where(b => b.Category == query.Category);

        q = query.Sort?.ToLower() switch
        {
            "amount" => query.Order == "desc" ? q.OrderByDescending(b => b.Amount) : q.OrderBy(b => b.Amount),
            "title"  => query.Order == "desc" ? q.OrderByDescending(b => b.Title) : q.OrderBy(b => b.Title),
            _        => query.Order == "desc" ? q.OrderByDescending(b => b.DueDate) : q.OrderBy(b => b.DueDate)
        };

        var totalCount = await q.CountAsync();
        var bills = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResponse<BillResponse>
        {
            Data = bills.Select(MapToResponse).ToList(),
            Pagination = new PaginationMeta
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            }
        };
    }

    public async Task<BillResponse?> GetBillByIdAsync(int id, int userId)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        return bill is null ? null : MapToResponse(bill);
    }

    public async Task<BillResponse> CreateBillAsync(int userId, CreateBillRequest request)
    {
        var bill = new Bill
        {
            UserId = userId,
            Title = request.Title,
            Category = request.Category,
            Amount = request.Amount,
            DueDate = request.DueDate.Date,
            Frequency = request.Frequency,
            IsRecurring = request.IsRecurring,
            Notes = request.Notes,
            Institution = request.Institution,
            AccountInfo = request.AccountInfo,
            Status = "due",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Bills.Add(bill);
        await _db.SaveChangesAsync();

        // Auto-create reminders
        await _reminderService.CreateRemindersForBillAsync(bill, userId);

        return MapToResponse(bill);
    }

    public async Task<BillResponse?> UpdateBillAsync(int id, int userId, UpdateBillRequest request)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (bill is null) return null;

        var dueDateChanged = false;

        if (request.Title is not null) bill.Title = request.Title;
        if (request.Category is not null) bill.Category = request.Category;
        if (request.Amount.HasValue) bill.Amount = request.Amount.Value;
        if (request.DueDate.HasValue)
        {
            dueDateChanged = bill.DueDate.Date != request.DueDate.Value.Date;
            bill.DueDate = request.DueDate.Value.Date;
        }
        if (request.Frequency is not null) bill.Frequency = request.Frequency;
        if (request.IsRecurring.HasValue) bill.IsRecurring = request.IsRecurring.Value;
        if (request.Notes is not null) bill.Notes = request.Notes;
        if (request.Institution is not null) bill.Institution = request.Institution;
        if (request.AccountInfo is not null) bill.AccountInfo = request.AccountInfo;

        await _db.SaveChangesAsync();

        if (dueDateChanged)
            await _reminderService.RescheduleRemindersForBillAsync(bill, userId);

        return MapToResponse(bill);
    }

    public async Task<bool> DeleteBillAsync(int id, int userId)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (bill is null) return false;
        _db.Bills.Remove(bill);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<MarkPaidResponse?> MarkPaidAsync(int id, int userId)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (bill is null) return null;

        // Guard against double-tap: if already paid, return current state without creating another recurring bill
        if (bill.Status == "paid")
            return new MarkPaidResponse { Bill = MapToResponse(bill), NextBill = null };

        bill.Status = "paid";
        await _db.SaveChangesAsync();

        // Cancel pending reminders for this bill
        await _reminderService.CancelPendingRemindersForBillAsync(id);

        BillResponse? nextBill = null;
        if (bill.IsRecurring && bill.Frequency != "One-time")
        {
            var nextDueDate = ComputeNextDueDate(bill.DueDate, bill.Frequency);
            var newBill = new Bill
            {
                UserId = userId,
                Title = bill.Title,
                Category = bill.Category,
                Amount = bill.Amount,
                DueDate = nextDueDate,
                Frequency = bill.Frequency,
                IsRecurring = bill.IsRecurring,
                Notes = bill.Notes,
                Institution = bill.Institution,
                AccountInfo = bill.AccountInfo,
                Status = "due",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Bills.Add(newBill);
            await _db.SaveChangesAsync();
            await _reminderService.CreateRemindersForBillAsync(newBill, userId);
            nextBill = MapToResponse(newBill);
        }

        return new MarkPaidResponse { Bill = MapToResponse(bill), NextBill = nextBill };
    }

    public async Task<BillResponse?> MarkUnpaidAsync(int id, int userId)
    {
        var bill = await _db.Bills.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (bill is null) return null;

        // Guard: if bill is already unpaid, return current state without resetting reminders
        if (bill.Status != "paid")
            return MapToResponse(bill);

        bill.Status = bill.DueDate.Date < DateTime.UtcNow.Date ? "overdue" : "due";
        await _db.SaveChangesAsync();

        // Re-create reminders
        await _reminderService.RescheduleRemindersForBillAsync(bill, userId);

        return MapToResponse(bill);
    }

    public static BillResponse MapToResponse(Bill bill)
    {
        var today = DateTime.UtcNow.Date;
        var dueDate = bill.DueDate.Date;
        var computedStatus = bill.Status == "paid"
            ? "paid"
            : dueDate < today ? "overdue" : "due";

        return new BillResponse
        {
            Id = bill.Id,
            UserId = bill.UserId,
            Title = bill.Title,
            Category = bill.Category,
            Amount = bill.Amount,
            DueDate = bill.DueDate,
            Frequency = bill.Frequency,
            IsRecurring = bill.IsRecurring,
            Status = bill.Status,
            ComputedStatus = computedStatus,
            Notes = bill.Notes,
            Institution = bill.Institution,
            AccountInfo = bill.AccountInfo,
            OverdueDays = computedStatus == "overdue" ? (today - dueDate).Days : 0,
            IsDueWithin7Days = computedStatus == "due" && (dueDate - today).Days <= 7 && (dueDate - today).Days >= 0,
            CreatedAt = bill.CreatedAt,
            UpdatedAt = bill.UpdatedAt
        };
    }

    private static DateTime ComputeNextDueDate(DateTime current, string frequency) => frequency switch
    {
        "Monthly"   => current.AddMonths(1),
        "Quarterly" => current.AddMonths(3),
        "Yearly"    => current.AddYears(1),
        _           => current.AddMonths(1)
    };
}
