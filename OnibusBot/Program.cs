using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualBasic;
using OnibusBot.Interfaces;
using OnibusBot.Service;
using OnibusBot.Utils;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OnibusBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configuração para funcionar como daemon e aplicação normal
            var host = Host.CreateDefaultBuilder(args)
                .UseSystemd() // Habilita integração com systemd (Linux)
                .UseWindowsService() // Habilita integração com Windows Services
                .ConfigureServices((context, services) =>
                {
                    services.AddHostedService<OnibusService>();
                    services.AddLogging();
                    services.AddHttpClient();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    
                    // Logs específicos por plataforma
                    if (OperatingSystem.IsLinux())
                    {
                        logging.AddSystemdConsole();
                    }
                    
                    if (OperatingSystem.IsWindows())
                    {
                        logging.AddEventLog(); // Event Viewer do Windows
                    }
                })
                .Build();

            await host.RunAsync();
        }
    }

    public class OnibusService : BackgroundService
    {
        private readonly ILogger<OnibusService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        
        private List<UserSubscription> userSubscriptions = new List<UserSubscription>();
        private System.Threading.Timer notificationTimer;
        private TelegramBotClient globalBot;
        private UltimaPosicao globalUltimaPosicao;
        private System.Threading.Timer dataUpdateTimer;
        private ApiCall globalApiCall;
        private CleanObjects globalCleanObjects;
        private string versaoDoBot = "1.0.1";
        private CancellationToken serviceCancellationToken;

        public OnibusService(ILogger<OnibusService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                serviceCancellationToken = stoppingToken;
                _logger.LogInformation("Iniciando OnibusBot daemon versão {Versao}...", versaoDoBot);

                var botToken = GetBotToken();
                if (string.IsNullOrEmpty(botToken))
                {
                    _logger.LogError("Token do bot não encontrado!");
                    return;
                }

                var bot = new TelegramBotClient(botToken, cancellationToken: stoppingToken);

                var httpClient = _httpClientFactory.CreateClient();
                var httpHandle = new HttpClientHandler();
                var apiCall = new ApiCall(httpClient, httpHandle);
                var cleanObjects = new CleanObjects();

                _logger.LogInformation("Carregando dados iniciais...");
                var linhasDeOnibus = await apiCall.GetLinhasDeOnibus();
                var ultimaPosicaoFrota = await LoadInitialData(apiCall, cleanObjects);
                var linhasDisponiveis = await AvailableLines(linhasDeOnibus);

                globalBot = bot;
                globalUltimaPosicao = ultimaPosicaoFrota;
                globalApiCall = apiCall;
                globalCleanObjects = cleanObjects;

                // Timer para notificações periódicas
                notificationTimer = new System.Threading.Timer(
                    callback: async _ => await EnviarNotificacoesPeriodicas(),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(2),
                    period: TimeSpan.FromMinutes(2)
                );

                // Timer para atualização de dados
                dataUpdateTimer = new System.Threading.Timer(
                    callback: async _ => await AtualizarDadosFrota(),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(1),
                    period: TimeSpan.FromMinutes(1)
                );

                bot.OnMessage += async (msg, type) =>
                    await OnMessage(msg, type, bot, ultimaPosicaoFrota, linhasDisponiveis, versaoDoBot);
                bot.OnUpdate += async (update) => await OnUpdate(bot, update, ultimaPosicaoFrota);

                _logger.LogInformation("OnibusBot daemon iniciado com sucesso! Aguardando mensagens...");

                // Mantém o serviço rodando até o cancelamento
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OnibusBot daemon foi parado graciosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro crítico no OnibusBot daemon: {Message}", ex.Message);
                throw; // Re-throw para que o systemd saiba que houve falha
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando OnibusBot daemon...");
            
            notificationTimer?.Dispose();
            dataUpdateTimer?.Dispose();
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("OnibusBot daemon parado com sucesso.");
        }

        private async Task OnUpdate(TelegramBotClient bot, Update update, UltimaPosicao ultimaPosicao)
        {
            try
            {
                if (update.CallbackQuery == null) return;

                var callbackData = update.CallbackQuery.Data;
                var chatId = update.CallbackQuery.Message.Chat.Id;

                await bot.AnswerCallbackQuery(update.CallbackQuery.Id);

                if (callbackData.StartsWith("stop_"))
                {
                    var removidos = userSubscriptions.RemoveAll(x => x.ChatId == chatId);
                    await bot.SendMessage(chatId, $"✅ Notificações canceladas!");
                    _logger.LogInformation("Notificações canceladas para chat {ChatId}, {Removidos} inscrições removidas", 
                        chatId, removidos);
                    return;
                }

                if (callbackData.StartsWith("sentido_"))
                {
                    var parts = callbackData.Split('_');
                    if (parts.Length >= 3)
                    {
                        var linha = parts[1];
                        var sentido = parts[2];

                        await ProcessAndSendBusStatus(bot, chatId, linha, sentido, ultimaPosicao);
                    }

                    return;
                }

                var linhaSelecionada = callbackData;

                var sentidoKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("IDA", $"sentido_{linhaSelecionada}_0"),
                        InlineKeyboardButton.WithCallbackData("VOLTA", $"sentido_{linhaSelecionada}_1")
                    }
                });

                await bot.SendMessage(chatId,
                    $"Escolha o sentido:",
                    replyMarkup: sentidoKeyboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no OnUpdate para callback {CallbackData}: {Message}", 
                    update.CallbackQuery?.Data, ex.Message);
            }
        }

        private async Task ProcessAndSendBusStatus(TelegramBotClient bot, long chatId,
            string linha, string sentido, UltimaPosicao ultimaPosicao)
        {
            try
            {
                var stopKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("❌ Parar notificações", $"stop_{chatId}") }
                });

                var jaExiste = userSubscriptions.Any(x => x.ChatId == chatId && x.Linha == linha && x.Sentido == sentido);

                if (!jaExiste)
                {
                    userSubscriptions.Add(new UserSubscription
                    {
                        ChatId = chatId,
                        Linha = linha,
                        Sentido = sentido
                    });
                    _logger.LogInformation("Nova inscrição: Chat {ChatId}, Linha {Linha}, Sentido {Sentido}", 
                        chatId, linha, sentido);
                }

                var foundObjects = await ProcessLineSelection(bot, null, linha, sentido, ultimaPosicao);

                var sentidoTexto = sentido == "0" ? "IDA" : "VOLTA";
                await bot.SendMessage(chatId,
                    $"🚌 Encontrados {foundObjects.Count} ônibus da linha {linha} no sentido {sentidoTexto}:");

                foreach (var onibus in foundObjects.Take(10))
                {
                    await bot.SendLocation(chatId, latitude: (float)onibus.Geometry.Coordinates[1],
                        longitude: (float)onibus.Geometry.Coordinates[0]);

                    await Task.Delay(500, serviceCancellationToken);
                }

                if (foundObjects.Count > 10)
                {
                    await bot.SendMessage(chatId,
                        $"... e mais {foundObjects.Count - 10} ônibus circulando nesta linha!");
                }

                await bot.SendMessage(chatId,
                    "Você será notificado a cada 2 minutos, deseja parar de receber notificações?",
                    replyMarkup: stopKeyboard);

                _logger.LogDebug("Status enviado para chat {ChatId}: {Count} ônibus da linha {Linha}", 
                    chatId, foundObjects.Count, linha);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar status do ônibus para chat {ChatId}: {Message}", 
                    chatId, ex.Message);
            }
        }

        private async Task OnMessage(Message message, UpdateType type, TelegramBotClient bot,
            UltimaPosicao ultimaPosicaoFrota, List<string> linhasDisponiveis, string versaoDoBot)
        {
            try
            {
                if (message.Text == "/start" || message.Text == "oi" || message.Text == "Oi")
                {
                    await bot.SendMessage(message.Chat, "Olá!\nQual linha você quer acompanhar?\nexemplos: 175");
                    _logger.LogInformation("Comando /start recebido do chat {ChatId}", message.Chat.Id);
                    return;
                }

                if (double.TryParse(message.Text, out var linhaEnviadaPeloUsuario))
                {
                    var linhasEncontradas = await GetMatchingLines(linhaEnviadaPeloUsuario, linhasDisponiveis);

                    if (linhasEncontradas.Count < 1)
                    {
                        await bot.SendMessage(message.Chat, "Nenhuma linha encontrada.");
                        _logger.LogDebug("Nenhuma linha encontrada para {Linha} do chat {ChatId}", 
                            linhaEnviadaPeloUsuario, message.Chat.Id);
                    }
                    else
                    {
                        var kbd = new InlineKeyboardMarkup(
                            linhasEncontradas.Select(linha => new[]
                                { InlineKeyboardButton.WithCallbackData(linha, $"{linha}"), })
                        );
                        await bot.SendMessage(message.Chat, "Selecione a linha", replyMarkup: kbd);
                        _logger.LogDebug("Enviadas {Count} opções de linha para chat {ChatId}", 
                            linhasEncontradas.Count, message.Chat.Id);
                    }
                }

                if (message.Text == "/info")
                {
                    await bot.SendMessage(message.Chat, $"Porque criei este BOT?\n\nO criador tem um plano de internet muito ruim (que remete à internet discada), e nesse plano apenas dados muito pequenos funcionam com alguma tranquilidade (geralmente aplicativos de mensagem). Os aplicativos disponíveis atualmente ainda sim são muito \"pesados\" para a internet desse plano.\n\nO Bot é de código aberto?\n\nSim e está disponível em https://github.com/imponateado/OnibusBot\n\nVersão do BOT: {versaoDoBot}");
                    _logger.LogInformation("Comando /info executado para chat {ChatId}", message.Chat.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no OnMessage para chat {ChatId}: {Message}", 
                    message.Chat.Id, ex.Message);
            }
        }

        private async Task<List<string>> GetMatchingLines(double linhaEnviadaPeloUsuario,
            List<string> linhasDisponiveis)
        {
            var res = linhasDisponiveis.Where(x => x.Contains(linhaEnviadaPeloUsuario.ToString())).ToList();
            return res;
        }

        private async Task<List<UltimaFeature>> ProcessLineSelection(TelegramBotClient bot, Message msg,
            string linhaSelecionada, string sentidoSelecionado,
            UltimaPosicao ultimaPosicaoFrota)
        {
            var foundObjects = ultimaPosicaoFrota.Features.Where(x =>
                    x.Properties.Linha == linhaSelecionada.ToString() &&
                    x.Properties.Sentido == sentidoSelecionado)
                .ToList();

            return foundObjects;
        }

        private async Task<UltimaPosicao> LoadInitialData(ApiCall apiCall, CleanObjects cleanObjects)
        {
            var ultimasPosicoesFrota = await apiCall.GetUltimaPosicaoFrota();
            ultimasPosicoesFrota = cleanObjects.CleanUltimaPosicaoObject(ultimasPosicoesFrota);
            return ultimasPosicoesFrota;
        }

        private async Task<List<string>> AvailableLines(LinhasDeOnibus linhasDeOnibus)
        {
            return linhasDeOnibus.Features.Select(x => x.Properties.Linha).Distinct().ToList();
        }

        private string GetBotToken()
        {
            var tokenFromEnv = Environment.GetEnvironmentVariable("BOT_TOKEN");
            if (!string.IsNullOrEmpty(tokenFromEnv))
                return tokenFromEnv;

            var searchPaths = new[]
            {
                ".env", // Diretório atual
                "../.env", // Um nível acima
                "../../.env", // Dois níveis acima
                "../../../.env", // Três níveis acima
                "../../../../.env", // Quatro níveis acima
                "../../../../../.env", // Cinco níveis acima (seu caso real)
                "../../../../../../.env", // Seis níveis acima (por segurança)
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env") // Pasta do executável
            };

            foreach (var path in searchPaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        var lines = File.ReadAllLines(path);
                        var tokenLine = lines.FirstOrDefault(x => x.StartsWith("BOT_TOKEN="));
                        var token = tokenLine?.Substring("BOT_TOKEN=".Length).Trim();

                        if (!string.IsNullOrEmpty(token))
                        {
                            _logger.LogInformation("Token encontrado em: {Path}", Path.GetFullPath(path));
                            return token;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Erro ao ler {Path}: {Message}", path, ex.Message);
                }
            }

            _logger.LogError("Token não encontrado em nenhum local");
            return "";
        }

        private async Task EnviarNotificacoesPeriodicas()
        {
            try
            {
                _logger.LogDebug("Iniciando envio de notificações periódicas para {Count} inscrições", 
                    userSubscriptions.Count);

                foreach (var subscription in userSubscriptions.ToList())
                {
                    try
                    {
                        await ProcessAndSendBusStatus(globalBot, subscription.ChatId,
                            subscription.Linha, subscription.Sentido, globalUltimaPosicao);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao enviar notificação para chat {ChatId}: {Message}", 
                            subscription.ChatId, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro geral no envio de notificações periódicas: {Message}", ex.Message);
            }
        }

        private async Task AtualizarDadosFrota()
        {
            try
            {
                var novosDados = await LoadInitialData(globalApiCall, globalCleanObjects);
                globalUltimaPosicao = novosDados;
                _logger.LogDebug("Dados da frota atualizados com sucesso - {Count} features carregadas", 
                    novosDados?.Features?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar dados da frota: {Message}", ex.Message);
            }
        }

        private async Task ProcessComentedCode(ApiCall apiCall)
        {
            // Código comentado mantido como no original
            //var paradasDeOnibus = await apiCall.GetParadasDeOnibus();
            //Console.WriteLine($"Paradas de Ônibus API");
            //linhas e paradas vem no padrão utm, precisam ser convertidos para wgs84
            //linhasDeOnibus = convertStuff.ConverterLinhasDeOnibusCoords(linhasDeOnibus).Result;
            //Console.WriteLine($"Conversão das linhas em UTM");
            //paradasDeOnibus = convertStuff.ConverterParadasCoords(paradasDeOnibus).Result;
            //Console.WriteLine($"Conversão das últimas posições");
            //remove features não necessárias
            //paradasDeOnibus = cleanObjects.CleanParadasDeOnibusObject(paradasDeOnibus);
            //Console.WriteLine($"Limpeza objeto paradas");
            //var linhaEncontradaDoInputDoUsuario = Manage.GetLinhas(linhasDeOnibus, linhaSelecionada);
            //Console.WriteLine($"A linha encontrada foi {linhaEncontradaDoInputDoUsuario.First().Properties.Linha}");
            /*var essaLinhaExisteNoUltimasPosicoes =
                Manage.LineExistsAtLastPosition(ultimaPosicaoFrota, linhaSelecionada); */
            //Console.WriteLine($"Essa linha está nas últimas posições? {essaLinhaExisteNoUltimasPosicoes} ");
            /*var coordenadasDaLinhaSelecionadaPeloUsuario =
                linhasDeOnibus.Features.Where(x => x.Properties.Linha == linhaSelecionada).First().Geometry.Coordinates; */
            //var posicaoNasCoordenadasDaLinhaMaisProximaDoUsuario = Manage.GetClosestPoint(coordenadasDaLinhaSelecionadaPeloUsuario, latlon);
            //Console.WriteLine($"Latitude,Longitude,Nome");
            /*for (int i = 0; i <= 200; i++)
            {
                Console.WriteLine($"{posicaoNasCoordenadasDaLinhaMaisProximaDoUsuario[i][1].ToString(CultureInfo.InvariantCulture)},{posicaoNasCoordenadasDaLinhaMaisProximaDoUsuario[i][0].ToString(CultureInfo.InvariantCulture)},Ponto {i}");
            } */
        }
    }
}