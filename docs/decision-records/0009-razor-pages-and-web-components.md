# ADR-0009 — Web UI: Razor Pages + modern web components

- Status: Accepted
- Date: 2026-06-03

## Context

We need an **internal web app** for employees to submit units and admins to review them.
Options: Razor Pages alone (traditional), Blazor (C# in browser), SPA (React/Vue/Angular),
or **Razor Pages + web components** (modern, lightweight, no separate build).

## Decision

Use **Razor Pages + web components (Lit or Shoelace)**:

- **Razor Pages** for layout, form binding, server-side rendering, and authorization checks.
- **Web components** (custom HTML elements) for interactive, reusable UI:
  - `<unit-form>` — submit new unit (takes API endpoint, emits FormData)
  - `<approval-queue>` — admin dashboard of pending units (fetches, renders, handles approve/reject)
  - `<unit-card>` — reusable card for displaying a unit
- **Framework:** **Lit** (lightweight, 5KB, zero dependencies) or **Shoelace** (Lit-based + styled components).
- **State management:** Keep it simple — component-local state + `fetch()` to the API (no Redux/Vuex).
- **No npm build required:** serve .js files from `wwwroot/components/` directly (web components work in the browser).

## Rationale

- **Lighter than Blazor/Angular:** no WASM payload, no separate npm build pipeline.
- **Web components are standardized:** no vendor lock-in; works in any framework.
- **Razor Pages + web components is a pro pattern:** industry standard (Microsoft, Shopify, etc.).
- **Faster iteration:** change a .js file, refresh browser; no dotnet build loop.
- **SEO-friendly:** server-rendered HTML for public pages; components enhance.

## Consequences

- Team must learn web components (small, quick learning curve if they know HTML/JS).
- Build / package setup is simpler: just HTML, CSS, JS in `wwwroot/`.
- No TypeScript compilation step (can add later if desired).
- Shoelace has dependencies (iconography); Lit is minimal.

## Alternatives considered

- **Blazor** — overkill for forms; adds WASM bloat, build complexity.
- **React/Vue/Angular** — separate npm build, bundle, CI complexity; overkill for a few forms.
- **Razor Pages alone** — works, but no interactivity (every click is a full round-trip).
- **htmx** — solid alternative; web components have better encapsulation and reusability.
