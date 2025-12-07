using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

class Bot
{
    private readonly DiscordSocketClient _client;
    private readonly string _token;

    public Bot(string token)
    {
        _token = token;

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent
        });

        _client.Log += LogAsync;
    }

    public async Task StartAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();

        _ = StartReminderLoop();

        await Task.Delay(-1); // mantém vivo
    }

    private async Task StartReminderLoop()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromHours(24)); // ou o intervalo que quiser

            foreach (var guild in _client.Guilds)
            {
                var channel = guild.DefaultChannel;
                if (channel != null)
                {
                    await channel.SendMessageAsync(
                        "@everyone ⚠️ **VOCÊ FEZ O DUOLINGO HOJE???** ⚠️\n" +
                        "Não me faça ir aí te lembrar pessoalmente. 🦉💢"
                    );
                }
            }
        }
    }

    private Task LogAsync(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
}
