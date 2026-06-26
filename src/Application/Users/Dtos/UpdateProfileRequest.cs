namespace HungStore.Application.Users.Dtos;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
}
