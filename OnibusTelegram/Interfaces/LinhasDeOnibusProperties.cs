namespace OnibusTelegram.Interfaces;

public class LinhasDeOnibusProperties
{
    public int? Id { get; set; }
    public string Linha { get; set; }
    public string Nome { get; set; }
    public string Sentido { get; set; }
    public string FaixaTarifaria { get; set; }
    public decimal? Tarifa { get; set; }
    public string Situacao { get; set; }
    public string Bacia { get; set; }
    public string TipoLinha { get; set; }
    public string SituacaoDaLinha { get; set; }
}