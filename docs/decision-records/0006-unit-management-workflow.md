# ADR-0006 — Unit management workflow & approval

- Status: Accepted
- Date: 2026-06-03

## Context

v1 was read-only; conversions used a static catalog. We now need **employees to propose new units**,
**admins to review and approve**, and **the API to serve only approved units** publicly. This
requires a new aggregate and workflow in the domain.

## Decision

Model unit submission as a **state machine** in the domain:
- **Unit** gains a `Status` enum (`Pending`, `Approved`, `Rejected`) and `SubmittedBy` (user id).
- New aggregate: **`UnitApprovalRequest`** (what, who, when, approval decision, reason).
- Workflow invariants: only **Pending** units can be reviewed; only **Approved** units appear in
  public API; **Rejected** units are archived (soft-delete).
- **`GetApprovedUnits()`** filters the catalog on the **read path** (no change to conversion logic).
- **`RequestNewUnit(Unit, UserId)`** and **`ApproveUnit(UnitId, AdminId, Reason)`** are use cases.

## Rationale

- Separates **business logic** (approval workflow) from **persistence** (which DB we're using).
- Domain remains testable: mock the repository, test the state machine.
- **OWASP compliance:** audit trail (who approved what, when) for compliance/rollback.

## Consequences

- Schema gains `Units.Status`, `Units.SubmittedBy`, and a new `UnitApprovals` table.
- Every DB migration must handle the new fields.
- API **read** path filters on Status; **write** path is gated by authorization (separate ADR).

## Alternatives considered

- **No formal workflow** — just a boolean `IsApproved` flag. Rejected: loses audit trail and state machine rigor.
- **Approval as an external service** — overkill for v1; deferred to ADR-future.
