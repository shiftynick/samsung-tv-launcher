using System;
using System.IO;
public static class ThemePreferenceTests {
 static void Check(bool ok,string what){if(!ok)throw new Exception(what);}
 public static void Main(){string dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"quiet-theme-tests-"+Guid.NewGuid());string path=System.IO.Path.Combine(dir,"data","theme.txt");
 try{
  Check(ThemePreference.Load(path)=="cinema","Missing preference must use cinematic default");
  foreach(string id in ThemePreference.Ids){ThemePreference.Save(path,id);Check(ThemePreference.Load(path)==id,"Theme must survive reload: "+id);Check(!File.Exists(path+".new"),"Atomic save left temporary file");}
  bool rejected=false;try{ThemePreference.Save(path,"../other");}catch(ArgumentException){rejected=true;}Check(rejected,"Unknown theme accepted");Check(ThemePreference.Load(path)=="den","Rejected theme overwrote selection");
  File.WriteAllText(path,"invalid");Check(ThemePreference.Load(path)=="cinema","Corrupt preference must use safe default");
  File.WriteAllText(path,"pixel\n");Check(ThemePreference.Load(path)=="pixel","Persisted theme must permit trailing newline");
  Check(ThemePreference.Name("fossil")=="Fossil Club","Display label mismatch");
  Console.WriteLine("Theme persistence: all four themes round-trip; invalid values rejected without overwriting saved selection; corrupt and missing preferences recover safely.");
 }finally{if(Directory.Exists(dir))Directory.Delete(dir,true);}}
}
