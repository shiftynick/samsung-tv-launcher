using System;
using System.IO;
using System.Text.Json;
public static class PhotoCatalogTests
{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Reject(Action action){bool rejected=false;try{action();}catch{rejected=true;}Check(rejected,"Unsafe photo album was accepted");}
 public static void Main(string[] args){
  string root=Path.Combine(Path.GetTempPath(),"photo-tests-"+Guid.NewGuid());Directory.CreateDirectory(Path.Combine(root,"images"));
  try{
   byte[] jpeg=Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAACAAQDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAABAb/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCHALkZ/9k=");
   string photo=Path.Combine(root,"images/photo-0001.jpg"),manifest=Path.Combine(root,"photos.json");File.WriteAllBytes(photo,jpeg);
   string valid="{\"schemaVersion\":1,\"photos\":[{\"file\":\"images/photo-0001.jpg\",\"album\":\"Example\",\"caption\":\"Scenery\"}]}";
   File.WriteAllText(manifest,valid);var catalog=PhotoCatalog.Load(root);Check(catalog.Photos.Count==1&&catalog.Albums.Count==1,"Valid private album rejected");
   for(int count=1;count<=200;count++)for(int index=0;index<count;index++){Check(PhotoCatalog.Move(count,index,1)>=0&&PhotoCatalog.Move(count,index,1)<count,"Next out of bounds");Check(PhotoCatalog.Move(count,index,-1)>=0&&PhotoCatalog.Move(count,index,-1)<count,"Previous out of bounds");}
   foreach(string bad in new[]{"{}",valid.Replace("schemaVersion\":1","schemaVersion\":2"),valid.Replace("images/photo-0001.jpg","../photo.jpg"),valid.Replace("images/photo-0001.jpg","https://example.com/photo.jpg"),valid.Replace("\"caption\":","\"unknown\":"),valid.Replace("Example",""),"{\"schemaVersion\":1,\"photos\":[]}"}){File.WriteAllText(manifest,bad);Reject(()=>PhotoCatalog.Load(root));}
   File.WriteAllText(manifest,valid);File.WriteAllBytes(photo,new byte[]{1,2,3});Reject(()=>PhotoCatalog.Load(root));File.WriteAllBytes(photo,jpeg);
   if(Path.DirectorySeparatorChar=='/'){
    string other=Path.Combine(root,"other.jpg");File.WriteAllBytes(other,jpeg);File.Delete(photo);File.CreateSymbolicLink(photo,other);Reject(()=>PhotoCatalog.Load(root));File.Delete(photo);File.WriteAllBytes(photo,jpeg);
   }
   if(args.Length==1){var installed=PhotoCatalog.Load(args[0]);Check(installed.Photos.Count>0,"No private photos validated");Console.WriteLine("Private album validated: "+installed.Photos.Count+" photos, "+installed.Albums.Count+" albums.");}
   Console.WriteLine("Photo paths, file bounds, malformed manifests and navigation checks passed.");
  }finally{Directory.Delete(root,true);}
 }
}
