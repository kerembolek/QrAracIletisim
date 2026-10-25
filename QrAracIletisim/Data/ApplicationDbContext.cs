using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QrAracIletisim.Models;

namespace QrAracIletisim.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<StickerBatch> StickerBatches => Set<StickerBatch>();
    public DbSet<Sticker> Stickers => Set<Sticker>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<PhoneVerification> PhoneVerifications => Set<PhoneVerification>();
    public DbSet<ScanEvent> ScanEvents => Set<ScanEvent>();
    public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();
    public DbSet<BlockedVisitor> BlockedVisitors => Set<BlockedVisitor>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Sticker>(e =>
        {
            e.HasIndex(s => s.Token).IsUnique();
            e.HasIndex(s => s.Status);
            e.HasOne(s => s.Batch).WithMany(b => b.Stickers).HasForeignKey(s => s.BatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Owner).WithMany().HasForeignKey(s => s.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Vehicle).WithMany().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StickerBatch>()
            .HasOne(b => b.CreatedBy).WithMany().HasForeignKey(b => b.CreatedById).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Vehicle>(e =>
        {
            e.HasIndex(v => new { v.OwnerId, v.Plate }).IsUnique();
            e.HasOne(v => v.Owner).WithMany().HasForeignKey(v => v.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PhoneVerification>(e =>
        {
            e.HasIndex(p => new { p.PhoneNumber, p.CreatedAt });
            e.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Rate limits count recent rows per sticker and per visitor, so both lookups are indexed
        builder.Entity<ContactRequest>(e =>
        {
            e.HasIndex(c => new { c.StickerId, c.CreatedAt });
            e.HasIndex(c => new { c.IpHash, c.CreatedAt });
        });

        builder.Entity<ScanEvent>().HasIndex(s => new { s.StickerId, s.ScannedAt });

        builder.Entity<BlockedVisitor>().HasIndex(b => new { b.StickerId, b.IpHash }).IsUnique();

        builder.Entity<OutboxMessage>().HasIndex(o => o.CreatedAt);
    }
}
