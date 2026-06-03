# ADR-0010 — C# interceptors for cross-cutting logging & telemetry

- Status: Accepted
- Date: 2026-06-03
- **Language feature:** stable (built-in C# / .NET 9.0.2xx+ SDK); no longer experimental.

## Context

We want to log entry/exit and emit telemetry for key methods (use cases, repositories) without
polluting business logic with `ILogger` calls. C# **interceptors** (stable, built-in compiler
feature since .NET 9.0.2xx SDK) allow **compile-time call rewriting** via a source generator.

## Decision

Leverage the **built-in C# interceptor language feature** (no special packages needed):

1. Create a **source generator** (`UnitConverter.Interceptors` project) using the Roslyn
   `SemanticModel.GetInterceptableLocation()` API (stable in .NET 9+).
2. The generator scans types decorated with `[InterceptableService]` (domain/application interfaces).
3. For each interceptable method, generate an **interceptor method** with the `[InterceptsLocation(...)]`
   attribute (built-in, no NuGet).
4. Interceptor methods emit telemetry spans + metrics + structured logs, then call the original.
5. The **C# compiler automatically rewrites** call sites to the interceptor.
6. **No magic:** inspect the generated `.g.cs` files to understand what's happening.

**Example (before):**
```csharp
// No logging code here — domain stays clean
public sealed class ConvertQuantityUseCase(IUnitCatalog catalog) : IConvertQuantityUseCase
{
    public ConversionResult Handle(ConvertCommand cmd) { /* math */ }
}
```

**Example (after):**
At compile time, calls to `.Handle()` are rewritten to call a generated interceptor that
logs/telemeters, then delegates to the original. Same result, telemetry is layered on.

**Setup (.csproj):**
```xml
<!-- UnitConverter.Interceptors.csproj -->
<ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.9.0" />
</ItemGroup>

<!-- UnitConverter.Application.csproj (or any project calling interceptable methods) -->
<PropertyGroup>
    <InterceptorsNamespaces>$(InterceptorsNamespaces);UnitConverter.Interceptors.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

## Rationale

- **Built-in compiler feature** (stable in .NET 9+) — no risk of disappearing; Microsoft's official AOP answer.
- **Zero runtime overhead:** interception is compile-time; generated code is just methods.
- **Business logic stays clean:** no `ILogger` calls in use cases or repositories.
- **Centralized telemetry:** all logging rules in one generator; evolve once, regenerate.
- **Auditable:** generated `.g.cs` files are committed; diffs show exactly what logging changed.
- **No third-party AOP libraries** (e.g., Castle.DynamicProxy, PostSharp) → simpler dependency graph.

## Consequences

- **Source generator is custom tooling:** must be maintained, tested, documented.
  Trade-off: complexity for clean separation.
- **Requires explicit opt-in:** `<InterceptorsNamespaces>` must be added to projects using interceptors.
- **Debugging:** inspect generated `.g.cs` files; debugger shows generated line numbers (works fine).
- **Performance:** generated methods are JIT-compiled once (zero warm-up cost vs runtime proxying).
- **Generator versioning:** if interceptor rules change, regenerate and review the diff.

## Alternatives considered

- **Decorator pattern (DI)** — simpler, no source generator. Trade-off: manual wrapping per type; verbose.
- **Runtime proxying** (Castle.DynamicProxy, PostSharp) — magic, warm-up cost, harder to debug.
- **Manual logging in every method** — rejected; violates DRY, error-prone, scattered.
- **No telemetry** — rejected; domain needs tracing for debugging and observability.
