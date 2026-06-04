# End-to-end manual test

Setup: [TEST-SETUP.md](TEST-SETUP.md). Details: [catalog/conversions](TEST-CATALOG-AND-UNIT-CONVERSIONS.md) · [auth](TEST-USER-MANAGEMENT-API.md) · [unit CRUD](TEST-UNIT-DEFINITIONS-CRUD.md).

## Flow (in order)

### 1. Sign in → JWT (seeded user)

`POST http://localhost:5180/api/v1/sessions` — password **`Test@12345`**. Example: `admin@unitconverter.local`.

```powershell
$s = Invoke-RestMethod -Uri "http://localhost:5180/api/v1/sessions" -Method Post `
  -Body (@{ email="admin@unitconverter.local"; password="Test@12345" } | ConvertTo-Json) `
  -ContentType "application/json"
$h = @{ Authorization = "Bearer $($s.accessToken)" }
```

Expect **200** + `accessToken`. **Scalar:** get token on 5180; on 5173 set header `Authorization` = `Bearer <token>` for unit-definitions — see [TEST-UNIT-DEFINITIONS-CRUD.md](TEST-UNIT-DEFINITIONS-CRUD.md).

### 2. Catalog (no token)

```powershell
Invoke-RestMethod "http://localhost:5173/api/v1/catalog/categories"
Invoke-RestMethod "http://localhost:5173/api/v1/catalog/units?categoryName=length&page=1&pageSize=20"
```

Expect **200**; length units include `m`, `km`.

### 3. Convert (no token)

```powershell
$b = @{ value=1000; fromUnit="m"; toUnit="km"; category=1 } | ConvertTo-Json
Invoke-RestMethod "http://localhost:5173/api/v1/unit-conversions" -Method Post -Body $b -ContentType "application/json"
```

Expect **200**, `value`: **1**, `symbol`: **km**.

### 4. Unit definitions — list

```powershell
Invoke-RestMethod "http://localhost:5173/api/v1/unit-definitions?page=1&pageSize=10" -Headers $h
```

Expect **200**.

### 5. Create (new symbol each run)

```powershell
$c = Invoke-RestMethod "http://localhost:5173/api/v1/unit-definitions" -Method Post -Headers $h `
  -ContentType "application/json" `
  -Body '{"symbol":"e2e1","displayName":"E2E Unit","category":1,"multiplierToBase":10,"offsetToBase":0}'
$id = $c.id
```

Expect **201**, `id` > 0.

### 6. Read by id

```powershell
Invoke-RestMethod "http://localhost:5173/api/v1/unit-definitions/$id" -Headers $h
```

Expect **200**.

### 7. Update

```powershell
Invoke-RestMethod "http://localhost:5173/api/v1/unit-definitions/$id" -Method Put -Headers $h `
  -ContentType "application/json" `
  -Body '{"displayName":"E2E Updated","multiplierToBase":10,"offsetToBase":0}'
```

Expect **200**.

### 8. Delete (Admin only)

```powershell
Invoke-WebRequest "http://localhost:5173/api/v1/unit-definitions/$id" -Method Delete -Headers $h
```

Expect **204**; `GET` same id → **404**.

### 9. Optional — register & refresh

```powershell
# Register — new email + idempotencyKey (password: 12+ chars, upper/lower/digit/special)
Invoke-RestMethod "http://localhost:5180/api/v1/users" -Method Post -ContentType "application/json" `
  -Body (@{ email="e2e@example.com"; password="TestUser@12345"; firstName="E"; lastName="E"; organizationName="O"; idempotencyKey=[guid]::NewGuid().ToString() } | ConvertTo-Json)

# Refresh
Invoke-RestMethod "http://localhost:5180/api/v1/sessions/refresh" -Method Post -ContentType "application/json" `
  -Body (@{ refreshToken = $s.refreshToken } | ConvertTo-Json)
```

Expect **201** register, **200** refresh.

## Pass checklist

| Step | OK? |
|------|-----|
| Sign-in 200 + token | ☐ |
| Catalog categories + units 200 | ☐ |
| Convert 1000 m → 1 km | ☐ |
| Unit-definitions list 200 | ☐ |
| Create 201 → Update 200 → Delete 204 | ☐ |

## Tests (no running APIs)

```powershell
dotnet test
```

## If something fails

| Symptom | Fix |
|---------|-----|
| Migration error on start | `.\scripts\database\setup-local.ps1` |
| Catalog on 5180 | Use **5173** |
| 401 on unit-definitions | Repeat step 1; use `Bearer` header |
| 403 on delete | Sign in as **admin** |
| 409 on create | New `symbol` in that category |
