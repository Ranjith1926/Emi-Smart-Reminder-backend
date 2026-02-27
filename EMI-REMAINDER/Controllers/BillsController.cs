using EMI_REMAINDER.DTOs;
using EMI_REMAINDER.DTOs.Bills;
using EMI_REMAINDER.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMI_REMAINDER.Controllers;

[ApiController]
[Route("api/bills")]
[Authorize]
[Produces("application/json")]
public class BillsController : ControllerBase
{
    private readonly BillService _billService;
    private readonly JwtService _jwtService;

    public BillsController(BillService billService, JwtService jwtService)
    {
        _billService = billService;
        _jwtService = jwtService;
    }

    /// <summary>Get all bills for current user with filters and pagination</summary>
    [HttpGet]
    public async Task<IActionResult> GetBills([FromQuery] BillQueryParams query)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _billService.GetBillsAsync(userId.Value, query);
        return Ok(result);
    }

    /// <summary>Get a specific bill by ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<BillResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBill(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var bill = await _billService.GetBillByIdAsync(id, userId.Value);
        if (bill is null) return NotFound(ApiResponse.Fail("Bill not found."));

        return Ok(ApiResponse<BillResponse>.Ok(bill));
    }

    /// <summary>Create a new bill and auto-schedule reminders</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BillResponse>), 201)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateBill([FromBody] CreateBillRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var bill = await _billService.CreateBillAsync(userId.Value, request);
        return CreatedAtAction(nameof(GetBill), new { id = bill.Id },
            new { success = true, data = bill, message = "Bill created successfully." });
    }

    /// <summary>Update a bill (reschedules reminders if due date changes)</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<BillResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateBill(int id, [FromBody] UpdateBillRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var bill = await _billService.UpdateBillAsync(id, userId.Value, request);
        if (bill is null) return NotFound(ApiResponse.Fail("Bill not found."));

        return Ok(ApiResponse<BillResponse>.Ok(bill));
    }

    /// <summary>Delete a bill (cascades to reminders)</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteBill(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var deleted = await _billService.DeleteBillAsync(id, userId.Value);
        if (!deleted) return NotFound(ApiResponse.Fail("Bill not found."));

        return Ok(ApiResponse.Ok("Bill deleted successfully."));
    }

    /// <summary>Mark bill as paid (auto-creates next recurring bill)</summary>
    [HttpPatch("{id:int}/mark-paid")]
    [ProducesResponseType(typeof(ApiResponse<MarkPaidResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkPaid(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _billService.MarkPaidAsync(id, userId.Value);
        if (result is null) return NotFound(ApiResponse.Fail("Bill not found."));

        return Ok(new { success = true, data = new { bill = result.Bill, nextBill = result.NextBill } });
    }

    /// <summary>Mark bill as unpaid (re-creates reminders)</summary>
    [HttpPatch("{id:int}/mark-unpaid")]
    [ProducesResponseType(typeof(ApiResponse<BillResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkUnpaid(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var bill = await _billService.MarkUnpaidAsync(id, userId.Value);
        if (bill is null) return NotFound(ApiResponse.Fail("Bill not found."));

        return Ok(ApiResponse<BillResponse>.Ok(bill));
    }

    private int? GetUserId() => _jwtService.GetUserIdFromContext(HttpContext);
}
