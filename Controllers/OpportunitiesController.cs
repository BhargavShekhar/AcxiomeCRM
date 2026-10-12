using AcxiomCRM.Data;using AcxiomCRM.Models;using AcxiomCRM.Services;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize] public class OpportunitiesController(ApplicationDbContext db,IAuditService audit):Controller {
 private string? CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
 private bool CanAccess(string? owner)=>User.IsInRole("Admin") || User.IsInRole("Manager") || owner == CurrentUserId;
 private static readonly string[] ValidStages = ["Qualification","Proposal","Negotiation","Won","Lost"];

 public async Task<IActionResult> Index(string? q,string? stage){var x=db.Opportunities.AsQueryable();if(!User.IsInRole("Admin")&&!User.IsInRole("Manager"))x=x.Where(a=>a.AssignedTo==CurrentUserId);if(!string.IsNullOrWhiteSpace(q))x=x.Where(a=>a.OpportunityName.Contains(q));if(!string.IsNullOrWhiteSpace(stage))x=x.Where(a=>a.Stage==stage);ViewBag.Query=q;ViewBag.Stage=stage;return View(await x.OrderByDescending(a=>a.OpportunityId).ToListAsync());}
 public IActionResult Create()=>View(new Opportunity());
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult>Create(Opportunity m){ValidateBusiness(m);if(!ModelState.IsValid)return View(m);m.AssignedTo=CurrentUserId;ApplyOutcomeState(m);db.Add(m);await db.SaveChangesAsync();await audit.WriteAsync("Create","Opportunity",m.OpportunityId.ToString(),null,m.OpportunityName);return RedirectToAction(nameof(Index));}
 public async Task<IActionResult>Edit(int id){var m=await db.Opportunities.FindAsync(id);if(m==null)return NotFound();if(!CanAccess(m.AssignedTo))return Forbid();return View(m);}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult>Edit(int id,Opportunity m){if(id!=m.OpportunityId)return BadRequest();var old=await db.Opportunities.AsNoTracking().FirstOrDefaultAsync(a=>a.OpportunityId==id);if(old==null)return NotFound();if(!CanAccess(old.AssignedTo))return Forbid();ValidateBusiness(m, old.Stage);if(!ModelState.IsValid)return View(m);m.AssignedTo=old.AssignedTo;m.CreatedDate=old.CreatedDate;ApplyOutcomeState(m);db.Update(m);await db.SaveChangesAsync();await audit.WriteAsync("Update","Opportunity",id.ToString(),old.Stage,m.Stage);return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult>Delete(int id){var m=await db.Opportunities.FindAsync(id);if(m==null)return NotFound();if(!CanAccess(m.AssignedTo))return Forbid();m.Status="Closed";m.Stage="Lost";await db.SaveChangesAsync();await audit.WriteAsync("Close","Opportunity",id.ToString());return RedirectToAction(nameof(Index));}
 private void ValidateBusiness(Opportunity m, string? previousStage = null){ if(!ValidStages.Contains(m.Stage)) ModelState.AddModelError(nameof(m.Stage),"Select a valid opportunity stage."); if(m.Amount<=0) ModelState.AddModelError(nameof(m.Amount),"Opportunity Amount must be greater than 0."); if(m.Probability<0||m.Probability>100) ModelState.AddModelError(nameof(m.Probability),"Probability must be between 0 and 100."); if((m.Status == "Open" || string.IsNullOrWhiteSpace(m.Status)) && m.ExpectedCloseDate.Date < DateTime.Today) ModelState.AddModelError(nameof(m.ExpectedCloseDate),"Expected Close Date cannot be in the past."); if(previousStage is not null && !IsAllowedStageTransition(previousStage, m.Stage)) ModelState.AddModelError(nameof(m.Stage), $"Stage cannot change from {previousStage} to {m.Stage}."); }
 private static void ApplyOutcomeState(Opportunity m){ if(m.Stage == "Won" || m.Stage == "Lost") { m.Status = "Closed"; } else if (string.IsNullOrWhiteSpace(m.Status) || m.Status == "Closed") { m.Status = "Open"; } }
 private static bool IsAllowedStageTransition(string from, string to) => (from, to) switch {
    ("Qualification","Proposal") or ("Qualification","Negotiation") or ("Qualification","Lost") => true,
    ("Proposal","Negotiation") or ("Proposal","Lost") or ("Proposal","Won") => true,
    ("Negotiation","Won") or ("Negotiation","Lost") => true,
    ("Won","Won") or ("Lost","Lost") => true,
    _ => false
 };
}
