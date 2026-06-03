# Feature: Security and error handling

As a security-conscious system
I want to validate inputs and handle errors gracefully
So that the API is resilient to misuse and attacks

@security
## Scenario: SQL injection in unit name is rejected
Given I am an employee
When I submit a unit with name "'; DROP TABLE Units; --"
Then the system should reject it
And the HTTP status should be 400 Bad Request
And the error should mention "Invalid unit name format"

@security
## Scenario: Rate limiting protects against abuse
Given a user makes more than 100 requests per minute to /api/conversions
When the 101st request arrives
Then the HTTP status should be 429 Too Many Requests

@security
## Scenario: No stack traces in error responses
Given an internal server error occurs
When I call any endpoint
Then the error response should NOT include stack traces
And the error should include only a correlation ID
And the HTTP status should be 500 Internal Server Error

@security
## Scenario: Sensitive error messages don't leak info
Given I submit a unit with an invalid API key
When the authentication fails
Then the error message should be generic "Invalid credentials"
And NOT "API key not found in database" (which reveals DB structure)

@security
## Scenario: HTTPS is enforced
Given I try to connect via HTTP
Then the system should redirect to HTTPS
Or reject the connection entirely

## Scenario: Correlation IDs are included in errors
Given an error occurs during unit approval
When I receive the error response
Then the response should include a "X-Correlation-Id" header
And the same ID should appear in server logs for debugging
