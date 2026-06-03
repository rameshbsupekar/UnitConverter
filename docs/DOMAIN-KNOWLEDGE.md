# Domain Knowledge: Units & Unit Conversion

**Purpose:** Understand the domain deeply so we design the right interfaces and data models.

---

## 1. What is a Unit?

A **unit** is a standardized quantity used to measure physical quantities.

### Examples
- **Length:** meter (m), kilometer (km), centimeter (cm), inch (in), foot (ft), mile (mi)
- **Mass/Weight:** kilogram (kg), gram (g), milligram (mg), pound (lb), ounce (oz), tonne (t)
- **Temperature:** Kelvin (K), Celsius (°C), Fahrenheit (°F)
- **Volume:** liter (L), milliliter (mL), gallon (gal), cubic meter (m³)
- **Time:** second (s), minute (min), hour (h), day (d)
- **Speed:** meter per second (m/s), kilometer per hour (km/h), mile per hour (mph)

### Unit Components

Each unit has:
1. **Symbol** — short, unique identifier (e.g., "m", "kg", "°C")
2. **Name** — human-readable (e.g., "metre", "kilogram", "Celsius")
3. **Category** — what it measures (e.g., Length, Weight, Temperature)
4. **Base unit** — the reference unit in the category (e.g., metre for length, kilogram for weight, Kelvin for temperature)
5. **Conversion factor** — how much to multiply/divide to convert to/from the base

---

## 2. What is Unit Conversion?

**Unit conversion** is translating a quantity from one unit to another within the same category.

### The Math: Three Types

#### Type 1: Linear Conversion (Most units)

**Formula:** `value_in_target = (value_in_source * scale_from_source) / scale_to_target`

Or equivalently: convert to base unit, then to target.

**Example: Length (base = metre)**
- 1 km = 1000 m (scale factor: 1000)
- 1 m = 1 m (scale factor: 1)
- 1 cm = 0.01 m (scale factor: 0.01)
- 1 inch = 0.0254 m (scale factor: 0.0254)

**Conversion: 1000 m to km**
```
inBase = 1000 * 1 = 1000 m
result = 1000 / 1000 = 1 km ✅
```

**Conversion: 5 feet to metres**
```
1 foot = 0.3048 m (scale factor)
inBase = 5 * 0.3048 = 1.524 m
result = 1.524 / 1 = 1.524 m ✅
```

#### Type 2: Affine Conversion (Temperature)

**Formula:** `value_in_base = value_in_source * scale + offset`

Temperature is special because 0°C, 0°F, and 0 K are not the same point. This requires an **offset**.

**Example: Temperature (base = Kelvin)**

- **Celsius to Kelvin:** K = °C + 273.15
  - Scale: 1, Offset: 273.15
- **Fahrenheit to Kelvin:** K = (°F - 32) * 5/9 + 273.15
  - Scale: 5/9, Offset: 255.372... (pre-computed)
- **Kelvin to Kelvin:** K = K * 1 + 0
  - Scale: 1, Offset: 0

**Conversion: 0°C to °F**
```
inBase = 0 * 1 + 273.15 = 273.15 K
°F = (273.15 - 255.372) / (5/9) = 32 °F ✅
```

**Conversion: 100°C to Kelvin**
```
inBase = 100 * 1 + 273.15 = 373.15 K ✅
```

#### Type 3: Complex/Formula Conversion (Rare)

Some conversions don't fit linear or affine (e.g., pressure in different systems). Store custom logic.

For now: **only support linear + affine** (covers 99% of common units).

---

## 3. Unit Categories (Supported in v1)

### Category 1: Length

| Unit | Symbol | Scale to Base (metre) | Example |
|------|--------|----------------------|---------|
| Metre | m | 1 | SI base unit |
| Kilometre | km | 1000 | Distance between cities |
| Centimetre | cm | 0.01 | Ruler measurement |
| Millimetre | mm | 0.001 | Precision work |
| Inch | in | 0.0254 | Screen sizes, tools |
| Foot | ft | 0.3048 | Height, short distances |
| Yard | yd | 0.9144 | Field measurements |
| Mile | mi | 1609.344 | Long distances |
| Nautical Mile | nmi | 1852 | Maritime/aviation |

### Category 2: Weight / Mass

| Unit | Symbol | Scale to Base (kg) | Example |
|------|--------|-------------------|---------|
| Kilogram | kg | 1 | SI base unit |
| Gram | g | 0.001 | Food packaging |
| Milligram | mg | 0.000001 | Medicine dosage |
| Tonne (metric) | t | 1000 | Heavy cargo |
| Pound | lb | 0.45359237 | Body weight (US) |
| Ounce | oz | 0.0283495 | Cooking, postal |
| Stone | st | 6.35029 | Body weight (UK) |

### Category 3: Temperature

| Unit | Symbol | Scale | Offset | Example |
|------|--------|-------|--------|---------|
| Kelvin | K | 1 | 0 | SI base, absolute zero |
| Celsius | °C | 1 | 273.15 | Weather, science |
| Fahrenheit | °F | 5/9 | 255.372... | US weather |

**Why different scales?** Because the increments are different (°F increments are smaller than °C).

### Future Categories (v2+)

- **Volume:** litre, millilitre, gallon, cubic metre
- **Time:** second, minute, hour, day, week
- **Speed:** m/s, km/h, mph, knot
- **Energy:** joule, calorie, watt-hour
- **Pressure:** pascal, bar, atmosphere
- **Area:** square metre, square kilometre, hectare, acre

---

## 4. Input Specifications

### What can be INPUT?

#### User Input (POST /api/conversions, web form)

```json
{
  "value": 1000,              // decimal number
  "fromUnit": "m",            // unit symbol (lowercase, trimmed)
  "toUnit": "km"              // unit symbol (lowercase, trimmed)
}
```

#### Requirements for `value`
- **Type:** Decimal (not integer) for precision (e.g., 1.5 metres)
- **Range:** Must be a finite number (no NaN, no Infinity)
- **Precision:** Support up to 28 significant digits (C# `decimal` limit)
- **Sign:** Can be positive or negative (e.g., -5 degrees)
- **Example valid:** 1000, 1.5, -273.15, 0.0001, 999999999999999999999999999

#### Requirements for `fromUnit`, `toUnit`
- **Type:** String
- **Format:** lowercase, unique identifier (e.g., "m", "km", "celsius", "fahrenheit")
- **Length:** 1–20 characters (reasonable limit)
- **Characters:** alphanumeric + "." (e.g., "°" → escape as "deg" or use "celsius")
- **Whitespace:** trim leading/trailing spaces
- **Case:** normalize to lowercase (user can submit "M" → becomes "m")
- **Example valid:** "m", "km", "lb", "oz", "celsius", "fahrenheit"

#### Requirements for submitting a NEW unit (POST /api/units)

```json
{
  "symbol": "foot",           // unique in category
  "name": "Foot",             // human-readable
  "category": "length",       // existing category name
  "scaleToBase": 0.3048,      // how to convert to base unit
  "offsetToBase": 0           // for affine (temperature only)
}
```

**Validation rules:**
- `symbol`: 1–20 lowercase alphanumeric, unique within category (unique symbol)
- `name`: 1–100 characters, non-empty
- `category`: must exist (e.g., "length", "weight", "temperature")
- `scaleToBase`: must be positive (> 0), not zero
- `offsetToBase`: optional (defaults to 0), must be finite
- Both must be `decimal` for precision

---

## 5. Output Specifications

### What is OUTPUT?

#### Successful Conversion Response (200 OK)

```json
{
  "value": 1000,
  "fromUnit": "m",
  "toUnit": "km",
  "convertedValue": 1.0
}
```

**Requirements:**
- `convertedValue`: `decimal` (same precision as input)
- Round-trip accuracy: convert A→B→A should return original within tolerance (0.001%)

#### List Units Response (200 OK)

```json
[
  {
    "symbol": "m",
    "name": "metre",
    "category": "length",
    "status": "Approved"
  },
  {
    "symbol": "km",
    "name": "kilometre",
    "category": "length",
    "status": "Approved"
  }
]
```

#### Error Responses

**400 Bad Request** (validation failed):
```json
{
  "type": "https://example.com/validation-error",
  "title": "Validation Failed",
  "status": 400,
  "detail": "Field 'value' must be a valid decimal number.",
  "errors": {
    "value": ["Must be a finite decimal"]
  },
  "traceId": "00-abc123-def456-00"
}
```

**404 Not Found** (unknown unit):
```json
{
  "type": "https://example.com/unknown-unit",
  "title": "Unknown Unit",
  "status": 404,
  "detail": "Unit 'bazinga' not found.",
  "traceId": "00-abc123-def456-00"
}
```

**422 Unprocessable Entity** (incompatible units):
```json
{
  "type": "https://example.com/incompatible-units",
  "title": "Incompatible Units",
  "status": 422,
  "detail": "Cannot convert 'm' (length) to 'kg' (weight).",
  "traceId": "00-abc123-def456-00"
}
```

---

## 6. Data Validation Rules

### At the API Boundary (Input Validation)

**For conversions:**

| Field | Rule | Example Invalid | Action |
|-------|------|-----------------|--------|
| `value` | Must be finite decimal | NaN, Infinity, "abc" | 400 Bad Request |
| `fromUnit` | Must be known symbol | "bazinga", "" | 404 Not Found |
| `toUnit` | Must be known symbol | "bazinga", "" | 404 Not Found |
| `fromUnit` ≠ `toUnit` (same category) | m to km is OK; m to kg is NOT | m to kg | 422 Unprocessable |

**For unit submission:**

| Field | Rule | Example Invalid | Action |
|-------|------|-----------------|--------|
| `symbol` | Unique in category + valid format | "M" (uppercase), "" | 400 Bad Request |
| `name` | Non-empty, ≤ 100 chars | "", "x"*101 | 400 Bad Request |
| `category` | Must exist | "mass" (vs "weight") | 400 Bad Request |
| `scaleToBase` | Positive, not zero | 0, -1, NaN | 400 Bad Request |
| `offsetToBase` | Finite, optional | NaN, Infinity | 400 Bad Request |
| `symbol` duplicate | Not already in system | "m" (already exists) | 409 Conflict |

### In the Domain (Invariants)

**Unit invariants:**
- A unit **must** have a symbol, name, category, and status
- A unit's `status` is one of: Pending, Approved, Rejected
- A unit **cannot** be edited after it's Approved (except by admin)
- An Approved unit **must** have `approvedBy` and `approvedAt` set

**Conversion invariants:**
- You can only convert **within** a category (length to length, weight to weight)
- You can only convert **approved** units (Pending/Rejected hidden from public)
- The result **must** be finite and not exceed `decimal` precision

---

## 7. Precision & Rounding

### The Problem

```
1 °C → °F:
  Mathematically: (1 * 9/5) + 32 = 33.8
  In decimal: 33.8 (exact, no loss)

0 °C → °F → °C:
  0 °C → 32 °F (exact)
  32 °F → ((32 - 32) * 5/9) + 0 = 0 °C (exact round-trip) ✅

But edge cases:
  1/3 °C:
    In decimal: 0.33333... (repeats infinitely)
    Stored in decimal: 0.3333333333333333333333333333 (28 digits max)
    32.99999... °F
    Back: 0.33333... °C (might lose precision in last digit)
```

### Solution

1. **Use `decimal` type** (C# native, 128-bit, 28 significant digits)
2. **Accept ±0.001% tolerance** for round-trip tests (common in unit conversion libraries)
3. **Document precision:** "Conversions maintain 99.9% accuracy for round-trip (A→B→A)"
4. **Example test:**
   ```csharp
   [TestMethod]
   public void RoundTrip_Precision()
   {
       var original = 98.6m;  // Fahrenheit
       var toCelsius = Convert(original, "fahrenheit", "celsius");
       var backToF = Convert(toCelsius, "celsius", "fahrenheit");
       
       // Within 0.01 of original (0.01% tolerance)
       Assert.IsTrue(Math.Abs(original - backToF) < 0.01m);
   }
   ```

---

## 8. Design Implications (For LLD)

### Domain Layer

```csharp
// Unit: immutable value object with invariants
public sealed record Unit(
    string Symbol,           // "m", "km", "celsius"
    string Name,             // "metre", "celsius"
    UnitCategory Category,   // Length, Weight, Temperature
    UnitStatus Status,       // Pending, Approved, Rejected
    decimal ScaleToBase,     // 1, 1000, 0.0254, 1 (for Kelvin)
    decimal OffsetToBase = 0 // 0 (linear), 273.15 (Celsius), 255.372 (Fahrenheit)
);

// Conversion rule: strategy pattern
public interface IConversionRule
{
    bool CanConvert(Unit from, Unit to);
    decimal Convert(decimal value, Unit from, Unit to);
    // Throws: IncompatibleUnitsException, UnknownUnitException
}

// Quantities
public sealed record Quantity(decimal Value, Unit Unit);
public sealed record ConversionResult(decimal Value, Unit Unit);
```

### Application Layer (Ports)

```csharp
// Query: public API sees approved units only
public interface IUnitQueryRepository
{
    Task<Unit?> GetBySymbolAsync(string symbol);           // Get unit by symbol
    Task<List<Unit>> ListApprovedAsync();                  // List all approved
    Task<List<Unit>> ListApprovedByCategoryAsync(string);  // List by category
}

// Command: submit, edit, approve
public interface IUnitCommandRepository
{
    Task CreateAsync(Unit unit);                           // Submit new (Pending)
    Task UpdateAsync(Unit unit);                           // Edit (if Pending)
    Task ApproveAsync(string unitId, string approvedBy);   // Approve
    Task SaveAsync();                                      // Persist
}
```

### API Layer (Contracts)

```csharp
public sealed record ConvertRequest(decimal Value, string FromUnit, string ToUnit);
public sealed record ConvertResponse(decimal Value, string FromUnit, string ToUnit, decimal ConvertedValue);

public sealed record SubmitUnitRequest(
    string Symbol, string Name, string Category,
    decimal ScaleToBase, decimal OffsetToBase = 0
);
```

### Validation Layer (At Boundary)

```csharp
// Input validation (before domain)
public class ConvertRequestValidator
{
    public ValidationResult Validate(ConvertRequest req)
    {
        if (!IsFiniteDecimal(req.Value))
            return Fail("value must be a finite decimal");
        if (string.IsNullOrWhiteSpace(req.FromUnit))
            return Fail("fromUnit required");
        if (string.IsNullOrWhiteSpace(req.ToUnit))
            return Fail("toUnit required");
        return Success();
    }
}

// Domain validation (after mapping)
// Happens in IConversionRule.Convert() — throws if units incompatible
```

---

## 9. Example Scenarios

### Scenario 1: Convert 1000 meters to kilometers

**Input:**
```json
{ "value": 1000, "fromUnit": "m", "toUnit": "km" }
```

**Processing:**
1. Validate: value (1000 ✓), fromUnit ("m" ✓), toUnit ("km" ✓)
2. Lookup: Unit("m", "metre", Length, Approved, scale=1, offset=0)
3. Lookup: Unit("km", "kilometre", Length, Approved, scale=1000, offset=0)
4. Check compatibility: Length == Length ✓
5. Apply rule: inBase = 1000 * 1 = 1000 m; result = 1000 / 1000 = 1 km
6. Return: `ConversionResponse(1000, "m", "km", 1)`

**Output:**
```json
{ "value": 1000, "fromUnit": "m", "toUnit": "km", "convertedValue": 1 }
```

### Scenario 2: Convert 0 Celsius to Fahrenheit

**Input:**
```json
{ "value": 0, "fromUnit": "celsius", "toUnit": "fahrenheit" }
```

**Processing:**
1. Validate: all inputs ✓
2. Lookup: Unit("celsius", "Celsius", Temperature, Approved, scale=1, offset=273.15)
3. Lookup: Unit("fahrenheit", "Fahrenheit", Temperature, Approved, scale=5/9, offset=255.372...)
4. Check compatibility: Temperature == Temperature ✓
5. Apply **affine** rule:
   - inBase = 0 * 1 + 273.15 = 273.15 K
   - result = (273.15 - 255.372...) / (5/9) = 32.0 °F
6. Return: `ConversionResponse(0, "celsius", "fahrenheit", 32.0)`

**Output:**
```json
{ "value": 0, "fromUnit": "celsius", "toUnit": "fahrenheit", "convertedValue": 32.0 }
```

### Scenario 3: Convert meters to kilograms (ERROR)

**Input:**
```json
{ "value": 10, "fromUnit": "m", "toUnit": "kg" }
```

**Processing:**
1. Validate: all inputs ✓
2. Lookup: Unit("m", ..., **Length**, ...)
3. Lookup: Unit("kg", ..., **Weight**, ...)
4. Check compatibility: Length ≠ Weight ✗
5. Throw: `IncompatibleUnitsException("Cannot convert 'm' (length) to 'kg' (weight)")`

**Output (422 Unprocessable Entity):**
```json
{
  "type": "https://example.com/incompatible-units",
  "title": "Incompatible Units",
  "status": 422,
  "detail": "Cannot convert 'm' (length) to 'kg' (weight).",
  "traceId": "00-abc123-def456-00"
}
```

---

## Summary

| Aspect | Rule | Example |
|--------|------|---------|
| **Unit** | Standardized quantity with symbol, name, category, scale, offset | "metre" (m), "kilogram" (kg), "Celsius" (°C) |
| **Category** | Groups compatible units | Length (m, km, ft, mi), Weight (kg, lb, oz), Temperature (K, °C, °F) |
| **Base Unit** | Reference unit in category | Metre (length), Kilogram (weight), Kelvin (temperature) |
| **Scale** | Multiplication factor to base | km=1000, cm=0.01, lb=0.45359237 |
| **Offset** | Addition factor (affine, temperature only) | Celsius=+273.15, Fahrenheit=+255.372 |
| **Conversion** | Transform value from source to target unit | 1000m ÷ 1000 = 1km |
| **Validation** | Check value, symbol, category, compatibility | value ≠ NaN; symbol known; categories match |
| **Error** | Reject invalid input or incompatible units | 400 (bad value), 404 (unknown unit), 422 (incompatible) |

