using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

await MainAsync();
return;

async Task MainAsync()
{
    var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    
    var client = new DiscordSocketClient(new DiscordSocketConfig
    {
        GatewayIntents = 
            GatewayIntents.Guilds | 
            GatewayIntents.GuildPresences
    });

    client.Log += Log;

    //  You can assign your bot token to a string, and pass that in to connect.
    //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
    var token = configuration["Token"];

    // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
    // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
    // var token = File.ReadAllText("token.txt");
    // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;

    await client.LoginAsync(TokenType.Bot, token);
    await client.StartAsync();
    // Block this task until the program is closed.
    await Task.Delay(10000);

    var channel = await client.GetChannelAsync(ulong.Parse(configuration["ChannelId"])) as ITextChannel;
    var me = await client.GetUserAsync(ulong.Parse(configuration["MyId"]));
    var botToHealthCheck = await client.GetUserAsync(ulong.Parse(configuration["BotToHealthCheckId"]));

    if (channel is null || me is null || botToHealthCheck is null)
    {
        await Log(new LogMessage(LogSeverity.Error, "Main", "Some of the required entities is null."));
        await DisposeAsync(client);
        return;
    }

    if (botToHealthCheck.Status == UserStatus.Online)
    {
        var messageFormats = configuration.GetSection("MessageFormats").GetChildren().ToArray();
        await channel.SendMessageAsync(
            string.Format(messageFormats[new Random().Next(messageFormats.Length)].Value ?? 
                          "{0}, {1} устал, прилёг отдохнуть", me.Mention, botToHealthCheck.Mention));
        await Log(new LogMessage(LogSeverity.Info, "Main", "Healthcheck passed."));
    }
    else
    {
        await Log(new LogMessage(LogSeverity.Error, "Main", $"Healthcheck failed. Bot status: {botToHealthCheck.Status}"));
    }
    
    await DisposeAsync(client);
}

Task Log(LogMessage msg)
{
    Console.WriteLine(msg.ToString());
    return Task.CompletedTask;
}

async Task DisposeAsync(IDiscordClient client)
{
    await client.StopAsync();
    await client.DisposeAsync();
}