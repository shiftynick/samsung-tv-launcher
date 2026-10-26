using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

public sealed class ThemePalette
{
 public string Background {get;set;}="#101918";
 public string Text {get;set;}="#FAF5E6";
 public string Muted {get;set;}="#C7CFC9";
 public string Accent {get;set;}="#FFC247";
 public string Card {get;set;}="#081416";
 public string Control {get;set;}="#081416D9";
 public string Caption {get;set;}="#031016E6";
 public string Overlay {get;set;}="#041018EB";
 public string Header {get;set;}="#00000038";
 public string Footer {get;set;}="#000000B3";
}

// Data-only theme format shared by the TV and host authoring/validation tool.
public sealed class ThemePack
{
 public int SchemaVersion {get;set;}=1;
 public string Id {get;set;}="neutral";
 public string Name {get;set;}="Neutral";
 public int Order {get;set;}
 public string Background {get;set;}
 public string Preview {get;set;}
 public Dictionary<string,string> Icons {get;set;}=new Dictionary<string,string>();
 public ThemePalette Palette {get;set;}=new ThemePalette();
 public int CornerRadius {get;set;}=18;
 public bool PixelArt {get;set;}
 public int AnimationMs {get;set;}=160;
 public string FontFamily {get;set;}="Quiet Inter";
 public bool FontVariants {get;set;}=true;
 public string FontFile {get;set;}
 public string FontLicense {get;set;}
 public string License {get;set;}
 [JsonIgnore] public string RootDirectory {get;private set;}
 readonly Dictionary<string,string> resolvedAssets=new Dictionary<string,string>();
 [JsonIgnore] public string BackgroundPath=>Asset(Background);
 [JsonIgnore] public string PreviewPath=>Asset(Preview??Background);
 [JsonIgnore] public string FontPath=>Asset(FontFile);
 public static readonly JsonSerializerOptions JsonOptions=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase,WriteIndented=true};
 static readonly HashSet<string> Fields=new HashSet<string>{"schemaVersion","id","name","order","background","preview","icons","palette","cornerRadius","pixelArt","animationMs","fontFamily","fontVariants","fontFile","fontLicense","license"};
 static readonly HashSet<string> PaletteFields=new HashSet<string>{"background","text","muted","accent","card","control","caption","overlay","header","footer"};
 public static bool ValidId(string id)=>id!=null&&Regex.IsMatch(id,@"\A[a-z0-9][a-z0-9-]{0,47}\z");
 public string IconPath(string key)=>key!=null&&Icons.TryGetValue(key,out var file)?Asset(file):null;
 public static float[] Color(string value)
 {
  if(value==null||!Regex.IsMatch(value,@"\A#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?\z"))throw new InvalidDataException("Colors must use #RRGGBB or #RRGGBBAA.");
  return new[]{Convert.ToInt32(value.Substring(1,2),16)/255f,Convert.ToInt32(value.Substring(3,2),16)/255f,Convert.ToInt32(value.Substring(5,2),16)/255f,value.Length==9?Convert.ToInt32(value.Substring(7,2),16)/255f:1f};
 }
 static void ObjectFields(JsonElement element,HashSet<string> allowed,string label)
 {
  if(element.ValueKind!=JsonValueKind.Object)throw new InvalidDataException(label+" must be an object.");
  var seen=new HashSet<string>();
  foreach(var p in element.EnumerateObject())if(!seen.Add(p.Name)||allowed!=null&&!allowed.Contains(p.Name))throw new InvalidDataException("Unknown or duplicate "+label+" field: "+p.Name);
 }
 static void NoLink(string path){if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Theme symlinks are not supported.");}
 string Asset(string relative)
 {
  if(relative==null)return null;
  if(resolvedAssets.TryGetValue(relative,out var resolved))return File.Exists(resolved)?resolved:null;
  if(RootDirectory==null||relative.Length==0||relative.Length>240||relative.Contains('\\')||Path.IsPathRooted(relative))throw new InvalidDataException("Assets must have portable relative paths.");
  string path=RootDirectory;
  foreach(string part in relative.Split('/')){
   if(part=="."||part==".."||!Regex.IsMatch(part,@"\A[A-Za-z0-9_. -]+\z"))throw new InvalidDataException("Unsafe asset path.");
   path=Path.Combine(path,part);NoLink(path);
  }
  if(!File.Exists(path))throw new InvalidDataException("Missing asset: "+relative);
  resolvedAssets[relative]=path;
  return path;
 }
 static uint BigEndian(byte[] bytes,int start)=>(uint)bytes[start]<<24|(uint)bytes[start+1]<<16|(uint)bytes[start+2]<<8|bytes[start+3];
 public static long PngPixels(string path)
 {
  if(!string.Equals(Path.GetExtension(path),".png",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Images must be PNG files.");
  using(var stream=File.OpenRead(path)){
   if(stream.Length<57||stream.Length>16*1024*1024)throw new InvalidDataException("PNG size is outside the supported bounds.");
   byte[] head=new byte[33];if(stream.Read(head,0,head.Length)!=head.Length||!head.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})||BigEndian(head,8)!=13||System.Text.Encoding.ASCII.GetString(head,12,4)!="IHDR")throw new InvalidDataException("Invalid PNG header.");
   uint width=BigEndian(head,16),height=BigEndian(head,20);long pixels=(long)width*height;
   if(width==0||height==0||width>3840||height>3840||pixels>8294400)throw new InvalidDataException("PNG dimensions exceed the theme budget.");
   bool imageData=false,end=false;byte[] chunk=new byte[8];int chunks=0;
   while(stream.Position<stream.Length&&++chunks<=4096){
    if(stream.Read(chunk,0,8)!=8)throw new InvalidDataException("Truncated PNG chunk.");
    uint size=BigEndian(chunk,0);string type=System.Text.Encoding.ASCII.GetString(chunk,4,4);
    if(size>stream.Length-stream.Position-4)throw new InvalidDataException("Truncated PNG payload.");
    if(type=="IDAT")imageData=true;
    stream.Seek((long)size+4,SeekOrigin.Current);
    if(type=="IEND"){if(size!=0||stream.Position!=stream.Length)throw new InvalidDataException("Invalid PNG end.");end=true;break;}
   }
   if(!imageData||!end)throw new InvalidDataException("PNG is missing image data or end marker.");
   return pixels;
  }
 }
 public static ThemePack Load(string manifest)
 {
  manifest=Path.GetFullPath(manifest);NoLink(manifest);
  if(new FileInfo(manifest).Length>65536)throw new InvalidDataException("Theme manifest exceeds 64 KiB.");
  string text=File.ReadAllText(manifest);
  using(var document=JsonDocument.Parse(text)){
   ObjectFields(document.RootElement,Fields,"theme");
   foreach(string required in new[]{"schemaVersion","id","name"})if(!document.RootElement.TryGetProperty(required,out _))throw new InvalidDataException("Missing theme field: "+required);
   if(document.RootElement.TryGetProperty("palette",out var palette))ObjectFields(palette,PaletteFields,"palette");
   if(document.RootElement.TryGetProperty("icons",out var icons))ObjectFields(icons,null,"icons");
  }
  var pack=JsonSerializer.Deserialize<ThemePack>(text,JsonOptions);
  pack.RootDirectory=Path.GetDirectoryName(manifest);NoLink(pack.RootDirectory);
  if(pack.SchemaVersion!=1||!ValidId(pack.Id)||string.IsNullOrWhiteSpace(pack.Name)||pack.Name.Length>40||pack.Name.Any(char.IsControl))throw new InvalidDataException("Invalid theme version, ID or name.");
  if(pack.Order<0||pack.Order>1000||pack.CornerRadius<0||pack.CornerRadius>40||pack.AnimationMs<0||pack.AnimationMs>250)throw new InvalidDataException("Invalid style bounds.");
  if(string.IsNullOrWhiteSpace(pack.FontFamily)||pack.FontFamily.Length>64||pack.FontFamily.Any(char.IsControl)||pack.Palette==null||pack.Icons==null||pack.Icons.Count>64)throw new InvalidDataException("Invalid typography, palette or icon map.");
  foreach(var color in new[]{pack.Palette.Background,pack.Palette.Text,pack.Palette.Muted,pack.Palette.Accent,pack.Palette.Card,pack.Palette.Control,pack.Palette.Caption,pack.Palette.Overlay,pack.Palette.Header,pack.Palette.Footer})Color(color);
  long pixelBudget=0,encodedBudget=0;
  var uniqueFiles=new HashSet<string>();
  var images=new List<string>();if(pack.Background!=null)images.Add(pack.Background);if(pack.Preview!=null)images.Add(pack.Preview);
  foreach(var entry in pack.Icons){if(!ValidId(entry.Key)||entry.Value==null)throw new InvalidDataException("Invalid icon key or asset.");images.Add(entry.Value);}
  foreach(string relative in images){string file=pack.Asset(relative);pixelBudget+=PngPixels(file);if(uniqueFiles.Add(file))encodedBudget+=new FileInfo(file).Length;}
  if(pixelBudget>16777216||encodedBudget>64*1024*1024)throw new InvalidDataException("Theme images exceed the decoded/encoded pack budget.");
  if(pack.FontFile!=null){
   string file=pack.Asset(pack.FontFile);
   if(Path.GetExtension(file).ToLowerInvariant()!=".ttf"||new FileInfo(file).Length>2*1024*1024||pack.FontLicense==null)throw new InvalidDataException("Custom fonts require a bounded TTF and a license file.");
   if(new FileInfo(pack.Asset(pack.FontLicense)).Length>65536)throw new InvalidDataException("Font license is too large.");
  }else if(pack.FontLicense!=null&&new FileInfo(pack.Asset(pack.FontLicense)).Length>65536)throw new InvalidDataException("Font license is too large.");
  if(pack.License!=null&&new FileInfo(pack.Asset(pack.License)).Length>65536)throw new InvalidDataException("Theme license is too large.");
  return pack;
 }
 public static ThemePack Neutral()=>new ThemePack();
}

public sealed class ThemeCatalog
{
 readonly List<ThemePack> items=new List<ThemePack>();
 readonly List<string> errors=new List<string>();
 public IReadOnlyList<ThemePack> Items=>items;
 public IReadOnlyList<string> Errors=>errors;
 public ThemePack Find(string id)=>items.Find(p=>p.Id==id);
 public string FallbackId=>"neutral";
 public static ThemeCatalog Discover(params string[] roots)
 {
  var catalog=new ThemeCatalog();
  foreach(string root in roots){
   if(string.IsNullOrEmpty(root)||!Directory.Exists(root))continue;
   string[] directories;
   try{
    if((File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Theme roots cannot be symlinks.");
    directories=Directory.GetDirectories(root);
   }catch(Exception error)when(error is InvalidDataException||error is IOException||error is UnauthorizedAccessException){catalog.errors.Add("Theme directory unavailable: "+error.Message);continue;}
   foreach(string directory in directories.OrderBy(p=>p,StringComparer.Ordinal)){
    string manifest=Path.Combine(directory,"theme.json");if(!File.Exists(manifest))continue;
    try{
     if(catalog.items.Count>=64)throw new InvalidDataException("At most 64 theme packs are supported.");
     ThemePack pack=ThemePack.Load(manifest);
     if(catalog.Find(pack.Id)!=null)throw new InvalidDataException("Duplicate theme ID: "+pack.Id);
     catalog.items.Add(pack);
    }catch(Exception error)when(error is InvalidDataException||error is IOException||error is UnauthorizedAccessException||error is JsonException||error is ArgumentException){catalog.errors.Add(Path.GetFileName(directory)+": "+error.Message);}
   }
  }
  if(catalog.Find("neutral")==null)catalog.items.Add(ThemePack.Neutral());
  catalog.items.Sort((a,b)=>{int c=a.Order.CompareTo(b.Order);return c==0?StringComparer.Ordinal.Compare(a.Name,b.Name):c;});
  return catalog;
 }
}
