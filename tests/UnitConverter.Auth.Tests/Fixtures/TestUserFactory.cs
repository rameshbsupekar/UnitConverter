using System;
using System.Collections.Generic;
using System.Linq;
using UnitConverter.Auth.Core.Domain.Entities;
using UnitConverter.Auth.Core.Domain.ValueObjects;

namespace UnitConverter.Auth.Tests.Fixtures;

/// <summary>
/// Factory for creating test User objects with sensible defaults.
/// Follows DRY principle by centralizing test data creation.
/// </summary>
public static class TestUserFactory
{
    /// <summary>
    /// Creates a valid test user with default or custom values.
    /// </summary>
    /// <param name="userId">User ID (default: 1)</param>
    /// <param name="email">Email address (default: user@example.com)</param>
    /// <param name="firstName">First name (default: John)</param>
    /// <param name="lastName">Last name (default: Doe)</param>
    /// <param name="organizationName">Organization name (default: ACME Corp)</param>
    /// <param name="passwordHash">Password hash (default: hashed_password_1234567890)</param>
    /// <returns>A valid User instance with specified values</returns>
    public static User CreateValidUser(
        long userId = 1,
        string email = "user@example.com",
        string firstName = "John",
        string lastName = "Doe",
        string organizationName = "ACME Corp",
        string passwordHash = "hashed_password_1234567890")
    {
        return User.Create(
            userId,
            email,
            firstName,
            lastName,
            organizationName,
            passwordHash
        );
    }

    /// <summary>
    /// Creates multiple valid test users with incremented emails and IDs.
    /// Useful for testing batch operations.
    /// </summary>
    /// <param name="count">Number of users to create</param>
    /// <returns>List of User instances with unique IDs and emails</returns>
    public static List<User> CreateMultipleUsers(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateValidUser(
                userId: i,
                email: $"user{i}@example.com",
                firstName: $"User{i}",
                lastName: "Test"))
            .ToList();
    }

    /// <summary>
    /// Creates a test user with long/edge-case values for edge-case testing.
    /// </summary>
    public static User CreateUserWithLongEmail()
    {
        var longEmail = "very.long.email.address.for.testing.purposes@subdomain.example.co.uk";
        return CreateValidUser(email: longEmail);
    }

    /// <summary>
    /// Creates a test user with unicode characters in organization name.
    /// </summary>
    public static User CreateUserWithUnicodeOrganization()
    {
        return CreateValidUser(organizationName: "Acme 公司 Société");
    }
}
