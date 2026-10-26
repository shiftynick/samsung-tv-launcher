using System;
using System.IO;

public static class MotionPreference
{
 public static bool Valid(string mode)=>mode=="off"||mode=="subtle"||mode=="full";
 public static string Next(string mode)=>mode=="off"?"subtle":mode=="subtle"?"full":"off";
 public static string Load(string path){try{string value=File.ReadAllText(path).Trim();return Valid(value)?value:"subtle";}catch(IOException){return "subtle";}catch(UnauthorizedAccessException){return "subtle";}}
 public static void Save(string path,string value){
  if(!Valid(value))throw new ArgumentException("Unknown motion mode",nameof(value));
  Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
  string staging=path+".new";
  try{File.WriteAllText(staging,value+"\n");File.Move(staging,path,true);}finally{if(File.Exists(staging))File.Delete(staging);}
 }
 public static int ParticleCount(string mode,int requested)=>mode=="off"?0:Math.Min(mode=="subtle"?6:18,Math.Clamp(requested,0,18));
 public static bool Active(string mode,bool foreground,int page)=>Valid(mode)&&mode!="off"&&foreground&&page==0;
}
