namespace OnibusTelegram.Interfaces;

public class UltimaPosicaoFrotaProperties
{
    int? IdOperadora { get; set; }
    string Prefixo { get; set; }
    DateTime? DataLocal { get; set; }
    decimal? Velocidade { get; set; }
    string CdLinha { get; set; }
    decimal? Direcao { get; set; }
    decimal? Latitude { get; set; }
    decimal? Longitude { get; set; }
    DateTime? DataRegistro { get; set; }
    string Imei { get; set; }
    string Sentido { get; set; }
}