using UnitConverter.UserManagement.Contracts.Requests;

namespace UnitConverter.UserManagement.Api.Tests.TestHelpers;

internal static class RegisterUserRequestFactory
{
    public static RegisterUserRequest Create(
        string email = "john@example.com",
        string password = "ValidPass123!@#",
        string firstName = "John",
        string lastName = "Doe",
        string organizationName = "ACME Corp",
        string? idempotencyKey = null,
        bool generateIdempotencyKeyIfMissing = true)
    {
        var key = ResolveIdempotencyKey(idempotencyKey, generateIdempotencyKeyIfMissing);

        return new RegisterUserRequest(
            Email: email,
            Password: password,
            FirstName: firstName,
            LastName: lastName,
            OrganizationName: organizationName,
            IdempotencyKey: key);
    }

    private static string ResolveIdempotencyKey(string? idempotencyKey, bool generateIfMissing)
    {
        if (idempotencyKey is not null)
        {
            return idempotencyKey;
        }

        return generateIfMissing ? Guid.NewGuid().ToString() : string.Empty;
    }
}
