using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;
using QrAracIletisim.Services;

namespace QrAracIletisim.Controllers;

public class AccountController(SignInManager<ApplicationUser> signInManager) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        // Owners type their phone in any common shape; admins use their e-mail, which is also their user name
        var userName = model.Login.Contains('@') ? model.Login.Trim() : PhoneNumbers.Normalize(model.Login);
        if (userName is not null)
        {
            var result = await signInManager.PasswordSignInAsync(userName, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
                return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home")!);
            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Çok fazla hatalı deneme yapıldı. Hesap 15 dakika kilitlendi.");
                return View(model);
            }
        }

        ModelState.AddModelError("", "Telefon/e-posta veya şifre hatalı.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
