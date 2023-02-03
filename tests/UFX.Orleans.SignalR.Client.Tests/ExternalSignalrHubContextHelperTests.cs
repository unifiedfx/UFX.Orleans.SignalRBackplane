namespace UFX.Orleans.SignalR.Client.Tests
{
    public class ExternalSignalrHubContextHelperTests
    {
        [Fact]
        public void GetHubTypeName_ReturnsHubTypeNameInLowercase()
        {
            // Act
            var hubTypeName = ExternalSignalrHubContextHelper.GetHubTypeName<Hub1>();

            // Assert
            hubTypeName.Should().Be("ufx.orleans.signalr.client.tests.externalsignalrhubcontexthelpertests+hub1");
        }

        private class Hub1 { }
    }
}