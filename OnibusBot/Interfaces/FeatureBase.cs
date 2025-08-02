namespace OnibusBot.Interfaces;

public class FeatureBase<T>
{
    public string Type { get; set; }
    public object Id { get; set; }
    public Geometry Geometry { get; set; }
    public string GeometryName { get; set; }
    public T Properties { get; set; }
}
