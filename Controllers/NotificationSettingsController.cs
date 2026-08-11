using backend.Data;using backend.Models;using backend.Security;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace backend.Controllers;
[ApiController,Authorize,Route("api/notification-settings"),Tags("Notifications")]
public class NotificationSettingsController(CrmDbContext db):ControllerBase{
 [HttpGet,RequirePermission(PermissionCodes.NotificationsManage)]public async Task<ActionResult>Get()=>Ok(await db.NotificationSettings.SingleOrDefaultAsync(x=>x.Id==1)??new NotificationSettings());
 [HttpPut,RequirePermission(PermissionCodes.NotificationsManage)]public async Task<ActionResult>Put(NotificationSettings r){if(r.DueCheckIntervalMinutes is <1 or >1440||r.DueSoonDays is <0 or >30)return BadRequest();var x=await db.NotificationSettings.SingleOrDefaultAsync(x=>x.Id==1);if(x is null){x=new NotificationSettings();db.Add(x);}x.DueCheckIntervalMinutes=r.DueCheckIntervalMinutes;x.DueSoonDays=r.DueSoonDays;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();return Ok(x);}}
