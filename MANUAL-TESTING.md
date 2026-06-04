# Manual testing guide

## Part A — Setup (once)

### A1. Clone and open solution

1. Visual Studio → **Git** → **Clone Repository**
2. URL: `https://github.com/rameshbsupekar/UnitConverter.git`
3. Open **`UnitConverter.slnx`** (solution in the repo root)

### A2. Build database

1. Open **PowerShell**
2. Run:

```powershell
cd <your-repo-path>\UnitConverter
dotnet restore
dotnet build
.\scripts\database\setup-local.ps1
dotnet dev-certs https --trust
```

### A3. Start both APIs

1. Menu: **Solution → Configure Startup Projects…**
2. Select **Multiple startup projects**
3. Set **Start** for:
   - `UnitConverter.UserManagement.Api`
   - `UnitConverter.UnitsDefinitions.Api`
4. Press **F5** (each project uses the **`https`** launch profile — listed first in `launchSettings.json`).
5. Accept the dev certificate if the browser prompts you.
6. Confirm Scalar opens (two tabs, or open URLs in Part B manually).

**Credentials:** `admin@unitconverter.local` / `Test@12345`

---

## Part B — Scalar URLs (use only these)

| Test cases | Scalar |
|------------|--------|
| **Test Case API 1** (sign-in) | https://localhost:7180/scalar/v1 |
| **Test Case API 2–11** | https://localhost:7100/scalar/v1 |

---

## Part C — Test cases (11 separate API tests)

Each block below is its **own test case** — not a step in one workflow.  
You can run them in any order; **Test Case API 1** first is recommended so you have a token for **Test Case API 7–11**.

### Token (only some test cases)

| Test Case API | What you test | `Authorization: Bearer …`? |
|---------------|---------------|----------------------------|
| **1** | Sign in | No (you **get** the token here) |
| **2** | Catalog categories | No |
| **3** | Catalog units | No |
| **4** | Convert length | No |
| **5** | Convert temperature | No |
| **6** | Convert mass | No |
| **7** | List unit-definitions | **Yes** |
| **8** | Create unit-definition | **Yes** |
| **9** | Get unit-definition by id | **Yes** |
| **10** | Update unit-definition | **Yes** |
| **11** | Delete unit-definition | **Yes** |

Run **Test Case API 1** once and copy `accessToken`. For **Test Case API 7, 8, 9, 10, and 11** add:

`Authorization` = `Bearer ` + paste token (no quotes around the token).

### How to run a test case in Scalar

- Open the **Scalar URL** for that test case (Part B).
- Find the **operation** (e.g. `POST /api/v1/sessions`).
- Expand it → set **Headers** / **Body** / **Query** / **Path** as the test case describes.
- Click **Send** → check **Status** matches **Expect**.

---

### Test Case API 1 — Sign in (get token)

| | |
|--|--|
| **Scalar** | https://localhost:7180/scalar/v1 |
| **Operation** | `POST /api/v1/sessions` |
| **Headers** | `Content-Type` = `application/json` |
| **Body** | See below |
| **Expect** | Status **200**. Copy **`accessToken`** from the JSON body (needed for Test Case API 7–11). |

```json
{
  "email": "admin@unitconverter.local",
  "password": "Test@12345"
}
```

---

### Test Case API 2 — List categories

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `GET /api/v1/catalog/categories` |
| **Headers** | None |
| **Expect** | Status **200**. Response lists **3** categories. |

---

### Test Case API 3 — List length units

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `GET /api/v1/catalog/units` |
| **Headers** | None |
| **Query** | `categoryName` = `length`, `page` = `1`, `pageSize` = `20` |
| **Expect** | Status **200**. Items include symbols **`m`** and **`km`**. |

---

### Test Case API 4 — Convert length (1000 m → km)

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `POST /api/v1/unit-conversions` |
| **Headers** | `Content-Type` = `application/json` |
| **Body** | See below |
| **Expect** | Status **200**. Response JSON: **`value`** = **1**, **`symbol`** = **`km`**. |

```json
{
  "value": 1000,
  "fromUnit": "m",
  "toUnit": "km",
  "category": 1
}
```

---

### Test Case API 5 — Convert temperature (0 °C → °F)

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `POST /api/v1/unit-conversions` |
| **Headers** | `Content-Type` = `application/json` |
| **Body** | See below |
| **Expect** | Status **200**. **`value`** is about **32**. |

```json
{
  "value": 0,
  "fromUnit": "c",
  "toUnit": "f",
  "category": 3
}
```

---

### Test Case API 6 — Convert mass (1 kg → lb)

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `POST /api/v1/unit-conversions` |
| **Headers** | `Content-Type` = `application/json` |
| **Body** | See below |
| **Expect** | Status **200**. |

```json
{
  "value": 1,
  "fromUnit": "kg",
  "toUnit": "lb",
  "category": 2
}
```

---

### Test Case API 7 — List unit definitions

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `GET /api/v1/unit-definitions` |
| **Headers** | `Authorization` = `Bearer <accessToken from Test Case API 1>` |
| **Query** | `page` = `1`, `pageSize` = `50` |
| **Expect** | Status **200**. |

---

### Test Case API 8 — Create unit definition

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `POST /api/v1/unit-definitions` |
| **Headers** | `Authorization` = `Bearer <token>` · `Content-Type` = `application/json` |
| **Body** | See below (change **`symbol`** if you already ran this case with `myu1`) |
| **Expect** | Status **201**. Note **`id`** in the response (used in Test Case API 9–11). |

```json
{
  "symbol": "myu1",
  "displayName": "My Unit",
  "category": 1,
  "multiplierToBase": 100,
  "offsetToBase": 0
}
```

---

### Test Case API 9 — Get unit by id

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `GET /api/v1/unit-definitions/{id}` |
| **Headers** | `Authorization` = `Bearer <token>` |
| **Path** | `id` = number from **Test Case API 8** response (e.g. `42`) |
| **Expect** | Status **200**. Same **`symbol`** as Test Case API 8. |

---

### Test Case API 10 — Update unit definition

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `PUT /api/v1/unit-definitions/{id}` |
| **Headers** | `Authorization` = `Bearer <token>` · `Content-Type` = `application/json` |
| **Path** | `id` = same as **Test Case API 8** |
| **Body** | See below |
| **Expect** | Status **200**. **`displayName`** is **My Unit Updated**. |

```json
{
  "displayName": "My Unit Updated",
  "multiplierToBase": 100,
  "offsetToBase": 0
}
```

---

### Test Case API 11 — Delete unit definition (Admin)

| | |
|--|--|
| **Scalar** | https://localhost:7100/scalar/v1 |
| **Operation** | `DELETE /api/v1/unit-definitions/{id}` |
| **Headers** | `Authorization` = `Bearer <token>` |
| **Path** | `id` = same as **Test Case API 8** |
| **Expect** | Status **204** (empty body). |

**Optional:** run **Test Case API 9** again with the same `id` → status **404**.

---

## Part D — Checklist

| Test Case API | Done |
|---------------|------|
| A Setup + F5 | ☐ |
| 1 Sign-in, saved token | ☐ |
| 2 Categories | ☐ |
| 3 Catalog units | ☐ |
| 4 Length conversion | ☐ |
| 5 Temperature conversion | ☐ |
| 6 Mass conversion | ☐ |
| 7 List unit-definitions | ☐ |
| 8 Create | ☐ |
| 9 Get by id | ☐ |
| 10 Update | ☐ |
| 11 Delete | ☐ |

**Suggested order for first full run:** 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11

---

## Part E — If something fails

| Symptom | What to do |
|---------|------------|
| Browser certificate warning | Run `dotnet dev-certs https --trust`, restart APIs |
| API won’t start / DB error | Run `.\scripts\database\setup-local.ps1` again |
| **401** on Test Case API 7–11 | Re-run **Test Case API 1**; header must be `Bearer ` + full token |
| **400** on POST/PUT | Add `Content-Type: application/json` and valid JSON body |
| **403** on Test Case API 11 | **Test Case API 1** must use **`admin@unitconverter.local`** |
| **409** on Test Case API 8 | Change **`symbol`** in the body (e.g. `myu2`) |
| Scalar page not found | APIs not running — press **F5** again |
| Wrong solution file | Open **`UnitConverter.slnx`**, not a parent folder |

**Automated tests:** stop debugging, then `dotnet test`
