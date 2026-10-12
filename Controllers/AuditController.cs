using AcxiomCRM.Data;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;
[Authorize(Roles="Admin")] public class AuditController(ApplicationDbContext db):Controller { public async Task<IActionResult>Index(string? q){var x=db.AuditLogs.AsQueryable();if(!string.IsNullOrWhiteSpace(q))x=x.Where(a=>a.Action.Contains(q)||a.EntityName.Contains(q)|| (a.UserId??"").Contains(q));ViewBag.Query=q;return View(await x.OrderByDescending(a=>a.CreatedDate).Take(500).ToListAsync());} }
