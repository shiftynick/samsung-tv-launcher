using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

// Owner tile order, hidden Home tiles and the HDMI shortcut, saved in the
// launcher's data directory. The bundled apps.json stays the source of app IDs.
public sealed class LauncherLayout
{
 public const int HomeSlots=6, MaxLabel=16;
 public static readonly int[] HdmiSources={13,14,15}; // HDMI 1-3
 public List<string> Order {get;set;}=new List<string>();
 public List<string> Hidden {get;set;}=new List<string>();
 public string HdmiLabel {get;set;}="HDMI 3";
 public int HdmiSource {get;set;}=15;

 public static string HdmiName(int source)=>"HDMI "+(source-12);
 public List<LibraryApp> Ordered(List<LibraryApp> library)
 {
  var result=new List<LibraryApp>();
  foreach(string id in Order){var app=library.Find(a=>a.Id==id);if(app!=null&&!result.Contains(app))result.Add(app);}
  foreach(var app in library)if(!result.Contains(app))result.Add(app);
  return result;
 }
 public List<LibraryApp> Home(List<LibraryApp> library)=>Ordered(library).Where(a=>!Hidden.Contains(a.Id)).Take(HomeSlots).ToList();

 // Throws for anything the phone should not be able to save.
 public static void Validate(LauncherLayout layout,List<LibraryApp> library)
 {
  if(layout==null||layout.Order==null||layout.Hidden==null)throw new InvalidDataException("Layout is incomplete.");
  var ids=new HashSet<string>(library.Select(a=>a.Id));
  if(layout.Order.Count>ids.Count||layout.Order.Distinct().Count()!=layout.Order.Count||!layout.Order.All(ids.Contains))throw new InvalidDataException("Unknown or repeated app in order.");
  if(layout.Hidden.Distinct().Count()!=layout.Hidden.Count||!layout.Hidden.All(ids.Contains))throw new InvalidDataException("Unknown or repeated hidden app.");
  if(layout.Hidden.Count>=ids.Count)throw new InvalidDataException("Keep at least one app on Home.");
  if(!HdmiSources.Contains(layout.HdmiSource))throw new InvalidDataException("Unknown HDMI input.");
  if(string.IsNullOrWhiteSpace(layout.HdmiLabel)||layout.HdmiLabel.Trim().Length>MaxLabel||layout.HdmiLabel.Any(char.IsControl))throw new InvalidDataException("HDMI name must be 1 to "+MaxLabel+" characters.");
  layout.HdmiLabel=layout.HdmiLabel.Trim();
 }
 public static LauncherLayout Load(string path,List<LibraryApp> library)
 {
  try{
   if(!File.Exists(path)||new FileInfo(path).Length>16384)return new LauncherLayout();
   var layout=JsonSerializer.Deserialize<LauncherLayout>(File.ReadAllText(path));
   Validate(layout,library);return layout;
  }catch(Exception e)when(e is JsonException||e is IOException||e is UnauthorizedAccessException||e is InvalidDataException||e is NotSupportedException){return new LauncherLayout();}
 }
 public static void Save(string path,LauncherLayout layout,List<LibraryApp> library)
 {
  Validate(layout,library);
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
  string staging=path+".new";
  try{File.WriteAllText(staging,JsonSerializer.Serialize(layout));File.Move(staging,path,true);}
  finally{if(File.Exists(staging))File.Delete(staging);}
 }
}
