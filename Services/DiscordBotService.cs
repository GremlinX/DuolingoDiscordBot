using Discord;
using Discord.WebSocket;

namespace DiscordDuolingo.Services;

public class DiscordBotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;

    public DiscordBotService(IConfiguration config)
    {
        _config = config;

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds |
                             GatewayIntents.GuildMessages |
                             GatewayIntents.MessageContent
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("[BOT] Iniciando...");

            _client.Log += msg =>
            {
                Console.WriteLine("[DISCORD] " + msg);
                return Task.CompletedTask;
            };

            string token = _config["DISCORD_TOKEN"]!;

            Console.WriteLine("[BOT] TOKEN LIDO: " + (token != null ? "OK" : "NULO"));

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            Console.WriteLine("[BOT] Conectado! Iniciando loop...");
            _ = ReminderLoop();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[BOT ERRO] " + ex.ToString());
        }
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
    }

    private async Task ReminderLoop()
    {
        // Fuso horário de Brasília
        TimeZoneInfo brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); // Windows
                                                                                                           // Linux: "America/Sao_Paulo"

        while (true)
        {
            // Hora atual em Brasília
            DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, brasiliaZone);

            // Próximo horário 20:00
            DateTime next20h = new DateTime(now.Year, now.Month, now.Day, 20, 0, 0);
            if (now > next20h)
            {
                next20h = next20h.AddDays(1);
            }

            TimeSpan delay = next20h - now;
            Console.WriteLine($"[BOT] Próximo lembrete às {next20h} (em {delay.TotalMinutes:N0} minutos)");

            // Espera até 20:00
            await Task.Delay(delay);

            foreach (var guild in _client.Guilds)
            {
                // Procura canal chamado "span"
                var channel = guild.TextChannels.FirstOrDefault(c => c.Name == "spam");

                if (channel != null)
                {
                    try
                    {
                        await channel.SendMessageAsync("@everyone ⚠️ **VOCÊ FEZ O DUOLINGO HOJE?** 🦉🔥");
                    }
                    catch (Discord.Net.HttpException ex)
                    {
                        Console.WriteLine($"[BOT] Não foi possível enviar mensagem no canal #span do servidor {guild.Name}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[BOT] Não existe canal #span no servidor {guild.Name}");
                }
            }

        }
    }

}
