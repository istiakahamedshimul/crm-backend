using backend.Data;
using backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
[Tags("Notifications")]
public class NotificationsController(CrmDbContext db) : ControllerBase
{
    [HttpGet("admin")]
    [backend.Security.RequirePermission(backend.Models.PermissionCodes.NotificationsManage)]
    public async Task<ActionResult> Admin([FromQuery] int page=1,[FromQuery] int pageSize=50,[FromQuery] bool? unread=null)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var query=db.AppNotifications.Include(x=>x.User).AsQueryable();if(unread.HasValue)query=query.Where(x=>x.IsRead!=unread.Value);var total=await query.CountAsync();var items=await query.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new{x.Id,x.UserId,Recipient=x.User.FullName,x.CustomerId,x.CustomerName,x.FileId,x.DueAmount,x.OutstandingBalance,x.DueDate,x.Type,x.Title,x.Message,x.IsRead,x.CreatedAt}).ToListAsync();return Ok(new{items,total,page,pageSize});
    }
    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.AppNotifications.Where(x => x.UserId == User.UserId());
        var total = await query.CountAsync();
        var unreadCount = await query.CountAsync(x => !x.IsRead);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.Title, x.Message, x.Type, x.Screen, x.LeadId, x.CustomerId, x.IsRead, x.CreatedAt, x.ReadAt })
            .ToListAsync();
        return Ok(new { items, page, pageSize, total, unreadCount });
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult> UnreadCount() =>
        Ok(new { count = await db.AppNotifications.CountAsync(x => x.UserId == User.UserId() && !x.IsRead) });

    [HttpPut("{id:long}/read")]
    public async Task<ActionResult> MarkRead(long id)
    {
        var item = await db.AppNotifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == User.UserId());
        if (item is null) return NotFound(new { message = "Notification not found." });
        if (!item.IsRead) { item.IsRead = true; item.ReadAt = DateTime.UtcNow; await db.SaveChangesAsync(); }
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllRead()
    {
        await db.AppNotifications.Where(x => x.UserId == User.UserId() && !x.IsRead)
            .ExecuteUpdateAsync(x => x.SetProperty(n => n.IsRead, true).SetProperty(n => n.ReadAt, DateTime.UtcNow));
        return NoContent();
    }
}
