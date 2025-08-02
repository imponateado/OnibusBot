namespace OnibusTelegram.Interfaces;

public class HorariosDasLinhasProperties
{
    public int? IdLinha { get; set; }
    public int? IdOperadora { get; set; }
    public string CdLinha { get; set; }
    public string NmOperadora { get; set; }
    public string Sentido { get; set; }
    public TimeSpan? HrPrevista { get; set; }
    public TimeSpan? TempoPercurso { get; set; }
    public string DiasSemana { get; set; }
    public string DiaLabel { get; set; }
    public DateTime? DtInicioVigencia { get; set; }
    public DateTime? DtFinalVigencia { get; set; }
}