using AcxiomCRM.Data; using AcxiomCRM.Models; using System.Security.Claims;
namespace AcxiomCRM.Services;
public class AuditService(ApplicationDbContext db, IHttpContextAccessor accessor) : IAuditService {
 public async Task WriteAsync(string action, string entity, string? recordId=null, string? oldValue=null, string? newValue=null) { var ctx=accessor.HttpContext; db.AuditLogs.Add(new AuditLog { UserId=ctx?.User.FindFirstValue(ClaimTypes.NameIdentifier), Action=action, EntityName=entity, RecordId=recordId, OldValue=oldValue, NewValue=newValue, IpAddress=ctx?.Connection.RemoteIpAddress?.ToString(), CreatedDate=DateTime.UtcNow }); await db.SaveChangesAsync(); }
}
