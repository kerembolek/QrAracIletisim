using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Data;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;
using QrAracIletisim.Services;

namespace QrAracIletisim.Controllers;

public class AccountController(
    ApplicationDbContext context,
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    PhoneVerificationService verifications) : Controller
{
    private const string RegisterPhoneKey = "RegisterPhone";

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
            {
                if (Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                var user = await userManager.FindByNameAsync(userName);
                return await userManager.IsInRoleAsync(user!, Roles.Admin)
                    ? RedirectToAction("Index", "Dashboard")
                    : RedirectToAction("Index", "MyStickers");
            }
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

    // Accounts start from an unclaimed sticker, which keeps strangers from spending SMS credit through an open form
    [HttpGet]
    public async Task<IActionResult> Register(string? claim)
    {
        if (!await IsClaimableAsync(claim))
            return StickerNeeded();
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Claim", new { token = claim });

        return View(new RegisterPhoneViewModel { Claim = claim! });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterPhoneViewModel model)
    {
        if (!await IsClaimableAsync(model.Claim))
            return StickerNeeded();
        if (!ModelState.IsValid)
            return View(model);

        var phone = PhoneNumbers.Normalize(model.Phone);
        if (phone is null)
        {
            ModelState.AddModelError(nameof(model.Phone), "Geçerli bir cep telefonu numarası girin (ör. 0532 123 45 67).");
            return View(model);
        }

        var numberTaken = await userManager.FindByNameAsync(phone) is not null;
        if (!await verifications.SendCodeAsync(phone, VerificationPurpose.Register, userId: null, numberTaken))
        {
            ModelState.AddModelError("", "Bu numara için çok fazla kod istendi. Lütfen birkaç dakika sonra tekrar deneyin.");
            return View(model);
        }

        // Kept in TempData so the phone number stays out of the address bar
        TempData[RegisterPhoneKey] = phone;
        return RedirectToAction(nameof(VerifyPhone), new { claim = model.Claim });
    }

    [HttpGet]
    public IActionResult VerifyPhone(string? claim)
    {
        if (TempData.Peek(RegisterPhoneKey) is not string phone)
            return RedirectToAction(nameof(Register), new { claim });

        return View(new VerifyPhoneViewModel { Phone = phone, Claim = claim ?? "" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyPhone(VerifyPhoneViewModel model)
    {
        if (!await IsClaimableAsync(model.Claim))
            return StickerNeeded();

        // The code is bound to the number, so a changed hidden field simply fails verification
        var phone = PhoneNumbers.Normalize(model.Phone);
        if (phone is null)
            return RedirectToAction(nameof(Register), new { claim = model.Claim });
        if (!ModelState.IsValid)
            return View(model);

        // Checked before the code, so a weak password does not burn a valid code
        var user = new ApplicationUser
        {
            UserName = phone,
            PhoneNumber = phone,
            PhoneNumberConfirmed = true,
            DisplayName = model.DisplayName.Trim()
        };
        foreach (var validator in userManager.PasswordValidators)
        {
            var check = await validator.ValidateAsync(userManager, user, model.Password);
            foreach (var error in check.Errors)
                ModelState.AddModelError(nameof(model.Password), error.Description);
        }
        if (!ModelState.IsValid)
            return View(model);

        if (!await verifications.VerifyAsync(phone, model.Code, VerificationPurpose.Register, userId: null))
        {
            ModelState.AddModelError(nameof(model.Code), "Kod hatalı veya süresi dolmuş.");
            return View(model);
        }

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Code), "Kod hatalı veya süresi dolmuş.");
            return View(model);
        }

        await userManager.AddToRoleAsync(user, Roles.Owner);
        await signInManager.SignInAsync(user, isPersistent: false);
        TempData.Remove(RegisterPhoneKey);
        return RedirectToAction("Index", "Claim", new { token = model.Claim });
    }

    private Task<bool> IsClaimableAsync(string? token) =>
        string.IsNullOrEmpty(token)
            ? Task.FromResult(false)
            : context.Stickers.AnyAsync(s => s.Token == token && s.Status == StickerStatus.Unclaimed);

    private RedirectToActionResult StickerNeeded()
    {
        TempData["Error"] = "Kayıt olmak için sahipsiz bir QR etiketi okutmanız gerekir.";
        return RedirectToAction("Index", "Home");
    }
}
