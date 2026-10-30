using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Models;
using QrAracIletisim.Services;
using QrAracIletisim.Services.Providers;

namespace QrAracIletisim.Data;

// Development-only demo data; safe to run on every start because each record is matched on its e-mail, phone or token first
public static class DemoDataSeeder
{
    public const string AdminEmail = "admin@qrarac.demo";
    public const string OwnerPhone = "+905320000001";
    public const string SecondOwnerPhone = "+905320000002";
    public const string DemoPassword = "Demo.12345";

    public const string ActiveToken = "demo-aktif";
    public const string PausedToken = "demo-duraklatilmis";
    public const string RevokedToken = "demo-iptal";
    public const string SecondOwnerToken = "demo-ikinci-sahip";
    public const string UnclaimedToken = "demo-sahipsiz";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var context = services.GetRequiredService<ApplicationDbContext>();
        var secureHash = services.GetRequiredService<SecureHash>();

        var admin = await EnsureUserAsync(userManager, AdminEmail, null, "Demo Yönetici", Roles.Admin);
        var owner = await EnsureUserAsync(userManager, OwnerPhone, OwnerPhone, "Ayşe Demir", Roles.Owner);
        var secondOwner = await EnsureUserAsync(userManager, SecondOwnerPhone, SecondOwnerPhone, "Mehmet Kaya", Roles.Owner);

        if (await context.Stickers.AnyAsync(s => s.Token == ActiveToken))
            return;

        var now = DateTime.UtcNow;
        var car = new Vehicle { OwnerId = owner.Id, Plate = "34ABC123", Nickname = "Aile arabası" };
        var van = new Vehicle { OwnerId = owner.Id, Plate = "35DE4567", Nickname = "İş aracı" };
        var scooter = new Vehicle { OwnerId = owner.Id, Plate = "06F789" };
        var secondCar = new Vehicle { OwnerId = secondOwner.Id, Plate = "16GH2345" };

        Sticker Claimed(string token, StickerStatus status, ApplicationUser user, Vehicle vehicle, int daysAgo) => new()
        {
            Token = token,
            Status = status,
            OwnerId = user.Id,
            Vehicle = vehicle,
            ClaimedAt = now.AddDays(-daysAgo)
        };

        var active = Claimed(ActiveToken, StickerStatus.Active, owner, car, 20);
        var paused = Claimed(PausedToken, StickerStatus.Paused, owner, van, 14);
        var revoked = Claimed(RevokedToken, StickerStatus.Revoked, owner, scooter, 30);
        var second = Claimed(SecondOwnerToken, StickerStatus.Active, secondOwner, secondCar, 7);

        var batch = new StickerBatch
        {
            Count = 8,
            Note = "Demo partisi",
            CreatedById = admin.Id,
            CreatedAt = now.AddDays(-35),
            Stickers = [active, paused, revoked, second, new Sticker { Token = UnclaimedToken }]
        };
        // The rest of the batch stays unclaimed with normal random tokens
        for (var i = batch.Stickers.Count; i < batch.Count; i++)
            batch.Stickers.Add(new Sticker { Token = StickerTokens.Generate() });
        context.StickerBatches.Add(batch);

        string Visitor(int number) => secureHash.Compute($"ip:demo-visitor-{number}");

        for (var i = 0; i < 12; i++)
            context.ScanEvents.Add(new ScanEvent { Sticker = active, IpHash = Visitor(i % 4), ScannedAt = now.AddHours(-7 * i - 1) });
        for (var i = 0; i < 4; i++)
            context.ScanEvents.Add(new ScanEvent { Sticker = second, IpHash = Visitor(i + 4), ScannedAt = now.AddDays(-i - 1) });

        ContactRequest Contact(ContactType type, string? preset, int visitor, ContactResult result, int hoursAgo, string? maskedNumber = null) => new()
        {
            Sticker = active,
            Type = type,
            PresetKey = preset,
            IpHash = Visitor(visitor),
            Result = result,
            MaskedNumber = maskedNumber,
            CreatedAt = now.AddHours(-hoursAgo)
        };
        context.ContactRequests.AddRange(
            Contact(ContactType.Message, "lights", 0, ContactResult.Delivered, 3),
            Contact(ContactType.CallRequest, null, 1, ContactResult.Delivered, 26, "0850 555 01 23"),
            Contact(ContactType.Message, "blocking", 2, ContactResult.Delivered, 50),
            Contact(ContactType.Message, "alarm", 3, ContactResult.Delivered, 75),
            Contact(ContactType.Message, "alarm", 3, ContactResult.RateLimited, 75),
            Contact(ContactType.Message, "towing", 3, ContactResult.Blocked, 70));
        context.BlockedVisitors.Add(new BlockedVisitor { Sticker = active, IpHash = Visitor(3), CreatedAt = now.AddHours(-72) });

        context.OutboxMessages.AddRange(
            new OutboxMessage { Channel = OutboxChannel.Sms, To = OwnerPhone, Provider = FakeProviderBase.ProviderName, CreatedAt = now.AddHours(-3),
                Body = $"Aracınız ({Plates.Mask(car.Plate)}) için mesaj: {PresetMessages.TextFor("lights")}" },
            new OutboxMessage { Channel = OutboxChannel.Call, To = OwnerPhone, Provider = FakeProviderBase.ProviderName, CreatedAt = now.AddHours(-26),
                Body = "Maskeli arama hattı açıldı: 0850 555 01 23 numarasını arayan kişi 10 dakika boyunca size bağlanır." });

        await context.SaveChangesAsync();
    }

    private static async Task<ApplicationUser> EnsureUserAsync(UserManager<ApplicationUser> userManager, string userName, string? phone, string name, string role)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is not null)
            return user;

        user = new ApplicationUser
        {
            UserName = userName,
            Email = phone is null ? userName : null,
            EmailConfirmed = phone is null,
            PhoneNumber = phone,
            PhoneNumberConfirmed = phone is not null,
            DisplayName = name
        };
        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Demo user could not be created: {result.Errors.First().Description}");

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
