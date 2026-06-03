# Feature: Conversion of units between different categories

As a user of the public API
I want to convert quantities between approved units
So that I can get accurate conversions without manual calculations

## Scenario: Convert meters to kilometers
Given the conversion system has approved units for length
When I request to convert 1000 meters to kilometers
Then the result should be 1 kilometer

## Scenario: Convert celsius to fahrenheit
Given the conversion system has approved units for temperature
When I request to convert 0 celsius to fahrenheit
Then the result should be 32 fahrenheit

## Scenario: Convert kilograms to pounds
Given the conversion system has approved units for weight
When I request to convert 1 kilogram to pounds
Then the result should be approximately 2.20462 pounds

## Scenario: Cannot convert across incompatible categories
Given the conversion system has approved units for length and weight
When I request to convert 10 meters to kilograms
Then the system should return an error "Cannot convert between different categories"
And the HTTP status should be 422 Unprocessable Entity

## Scenario: Reject conversion with unknown unit
Given the conversion system has approved units
When I request to convert 100 to an unknown unit "bazinga"
Then the system should return an error "Unknown unit: bazinga"
And the HTTP status should be 404 Not Found

## Scenario: Round-trip conversion maintains precision
Given I have a temperature value 98.6 fahrenheit
When I convert it to celsius and back to fahrenheit
Then the result should be approximately 98.6 fahrenheit (within 0.01 precision)
