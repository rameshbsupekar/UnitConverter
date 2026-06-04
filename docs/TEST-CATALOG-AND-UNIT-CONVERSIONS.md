# Test: catalog & conversions

**Host:** `http://localhost:5173` · **Auth:** none · **Setup:** [TEST-SETUP.md](TEST-SETUP.md) · **Full E2E:** [TEST-E2E.md](TEST-E2E.md)

## Catalog

| Step | Request | Expect |
|------|---------|--------|
| 1 | `GET /api/v1/catalog/categories` | 200, 3 categories |
| 2 | `GET /api/v1/catalog/units?category=1&page=1&pageSize=20` | 200, items with `m`, `km` |
| 3 | `GET /api/v1/catalog/units?categoryName=length&page=1&pageSize=20` | 200, same as step 2 |

```powershell
Invoke-RestMethod "http://localhost:5173/api/v1/catalog/categories"
Invoke-RestMethod "http://localhost:5173/api/v1/catalog/units?categoryName=length&page=1&pageSize=20"
```

## Conversions

`POST /api/v1/unit-conversions` · `Content-Type: application/json`

| Test | Body | Expect |
|------|------|--------|
| Length | `{"value":1000,"fromUnit":"m","toUnit":"km","category":1}` | 200, `value`: 1 |
| Temperature | `{"value":0,"fromUnit":"c","toUnit":"f","category":3}` | 200, `value` ≈ 32 |
| Bad unit | `{"value":1,"fromUnit":"xxx","toUnit":"m","category":1}` | 400 |

```powershell
$b = @{ value=1000; fromUnit="m"; toUnit="km"; category=1 } | ConvertTo-Json
Invoke-RestMethod "http://localhost:5173/api/v1/unit-conversions" -Method Post -Body $b -ContentType "application/json"
```

**HTTP file:** `src/UnitConverter.UnitsDefinitions.Api/UnitConverter.API.http`
