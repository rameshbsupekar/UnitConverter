# ADR-0013 — Partner API key authentication & programmatic access

- Status: Accepted
- Date: 2026-06-03

## Context

Partners need to submit and edit units **programmatically** via the REST API (not the web UI).
They authenticate differently from employees (no corporate identity provider), and they should
access the API without a human login session.

## Decision

Implement **API key (Bearer token) authentication** for partners:

### Partner Account Model

Create a `Partner` entity in the database:
```
Partners
├─ Id (GUID)
├─ Name (company name)
├─ ApiKey (SHA256 hash of the secret)
├─ IsActive (bool)
├─ CreatedAt (DateTime)
├─ CreatedBy (admin user id)
├─ ExpiresAt (DateTime, nullable; for key rotation)
└─ Metadata (JSON: contact email, org, etc.)
```

### Authentication Flow

1. **Admin creates a partner:**
   ```
   POST /api/admin/partners
   { "name": "Acme Corp", "contactEmail": "api@acme.com" }
   ```
   → System generates a 32-char random API key, hashes it, stores hash, returns plaintext once to admin
   → Admin shares plaintext key with partner securely

2. **Partner makes API calls:**
   ```
   POST /api/units
   Authorization: Bearer partner-key-xyz
   Content-Type: application/json
   { "symbol": "feet", "name": "foot", "category": "length", ... }
   ```

3. **Middleware authenticates:**
   - Extract Bearer token from header
   - Hash it, lookup in Partners table
   - If found and `IsActive=true` and (ExpiresAt is null or > now), create claims
   - Claims include: `sub` (partner id), `role` (Partner)

### Hashing & Security

- **Never store plaintext keys.** Use PBKDF2 or Argon2:
  ```csharp
  var hashedKey = PBKDF2.HashPassword(plaintext, saltFromPartner);
  ```
- **Rotate keys periodically:** set `ExpiresAt`, warn partner to generate new key
- **Revoke instantly:** set `IsActive = false` (no waiting for expiration)
- **Audit logs:** log every Partner create/update/revoke with admin id + timestamp

### Claims & Policies

Partner authentication produces a `ClaimsPrincipal` with claims:
```
sub: "partner-id-xyz"
role: "Partner"
org: "acme" (optional: partner organization)
```

Combined with existing Employee/Admin roles in policies:
```csharp
.AddPolicy("CanSubmitUnits", p => p.RequireRole("Employee", "Partner", "Admin"))
```

### Resource-Based Enforcement

Partners can only **edit their own** submitted units (not other partners' submissions):
```csharp
if (unit.SubmittedBy != User.GetUserId())
    return Forbid(); // 403
```

Admins can edit any unit.

## Rationale

- **API key is simpler than OAuth/OIDC** for B2B scenarios (no identity provider needed)
- **Stateless** — no sessions, perfect for REST APIs
- **Revokable** — can disable a key instantly without affecting the partner's other systems
- **Auditable** — every auth attempt can be logged (optional)
- **Industry standard** — GitHub, Stripe, Twilio use API keys

## Consequences

- **Key management burden:** partners must rotate keys; admin must track expiry
- **No inherent expiry:** must implement key rotation policy (e.g., auto-expire after 1 year)
- **Single key per partner:** if compromised, must revoke and regenerate (could support multiple keys/environments later)
- **No granular scopes:** all API keys have the same permissions (partner = can submit/edit own)

## Alternatives considered

- **OAuth2 / OIDC** — more complex; requires partners to have an identity provider
- **mTLS (mutual TLS)** — overkill for most partners; requires cert distribution
- **JWT tokens** — not stateless (must verify signature); API key + Bearer token is simpler
- **IP whitelisting** — fragile; partners' IPs may change; not alone sufficient
- **No authentication (public API)** — rejected; we need to track submissions to audit/reject
