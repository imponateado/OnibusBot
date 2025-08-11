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

            var ultimaPosicaoFrota = await LoadInitialData(apiCall, cleanObjects);
            var linhasDisponiveis = await AvailableLines(ultimaPosicaoFrota);

            bot.OnMessage += async (msg, type) => await OnMessage(msg, type, bot, ultimaPosicaoFrota, linhasDisponiveis);
            bot.OnUpdate += async (update) => await OnUpdate(bot, update, ultimaPosicaoFrota);

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
            if (update.CallbackQuery != null)
            {
                var linhaSelecionada = update.CallbackQuery.Data;
        
                // Responder o callback primeiro
                await bot.AnswerCallbackQuery(update.CallbackQuery.Id);
        
                // Criar botões para IDA/VOLTA
                var sentidoKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("IDA", $"sentido_{linhaSelecionada}_0"),
                        InlineKeyboardButton.WithCallbackData("VOLTA", $"sentido_{linhaSelecionada}_1")
                    }
                });
        
                await bot.SendMessage(update.CallbackQuery.Message.Chat, 
                    $"Linha {linhaSelecionada} selecionada! Escolha o sentido:", 
                    replyMarkup: sentidoKeyboard);
            }
    
            // Tratar quando o usuário escolher o sentido
            if (update.CallbackQuery?.Data?.StartsWith("sentido_") == true)
            {
                var parts = update.CallbackQuery.Data.Split('_');
                var linha = parts[1];
                var sentido = parts[2];
        
                // Agora você tem linha E sentido!
                var foundObjects = ProcessLineSelection(bot, update.CallbackQuery.Message, linha, sentido, ultimaPosicao);
            }
        }

        private static async Task OnMessage(Message message, UpdateType type, TelegramBotClient bot,
            UltimaPosicao ultimaPosicaoFrota, List<string> linhasDisponiveis)
        {
            if (message.Text == "/start")
            {
                await bot.SendMessage(message.Chat, "Qual linha você quer ser avisado?");
                return;
            }

            if (double.TryParse(message.Text, out var linhaEnviadaPeloUsuario)
            {
                var linhasEncontradas = await GetMatchingLines(linhaEnviadaPeloUsuario, linhasDisponiveis);
                var kbd = new InlineKeyboardMarkup(
                    linhasEncontradas.Select(linha => new[]
                        { InlineKeyboardButton.WithCallbackData(linha, $"{linha}"), })
                );
                await bot.SendMessage(message.Chat, "Selecione a linha", replyMarkup: kbd);
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
            //var linhasDeOnibus = await apiCall.GetLinhasDeOnibus();

            //Console.WriteLine($"Linhas de ônibus API");

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

        private static async Task<List<string>> AvailableLines(UltimaPosicao ultimaPosicao)
        {
            var res = new List<string>();

            foreach (var element in ultimaPosicao.Features)
            {
                res.Add(element.Properties.Linha);
            }

            return res;
        }

        private static string GetBotToken()
        {
            if (File.Exists(".env"))
            {
                var lines = File.ReadAllLines(".env");
                var tokenLine = lines.FirstOrDefault(x => x.StartsWith("BOT_TOKEN="));
                return tokenLine?.Replace("BOT_TOKEN", "").Trim() ?? "";
            }

            return "";
        }
    }
}