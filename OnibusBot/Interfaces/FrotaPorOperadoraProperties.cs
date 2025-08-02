namespace OnibusBot.Interfaces;

public class FrotaPorOperadoraProperties
{
    public int? IdFrota { get; set; }
    public DateTime? DataReferencia { get; set; }
    public string Servico { get; set; }
    public string Operadora { get; set; }
    public string PlacaVeiculo { get; set; }
    public string NumeroVeiculo { get; set; }
    public string TipoOnibus { get; set; }
    public int? QuantidadePassageirosSentados { get; set; }
    public decimal? AreaUtil { get; set; }
    public int? AnoFabrica { get; set; }
}
