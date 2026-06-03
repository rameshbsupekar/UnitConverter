# ADR-0008 — Authorization & role-based access control (including Partners)

- Status: Accepted (updated)
- Date: 2026-06-03

## Context

The API now has write operations (submit/edit/approve units) restricted by **role**. We now support
**three types of users**: Employees (internal), Partners (external, via API key), and Admins
(internal, approval authority). We need a consistent, testable authorization strategy.

## Decision

Use **ASP.NET Core policy-based authorization** with role-based AND resource-based (owner) gates:

### Roles

| Role | Authentication | Can submit? | Can edit own? | Can approve? | Read scope |
|------|---|---|---|---|---|
| **Public** | None | ❌ | ❌ | ❌ | Approved units only |
| **Employee** | Username/password (Identity) | ✅ | ✅ (own only, if Pending) | ❌ | Approved + own Pending/Rejected |
| **Partner** | API key (Bearer token) | ✅ | ✅ (own only, if Pending) | ❌ | Approved + own Pending/Rejected |
| **Admin** | Username/password (Identity) | ✅ | ✅ (any) | ✅ (any) | All units (any status) |

### Workflow

```
Employee/Partner submits unit
  ↓
Status = Pending, SubmittedBy = user
  ↓
Employee/Partner can edit (name, conversion factors) while Pending
  ↓
Admin reviews, clicks Approve or Reject
  ↓
If Approved: Status = Approved, SubmittedBy unchanged, now visible in public API
If Rejected: Status = Rejected, RejectionReason set, hidden from public
```

### Policies

```csharp
services.AddAuthorizationBuilder()
    .AddPolicy("CanSubmitUnits", policy => 
        policy.RequireRole("Employee", "Partner", "Admin"))
    .AddPolicy("CanApproveUnits", policy => 
        policy.RequireRole("Admin"));
```

### Resource-Based Checks (at the handler level)

```csharp
public async Task<IActionResult> EditUnit(string unitId, EditUnitRequest request)
{
    var unit = await repository.GetByIdAsync(unitId);
    
    // Resource-based: only owner (or Admin) can edit if Pending
    if (unit.Status == UnitStatus.Pending && unit.SubmittedBy != User.GetUserId())
        return Forbid(); // 403
    
    // ... proceed with edit
}
```

### Authentication

- **Employees/Admins:** ASP.NET Core Identity (username/password or Windows auth)
- **Partners:** API key as Bearer token
  ```
  Authorization: Bearer partner-key-xyz
  ```
  - API keys are seeded in a Partners table: `PartnerId`, `ApiKey` (hashed), `IsActive`, `CreatedAt`
  - Middleware validates and extracts claims (partner ID, partner organization)
  - Partner claims appear in `User` context just like Identity claims

### Implementation

**Program.cs:**
```csharp
services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", opts => {})
    .AddCookie() // for Identity
    .AddIdentity<IdentityUser, IdentityRole>();

services.AddAuthorization()
    .AddPolicy("CanSubmitUnits", p => p.RequireRole("Employee", "Partner", "Admin"))
    .AddPolicy("CanApproveUnits", p => p.RequireRole("Admin"));
```

**ApiKeyAuthenticationHandler** (custom):
```csharp
protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
{
    var key = Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
    if (string.IsNullOrEmpty(key))
        return AuthenticateResult.NoResult();
    
    var partner = await partners.GetByApiKeyAsync(key); // hashed comparison
    if (partner == null)
        return AuthenticateResult.Fail("Invalid API key");
    
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, partner.PartnerId) };
    var ticket = new AuthenticationTicket(
        new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey")), 
        "ApiKey");
    
    return AuthenticateResult.Success(ticket);
}
```

## Rationale

- **Policy-based is testable** — mock policies, verify authorization decisions
- **Owner-based editing** prevents partners from modifying each other's submissions
- **API key for partners** is simpler than OIDC/OAuth for B2B scenarios; no identity provider required
- **Same database, different auth paths** — no multi-tenant DB complexity (v1)

## Consequences

- Must hash API keys (never store plaintext); rotate keys regularly
- Partner editing is gated to `Status == Pending` (enforced in update handler)
- API key lifetime management (expiry, revocation) needed
- Admins see all units; future: per-org admin scoping (deferred to v2)

## Alternatives considered

- **OAuth/OIDC for partners** — requires partner company to have an identity provider; adds complexity
- **No editing after submit** — less flexible; rejected
- **Partner approval rights** — unclear governance; Admins approve all for now
- **Multi-tenant database** — too early; one DB, role-based filtering suffices

