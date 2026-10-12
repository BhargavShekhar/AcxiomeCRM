namespace AcxiomCRM.Services;
public interface IAuditService { Task WriteAsync(string action, string entity, string? recordId = null, string? oldValue = null, string? newValue = null); }
