using AcxiomCRM.Data;using AcxiomCRM.Models;using AcxiomCRM.Services;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize] public class FollowUpsController(ApplicationDbContext db,IAuditService audit):Controller {
 private string? CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
 private bool CanAccess(string? owner)=>User.IsInRole("Admin") || User.IsInRole("Manager") || owner == CurrentUserId;

 public async Task<IActionResult>Index(string? status){var x=db.FollowUps.AsQueryable();if(!User.IsInRole("Admin")&&!User.IsInRole("Manager"))x=x.Where(a=>a.AssignedTo==CurrentUserId);if(!string.IsNullOrWhiteSpace(status))x=x.Where(a=>a.Status==status);ViewBag.Status=status;return View(await x.OrderBy(a=>a.FollowUpDate).ToListAsync());}
 public IActionResult Create()=>View(new FollowUp());
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult>Create(FollowUp m){if(m.FollowUpDate.Date<DateTime.Today)ModelState.AddModelError(nameof(m.FollowUpDate),"Follow-up date cannot be earlier than today.");if(!ModelState.IsValid)return View(m);m.AssignedTo=CurrentUserId;db.Add(m);await db.SaveChangesAsync();await audit.WriteAsync("Create","FollowUp",m.FollowUpId.ToString());return RedirectToAction(nameof(Index));}
 [HttpPost,ValidateAntiForgeryToken]public async Task<IActionResult>Complete(int id){var m=await db.FollowUps.FindAsync(id);if(m==null)return NotFound();if(!CanAccess(m.AssignedTo))return Forbid();var old=m.Status;m.Status="Completed";await db.SaveChangesAsync();await audit.WriteAsync("Complete","FollowUp",id.ToString(),old,m.Status);return RedirectToAction(nameof(Index));}
}
