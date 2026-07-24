using backend.Data;
using backend.Dtos;
using backend.Extensions;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExcelDataReader;
using System.Data;
using System.Text;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/leads")]
[Tags("Leads")]
public class LeadsController(
    CrmDbContext db,
    ILeadAssignmentService assignmentService,
    IOneSignalNotificationService notificationService) : ControllerBase
{
    private static readonly string[] ImportColumns =
        ["Name", "Phone", "Email", "AlternativePhone", "Address", "BudgetRange", "PreferredLocation", "Remarks"];

    [HttpGet("import-template")]
    [AllowAnonymous]
    public IActionResult DownloadImportTemplate()
    {
        var csv = string.Join(",", ImportColumns) +
                  "\r\nRahim Ahmed,01700000001,rahim@example.com,,Dhaka,50-70 lakh,Gulshan,Website enquiry";
        return File(Encoding.UTF8.GetBytes(csv), "text/csv", "lead-import-template.csv");
    }

    [HttpPost("import")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult> Import(IFormFile file, [FromForm] bool autoAssign = false)
    {
        if (file.Length == 0) return BadRequest(new { message = "Choose a non-empty CSV, XLS, or XLSX file." });
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".csv" or ".xls" or ".xlsx"))
            return BadRequest(new { message = "Only CSV, XLS, and XLSX files are supported." });

        var rows = new List<Dictionary<string, string>>();
        await using var stream = file.OpenReadStream();
        if (extension == ".csv")
        {
            using var reader = new StreamReader(stream);
            var lines = new List<string>();
            while (await reader.ReadLineAsync() is { } line) lines.Add(line);
            if (lines.Count > 0)
            {
                var headers = ParseCsvLine(lines[0]);
                foreach (var line in lines.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var values = ParseCsvLine(line);
                    rows.Add(headers.Select((h, i) => (h, value: i < values.Count ? values[i] : ""))
                        .ToDictionary(x => x.h.Trim(), x => x.value.Trim(), StringComparer.OrdinalIgnoreCase));
                }
            }
        }
        else
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var excel = extension == ".xls"
                ? ExcelReaderFactory.CreateBinaryReader(stream)
                : ExcelReaderFactory.CreateOpenXmlReader(stream);
            var table = excel.AsDataSet().Tables.Cast<DataTable>().FirstOrDefault();
            if (table is not null && table.Rows.Count > 0)
            {
                var headers = table.Rows[0].ItemArray.Select(x => x?.ToString()?.Trim() ?? "").ToArray();
                foreach (DataRow row in table.Rows.Cast<DataRow>().Skip(1))
                    rows.Add(headers.Select((h, i) => (h, value: row[i]?.ToString()?.Trim() ?? ""))
                        .ToDictionary(x => x.h, x => x.value, StringComparer.OrdinalIgnoreCase));
            }
        }

        var missing = new[] { "Name", "Phone" }.Where(c => rows.Count == 0 || !rows[0].ContainsKey(c)).ToArray();
        if (missing.Length > 0) return BadRequest(new { message = $"Missing mandatory column(s): {string.Join(", ", missing)}." });
        var executives = autoAssign
            ? await db.Users.Include(x => x.Role).Where(x => x.IsActive && x.Role.Name == "SalesExecutive").OrderBy(x => x.Id).ToListAsync()
            : [];
        if (autoAssign && executives.Count == 0) return BadRequest(new { message = "No active sales executives are available for auto-assignment." });

        var imported = 0;
        var importedAssignedLeads = new List<Lead>();
        var skipped = new List<object>();
        foreach (var (row, index) in rows.Select((value, index) => (value, index)))
        {
            var name = row.GetValueOrDefault("Name")?.Trim() ?? "";
            var phone = row.GetValueOrDefault("Phone")?.Trim() ?? "";
            if (name.Length == 0 || phone.Length == 0) { skipped.Add(new { row = index + 2, reason = "Name and Phone are mandatory." }); continue; }
            var email = NullIfBlank(row.GetValueOrDefault("Email"));
            if (await db.Leads.AnyAsync(x => x.Phone == phone || (email != null && x.Email == email)))
            { skipped.Add(new { row = index + 2, reason = "Duplicate phone or email." }); continue; }
            var assignedId = autoAssign ? executives[imported % executives.Count].Id : (int?)null;
            var lead = new Lead {
                CustomerName = name, Phone = phone, Email = email,
                AlternativePhone = NullIfBlank(row.GetValueOrDefault("AlternativePhone")),
                Address = NullIfBlank(row.GetValueOrDefault("Address")),
                BudgetRange = NullIfBlank(row.GetValueOrDefault("BudgetRange")),
                PreferredLocation = NullIfBlank(row.GetValueOrDefault("PreferredLocation")),
                Remarks = NullIfBlank(row.GetValueOrDefault("Remarks")),
                Source = LeadSource.Other, Priority = LeadPriority.Warm,
                Status = assignedId.HasValue ? LeadStatus.Assigned : LeadStatus.New,
                AssignedToId = assignedId, CreatedById = User.UserId()
            };
            db.Leads.Add(lead);
            if (assignedId.HasValue) importedAssignedLeads.Add(lead);
            imported++;
        }
        await db.SaveChangesAsync();
        foreach (var lead in importedAssignedLeads)
            await notificationService.SendLeadAssignedAsync(
                lead.AssignedToId!.Value, lead.Id, lead.CustomerName, HttpContext.RequestAborted);
        return Ok(new { imported, skipped, autoAssigned = autoAssign });
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>(); var current = new StringBuilder(); var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') { if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; } else quoted = !quoted; }
            else if (line[i] == ',' && !quoted) { values.Add(current.ToString()); current.Clear(); }
            else current.Append(line[i]);
        }
        values.Add(current.ToString()); return values;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeadDto>>> GetLeads([FromQuery] ProjectType? projectType = null)
    {
        var query = db.Leads.Include(x => x.AssignedTo).Include(x => x.Project).AsQueryable();
        if (User.IsInRole("SalesExecutive")) query = query.Where(x => x.AssignedToId == User.UserId());
        if (projectType.HasValue)
            query = query.Where(x => x.Project != null && x.Project.Type == projectType);

        var leads = await query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new LeadDto(x.Id, x.CustomerName, x.Phone, x.Email, x.Source, x.Status, x.AssignedToId, x.AssignedTo == null ? null : x.AssignedTo.FullName, x.ProjectId, x.Project == null ? null : x.Project.Name, x.Project == null ? null : x.Project.Type, x.NextFollowUpAt))
            .ToListAsync();

        return Ok(leads);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> CreateLead(CreateLeadRequest request)
    {
        if (!request.AssignedToId.HasValue)
        {
            return BadRequest(new { message = "Lead must be assigned to a sales executive by admin." });
        }

        var salesExecutive = await assignmentService.GetActiveSalesExecutiveAsync(request.AssignedToId.Value);
        if (salesExecutive is null)
        {
            return BadRequest(new { message = "Assigned user must be an active sales executive." });
        }

        if (request.ProjectId.HasValue && !await db.Projects.AnyAsync(x => x.Id == request.ProjectId.Value))
        {
            return BadRequest(new { message = "Selected project was not found." });
        }

        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await db.Customers.FindAsync(request.CustomerId.Value);
            if (customer is null) return BadRequest(new { message = "Selected customer was not found." });

            var hasActiveAssignment = await db.Leads.AnyAsync(lead =>
                lead.Status != LeadStatus.Booked &&
                (lead.Phone == customer.Phone ||
                 (customer.Email != null && lead.Email != null && lead.Email.ToLower() == customer.Email.ToLower())));
            if (hasActiveAssignment)
                return Conflict(new { message = "This customer is already assigned. They can be assigned again after the current lead is Booked." });
        }

        var customerName = customer?.Name ?? request.CustomerName;
        var phone = customer?.Phone ?? request.Phone;
        var alternativePhone = customer?.AlternativePhone ?? request.AlternativePhone;
        var email = customer?.Email ?? request.Email;
        var address = customer?.Address ?? request.Address;

        var conflict = await assignmentService.GetAssignmentConflictAsync(
            phone, email, request.AssignedToId.Value, ignoreCustomerId: customer?.Id);
        if (conflict is not null)
        {
            return Conflict(new { message = conflict });
        }

        var lead = new Lead
        {
            CustomerName = customerName,
            Phone = phone,
            AlternativePhone = alternativePhone,
            Email = email,
            Address = address,
            BudgetRange = request.BudgetRange,
            PreferredLocation = request.PreferredLocation,
            ProjectId = request.ProjectId,
            Source = request.Source,
            Status = LeadStatus.Assigned,
            AssignedToId = request.AssignedToId,
            CreatedById = User.UserId(),
            Remarks = request.Remarks
        };

        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        await notificationService.SendLeadAssignedAsync(
            request.AssignedToId.Value, lead.Id, lead.CustomerName, HttpContext.RequestAborted);
        return Created($"/api/leads/{lead.Id}", new { lead.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateLead(int id, UpdateLeadRequest request)
    {
        var lead = await db.Leads.FindAsync(id);
        if (lead is null) return NotFound();
        if (User.IsInRole("SalesExecutive") && lead.AssignedToId != User.UserId()) return Forbid();
        var previousAssignedToId = lead.AssignedToId;
        if (request.AssignedToId.HasValue)
        {
            if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var salesExecutive = await assignmentService.GetActiveSalesExecutiveAsync(request.AssignedToId.Value);
            if (salesExecutive is null)
            {
                return BadRequest(new { message = "Assigned user must be an active sales executive." });
            }

            var conflict = await assignmentService.GetAssignmentConflictAsync(lead.Phone, lead.Email, request.AssignedToId.Value, ignoreLeadId: lead.Id);
            if (conflict is not null)
            {
                return Conflict(new { message = conflict });
            }
        }
        if (request.ProjectId.HasValue && !await db.Projects.AnyAsync(x => x.Id == request.ProjectId.Value))
            return BadRequest(new { message = "Selected project was not found." });

        lead.Status = request.Status ?? lead.Status;
        lead.AssignedToId = request.AssignedToId ?? lead.AssignedToId;
        lead.ProjectId = request.ProjectId ?? lead.ProjectId;
        lead.NextFollowUpAt = request.NextFollowUpAt ?? lead.NextFollowUpAt;
        lead.Remarks = request.Remarks ?? lead.Remarks;

        if (lead.Status == LeadStatus.Booked && !await db.Customers.AnyAsync(x => x.LeadId == lead.Id))
        {
            db.Customers.Add(new Customer
            {
                LeadId = lead.Id, Name = lead.CustomerName, Phone = lead.Phone,
                AlternativePhone = lead.AlternativePhone, Email = lead.Email, Address = lead.Address,
                AssignedToId = lead.AssignedToId, ProjectId = lead.ProjectId,
                PaymentStatus = "Positive"
            });
        }
        await db.SaveChangesAsync();
        if (request.AssignedToId.HasValue && request.AssignedToId != previousAssignedToId)
        {
            await notificationService.SendLeadAssignedAsync(
                request.AssignedToId.Value, lead.Id, lead.CustomerName, HttpContext.RequestAborted);
        }
        return NoContent();
    }
}
