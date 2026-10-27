using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;

public sealed class PlaceMatch
{
 public string Name {get;set;}
 public double Latitude {get;set;}
 public double Longitude {get;set;}
}

// Open-Meteo needs no account or key. Requests carry only rounded coordinates
// or the owner's search text; no device or account identifiers.
public static class WeatherClient
{
 const int MaxResponse=65536;
 static readonly HttpClient Http=new HttpClient{Timeout=TimeSpan.FromSeconds(10),MaxResponseContentBufferSize=MaxResponse};
 static string Number(double value)=>value.ToString("0.##",CultureInfo.InvariantCulture);
 static string Get(string url)
 {
  using(var response=Http.GetAsync(url).GetAwaiter().GetResult()){
   if(!response.IsSuccessStatusCode)throw new IOException("The weather service answered "+(int)response.StatusCode+".");
   return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
  }
 }
 public static WeatherReading Forecast(SkySettings s)
 {
  string url="https://api.open-meteo.com/v1/forecast?latitude="+Number(s.Latitude)+"&longitude="+Number(s.Longitude)+"&current=temperature_2m,weather_code"+(s.Units=="f"?"&temperature_unit=fahrenheit":"");
  return WeatherReading.Parse(Get(url),s.Units,DateTime.UtcNow);
 }
 public static List<PlaceMatch> Search(string query)
 {
  query=(query??"").Trim();
  if(query.Length<2||query.Length>SkySettings.MaxPlace)throw new InvalidDataException("Enter a city or postcode.");
  return ParseSearch(Get("https://geocoding-api.open-meteo.com/v1/search?count=5&language=en&format=json&name="+Uri.EscapeDataString(query)));
 }
 public static List<PlaceMatch> ParseSearch(string json)
 {
  var result=new List<PlaceMatch>();
  var root=JsonDocument.Parse(json).RootElement;
  if(!root.TryGetProperty("results",out var rows)||rows.ValueKind!=JsonValueKind.Array)return result;
  foreach(var row in rows.EnumerateArray()){
   if(result.Count==5)break;
   string name=Text(row,"name"),admin=Text(row,"admin1"),country=Text(row,"country_code");
   if(name==""||!row.TryGetProperty("latitude",out var lat)||!row.TryGetProperty("longitude",out var lon))continue;
   string label=name+(admin==""?"":", "+admin)+(country==""?"":", "+country);
   if(label.Length>SkySettings.MaxPlace)label=label.Substring(0,SkySettings.MaxPlace);
   result.Add(new PlaceMatch{Name=label,Latitude=Math.Round(lat.GetDouble(),2),Longitude=Math.Round(lon.GetDouble(),2)});
  }
  return result;
 }
 static string Text(JsonElement row,string name)=>row.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString().Trim():"";
}
