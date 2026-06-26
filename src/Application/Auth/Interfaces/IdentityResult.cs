namespace HungStore.Application.Auth.Interfaces;

public class IdentityOperationResult
{
    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public string? UserId { get; init; }

    public static IdentityOperationResult Success(string userId) => new() { Succeeded = true, UserId = userId };
    public static IdentityOperationResult Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors.ToList() };
}
