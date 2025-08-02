namespace OnibusBot.Interfaces;

public class GeoJsonResponse<T>
{
    public string Type { get; set; }
    public IEnumerable<FeatureBase<T>> Features { get; set; }
    public int? TotalFeatures { get; set; }
    public int? NumberMatched { get; set; }
    public int? NumberReturned { get; set; }
    public DateTime? TimeStamp { get; set; }
    public Crs Crs { get; set; }
}
