using AcxiomCRM.Models; using AcxiomCRM.Services; using Microsoft.AspNetCore.Identity; using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc;
namespace AcxiomCRM.Controllers;
public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IAuditService audit) : Controller {
 [HttpGet] public IActionResult Login(string? returnUrl=null) {
    if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
    ViewBag.ReturnUrl=returnUrl;
    return View();
 }
 [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Login(LoginViewModel model,string? returnUrl=null) { 
    ViewBag.ReturnUrl=returnUrl; 
    if(!ModelState.IsValid)return View(model); 
    var user=await users.FindByEmailAsync(model.Email.Trim());
    if(user==null || !user.IsActive){
        ModelState.AddModelError("","Invalid credentials or inactive account.");
        await audit.WriteAsync("LoginFailed","Authentication",model.Email.Trim());
        return View(model);
    }
    var result=await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
    if(result.Succeeded){
        await audit.WriteAsync("Login","Authentication",user.Id);
        if(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index","Dashboard");
    }
    if(result.IsLockedOut){
        ModelState.AddModelError("","Account locked temporarily. Try again later.");
    }
    else if(result.RequiresTwoFactor){
        ModelState.AddModelError("","Two-factor authentication is required.");
    }
    else {
        ModelState.AddModelError("","Invalid login attempt.");
    }
    await audit.WriteAsync("LoginFailed","Authentication",user.Id);
    return View(model);
 }
 [HttpGet] public IActionResult Register(){ if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index","Dashboard"); return View(); }
 [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Register(RegisterViewModel model) { 
    if(!ModelState.IsValid)return View(model);
    var email = model.Email.Trim();
    var existing = await users.FindByEmailAsync(email);
    if(existing != null){
        ModelState.AddModelError(nameof(model.Email),"An account with this email already exists.");
        return View(model);
    }

    var user = new ApplicationUser{UserName=email,Email=email,FullName=model.FullName.Trim(),EmailConfirmed=true,IsActive=true};
    var result = await users.CreateAsync(user, model.Password);
    if(result.Succeeded){
        await users.AddToRoleAsync(user,"SalesExecutive");
        await audit.WriteAsync("Register","User",user.Id);
        await signIn.SignInAsync(user,false);
        return RedirectToAction("Index","Dashboard");
    }
    foreach(var e in result.Errors)ModelState.AddModelError("",e.Description);
    return View(model);
 }
 [HttpPost, Authorize, ValidateAntiForgeryToken] public async Task<IActionResult> Logout(){var id=users.GetUserId(User); await signIn.SignOutAsync(); await audit.WriteAsync("Logout","Authentication",id); return RedirectToAction("Login");}
 public IActionResult AccessDenied()=>View();
}
