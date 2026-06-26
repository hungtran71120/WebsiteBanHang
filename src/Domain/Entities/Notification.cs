using HungStore.Domain.Common;

namespace HungStore.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}
