namespace OnibusBot.Interfaces;

public class UserSubscription
{
    public long ChatId { get; set; }
    public string Linha { get; set; }
    public string Sentido { get; set; }
    public bool JaRecebeuPrimeiraMensagem { get; set; } = false;
}