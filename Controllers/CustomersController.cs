using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController(ApplicationDbContext db, IAuditService audit) : Controller
{
    private string? CurrentUserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    private bool CanAccess(string? ownerId) => User.IsInRole("Admin") || User.IsInRole("Manager") || ownerId == CurrentUserId;

    public async Task<IActionResult> Index(string? q)
    {
        var data = db.Customers.AsQueryable();
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            data = data.Where(x => x.OwnerId == CurrentUserId);
        if (!string.IsNullOrWhiteSpace(q))
            data = data.Where(x => x.CustomerName.Contains(q) || x.Email.Contains(q) || x.Phone.Contains(q) || (x.CompanyName ?? "").Contains(q));
        ViewBag.Query = q; return View(await data.OrderByDescending(x => x.CustomerId).ToListAsync());
    }
    public IActionResult Create() => View(new Customer());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer m)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Manager") && !User.IsInRole("SalesExecutive")) return Forbid();
        if (await db.Customers.AnyAsync(x => x.Email == m.Email)) ModelState.AddModelError(nameof(m.Email), "Email already exists.");
        if (await db.Customers.AnyAsync(x => x.Phone == m.Phone)) ModelState.AddModelError(nameof(m.Phone), "Phone already exists.");
        if (!ModelState.IsValid)
        {
            foreach (var item in ModelState)
            {
                foreach (var error in item.Value.Errors)
                {
                    Console.WriteLine(
                        $"VALIDATION ERROR [{item.Key}]: " +
                        $"{error.ErrorMessage} " +
                        $"{error.Exception?.Message}"
                    );
                }
            }

            return View(m);
        }
        m.CustomerCode = string.IsNullOrWhiteSpace(m.CustomerCode) ? $"CUS-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}" : m.CustomerCode;
        m.CreatedBy = User.Identity?.Name; m.OwnerId = CurrentUserId; db.Customers.Add(m); await db.SaveChangesAsync(); await audit.WriteAsync("Create", "Customer", m.CustomerId.ToString(), null, m.CustomerName); return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id) { var m = await db.Customers.FindAsync(id); if (m == null) return NotFound(); if (!CanAccess(m.OwnerId)) return Forbid(); return View(m); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Edit(int id, Customer m) { if (id != m.CustomerId) return BadRequest(); var old = await db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerId == id); if (old == null) return NotFound(); if (!CanAccess(old.OwnerId)) return Forbid(); if (await db.Customers.AnyAsync(x => x.CustomerId != id && (x.Email == m.Email || x.Phone == m.Phone))) ModelState.AddModelError("", "Email or phone already exists."); if (!ModelState.IsValid) return View(m); m.OwnerId = old.OwnerId; m.CustomerCode = old.CustomerCode; m.CreatedBy = old.CreatedBy; m.CreatedDate = old.CreatedDate; db.Update(m); await db.SaveChangesAsync(); await audit.WriteAsync("Update", "Customer", id.ToString(), old.CustomerName, m.CustomerName); return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id) { var m = await db.Customers.FindAsync(id); if (m == null) return NotFound(); if (!CanAccess(m.OwnerId)) return Forbid(); m.Status = "Inactive"; await db.SaveChangesAsync(); await audit.WriteAsync("Deactivate", "Customer", id.ToString(), null, m.CustomerName); return RedirectToAction(nameof(Index)); }
}
