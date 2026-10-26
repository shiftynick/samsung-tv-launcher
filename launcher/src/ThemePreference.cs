using System;
using System.IO;

public static class ThemePreference
{
 public static string Load(string path,ThemeCatalog catalog)
 {
  try{string id=File.ReadAllText(path).Trim();return catalog.Find(id)!=null?id:catalog.FallbackId;}
  catch(IOException){return catalog.FallbackId;}
  catch(UnauthorizedAccessException){return catalog.FallbackId;}
 }
 public static void Save(string path,string id,ThemeCatalog catalog)
 {
  if(catalog.Find(id)==null)throw new ArgumentException("Unknown theme",nameof(id));
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
  string staging=path+".new";
  try{File.WriteAllText(staging,id+"\n");File.Move(staging,path,true);}
  finally{if(File.Exists(staging))File.Delete(staging);}
 }
}
