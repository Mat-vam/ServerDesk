using ServerDesk.Infrastructure.SSH;
namespace ServerDesk.Tests;

public class SshScreenServiceTests
{
    [Fact]
    public void ParseSessions_ShouldReturnTwoSessions()
    {
        string output = """
            There are screens on:
                2451.minecraft (Detached)
                3012.terraria (Attached)
            2 Sockets in /run/screen/S-user.
            """;

        var sessions = SshScreenService.ParseSessions(output);

        Assert.Equal(2, sessions.Count);

        Assert.Equal("minecraft", sessions[0].Name);
        Assert.Equal("2451.minecraft", sessions[0].Id);
        Assert.Equal("Detached", sessions[0].State);

        Assert.Equal("terraria", sessions[1].Name);
        Assert.Equal("Attached", sessions[1].State);
    }

    [Fact]
    public void ParseSessions_ShouldHandleEmptyList()
    {
        const string output = "No Sockets found.";

        var sessions = SshScreenService.ParseSessions(output);

        Assert.Empty(sessions);
    }

    [Fact]
    public void ParseSessions_ShouldHandleTimestamps()
    {
        string output = """
        There are screens on:
            2451.serverdesk_test (10/16/26 01:42:10) (Detached)
            3012.minecraft (10/16/26 01:43:20) (Attached)
        2 Sockets in /run/screen/S-user.
        """;

        var sessions = SshScreenService.ParseSessions(output);

        Assert.Equal(2, sessions.Count);

        Assert.Equal("serverdesk_test", sessions[0].Name);
        Assert.Equal("2451.serverdesk_test", sessions[0].Id);
        Assert.Equal("Detached", sessions[0].State);

        Assert.Equal("minecraft", sessions[1].Name);
        Assert.Equal("Attached", sessions[1].State);
    }

}
