using System.Text.Json.Serialization;

namespace OnibusBot.Interfaces;

public class NominatimResponse
{
    [JsonPropertyName("place_id")]
    public int? PlaceId { get; set; }
    
    [JsonPropertyName("license")]
    public string? License { get; set; }
    
    [JsonPropertyName("osm_type")]
    public string? OsmType { get; set; }
    
    [JsonPropertyName("osm_id")]
    public string? OsmId { get; set; }
    
    [JsonPropertyName("lat")]
    public double? Latitude { get; set; }
    
    [JsonPropertyName("lon")]
    public double? Longitude { get; set; }
    
    [JsonPropertyName("class")]
    public string? Class { get; set; }
    
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("place_rank")]
    public int? PlaceRank { get; set; }
    
    [JsonPropertyName("importance")]
    public float? Importance { get; set; }
    
    [JsonPropertyName("addresstype")]
    public string? AddressType { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
    
    [JsonPropertyName("address")]
    public Address? Address { get; set; }
    
    [JsonPropertyName("boundingbox")]
    public List<double>? BoundingBox { get; set; }
}

public class Address
{
    [JsonPropertyName("road")]
    public string? Road { get; set; }
    
    [JsonPropertyName("neighbourhood")]
    public string? Neighbourhood { get; set; }
    
    [JsonPropertyName("suburb")]
    public string? Suburb { get; set; }
    
    [JsonPropertyName("city")]
    public string? City { get; set; }
    
    [JsonPropertyName("municipality")]
    public string? Municipality { get; set; }
    
    [JsonPropertyName("country")]
    public string? County { get; set; }
    
    [JsonPropertyName("state_district")]
    public string? StateDistrict { get; set; }
    
    [JsonPropertyName("state")]
    public string? State { get; set; }
    
    [JsonPropertyName("ISO3166-2-lvl4")]
    public string? ISO3166_2lvl4 { get; set; }
    
    [JsonPropertyName("region")]
    public string? Region { get; set; }
    
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }
    
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    
    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }
}