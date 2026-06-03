# Feature: Authorization and role-based access

As the system
I want to enforce role-based access control
So that only authorized users can perform sensitive operations

## Scenario: Public users can read approved units
Given I am an unauthenticated user
When I access GET /api/units
Then I should receive a list of approved units
And the HTTP status should be 200 OK

## Scenario: Employees can submit but not approve units
Given I am an employee
When I submit a new unit
Then the submission succeeds with status 201
When I try to approve a pending unit
Then the HTTP status should be 403 Forbidden
And the error should be "Insufficient permissions"

## Scenario: Partners can submit via API key but not access admin endpoints
Given I am a partner with a valid API key
When I POST to /api/units
Then the submission succeeds
When I try to GET /api/admin/units
Then the HTTP status should be 403 Forbidden

## Scenario: Admins can approve and edit any unit
Given I am an admin
And there is a pending unit
When I update its conversion factor
Then the unit is updated
And when I approve it
Then it becomes available to the public

## Scenario: API key must be valid to authenticate
Given I am a partner with an invalid API key
When I try to call any protected endpoint
Then the HTTP status should be 401 Unauthorized

## Scenario: Expired API keys are rejected
Given I am a partner with an API key that has expired
When I try to call /api/units
Then the HTTP status should be 401 Unauthorized
And the error should include "API key expired"

## Scenario: Revoked API keys are immediately unusable
Given an admin has revoked my partner API key
When I immediately try to call the API
Then the HTTP status should be 401 Unauthorized
