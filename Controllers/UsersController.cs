using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController(UserManager<ApplicationUser> users, IAuditService audit) : Controller
{
    public async Task<IActionResult> Index(string? q)
    {
        IQueryable<ApplicationUser> query = users.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x => x.FullName.Contains(q) || x.Email!.Contains(q));
        }

        var list = await query.OrderBy(x => x.Email).ToListAsync();
        ViewBag.Query = q;
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (user.Id == users.GetUserId(User))
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await users.UpdateAsync(user);
        await audit.WriteAsync(user.IsActive ? "ActivateUser" : "DeactivateUser", "User", id);
        TempData["Success"] = user.IsActive ? "User activated." : "User deactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        if (!new[] { "Admin", "Manager", "SalesExecutive" }.Contains(role)) return BadRequest();
        var user = await users.FindByIdAsync(id);
        if (user == null) return NotFound();

        var old = await users.GetRolesAsync(user);
        if (user.Id == users.GetUserId(User) && role != "Admin")
        {
            TempData["Error"] = "You cannot remove your own administrator role.";
            return RedirectToAction(nameof(Index));
        }

        if (old.Count > 0) await users.RemoveFromRolesAsync(user, old);
        await users.AddToRoleAsync(user, role);
        await audit.WriteAsync("RoleChange", "User", id, string.Join(",", old), role);
        TempData["Success"] = $"Role updated to {role}.";
        return RedirectToAction(nameof(Index));
    }
}
