# Test: User Management API

**Host:** `http://localhost:5180` · **Setup:** [TEST-SETUP.md](TEST-SETUP.md) · **Full E2E:** [TEST-E2E.md](TEST-E2E.md)

Seeded users (password `Test@12345`): `admin@…`, `employee@…`, `partner@…`, `public@…` @ `unitconverter.local`

## Sign in

`POST /api/v1/sessions`

```json
{ "email": "admin@unitconverter.local", "password": "Test@12345" }
```

```powershell
Invoke-RestMethod "http://localhost:5180/api/v1/sessions" -Method Post `
  -Body (@{ email="admin@unitconverter.local"; password="Test@12345" } | ConvertTo-Json) `
  -ContentType "application/json"
```

| Result | Status |
|--------|--------|
| Valid login | 200 + `accessToken`, `refreshToken` |
| Wrong password | 401 |

Use token on **5173**: `Authorization: Bearer {accessToken}`

## Register

`POST /api/v1/users` — all fields required; password 12+ chars with upper/lower/digit/special.

```json
{
  "email": "you@example.com",
  "password": "TestUser@12345",
  "firstName": "A",
  "lastName": "B",
  "organizationName": "Org",
  "idempotencyKey": "00000000-0000-0000-0000-000000000001"
}
```

| Result | Status |
|--------|--------|
| New user | 201 |
| Duplicate email | 409 |
| Missing fields | 400 |

## Refresh

`POST /api/v1/sessions/refresh` · body: `{ "refreshToken": "…" }` → **200**

## Sanity

`GET http://localhost:5180/health` → **200**  
`GET http://localhost:5180/api/v1/catalog/categories` → **404** (catalog is on 5173)

**HTTP file:** `src/UnitConverter.UserManagement.Api/UnitConverter.UserManagement.API.http`  
**Scalar:** http://localhost:5180/scalar/v1
