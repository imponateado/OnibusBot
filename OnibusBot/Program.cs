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

namespace OnibusBot
{
    class Program
    {
        private static List<UserSubscription> userSubscriptions = new List<UserSubscription>();
        private static System.Threading.Timer notificationTimer;
        private static TelegramBotClient globalBot;
        private static UltimaPosicao globalUltimaPosicao;

        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();
            var botToken = GetBotToken();
            var bot = new TelegramBotClient(botToken, cancellationToken: cts.Token);

            var httpClient = new HttpClient();
            var httpHandle = new HttpClientHandler();
            var apiCall = new ApiCall(httpClient, httpHandle);
            //var convertStuff = new ConvertStuff();
            var cleanObjects = new CleanObjects();
            //var latlon = new List<double> { -15.888812, -48.019994 };

            var linhasDeOnibus = await apiCall.GetLinhasDeOnibus();
            var ultimaPosicaoFrota = await LoadInitialData(apiCall, cleanObjects);
            var linhasDisponiveis = await AvailableLines(linhasDeOnibus);

            // Salvar referências globais
            globalBot = bot;
            globalUltimaPosicao = ultimaPosicaoFrota;

            // Configurar timer para notificações a cada 2 minutos
            notificationTimer = new System.Threading.Timer(
                callback: async _ => await EnviarNotificacoesPeriodicas(),
                state: null,
                dueTime: TimeSpan.FromMinutes(2),
                period: TimeSpan.FromMinutes(2)
            );

            bot.OnMessage += async (msg, type) =>
                await OnMessage(msg, type, bot, ultimaPosicaoFrota, linhasDisponiveis);
            bot.OnUpdate += async (update) => await OnUpdate(bot, update, ultimaPosicaoFrota);

            Console.WriteLine($"Hit whatever key to shut bot down.");
            Console.ReadKey();

            try
            {
                await ProcessComentedCode(apiCall);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static async Task OnUpdate(TelegramBotClient bot, Update update, UltimaPosicao ultimaPosicao)
        {
            if (update.CallbackQuery == null) return;

            var callbackData = update.CallbackQuery.Data;
            var chatId = update.CallbackQuery.Message.Chat.Id;

            // Responder o callback primeiro
            await bot.AnswerCallbackQuery(update.CallbackQuery.Id);

            // Se é uma seleção de linha (não começa com "sentido_")
            if (!callbackData.StartsWith("sentido_"))
            {
                var linhaSelecionada = callbackData;

                // Criar botões para IDA/VOLTA
                var sentidoKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("IDA", $"sentido_{linhaSelecionada}_0"),
                        InlineKeyboardButton.WithCallbackData("VOLTA", $"sentido_{linhaSelecionada}_1")
                    }
                });

                await bot.SendMessage(chatId,
                    $"Linha {linhaSelecionada} selecionada! Escolha o sentido:",
                    replyMarkup: sentidoKeyboard);
            }
            // Se é uma seleção de sentido
            else if (callbackData.StartsWith("sentido_"))
            {
                var parts = callbackData.Split('_');
                if (parts.Length >= 3)
                {
                    var linha = parts[1];
                    var sentido = parts[2];

                    await ProcessAndSendBusStatus(bot, chatId, linha, sentido, ultimaPosicao);
                }
            }

            // Se é comando para parar notificações
            else if (callbackData.StartsWith("stop_"))
            {
                var removidos = userSubscriptions.RemoveAll(x => x.ChatId == chatId);
                await bot.SendMessage(chatId, "✅ Notificações canceladas!");
            }
        }

        private static async Task ProcessAndSendBusStatus(TelegramBotClient bot, long chatId,
            string linha, string sentido, UltimaPosicao ultimaPosicao)
        {
            var foundObjects = await ProcessLineSelection(bot, null, linha, sentido, ultimaPosicao);

            if (!foundObjects.Any())
            {
                await bot.SendMessage(chatId,
                    $"❌ Nenhum ônibus encontrado para a linha {linha} no sentido {(sentido == "0" ? "IDA" : "VOLTA")}");
                return;
            }

            var sentidoTexto = sentido == "0" ? "IDA" : "VOLTA";
            await bot.SendMessage(chatId,
                $"🚌 Encontrados {foundObjects.Count} ônibus da linha {linha} no sentido {sentidoTexto}:");

            foreach (var onibus in foundObjects.Take(10))
            {
                var status = GetBusStatusMessage(onibus);
                await bot.SendMessage(chatId, status);

                await Task.Delay(500);
            }

            if (foundObjects.Count > 10)
            {
                await bot.SendMessage(chatId,
                    $"... e mais {foundObjects.Count - 10} ônibus circulando nesta linha!");
            }

            userSubscriptions.Add(new UserSubscription
            {
                ChatId = chatId,
                Linha = linha,
                Sentido = sentido
            });

            var stopKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("❌ Parar notificações", $"stop_{chatId}") }
            });

            await bot.SendMessage(chatId,
                "Você será notificado a cada 2 minutos, deseja parar de receber notificações?",
                replyMarkup: stopKeyboard);
        }

        private static string GetBusStatusMessage(UltimaFeature onibus)
        {
            var props = onibus.Properties;
            var coords = onibus.Geometry.Coordinates;

            var statusEmoji = "🟢";

            var message = $"{statusEmoji} **Ônibus {props.Linha ?? "N/A"}**\n" +
                          $"📍 Linha: {props.Linha}\n" +
                          $"🧭 Sentido: {(props.Sentido == "0" ? "IDA" : "VOLTA")}\n";

            if (coords != null)
            {
                message += $"🗺️ Localização: {coords[1]:F6}, {coords[0]:F6}\n";
            }

            return message;
        }

        private static async Task OnMessage(Message message, UpdateType type, TelegramBotClient bot,
            UltimaPosicao ultimaPosicaoFrota, List<string> linhasDisponiveis)
        {
            if (message.Text == "/start")
            {
                await bot.SendMessage(message.Chat, "Qual linha você quer ser avisado?");
                return;
            }

            if (double.TryParse(message.Text, out var linhaEnviadaPeloUsuario))
            {
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

        private static async Task ProcessDirectionSelection(string msgText)
        {
            var sentidoSelecionado = "";

            if (msgText == "IDA")
                sentidoSelecionado = "0";

            if (msgText == "VOLTA")
                sentidoSelecionado = "1";
        }

        private static async Task<List<string>> GetMatchingLines(double linhaEnviadaPeloUsuario,
            List<string> linhasDisponiveis)
        {
            var res = linhasDisponiveis.Where(x => x.Contains(linhaEnviadaPeloUsuario.ToString())).ToList();
            return res;
        }

        private static async Task<List<UltimaFeature>> ProcessLineSelection(TelegramBotClient bot, Message msg,
            string linhaSelecionada, string sentidoSelecionado,
            UltimaPosicao ultimaPosicaoFrota)
        {
            var foundObjects = ultimaPosicaoFrota.Features.Where(x =>
                    x.Properties.Linha == linhaSelecionada.ToString() &&
                    x.Properties.Sentido == sentidoSelecionado)
                .ToList();

            return foundObjects;
        }

        private static async Task<UltimaPosicao> LoadInitialData(ApiCall apiCall, CleanObjects cleanObjects)
        {
            var ultimasPosicoesFrota = await apiCall.GetUltimaPosicaoFrota();
            ultimasPosicoesFrota = cleanObjects.CleanUltimaPosicaoObject(ultimasPosicoesFrota);
            return ultimasPosicoesFrota;
        }

        private static async Task ProcessComentedCode(ApiCall apiCall)
        {
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

        private static async Task<List<string>> AvailableLines(LinhasDeOnibus linhasDeOnibus)
        {
            var res = new List<string>();

            foreach (var element in linhasDeOnibus.Features)
            {
                res.Add(element.Properties.Linha);
            }

            return res;
        }

        private static string GetBotToken()
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
                            Console.WriteLine($"Token encontrado em: {Path.GetFullPath(path)}");
                            return token;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao ler {path}: {ex.Message}");
                }
            }

            Console.WriteLine("Token não encontrado em nenhum local");
            return "";
        }

        private static async Task EnviarNotificacoesPeriodicas()
        {
            foreach (var subscription in userSubscriptions.ToList())
            {
                try
                {
                    await ProcessAndSendBusStatus(globalBot, subscription.ChatId,
                        subscription.Linha, subscription.Sentido, globalUltimaPosicao);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao enviar notificação para {subscription.ChatId}: {ex.Message}");
                }
            }
        }
    }
}