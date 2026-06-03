# Feature: Unit submission and management

As an employee or partner
I want to submit new units for approval
So that the conversion system can grow with new measurement types

## Scenario: Employee submits a new unit
Given I am authenticated as an employee
When I submit a new unit "foot" with conversion factor 0.3048 in category "length"
Then the unit should be created with status "Pending"
And only I should be able to see it in my submissions list
And the HTTP status should be 201 Created

## Scenario: Partner submits a new unit via API key
Given I am authenticated as a partner with a valid API key
When I POST to /api/units with a new unit "ounce" with conversion factor 0.0283495 in category "weight"
Then the unit should be created with status "Pending"
And the HTTP status should be 201 Created

## Scenario: Employee can see and edit their own pending submission
Given I am an employee with a pending unit submission
When I retrieve my submissions
Then I should see my pending unit in the list
And when I update the conversion factor
Then the unit should be updated

## Scenario: Employee cannot edit unit once approved
Given I am an employee with an approved unit submission
When I try to update the conversion factor
Then the system should return an error "Cannot edit approved unit"
And the HTTP status should be 409 Conflict

## Scenario: Partner cannot see other partners' submissions
Given I am partner-A with an API key
And partner-B has submitted a unit
When I retrieve my submissions via the API
Then I should NOT see partner-B's submission
And only my own submissions should appear

## Scenario: Unauthenticated user cannot submit units
Given I am not authenticated
When I try to POST to /api/units
Then the HTTP status should be 401 Unauthorized

## Scenario: Invalid unit data is rejected
Given I am an employee
When I submit a unit with invalid data (missing symbol, zero conversion factor)
Then the system should return validation errors
And the HTTP status should be 400 Bad Request
And no unit should be created
