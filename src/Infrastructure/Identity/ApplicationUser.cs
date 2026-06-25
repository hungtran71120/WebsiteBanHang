using Microsoft.AspNetCore.Identity;

namespace ShopeeClone.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
}
