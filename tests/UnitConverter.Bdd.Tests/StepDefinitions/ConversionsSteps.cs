using Reqnroll;

namespace UnitConverter.Bdd.Tests.StepDefinitions;

[Binding]
public class ConversionsSteps
{
    [Given(@"the conversion system has approved units for length")]
    public void GivenTheConversionSystemHasApprovedUnitsForLength()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I request to convert (.*) meters to kilometers")]
    public void WhenIRequestToConvertMetersToKilometers(double amount)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the result should be (.*) kilometer")]
    public void ThenTheResultShouldBeKilometer(double expected)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"the conversion system has approved units for temperature")]
    public void GivenTheConversionSystemHasApprovedUnitsForTemperature()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I request to convert (.*) celsius to fahrenheit")]
    public void WhenIRequestToConvertCelsiusToFahrenheit(double amount)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the result should be (.*) fahrenheit")]
    public void ThenTheResultShouldBeFahrenheit(double expected)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"the conversion system has approved units for weight")]
    public void GivenTheConversionSystemHasApprovedUnitsForWeight()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I request to convert (.*) kilogram to pounds")]
    public void WhenIRequestToConvertKilogramToPounds(double amount)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the result should be approximately (.*) pounds")]
    public void ThenTheResultShouldBeApproximatelyPounds(double expected)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"the conversion system has approved units for length and weight")]
    public void GivenTheConversionSystemHasApprovedUnitsForLengthAndWeight()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I request to convert (.*) meters to kilograms")]
    public void WhenIRequestToConvertMetersToKilograms(double amount)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the system should return an error ""(.*)""")]
    public void ThenTheSystemShouldReturnAnError(string errorMessage)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the HTTP status should be (.*) Unprocessable Entity")]
    public void ThenTheHTTPStatusShouldBeUnprocessableEntity(int statusCode)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"the conversion system has approved units")]
    public void GivenTheConversionSystemHasApprovedUnits()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I request to convert (.*) to an unknown unit ""(.*)""")]
    public void WhenIRequestToConvertToAnUnknownUnit(double amount, string unit)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the HTTP status should be (.*) Not Found")]
    public void ThenTheHTTPStatusShouldBeNotFound(int statusCode)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"I have a temperature value (.*) fahrenheit")]
    public void GivenIHaveATemperatureValueFahrenheit(double value)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I convert it to celsius and back to fahrenheit")]
    public void WhenIConvertItToCelsiusAndBackToFahrenheit()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the result should be approximately (.*) fahrenheit \(within (.*) precision\)")]
    public void ThenTheResultShouldBeApproximatelyFahrenheitWithinPrecision(double expected, double precision)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }
}
