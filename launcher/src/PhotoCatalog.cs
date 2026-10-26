using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

public sealed class LocalPhoto
{
 public string File {get;set;}
 public string Album {get;set;}
 public string Caption {get;set;}="";
 [System.Text.Json.Serialization.JsonIgnore] public string Path {get;set;}
}
public sealed class PhotoCatalog
{
 public IReadOnlyList<LocalPhoto> Photos {get;private set;}
 public IReadOnlyList<string> Albums=>Photos.Select(photo=>photo.Album).Distinct().ToArray();
 static void Fields(JsonElement element,params string[] allowed){
  if(element.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Expected a photo object.");
  var seen=new HashSet<string>();foreach(var field in element.EnumerateObject())if(!allowed.Contains(field.Name)||!seen.Add(field.Name))throw new InvalidDataException("Unknown or duplicate photo field.");
 }
 public static int Move(int count,int index,int delta)=>count==0?0:(int)(((long)index+delta%count+count)%count);
 public static PhotoCatalog Load(string directory){
  directory=System.IO.Path.GetFullPath(directory);
  string manifest=System.IO.Path.Combine(directory,"photos.json");
  if(new FileInfo(manifest).Length>262144||(System.IO.File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0||(System.IO.File.GetAttributes(manifest)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Unsafe photo manifest.");
  using(var doc=JsonDocument.Parse(System.IO.File.ReadAllText(manifest))){
   Fields(doc.RootElement,"schemaVersion","photos");
   if(doc.RootElement.GetProperty("schemaVersion").GetInt32()!=1)throw new InvalidDataException("Unsupported photo version.");
   var rows=doc.RootElement.GetProperty("photos");if(rows.ValueKind!=JsonValueKind.Array||rows.GetArrayLength()>200)throw new InvalidDataException("Photo count is outside bounds.");
   var photos=new List<LocalPhoto>();var files=new HashSet<string>();long bytes=0;
   foreach(var row in rows.EnumerateArray()){
    Fields(row,"file","album","caption");
    string file=row.GetProperty("file").GetString(),album=row.GetProperty("album").GetString();
    string caption=row.TryGetProperty("caption",out var value)?value.GetString():"";
    if(file==null||!Regex.IsMatch(file,@"\Aimages/[a-z0-9-]+\.jpg\z")||!files.Add(file)||string.IsNullOrWhiteSpace(album)||album.Length>100||caption==null||caption.Length>160||(album+caption).Any(char.IsControl))throw new InvalidDataException("Invalid photo entry.");
    string images=System.IO.Path.Combine(directory,"images"),path=System.IO.Path.Combine(directory,file);
    if((System.IO.File.GetAttributes(images)&FileAttributes.ReparsePoint)!=0||(System.IO.File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Photo symlinks are not supported.");
    bytes+=new FileInfo(path).Length;if(bytes>100*1024*1024)throw new InvalidDataException("Photo album exceeds its file budget.");
    ValidateJpeg(path);photos.Add(new LocalPhoto{File=file,Album=album,Caption=caption,Path=path});
   }
   return new PhotoCatalog{Photos=photos};
  }
 }
 internal static void ValidateJpeg(string path){
  using(var stream=System.IO.File.OpenRead(path)){
   if(stream.Length<10||stream.Length>4*1024*1024||stream.ReadByte()!=255||stream.ReadByte()!=216)throw new InvalidDataException("Invalid JPEG.");
   while(stream.Position<Math.Min(stream.Length,1024*1024)){
    if(stream.ReadByte()!=255)throw new InvalidDataException("Invalid JPEG marker.");
    int marker;do{marker=stream.ReadByte();}while(marker==255);
    if(marker<0||marker==218||marker==217)break;
    int size=stream.ReadByte()*256+stream.ReadByte();if(size<2||size>stream.Length-stream.Position+2)break;
    if(marker==192||marker==194){
     if(size<8)break;stream.ReadByte();int h=stream.ReadByte()*256+stream.ReadByte(),w=stream.ReadByte()*256+stream.ReadByte();
     if(w<=h||w>1920||h<1||h>1080)throw new InvalidDataException("Photo dimensions exceed the display budget.");return;
    }
    stream.Seek(size-2,SeekOrigin.Current);
   }
   throw new InvalidDataException("Missing JPEG dimensions.");
  }
 }
}
