using Discord;
using Discord.WebSocket;
using DiscordDuolingo.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;

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

    private Task OnReadyAsync()
    {
        Console.WriteLine($"[BOT] READY — Guilds carregados: {_client.Guilds.Count}");

        foreach (var guild in _client.Guilds)
        {
            Console.WriteLine($"[BOT] Servidor disponível: {guild.Name}");
        }

        Console.WriteLine("[BOT] Iniciando loop de lembretes e notícias...");
        _ = ReminderLoop();

        return Task.CompletedTask;
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

            _client.Ready += OnReadyAsync;

            string token = _config["DISCORD_TOKEN"]!;

            Console.WriteLine("[BOT] TOKEN LIDO: " + (token != null ? "OK" : "NULO"));

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            Console.WriteLine("[BOT] Conectado! Iniciando loop...");
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

                // Envia notícias
                var newsMessage = await GetNewsAsync();
                if (newsMessage != null && channel != null)
                {
                    try
                    {
                        var newsMessageSent = await channel.SendMessageAsync(newsMessage);
                        Console.WriteLine($"[BOT] As notícias foram enviadas com sucesso! {newsMessageSent}");
                    }
                    catch (Discord.Net.HttpException ex)
                    {
                        Console.WriteLine($"[BOT] Não foi possível enviar notícias no canal #span do servidor {guild.Name}: {ex.Message}");
                    }
                }
            }

        }
    }

    private async Task<string?> GetNewsAsync()
    {
        string newsApiToken = _config["NEWSDATA_API_TOKEN"]!;

        using var client = new HttpClient();

        try
        {
            string url =
                $"https://newsdata.io/api/1/latest?apikey={newsApiToken}" +
                $"&country=br" +
                $"&domainurl=correiobraziliense.com.br" +
                $"&removeduplicate=1";

            var response = await client.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<NewsApiResponse>(response);

            if (data?.Results == null || data.Results.Count == 0)
                return "Não foram encontradas notícias.";

            // Pegue só as 4 mais recentes (ou mude o número)
            var selected = data.Results.Take(4).ToList();

            string msg = "📰 **Resumo das últimas notícias**\n\n";
            foreach (var article in selected)
            {
                msg += $"**{article.Title}**\n";
                msg += $"{article.Description}\n";
                msg += $"🔗 {article.Link}\n\n";
            }

            return msg;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar notícias: {ex.Message}");
            //return "Erro ao buscar notícias.";
            return null;
        }
    }

}
