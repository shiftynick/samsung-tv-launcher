using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

// Adds and removes photos in the launcher's private album. Every change is
// re-read through PhotoCatalog before it is kept, and rolled back if it fails.
public static class PhotoStore
{
 public const int MaxPhotos=200, MaxFileBytes=4*1024*1024;
 public const long MaxAlbumBytes=100L*1024*1024;
 static readonly object gate=new object();
 public static void CheckText(string album,string caption)
 {
  if(string.IsNullOrWhiteSpace(album)||album.Trim().Length>100||album.Any(char.IsControl))throw new InvalidDataException("Album name must be 1 to 100 characters.");
  if(caption==null||caption.Length>160||caption.Any(char.IsControl))throw new InvalidDataException("Caption must be at most 160 characters.");
 }
 public static string Add(string directory,byte[] jpeg,string album,string caption)
 {
  caption=(caption??"").Trim();CheckText(album,caption);album=album.Trim();
  if(jpeg==null||jpeg.Length<10||jpeg.Length>MaxFileBytes)throw new InvalidDataException("Photo must be a JPEG under 4 MB.");
  lock(gate){
   var rows=Existing(directory);
   if(rows.Count>=MaxPhotos)throw new InvalidDataException("The album already holds "+MaxPhotos+" photos.");
   string images=Path.Combine(directory,"images");Directory.CreateDirectory(images);
   long bytes=rows.Sum(r=>new FileInfo(Path.Combine(directory,r.File)).Length);
   if(bytes+jpeg.Length>MaxAlbumBytes)throw new InvalidDataException("The album is full.");
   string file="images/phone-"+Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant()+".jpg";
   string target=Path.Combine(directory,file),staging=Path.Combine(images,".upload.tmp");
   try{File.WriteAllBytes(staging,jpeg);PhotoCatalog.ValidateJpeg(staging);File.Move(staging,target);}
   finally{if(File.Exists(staging))File.Delete(staging);}
   var next=rows.ToList();next.Add(new LocalPhoto{File=file,Album=album,Caption=caption});
   try{Commit(directory,next);}catch{File.Delete(target);throw;}
   return file;
  }
 }
 public static bool Delete(string directory,string file)
 {
  lock(gate){
   var rows=Existing(directory);var next=rows.Where(r=>r.File!=file).ToList();
   if(next.Count==rows.Count)return false;
   Commit(directory,next);File.Delete(Path.Combine(directory,file));return true;
  }
 }
 public static List<LocalPhoto> Existing(string directory)
 {
  if(!File.Exists(Path.Combine(directory,"photos.json")))return new List<LocalPhoto>();
  return PhotoCatalog.Load(directory).Photos.ToList();
 }
 static void Commit(string directory,List<LocalPhoto> rows)
 {
  string manifest=Path.Combine(directory,"photos.json"),staging=manifest+".new",backup=manifest+".old";
  bool had=File.Exists(manifest);
  try{
   File.WriteAllText(staging,JsonSerializer.Serialize(new{schemaVersion=1,photos=rows.Select(r=>new{file=r.File,album=r.Album,caption=r.Caption})}));
   if(had)File.Copy(manifest,backup,true);
   File.Move(staging,manifest,true);
   try{PhotoCatalog.Load(directory);}
   catch{if(had)File.Move(backup,manifest,true);else File.Delete(manifest);throw;}
  }finally{if(File.Exists(staging))File.Delete(staging);if(File.Exists(backup))File.Delete(backup);}
 }
}
