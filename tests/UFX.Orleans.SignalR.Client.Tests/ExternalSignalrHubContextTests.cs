namespace UFX.Orleans.SignalR.Client.Tests;

public class ExternalSignalrHubContextTests
{
    private static readonly string HubTypeName = ExternalSignalrHubContextHelper.GetHubTypeName<Hub1>();

    [Fact]
    public async Task SendAllAsync_CallsHubGrain()
    {
        // Arrange
        var hubGrain = A.Fake<IHubGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IHubGrain>(HubTypeName, null)).Returns(hubGrain);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendAllAsync("method", Array.Empty<object>());

        // Assert
        A.CallTo(() => hubGrain.SendAllAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SendAllExceptAsync_CallsHubGrain()
    {
        // Arrange

        var hubGrain = A.Fake<IHubGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IHubGrain>(HubTypeName, null)).Returns(hubGrain);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendAllExceptAsync("method", Array.Empty<object>(), new[] { "connectionId" });

        // Assert
        A.CallTo(() => hubGrain.SendAllExceptAsync("method", A<object[]>.Ignored, A<string[]>.That.Contains("connectionId"))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SendConnectionAsync_CallsConnectionGrain()
    {
        // Arrange
        var connectionGrain = A.Fake<IConnectionGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IConnectionGrain>($"{HubTypeName}/connectionId", null)).Returns(connectionGrain);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendConnectionAsync("connectionId", "method", Array.Empty<object>());

        // Assert
        A.CallTo(() => connectionGrain.SendConnectionAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SendConnectionsAsync_CallsConnectionGrain()
    {
        // Arrange
        var connectionGrain = A.Fake<IConnectionGrain>();
        var connectionGrain2 = A.Fake<IConnectionGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IConnectionGrain>($"{HubTypeName}/connectionId", null)).Returns(connectionGrain);
        A.CallTo(() => clusterClient.GetGrain<IConnectionGrain>($"{HubTypeName}/connectionId2", null)).Returns(connectionGrain2);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendConnectionsAsync(new[] { "connectionId", "connectionId2" }, "method", Array.Empty<object>());

        // Assert
        A.CallTo(() => connectionGrain.SendConnectionAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => connectionGrain2.SendConnectionAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SendGroupAsync_CallsGroupGrain()
    {
        // Arrange
        var groupGrain = A.Fake<IGroupGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IGroupGrain>($"{HubTypeName}/groupName", null)).Returns(groupGrain);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendGroupAsync("groupName", "method", Array.Empty<object>());

        // Assert
        A.CallTo(() => groupGrain.SendGroupAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SendGroupsAsync_CallsGroupGrain()
    {
        // Arrange
        var groupGrain = A.Fake<IGroupGrain>();
        var groupGrain2 = A.Fake<IGroupGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IGroupGrain>($"{HubTypeName}/groupName", null)).Returns(groupGrain);
        A.CallTo(() => clusterClient.GetGrain<IGroupGrain>($"{HubTypeName}/groupName2", null)).Returns(groupGrain2);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendGroupsAsync(new[] { "groupName", "groupName2" }, "method", Array.Empty<object>());

        // Assert
        A.CallTo(() => groupGrain.SendGroupAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => groupGrain2.SendGroupAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SendGroupExceptAsync_CallsGroupGrain()
    {
        // Arrange
        var groupGrain = A.Fake<IGroupGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IGroupGrain>($"{HubTypeName}/groupName", null)).Returns(groupGrain);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendGroupExceptAsync("groupName", "method", Array.Empty<object>(), new[] { "connectionId" });

        // Assert
        A.CallTo(() => groupGrain.SendGroupExceptAsync("method", A<object[]>.Ignored, A<string[]>.That.Contains("connectionId"))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SendUserAsync_CallsUserGrain()
    {
        // Arrange
        var userGrain = A.Fake<IUserGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IUserGrain>($"{HubTypeName}/userId", null)).Returns(userGrain);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendUserAsync("userId", "method", Array.Empty<object>());

        // Assert
        A.CallTo(() => userGrain.SendUserAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SendUsersAsync_CallsUserGrain()
    {
        // Arrange
        var userGrain = A.Fake<IUserGrain>();
        var userGrain2 = A.Fake<IUserGrain>();
        var clusterClient = A.Fake<IClusterClient>();
        A.CallTo(() => clusterClient.GetGrain<IUserGrain>($"{HubTypeName}/userId", null)).Returns(userGrain);
        A.CallTo(() => clusterClient.GetGrain<IUserGrain>($"{HubTypeName}/userId2", null)).Returns(userGrain2);

        var context = new ExternalSignalrHubContext(clusterClient, HubTypeName);

        // Act
        await context.SendUsersAsync(new[] { "userId", "userId2" }, "method", Array.Empty<object>());

        // Assert
        A.CallTo(() => userGrain.SendUserAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
        A.CallTo(() => userGrain2.SendUserAsync("method", A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
    }

    private class Hub1 { }
}