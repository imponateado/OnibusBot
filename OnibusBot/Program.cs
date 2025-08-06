using System.Diagnostics;
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
            var convertStuff = new ConvertStuff();
            var cleanObjects = new CleanObjects();
            var latlon = new List<double> { -15.888812, -48.019994 };
            
            
            Console.WriteLine($"Instanciações");

            try
            {
                var linhasDeOnibus = await apiCall.GetLinhasDeOnibus();
                
                Console.WriteLine($"Linhas de ônibus API");
                
                var paradasDeOnibus = await apiCall.GetParadasDeOnibus();
                
                Console.WriteLine($"Paradas de Ônibus API");
                
                var ultimaPosicaoFrota = await apiCall.GetUltimaPosicaoFrota();
                
                Console.WriteLine($"Ultima posição API");
                
                //linhas e paradas vem no padrão utm, precisam ser convertidos para wgs84
                linhasDeOnibus = convertStuff.ConverterLinhasDeOnibusCoords(linhasDeOnibus).Result;
                
                Console.WriteLine($"Conversão das linhas em UTM");
                
                paradasDeOnibus = convertStuff.ConverterParadasCoords(paradasDeOnibus).Result;
                
                Console.WriteLine($"Conversão das últimas posições");

                //remove features não necessárias
                paradasDeOnibus = cleanObjects.CleanParadasDeOnibusObject(paradasDeOnibus);
                
                Console.WriteLine($"Limpeza objeto paradas");
                
                ultimaPosicaoFrota = cleanObjects.CleanUltimaPosicaoObject(ultimaPosicaoFrota);
                
                Console.WriteLine($"Limpeza objeto linhas");
                
                //encontra as 3 paradas mais próximas do usuário
                var closestBusStops = Manage.GetClosestBusStop(latlon, paradasDeOnibus);
                
                Console.WriteLine($"Paradas de ônibus mais próximas encontradas");
                
                var linesByBusStopCoord = Manage.GetLinesByBusStopCoordParallel(paradasDeOnibus, linhasDeOnibus);
                
                Console.WriteLine($"Linhas de ônibus para as paradas encontradas");

                if (linesByBusStopCoord.Any())
                {
                    var primeiraParada = linesByBusStopCoord.First();
                    var parada = primeiraParada.Key;
                    var primeiraLinha = primeiraParada.Value.First();
                    
                    Console.WriteLine($"A primeira parada encontrada foi {parada.Properties.Descricao} e a primeira linha encontrada dessa parada foi {primeiraLinha.Properties.Nome}!");
                }
                else
                {
                    Console.WriteLine($"Nada foi encontrado!");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
