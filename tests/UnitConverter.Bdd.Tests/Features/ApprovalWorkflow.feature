# Feature: Unit approval workflow (Admin dashboard)

As an admin
I want to review and approve/reject unit submissions
So that only validated units are available to the public

## Scenario: Admin sees all pending unit submissions
Given I am an admin user
When I access the approval queue
Then I should see all pending unit submissions
And each submission should show the submitter, submission date, and conversion details

## Scenario: Admin can approve a pending unit
Given I am an admin
And there is a pending unit "mile" submitted by an employee
When I click approve for that unit
Then the unit status should change to "Approved"
And the unit should now be visible in the public API
And the HTTP status should be 200 OK

## Scenario: Admin can reject a pending unit with reason
Given I am an admin
And there is a pending unit submitted
When I click reject with reason "Invalid conversion factor"
Then the unit status should change to "Rejected"
And the rejection reason should be stored
And the unit should NOT appear in the public API

## Scenario: Approved units are visible in the public API
Given I am an admin and have approved a unit
When I access the public conversion API as an unauthenticated user
Then the approved unit should appear in GET /api/units
And conversions using the approved unit should work

## Scenario: Rejected units are hidden from public and employees
Given a unit has been rejected
When an employee tries to see it in their submission list
Then it should still appear (so they know it was rejected) with the reason
When an unauthenticated user accesses the public API
Then the rejected unit should NOT appear

## Scenario: Employee is notified of approval/rejection
Given a unit I submitted is approved
When the admin approves it
Then I should see the unit with status "Approved" in my list
And similarly for rejection

## Scenario: Admin cannot see unapproved units in public API
Given I am an admin
When I use the public API endpoint GET /api/units (without special headers)
Then I should only see approved units (same as any user)
But when I use the admin endpoint GET /api/admin/units
Then I should see all units including Pending and Rejected
