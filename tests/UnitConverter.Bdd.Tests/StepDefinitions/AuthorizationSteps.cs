using Reqnroll;

namespace UnitConverter.Bdd.Tests.StepDefinitions;

[Binding]
public class AuthorizationSteps
{
    [Given(@"I am an unauthenticated user")]
    public void GivenIAmAnUnauthenticatedUser()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I access GET /api/units")]
    public void WhenIAccessGETApiUnits()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"I should receive a list of approved units")]
    public void ThenIShouldReceiveAListOfApprovedUnits()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the HTTP status should be (.*) OK")]
    public void ThenTheHTTPStatusShouldBeOK(int statusCode)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"I am an employee")]
    public void GivenIAmAnEmployee()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I submit a new unit")]
    public void WhenISubmitANewUnit()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the submission succeeds with status (.*)")]
    public void ThenTheSubmissionSucceedsWithStatus(int statusCode)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I try to approve a pending unit")]
    public void WhenITryToApproveAPendingUnit()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the HTTP status should be (.*) Forbidden")]
    public void ThenTheHTTPStatusShouldBeForbidden(int statusCode)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the error should be ""(.*)""")]
    public void ThenTheErrorShouldBe(string errorMessage)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"I am a partner with a valid API key")]
    public void GivenIAmAPartnerWithAValidAPIKey()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I POST to /api/units")]
    public void WhenIPOSTToApiUnits()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the submission succeeds")]
    public void ThenTheSubmissionSucceeds()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I try to GET /api/admin/units")]
    public void WhenITryToGETApiAdminUnits()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"I am an admin")]
    public void GivenIAmAnAdmin()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"there is a pending unit")]
    public void GivenThereIsAPendingUnit()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I update its conversion factor")]
    public void WhenIUpdateItsConversionFactor()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the unit is updated")]
    public void ThenTheUnitIsUpdated()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I approve it")]
    public void WhenIApproveIt()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"it becomes available to the public")]
    public void ThenItBecomesAvailableToThePublic()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"I am a partner with an invalid API key")]
    public void GivenIAmAPartnerWithAnInvalidAPIKey()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I try to call any protected endpoint")]
    public void WhenITryToCallAnyProtectedEndpoint()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the HTTP status should be (.*) Unauthorized")]
    public void ThenTheHTTPStatusShouldBeUnauthorized(int statusCode)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"I am a partner with an API key that has expired")]
    public void GivenIAmAPartnerWithAnAPIKeyThatHasExpired()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I try to call /api/units")]
    public void WhenITryToCallApiUnits()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Then(@"the error should include ""(.*)""")]
    public void ThenTheErrorShouldInclude(string errorMessage)
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [Given(@"an admin has revoked my partner API key")]
    public void GivenAnAdminHasRevokedMyPartnerAPIKey()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }

    [When(@"I immediately try to call the API")]
    public void WhenIImmediatelyTryToCallTheAPI()
    {
        // TODO: Implement step definition
        throw new PendingStepException();
    }
}
