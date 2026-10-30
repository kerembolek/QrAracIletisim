using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QrAracIletisim.Models;
using QrAracIletisim.Models.ViewModels;
using QrAracIletisim.Services;

namespace QrAracIletisim.Controllers;

[Authorize(Roles = Roles.Owner)]
public class ProfileController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    PhoneVerificationService verifications) : Controller
{
    private const string NewPhoneKey = "NewPhone";

    public async Task<IActionResult> Index()
    {
        var user = await userManager.GetUserAsync(User);
        return View(new ProfileViewModel
        {
            MaskedPhone = PhoneNumbers.Mask(user!.PhoneNumber),
            NotifyViaWhatsApp = user.NotifyViaWhatsApp
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetChannel(bool notifyViaWhatsApp)
    {
        var user = await userManager.GetUserAsync(User);
        user!.NotifyViaWhatsApp = notifyViaWhatsApp;
        await userManager.UpdateAsync(user);

        TempData["Success"] = "Bildirim tercihiniz kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPhoneChange(string? newPhone)
    {
        var phone = PhoneNumbers.Normalize(newPhone);
        var user = await userManager.GetUserAsync(User);
        if (phone is null)
        {
            TempData["Error"] = "Geçerli bir cep telefonu numarası girin (ör. 0532 123 45 67).";
            return RedirectToAction(nameof(Index));
        }
        if (phone == user!.PhoneNumber)
        {
            TempData["Error"] = "Bu numara zaten hesabınıza kayıtlı.";
            return RedirectToAction(nameof(Index));
        }

        var numberTaken = await userManager.FindByNameAsync(phone) is not null;
        if (!await verifications.SendCodeAsync(phone, VerificationPurpose.ChangePhone, user.Id, numberTaken))
        {
            TempData["Error"] = "Bu numara için çok fazla kod istendi. Lütfen birkaç dakika sonra tekrar deneyin.";
            return RedirectToAction(nameof(Index));
        }

        TempData[NewPhoneKey] = phone;
        return RedirectToAction(nameof(ConfirmPhoneChange));
    }

    [HttpGet]
    public IActionResult ConfirmPhoneChange()
    {
        if (TempData.Peek(NewPhoneKey) is not string phone)
            return RedirectToAction(nameof(Index));

        return View(new ConfirmPhoneChangeViewModel { NewPhone = phone });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPhoneChange(ConfirmPhoneChangeViewModel model)
    {
        var phone = PhoneNumbers.Normalize(model.NewPhone);
        if (phone is null)
            return RedirectToAction(nameof(Index));
        if (!ModelState.IsValid)
            return View(model);

        // The password guards against someone using an unlocked phone to move the account to their own number
        var user = await userManager.GetUserAsync(User);
        if (!await userManager.CheckPasswordAsync(user!, model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Şifre hatalı.");
            return View(model);
        }
        if (!await verifications.VerifyAsync(phone, model.Code, VerificationPurpose.ChangePhone, user!.Id))
        {
            ModelState.AddModelError(nameof(model.Code), "Kod hatalı veya süresi dolmuş.");
            return View(model);
        }

        // The phone is also the user name, so both move together; this also changes the security stamp and ends other sessions
        var renamed = await userManager.SetUserNameAsync(user, phone);
        if (!renamed.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Code), "Kod hatalı veya süresi dolmuş.");
            return View(model);
        }
        user.PhoneNumber = phone;
        user.PhoneNumberConfirmed = true;
        await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);

        TempData.Remove(NewPhoneKey);
        TempData["Success"] = "Telefon numaranız değiştirildi.";
        return RedirectToAction(nameof(Index));
    }
}
