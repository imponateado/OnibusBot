using System.Diagnostics;
using System.Globalization;
using OnibusBot.Service;
using OnibusBot.Utils;

namespace OnibusBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();
            var httpHandle = new HttpClientHandler();
            var apiCall = new ApiCall(httpClient, httpHandle);
            //var convertStuff = new ConvertStuff();
            var cleanObjects = new CleanObjects();
            //var latlon = new List<double> { -15.888812, -48.019994 };
            var linhaSelecionada = "871.3";
            var sentidoSelecionado = "1";
            
            
            //Console.WriteLine($"Instanciações");

            try
            {
                //var linhasDeOnibus = await apiCall.GetLinhasDeOnibus();
                
                //Console.WriteLine($"Linhas de ônibus API");
                
                //var paradasDeOnibus = await apiCall.GetParadasDeOnibus();
                
                //Console.WriteLine($"Paradas de Ônibus API");
                
                var ultimaPosicaoFrota = await apiCall.GetUltimaPosicaoFrota();
                
                Console.WriteLine($"Ultima posição API");
                
                //linhas e paradas vem no padrão utm, precisam ser convertidos para wgs84
                //linhasDeOnibus = convertStuff.ConverterLinhasDeOnibusCoords(linhasDeOnibus).Result;
                
                //Console.WriteLine($"Conversão das linhas em UTM");
                
                //paradasDeOnibus = convertStuff.ConverterParadasCoords(paradasDeOnibus).Result;
                
                //Console.WriteLine($"Conversão das últimas posições");

                //remove features não necessárias
                //paradasDeOnibus = cleanObjects.CleanParadasDeOnibusObject(paradasDeOnibus);
                
                //Console.WriteLine($"Limpeza objeto paradas");
                
                ultimaPosicaoFrota = cleanObjects.CleanUltimaPosicaoObject(ultimaPosicaoFrota);
                Console.WriteLine($"Limpeza objeto ultimas");

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

                var foundObjects = ultimaPosicaoFrota.Features.Where(x =>
                    x.Properties.Linha == linhaSelecionada && x.Properties.Sentido == sentidoSelecionado).ToList();

                if (foundObjects.Count > 0)
                {
                    foreach (var element in foundObjects)
                    {
                        Console.WriteLine($"Um ônibus está em curso.");
                    }
                }
                else
                {
                    Console.WriteLine($"Por enquanto nada.");
                }

            }
           catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
