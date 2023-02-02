namespace UFX.Orleans.SignalR.Client.Tests
{
    public class ExternalSignalrHubContextFactoryTests
    {
        [Fact]
        public void CreateHubContext_ReturnsSameContextInstance_WhenCalledTwiceWithSameHubName()
        {
            // Arrange
            var clusterClient = A.Fake<IClusterClient>();
            var factory = new ExternalSignalrHubContextFactory(clusterClient);
            
            // Act
            var context1 = factory.CreateHubContext("hub1");
            var context2 = factory.CreateHubContext("hub1");

            // Assert
            context1.Should().BeSameAs(context2);
        }

        [Fact]
        public void CreateHubContext_ReturnsDifferentContextInstance_WhenCalledTwiceWithDifferentHubName()
        {
            // Arrange
            var clusterClient = A.Fake<IClusterClient>();
            var factory = new ExternalSignalrHubContextFactory(clusterClient);
            
            // Act
            var context1 = factory.CreateHubContext("hub1");
            var context2 = factory.CreateHubContext("hub2");

            // Assert
            context1.Should().NotBeSameAs(context2);
        }

        [Fact]
        public void CreateHubContext_ReturnsSameContextInstance_WhenCalledTwiceWithSameHubType()
        {
            // Arrange
            var clusterClient = A.Fake<IClusterClient>();
            var factory = new ExternalSignalrHubContextFactory(clusterClient);
            
            // Act
            var context1 = factory.CreateHubContext<Hub1>();
            var context2 = factory.CreateHubContext<Hub1>();

            // Assert
            context1.Should().BeSameAs(context2);
        }

        [Fact]
        public void CreateHubContext_ReturnsDifferentContextInstance_WhenCalledTwiceWithDifferentHubType()
        {
            // Arrange
            var clusterClient = A.Fake<IClusterClient>();
            var factory = new ExternalSignalrHubContextFactory(clusterClient);
            
            // Act
            var context1 = factory.CreateHubContext<Hub1>();
            var context2 = factory.CreateHubContext<Hub2>();

            // Assert
            context1.Should().NotBeSameAs(context2);
        }

        private class Hub1 { }
        private class Hub2 { }
    }
}