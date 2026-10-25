using System.ComponentModel.DataAnnotations;

namespace QrAracIletisim.Models;

public enum OutboxChannel
{
    Sms,
    WhatsApp,
    Call
}

// What a provider sent (or, with the fake provider, would have sent); shown on the admin outbox page
public class OutboxMessage
{
    public int Id { get; set; }

    public OutboxChannel Channel { get; set; }

    [Required]
    [StringLength(20)]
    public string To { get; set; } = "";

    [Required]
    [StringLength(500)]
    public string Body { get; set; } = "";

    [Required]
    [StringLength(20)]
    public string Provider { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
