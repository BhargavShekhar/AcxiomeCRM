using AcxiomCRM.Data; using AcxiomCRM.Models; using AcxiomCRM.Services; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc; using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize] public class LeadsController(ApplicationDbContext db,IAuditService audit):Controller {
 private string? CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
 private bool CanAccess(string? owner)=>User.IsInRole("Admin") || User.IsInRole("Manager") || owner == CurrentUserId;
 private static readonly string[] ValidLeadStatuses = ["New","Contacted","Qualified","Unqualified","Converted","Lost"];

 public async Task<IActionResult> Index(string? q,string? status){var x=db.Leads.AsQueryable();if(!User.IsInRole("Admin")&&!User.IsInRole("Manager"))x=x.Where(a=>a.AssignedTo==CurrentUserId);if(!string.IsNullOrWhiteSpace(q))x=x.Where(a=>a.LeadName.Contains(q)||(a.CompanyName??"").Contains(q));if(!string.IsNullOrWhiteSpace(status))x=x.Where(a=>a.Status==status);ViewBag.Query=q;ViewBag.Status=status;return View(await x.OrderByDescending(a=>a.LeadId).ToListAsync());}
 public IActionResult Create()=>View(new Lead());
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(Lead m){ValidateLeadStatus(m);if(!ModelState.IsValid)return View(m);m.LeadCode=string.IsNullOrWhiteSpace(m.LeadCode)?$"LED-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}":m.LeadCode;m.AssignedTo=CurrentUserId;db.Add(m);await db.SaveChangesAsync();await audit.WriteAsync("Create","Lead",m.LeadId.ToString(),null,m.LeadName);return RedirectToAction(nameof(Index));}
 public async Task<IActionResult> Edit(int id){var m=await db.Leads.FindAsync(id);if(m==null)return NotFound();if(!CanAccess(m.AssignedTo))return Forbid();return View(m);}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Edit(int id,Lead m){if(id!=m.LeadId)return BadRequest();var old=await db.Leads.AsNoTracking().FirstOrDefaultAsync(a=>a.LeadId==id);if(old==null)return NotFound();if(!CanAccess(old.AssignedTo))return Forbid();ValidateLeadStatus(m, old.Status);if(!ModelState.IsValid)return View(m);m.AssignedTo=old.AssignedTo;m.LeadCode=old.LeadCode;m.CreatedDate=old.CreatedDate;db.Update(m);await db.SaveChangesAsync();await audit.WriteAsync("Update","Lead",id.ToString(),old.Status,m.Status);return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id){var m=await db.Leads.FindAsync(id);if(m==null)return NotFound();if(!CanAccess(m.AssignedTo))return Forbid();m.Status="Lost";await db.SaveChangesAsync();await audit.WriteAsync("MarkLost","Lead",id.ToString());return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Convert(int id){var lead = await db.Leads.FirstOrDefaultAsync(x => x.LeadId == id); if (lead == null) return NotFound(); if (!CanAccess(lead.AssignedTo)) return Forbid(); if (lead.Status != "Qualified") { TempData["Error"] = "Only qualified leads can be converted to a customer."; return RedirectToAction(nameof(Index)); }
    if (await db.Customers.AnyAsync(x => x.Email == lead.Email || x.Phone == lead.Phone)) { TempData["Error"] = "A customer with the same email or phone already exists."; return RedirectToAction(nameof(Index)); }
    var customer = new Customer { CustomerCode = $"CUS-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}", CustomerName = lead.LeadName, Email = lead.Email ?? string.Empty, Phone = lead.Phone ?? string.Empty, CompanyName = lead.CompanyName, Address = string.Empty, City = string.Empty, State = string.Empty, Status = "Active", CreatedDate = DateTime.UtcNow, CreatedBy = User.Identity?.Name, OwnerId = lead.AssignedTo ?? CurrentUserId };
    db.Customers.Add(customer); await db.SaveChangesAsync();
    lead.Status = "Converted"; lead.IsConverted = true; await db.SaveChangesAsync();
    await audit.WriteAsync("ConvertLead","Lead",lead.LeadId.ToString(),"Qualified","Converted");
    await audit.WriteAsync("Create","Customer",customer.CustomerId.ToString(),null,customer.CustomerName);
    TempData["Success"] = $"Lead converted to customer {customer.CustomerName}.";
    return RedirectToAction(nameof(Index));
 }
 private void ValidateLeadStatus(Lead m, string? previousStatus = null){ if(!ValidLeadStatuses.Contains(m.Status)) { ModelState.AddModelError(nameof(m.Status),"Select a valid lead status."); return; }
    if (previousStatus is not null && !IsAllowedTransition(previousStatus, m.Status)) { ModelState.AddModelError(nameof(m.Status), $"Status cannot change from {previousStatus} to {m.Status}."); }
    if (m.ExpectedValue < 0) ModelState.AddModelError(nameof(m.ExpectedValue),"Expected value cannot be negative.");
 }
 private static bool IsAllowedTransition(string from, string to) => (from, to) switch {
    ("New", "Contacted") or ("New", "Unqualified") or ("New", "Lost") or ("New", "Qualified") => true,
    ("Contacted", "Qualified") or ("Contacted", "Unqualified") or ("Contacted", "Lost") => true,
    ("Qualified", "Converted") or ("Qualified", "Lost") => true,
    ("Unqualified", "Lost") or ("Converted", "Converted") or ("Lost", "Lost") => true,
    _ => false
 };
}
