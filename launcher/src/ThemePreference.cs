using System;
using System.IO;

public static class ThemePreference
{
 public static readonly string[] Ids={"cinema","pixel","fossil","den"};
 public static readonly string[] Names={"Cinematic","8-bit Jungle","Fossil Club","Dino Den"};
 public static bool IsKnown(string id)=>Array.IndexOf(Ids,id)>=0;
 public static string Name(string id){int n=Array.IndexOf(Ids,id);return n<0?Names[0]:Names[n];}
 public static string Load(string path)
 {
  try{string id=File.ReadAllText(path).Trim();return IsKnown(id)?id:Ids[0];}
  catch(IOException){return Ids[0];}
  catch(UnauthorizedAccessException){return Ids[0];}
 }
 public static void Save(string path,string id)
 {
  if(!IsKnown(id))throw new ArgumentException("Unknown theme",nameof(id));
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
  string staging=path+".new";
  try{File.WriteAllText(staging,id+"\n");File.Move(staging,path,true);}
  finally{if(File.Exists(staging))File.Delete(staging);}
 }
}
