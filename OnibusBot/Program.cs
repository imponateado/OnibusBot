using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
            var host = Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .UseWindowsService()
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

                    if (OperatingSystem.IsLinux())
                    {
                        logging.AddSystemdConsole();
                    }

                    if (OperatingSystem.IsWindows())
                    {
                        logging.AddEventLog();
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
        private System.Threading.Timer unsubscribeButtonTimer;
        private TelegramBotClient globalBot;
        private UltimaPosicao globalUltimaPosicao;
        private System.Threading.Timer dataUpdateTimer;
        private ApiCall globalApiCall;
        private CleanObjects globalCleanObjects;
        private string versaoDoBot = "1.0.2";
        private CancellationToken serviceCancellationToken;
        private const int MAX_SUBSCRIPTIONS_PER_USER = 10;

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

                UltimaPosicao ultimaPosicaoFrota = null;
                try
                {
                    ultimaPosicaoFrota = await LoadInitialData(apiCall, cleanObjects);
                    _logger.LogInformation("Dados carregados da API com sucesso.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Erro ao carregar dados de última posição: {ex.Message}");
                }

                var linhasDisponiveis = await AvailableLines(linhasDeOnibus);

                globalBot = bot;
                globalUltimaPosicao = ultimaPosicaoFrota;
                globalApiCall = apiCall;
                globalCleanObjects = cleanObjects;

                notificationTimer = new System.Threading.Timer(
                    callback: async _ => await EnviarNotificacoesPeriodicas(),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(2),
                    period: TimeSpan.FromMinutes(2)
                );

                unsubscribeButtonTimer = new System.Threading.Timer(
                    callback: async _ => await EnviarBotaoDeDesinscrever(),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(10),
                    period: TimeSpan.FromMinutes(10)
                );

                bot.OnMessage += async (msg, type) =>
                    await OnMessage(msg, type, bot, ultimaPosicaoFrota, linhasDisponiveis, versaoDoBot);
                bot.OnUpdate += async (update) => await OnUpdate(bot, update, ultimaPosicaoFrota);

                _logger.LogInformation("OnibusBot daemon iniciado com sucesso! Aguardando mensagens...");

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
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Parando OnibusBot daemon...");

            notificationTimer?.Dispose();
            dataUpdateTimer?.Dispose();
            unsubscribeButtonTimer?.Dispose();

            await base.StopAsync(cancellationToken);
            _logger.LogInformation("OnibusBot daemon parado com sucesso.");
        }

        private void IniciarTimerSeNecessario()
        {
            if (userSubscriptions.Count > 0 && dataUpdateTimer == null)
            {
                _logger.LogInformation("Iniciando timer de atualização - primeira subscription ativa");
                dataUpdateTimer = new System.Threading.Timer(
                    callback: async _ => await AtualizarDadosFrota(),
                    state: null,
                    dueTime: TimeSpan.FromMinutes(1),
                    period: TimeSpan.FromMinutes(1)
                );
            }
        }

        private void PararTimerSeNecessario()
        {
            if (userSubscriptions.Count == 0 && dataUpdateTimer != null)
            {
                _logger.LogInformation("Parando timer de atualização - nenhuma subscription ativa");
                dataUpdateTimer?.Dispose();
                dataUpdateTimer = null;
            }
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

                    PararTimerSeNecessario();

                    await bot.SendMessage(chatId, $"✅ Todas as notificações foram canceladas!");
                    _logger.LogInformation(
                        "Notificações canceladas para chat {ChatId}, {Removidos} inscrições removidas",
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
                if (ultimaPosicao == null)
                {
                    await bot.SendMessage(chatId, "⚠️ Desculpe, o serviço está temporariamente indisponível.");
                    return;
                }

                var inscricoesUsuario = userSubscriptions.Count(x => x.ChatId == chatId);

                var stopKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("❌", $"stop_{chatId}") }
                });

                var jaExiste =
                    userSubscriptions.Any(x => x.ChatId == chatId && x.Linha == linha && x.Sentido == sentido);

                if (!jaExiste)
                {
                    userSubscriptions.Add(new UserSubscription
                    {
                        ChatId = chatId,
                        Linha = linha,
                        Sentido = sentido
                    });

                    IniciarTimerSeNecessario();

                    _logger.LogInformation("Nova inscrição: Chat {ChatId}, Linha {Linha}, Sentido {Sentido}",
                        chatId, linha, sentido);
                }

                if (inscricoesUsuario >= MAX_SUBSCRIPTIONS_PER_USER)
                {
                    var cancelKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("❌ Cancelar todas as inscrições.", $"stop_{chatId}"),
                        }
                    });

                    await bot.SendMessage(chatId,
                        $"🚫 Você atingiu o limite máximo de {MAX_SUBSCRIPTIONS_PER_USER} linhas monitoradas!\n\nPara adicionar uma nova linha, primeiro cancele algumas das existentes.", replyMarkup:cancelKeyboard);
                    return;
                }

                if (jaExiste)
                {
                    bot.SendMessage(chatId, $"Já foi encontrado uma inscrição pra linha {linha}!");
                    return;
                }

                var foundObjects = await ProcessLineSelection(bot, null, linha, sentido, ultimaPosicao);

                if (foundObjects.Count == 0)
                {
                    await bot.SendMessage(chatId,
                        "As localizações dos ônibus serão enviadas quando algum ônibus em curso for encontrado.\n\nClique no botão abaixo quando não quiser ser mais notificado:",
                        replyMarkup: stopKeyboard);
                }
                else
                {
                    await EnviarLocalizacoesDosOnibus(chatId, foundObjects, linha, sentido, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar status do ônibus para chat {ChatId}: {Message}",
                    chatId, ex.Message);
            }
        }

        private async Task EnviarLocalizacoesDosOnibus(long chatId, List<UltimaFeature> onibus, string linha,
            string sentido, bool primeiraVez)
        {
            var sentidoTexto = sentido == "0" ? "IDA" : "VOLTA";

            foreach (var bus in onibus.Take(10))
            {
                await globalBot.SendLocation(chatId, latitude: (float)bus.Geometry.Coordinates[1],
                    longitude: (float)bus.Geometry.Coordinates[0]);
                var endereco = await GetAddressInfo((float)bus.Geometry.Coordinates[1],
                    (float)bus.Geometry.Coordinates[0]);
                var addressInfo = $"{linha} :: {endereco}";
                await globalBot.SendMessage(chatId, addressInfo);

                await Task.Delay(1000, serviceCancellationToken);
            }

            if (onibus.Count > 10)
            {
                await globalBot.SendMessage(chatId, $".. e mais {onibus.Count - 10} circulando.");
            }

            if (primeiraVez)
            {
                var cancelKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("❌", $"stop_{chatId}") }
                });

                await globalBot.SendMessage(chatId,
                    "Você será notificado de 2 em 2 minutos.\n\nClique no botão abaixo quando não quiser ser mais notificado:",
                    replyMarkup: cancelKeyboard);

                var subscription =
                    userSubscriptions.FirstOrDefault(x =>
                        x.ChatId == chatId && x.Linha == linha && x.Sentido == sentido);
                if (subscription != null)
                {
                    subscription.JaRecebeuPrimeiraMensagem = true;
                }
            }
        }

        private async Task OnMessage(Message message, UpdateType type, TelegramBotClient bot,
            UltimaPosicao ultimaPosicaoFrota, List<string> linhasDisponiveis, string versaoDoBot)
        {
            try
            {
                if (message.Text == "/start" || message.Text == "oi" || message.Text == "Oi")
                {
                    if (ultimaPosicaoFrota == null)
                    {
                        await bot.SendMessage(message.Chat,
                            "⚠️ Desculpe, o serviço está temporariamente indisponível.");
                        return;
                    }

                    await bot.SendMessage(message.Chat, "Olá!\nQual linha você quer acompanhar?\nexemplos: 175");
                    return;
                }

                if (double.TryParse(message.Text, out var linhaEnviadaPeloUsuario))
                {
                    if (ultimaPosicaoFrota == null)
                    {
                        await bot.SendMessage(message.Chat,
                            "⚠️ Desculpe, o serviço está temporariamente indisponível.");
                        return;
                    }

                    var linhasEncontradas = await GetMatchingLines(linhaEnviadaPeloUsuario, linhasDisponiveis);

                    if (linhasEncontradas.Count < 1)
                    {
                        await bot.SendMessage(message.Chat, "Nenhuma linha encontrada.");
                    }
                    else
                    {
                        var kbd = new InlineKeyboardMarkup(
                            linhasEncontradas.Select(linha => new[]
                                { InlineKeyboardButton.WithCallbackData(linha, $"{linha}"), })
                        );
                        await bot.SendMessage(message.Chat, "Selecione a linha", replyMarkup: kbd);
                    }
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
                ".env",
                "../.env",
                "../../.env",
                "../../../.env",
                "../../../../.env",
                "../../../../../.env",
                "../../../../../../.env",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env")
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
                if (globalUltimaPosicao == null)
                {
                    _logger.LogError("Pulando notificação, últimas posições não disponíveis.");
                    return;
                }

                _logger.LogDebug("Iniciando envio de notificações periódicas para {Count} inscrições",
                    userSubscriptions.Count);

                foreach (var subscription in userSubscriptions.ToList())
                {
                    try
                    {
                        var foundObjects = await ProcessLineSelection(globalBot, null, subscription.Linha,
                            subscription.Sentido, globalUltimaPosicao);

                        if (foundObjects.Count > 0)
                        {
                            await EnviarLocalizacoesDosOnibus(subscription.ChatId, foundObjects, subscription.Linha,
                                subscription.Sentido, !subscription.JaRecebeuPrimeiraMensagem);
                        }
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

        private async Task EnviarBotaoDeDesinscrever()
        {
            try
            {
                var stopKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("❌", $"stop_"), }
                });

                foreach (var subscription in userSubscriptions.ToList())
                {
                    try
                    {
                        await globalBot.SendMessage(subscription.ChatId, "Deseja parar de receber notificações?",
                            replyMarkup: stopKeyboard);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            $"Erro ao enviar botão de desinscrição para chat {subscription.ChatId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro geral no envio do botão de desinscrição: {ex.Message}");
            }
        }

        private async Task AtualizarDadosFrota()
        {
            try
            {
                if (userSubscriptions.Count == 0)
                {
                    _logger.LogDebug("Nenhuma subscription ativa - pulando atualização dos dados da frota");
                    return;
                }

                var novosDados = await LoadInitialData(globalApiCall, globalCleanObjects);
                globalUltimaPosicao = novosDados;
                _logger.LogDebug(
                    "Dados da frota atualizados com sucesso - {Count} features carregadas para {Subscriptions} subscriptions",
                    novosDados?.Features?.Count ?? 0, userSubscriptions.Count);
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

        private async Task<string> GetAddressInfo(double latitude, double longitude)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    $"OnibusBot/{versaoDoBot} (leoteodoro0@hotmail.com)");
                var url =
                    $"https://nominatim.openstreetmap.org/reverse?format=json&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}";
                Console.WriteLine($"URL construída {url}");
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var nominatinRes = JsonSerializer.Deserialize<NominatimResponse>(jsonContent);

                    if (nominatinRes?.Address != null)
                    {
                        var parts = new List<string>();

                        if (!string.IsNullOrWhiteSpace(nominatinRes?.Address.Road))
                            parts.Add(nominatinRes?.Address.Road);

                        if (!string.IsNullOrWhiteSpace(nominatinRes?.Address.Neighbourhood))
                            parts.Add(nominatinRes?.Address.Neighbourhood);

                        if (!string.IsNullOrWhiteSpace(nominatinRes?.Address.Suburb))
                            parts.Add(nominatinRes?.Address.Suburb);

                        return parts.Count > 0 ? string.Join(" ", parts) : "Endereço não disponível";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Erro ao buscar endereço: {ex.Message}");
            }

            return "Endereço não disponível.";
        }
    }
}