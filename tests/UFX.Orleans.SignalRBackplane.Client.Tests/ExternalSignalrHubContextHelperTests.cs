namespace UFX.Orleans.SignalRBackplane.Client.Tests
{
    public class ExternalSignalrHubContextHelperTests
    {
        [Fact]
        public void GetHubTypeName_ReturnsHubTypeNameInLowercase()
        {
            // Act
            var hubTypeName = ExternalSignalrHubContextHelper.GetHubTypeName<Hub1>();

            // Assert
            hubTypeName.Should().Be("ufx.orleans.signalrbackplane.client.tests.externalsignalrhubcontexthelpertests+hub1");
        }

        private class Hub1 { }
    }
}