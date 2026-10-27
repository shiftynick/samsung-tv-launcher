using System;
using System.IO;
using System.Text.Json;

// Owner settings for reactive scenes, saved in the launcher's data directory.
// The location never leaves the TV except as rounded coordinates sent to the
// weather service.
public sealed class SkySettings
{
 public const int MaxPlace=60;
 public bool Enabled {get;set;}=true;
 public bool Weather {get;set;}
 public double Latitude {get;set;}
 public double Longitude {get;set;}
 public string Place {get;set;}="";
 public string Units {get;set;}="f";
 public bool HasLocation=>Place!="";

 public static void Validate(SkySettings s)
 {
  if(s==null||s.Place==null||s.Units==null)throw new InvalidDataException("Scene settings are incomplete.");
  if(s.Units!="f"&&s.Units!="c")throw new InvalidDataException("Units must be f or c.");
  if(s.Place.Length>MaxPlace||Has(s.Place,char.IsControl))throw new InvalidDataException("Place name is too long.");
  if(double.IsNaN(s.Latitude)||double.IsNaN(s.Longitude)||Math.Abs(s.Latitude)>90||Math.Abs(s.Longitude)>180)throw new InvalidDataException("Location is out of range.");
  if(s.Weather&&!s.HasLocation)throw new InvalidDataException("Choose a location before turning on weather.");
  // Two decimals is about a kilometre: enough for weather, not a street address.
  s.Latitude=Math.Round(s.Latitude,2);s.Longitude=Math.Round(s.Longitude,2);s.Place=s.Place.Trim();
 }
 static bool Has(string text,Func<char,bool> test){foreach(char c in text)if(test(c))return true;return false;}
 public static SkySettings Load(string path)
 {
  try{
   if(!File.Exists(path)||new FileInfo(path).Length>4096)return new SkySettings();
   var s=JsonSerializer.Deserialize<SkySettings>(File.ReadAllText(path));Validate(s);return s;
  }catch(Exception e)when(e is JsonException||e is IOException||e is UnauthorizedAccessException||e is InvalidDataException||e is NotSupportedException){return new SkySettings();}
 }
 public static void Save(string path,SkySettings s)
 {
  Validate(s);
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
  string staging=path+".new";
  try{File.WriteAllText(staging,JsonSerializer.Serialize(s));File.Move(staging,path,true);}
  finally{if(File.Exists(staging))File.Delete(staging);}
 }
}

// The last weather reading, cached so a restart does not need the network.
public sealed class WeatherReading
{
 public DateTime FetchedUtc {get;set;}
 public double Temperature {get;set;}
 public int Code {get;set;}
 public string Units {get;set;}="f";
 public bool Fresh(DateTime nowUtc)=>nowUtc-FetchedUtc<TimeSpan.FromHours(3)&&FetchedUtc<=nowUtc.AddMinutes(5);
 public static WeatherReading Load(string path)
 {
  try{
   if(!File.Exists(path)||new FileInfo(path).Length>4096)return null;
   var r=JsonSerializer.Deserialize<WeatherReading>(File.ReadAllText(path));
   return r!=null&&Sky.KnownCode(r.Code)&&(r.Units=="f"||r.Units=="c")&&Math.Abs(r.Temperature)<200?r:null;
  }catch(Exception e)when(e is JsonException||e is IOException||e is UnauthorizedAccessException||e is NotSupportedException){return null;}
 }
 public static void Save(string path,WeatherReading r)
 {
  string staging=path+".new";
  try{File.WriteAllText(staging,JsonSerializer.Serialize(r));File.Move(staging,path,true);}
  finally{if(File.Exists(staging))File.Delete(staging);}
 }
 // Reads an Open-Meteo forecast response with a "current" block.
 public static WeatherReading Parse(string json,string units,DateTime nowUtc)
 {
  var current=JsonDocument.Parse(json).RootElement.GetProperty("current");
  int code=current.GetProperty("weather_code").GetInt32();double t=current.GetProperty("temperature_2m").GetDouble();
  if(!Sky.KnownCode(code)||Math.Abs(t)>=200)throw new InvalidDataException("Unexpected weather reading.");
  return new WeatherReading{FetchedUtc=nowUtc,Temperature=t,Code=code,Units=units};
 }
}

// What the scene should show right now. Everything here is pure so it can be
// tested on the host.
public readonly struct SkyState
{
 public readonly string Phase,Weather,Season;
 public SkyState(string phase,string weather,string season){Phase=phase;Weather=weather;Season=season;}
 public string Key=>Phase+"/"+Weather+"/"+Season;
}

public static class Sky
{
 public static readonly string[] Phases={"dawn","day","dusk","night"};
 public static readonly string[] Weathers={"clear","cloudy","fog","rain","snow","storm"};
 public static readonly string[] Seasons={"spring","summer","autumn","winter"};

 public static bool KnownCode(int code)=>Array.IndexOf(new[]{0,1,2,3,45,48,51,53,55,56,57,61,63,65,66,67,71,73,75,77,80,81,82,85,86,95,96,99},code)>=0;
 // WMO weather interpretation codes, as used by Open-Meteo.
 public static string WeatherKind(int code)=>code<=1?"clear":code<=3?"cloudy":code<=48?"fog":code>=95?"storm":code>=71&&code<=77||code==85||code==86?"snow":"rain";
 public static string Describe(int code)=>code switch{
  0=>"Clear",1=>"Mostly clear",2=>"Partly cloudy",3=>"Overcast",45 or 48=>"Fog",
  51 or 53 or 55=>"Drizzle",56 or 57=>"Freezing drizzle",61=>"Light rain",63=>"Rain",65=>"Heavy rain",66 or 67=>"Freezing rain",
  71=>"Light snow",73=>"Snow",75=>"Heavy snow",77=>"Snow grains",80 or 81=>"Showers",82=>"Heavy showers",85 or 86=>"Snow showers",
  _=>"Thunderstorm"};

 public static string Season(DateTime local,double latitude)
 {
  int m=local.Month;string north=m>=3&&m<=5?"spring":m>=6&&m<=8?"summer":m>=9&&m<=11?"autumn":"winter";
  if(latitude>=0)return north;
  return north=="spring"?"autumn":north=="summer"?"winter":north=="autumn"?"spring":"summer";
 }

 // Solar elevation in degrees (NOAA approximation, good to about a degree).
 public static double SunElevation(DateTime utc,double latitude,double longitude)
 {
  double day=utc.DayOfYear-1+(utc.Hour-12+utc.Minute/60.0+utc.Second/3600.0)/24;
  double g=2*Math.PI/(DateTime.IsLeapYear(utc.Year)?366:365)*day;
  double eqTime=229.18*(0.000075+0.001868*Math.Cos(g)-0.032077*Math.Sin(g)-0.014615*Math.Cos(2*g)-0.040849*Math.Sin(2*g));
  double decl=0.006918-0.399912*Math.Cos(g)+0.070257*Math.Sin(g)-0.006758*Math.Cos(2*g)+0.000907*Math.Sin(2*g)-0.002697*Math.Cos(3*g)+0.00148*Math.Sin(3*g);
  double minutes=utc.Hour*60+utc.Minute+utc.Second/60.0+eqTime+4*longitude;
  double hourAngle=(minutes/4-180)*Math.PI/180,lat=latitude*Math.PI/180;
  double cosZenith=Math.Sin(lat)*Math.Sin(decl)+Math.Cos(lat)*Math.Cos(decl)*Math.Cos(hourAngle);
  return 90-Math.Acos(Math.Clamp(cosZenith,-1,1))*180/Math.PI;
 }
 // With a location the sun decides; without one, the clock does.
 public static string Phase(DateTime utc,DateTime local,SkySettings s)
 {
  if(s.HasLocation){
   double e=SunElevation(utc,s.Latitude,s.Longitude);
   if(e>=8)return "day";
   if(e<-8)return "night";
   double solarHours=(utc.Hour*60+utc.Minute+4*s.Longitude)/60.0;
   solarHours=((solarHours%24)+24)%24;
   return solarHours<12?"dawn":"dusk";
  }
  int h=local.Hour;
  return h>=6&&h<8?"dawn":h>=8&&h<18?"day":h>=18&&h<20?"dusk":"night";
 }
 public static SkyState Current(DateTime utc,DateTime local,SkySettings s,WeatherReading reading)
 {
  string weather=s.Weather&&reading!=null&&reading.Fresh(utc)?WeatherKind(reading.Code):"clear";
  return new SkyState(Phase(utc,local,s),weather,Season(local,s.HasLocation?s.Latitude:1));
 }
 public static string Temperature(WeatherReading r)=>Math.Round(r.Temperature).ToString("0")+"°"+(r.Units=="c"?"C":"F");
}
