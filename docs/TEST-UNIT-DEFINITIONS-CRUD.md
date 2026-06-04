# Test: unit definitions CRUD (seeded users)

**Units API:** `http://localhost:5173` · **Sign-in (JWT):** `http://localhost:5180` · **Setup:** [TEST-SETUP.md](TEST-SETUP.md) · **E2E:** [TEST-E2E.md](TEST-E2E.md)

Run `.\scripts\database\setup-local.ps1` once so seeded users and catalog exist.

## Seeded users

Password for all: **`Test@12345`**

| Email | Role | Create / update | Delete | Approve |
|-------|------|-----------------|--------|---------|
| `admin@unitconverter.local` | Admin | Yes → **Approved** | Yes | Yes |
| `employee@unitconverter.local` | Employee | Yes → **PendingApproval** | No (403) | No |
| `partner@unitconverter.local` | Partner | Yes → **PendingApproval** | No (403) | No |
| `public@unitconverter.local` | Public | No (403) | No | No |

**Categories:** `1` = length, `2` = mass, `3` = temperature. Use a **new `symbol`** each create (e.g. `myu1`, `emp1`) or you get **409**.

---

## Steps (Scalar)

### 1. Start both APIs

See [TEST-SETUP.md](TEST-SETUP.md). Units Scalar: http://localhost:5173/scalar/v1 · Auth Scalar: http://localhost:5180/scalar/v1

### 2. Get JWT (port 5180)

On **User Management** Scalar, run **`POST /api/v1/sessions`**:

```json
{
  "email": "admin@unitconverter.local",
  "password": "Test@12345"
}
```

Copy **`accessToken`** from the response (JWT only, no quotes).

For employee/partner flows, sign in with `employee@unitconverter.local` or `partner@unitconverter.local` instead.

### 3. Add token on Units API (port 5173)

Open **Units** Scalar: http://localhost:5173/scalar/v1

Open **`POST /api/v1/unit-definitions`** (or any protected unit-definitions call).

**Headers** — add:

| Name | Value |
|------|--------|
| `Authorization` | `Bearer <paste accessToken here>` |

Example: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` (one space after `Bearer`).

> There is no global **Authorize** button until OpenAPI declares a Bearer scheme. Always set this header per request (or on each operation’s Headers section).

Use the **same** header for `GET`, `PUT`, and `DELETE` on `/api/v1/unit-definitions`.

### 4. Create unit

**POST** `/api/v1/unit-definitions` with header from step 3.

```json
{
  "symbol": "myu1",
  "displayName": "My Test Unit",
  "category": 1,
  "multiplierToBase": 100,
  "offsetToBase": 0
}
```

| User | Expect |
|------|--------|
| Admin | **201**, `approvalStatus`: Approved |
| Employee / Partner | **201**, `approvalStatus`: PendingApproval |

### 5. Read / update / delete

| Step | Method | Notes |
|------|--------|--------|
| List | `GET /api/v1/unit-definitions?page=1&pageSize=20` | Same `Authorization` header |
| By id | `GET /api/v1/unit-definitions/{id}` | Use `id` from create response |
| Update | `PUT /api/v1/unit-definitions/{id}` | Body: `displayName`, `multiplierToBase`, `offsetToBase` |
| Delete | `DELETE /api/v1/unit-definitions/{id}` | **Admin only** → **204** |

**Update body:**

```json
{
  "displayName": "My Test Unit Updated",
  "multiplierToBase": 100,
  "offsetToBase": 0
}
```

### 6. Employee: approve so unit appears in public catalog

1. Sign in as **employee** on 5180 → create unit on 5173 (pending).
2. Sign in as **admin** on 5180 → new token in header.
3. **PUT** `/api/v1/unit-definitions/{id}/approve` → **200**.
4. **GET** `/api/v1/catalog/units?category=1` (no auth) → symbol visible when Approved.

---

## Steps (PowerShell)

```powershell
# Sign in (5180) — change email for employee/partner
$s = Invoke-RestMethod "http://localhost:5180/api/v1/sessions" -Method Post `
  -ContentType "application/json" `
  -Body (@{ email="admin@unitconverter.local"; password="Test@12345" } | ConvertTo-Json)

$h = @{ Authorization = "Bearer $($s.accessToken)" }

# Create (5173)
$body = @{
  symbol = "myu1"
  displayName = "My Test Unit"
  category = 1
  multiplierToBase = 100
  offsetToBase = 0
} | ConvertTo-Json

$created = Invoke-RestMethod "http://localhost:5173/api/v1/unit-definitions" -Method Post `
  -Headers $h -ContentType "application/json" -Body $body

$id = $created.id

Invoke-RestMethod "http://localhost:5173/api/v1/unit-definitions/$id" -Headers $h
Invoke-RestMethod "http://localhost:5173/api/v1/unit-definitions/$id" -Method Put -Headers $h `
  -ContentType "application/json" `
  -Body '{"displayName":"Updated","multiplierToBase":100,"offsetToBase":0}'

Invoke-WebRequest "http://localhost:5173/api/v1/unit-definitions/$id" -Method Delete -Headers $h
```

---

## HTTP file

`src/UnitConverter.UnitsDefinitions.Api/UnitConverter.API.http`

1. Run **Sign in as Admin** on 5180 (`@auth`).
2. Copy `accessToken` into `@token`.
3. Run unit-definitions requests (they send `Authorization: Bearer {{token}}`).

---

## Troubleshooting

| Result | Cause |
|--------|--------|
| **401** | Missing/wrong `Authorization` header or expired token |
| **403** | Wrong role (e.g. `public@`, employee delete) |
| **409** | Duplicate `symbol` in category — use `myu2`, etc. |
| Token ignored in Scalar | Set **Headers** → `Authorization` = `Bearer <token>` on 5173 (not 5180) |

**Automated tests:** `dotnet test --filter "FullyQualifiedName~UnitDefinitionsApiTests"`
