using System;
using System.IO;
using System.Linq;
using System.Text.Json;
public static class ThemePreferenceTests
{
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch(Exception e)when(e is IOException||e is InvalidDataException||e is ArgumentException||e is JsonException){rejected=true;}Check(rejected,message);}
 static string Pack(string root,string folder,string json){string dir=Path.Combine(root,folder);Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"theme.json"),json);return dir;}
 static string Basic(string id,string name)=>JsonSerializer.Serialize(new{schemaVersion=1,id,name});
 public static void Main(string[] args)
 {
  string root=Path.Combine(Path.GetTempPath(),"theme-tests-"+Guid.NewGuid());Directory.CreateDirectory(root);
  try{
   string packs=Path.Combine(root,"packs"),data=Path.Combine(root,"data","theme.txt");Directory.CreateDirectory(packs);
   Pack(packs,"custom",Basic("my-new-style","My new style"));
   var catalog=ThemeCatalog.Discover(packs);
   Check(catalog.Find("my-new-style")!=null,"New themes must be discovered without editing a C# list");
   Check(catalog.Find("neutral")==null,"Recovery must not be offered beside valid packs");
   Check(ThemePreference.Load(data,catalog)=="my-new-style","Missing preference must choose a valid pack");
   foreach(var pack in catalog.Items){ThemePreference.Save(data,pack.Id,catalog);Check(ThemePreference.Load(data,catalog)==pack.Id,"Preference must survive reload");Check(!File.Exists(data+".new"),"Atomic save left staging file");}
   ThemePreference.Save(data,"my-new-style",catalog);
   Reject(()=>ThemePreference.Save(data,"../escape",catalog),"Unknown theme must not be saved");
   Check(ThemePreference.Load(data,catalog)=="my-new-style","Rejected save overwrote valid selection");
   File.WriteAllText(data,"corrupt");Check(ThemePreference.Load(data,catalog)=="my-new-style","Corrupt preference must recover to a valid pack");
   File.WriteAllText(data,"my-new-style\n");Check(ThemePreference.Load(data,catalog)=="my-new-style","Saved choice permits trailing newline");
   File.Delete(Path.Combine(packs,"custom","theme.json"));catalog=ThemeCatalog.Discover(packs);
   Check(ThemePreference.Load(data,catalog)=="neutral","Removed saved pack must recover without crashing");
   string bad=Pack(root,"bad",Basic("bad","Bad"));string manifest=Path.Combine(bad,"theme.json");
   foreach(string json in new[]{"{}","{\"schemaVersion\":2,\"id\":\"bad\",\"name\":\"Bad\"}","{\"schemaVersion\":1,\"id\":\"bad\",\"name\":\"Bad\",\"unknown\":1}","{\"schemaVersion\":1,\"id\":\"bad\",\"id\":\"again\",\"name\":\"Bad\"}","{\"schemaVersion\":1,\"id\":\"../escape\",\"name\":\"Bad\"}","{\"schemaVersion\":1,\"id\":\"bad\",\"name\":\"Bad\",\"palette\":{\"text\":\"red\"}}","{\"schemaVersion\":1,\"id\":\"bad\",\"name\":\"Bad\",\"cornerRadius\":100}"}){
    File.WriteAllText(manifest,json);Reject(()=>ThemePack.Load(manifest),"Invalid manifest accepted: "+json);
   }
   byte[] png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jX1sAAAAASUVORK5CYII=");
   File.WriteAllBytes(Path.Combine(root,"outside.png"),png);
   foreach(string asset in new[]{"../outside.png","/outside.png","https://example.com/asset.png","nested/../../outside.png","C:\\asset.png"}){
    File.WriteAllText(manifest,JsonSerializer.Serialize(new{schemaVersion=1,id="bad",name="Bad",background=asset}));Reject(()=>ThemePack.Load(manifest),"Unsafe asset accepted");
   }
   File.WriteAllBytes(Path.Combine(bad,"image.png"),png);File.WriteAllText(manifest,"{\"schemaVersion\":1,\"id\":\"bad\",\"name\":\"Bad\",\"background\":\"image.png\"}");
   var loaded=ThemePack.Load(manifest);Check(loaded.BackgroundPath!=null,"Valid PNG rejected");
   File.Delete(Path.Combine(bad,"image.png"));Check(loaded.BackgroundPath==null,"Deleted artwork must fall back safely");
   byte[] oversized=(byte[])png.Clone();oversized[16]=0;oversized[17]=0;oversized[18]=19;oversized[19]=136;File.WriteAllBytes(Path.Combine(bad,"image.png"),oversized);
   Reject(()=>ThemePack.Load(manifest),"Oversized image accepted");
   File.WriteAllBytes(Path.Combine(bad,"image.png"),png.Take(png.Length-8).ToArray());Reject(()=>ThemePack.Load(manifest),"Truncated PNG accepted");
   if(Path.DirectorySeparatorChar=='/'){
    File.Delete(Path.Combine(bad,"image.png"));File.CreateSymbolicLink(Path.Combine(bad,"image.png"),Path.Combine(root,"outside.png"));Reject(()=>ThemePack.Load(manifest),"Symlink asset accepted");File.Delete(Path.Combine(bad,"image.png"));
   }
   File.WriteAllText(Path.Combine(bad,"font-license.txt"),new string('x',65537));
   File.WriteAllText(manifest,"{\"schemaVersion\":1,\"id\":\"bad\",\"name\":\"Bad\",\"fontLicense\":\"font-license.txt\"}");
   Reject(()=>ThemePack.Load(manifest),"Oversized standalone font license accepted");
   foreach(string invalidMotion in new[]{"null","{\"effect\":\"script\"}","{\"count\":100}","{\"seconds\":1}","{\"unexpected\":true}"}){
    File.WriteAllText(manifest,"{\"schemaVersion\":1,\"id\":\"bad\",\"name\":\"Bad\",\"motion\":"+invalidMotion+"}");Reject(()=>ThemePack.Load(manifest),"Unbounded/unknown motion accepted");
   }
   string motionFile=Path.Combine(root,"motion.txt");
   Check(MotionPreference.Load(motionFile)=="subtle","Motion default must be subtle");
   foreach(string mode in new[]{"off","subtle","full"}){MotionPreference.Save(motionFile,mode);Check(MotionPreference.Load(motionFile)==mode,"Motion choice did not persist");Check(!File.Exists(motionFile+".new"),"Motion staging leaked");Check(!MotionPreference.Active(mode,false,0),"Motion must pause behind another app");Check(!MotionPreference.Active(mode,true,3),"Motion must be off in the theme picker");}
   Reject(()=>MotionPreference.Save(motionFile,"invalid"),"Unknown motion saved");Check(MotionPreference.Load(motionFile)=="full","Rejected motion save overwrote preference");
   Check(MotionPreference.Next(MotionPreference.Next(MotionPreference.Next("off")))=="off","Motion cycle must include all choices");
   Check(MotionPreference.ParticleCount("off",18)==0&&MotionPreference.ParticleCount("subtle",18)==6&&MotionPreference.ParticleCount("full",100)==18,"Motion budget incorrect");
   Pack(packs,"cinema",Basic("cinema","Cinematic"));Pack(packs,"custom",Basic("my-new-style","My Style"));catalog=ThemeCatalog.Discover(packs);
   Check(catalog.FallbackId=="cinema","Cinematic must be the bundled fallback");
   string duplicate=Path.Combine(root,"duplicates");Pack(duplicate,"one",Basic("same","One"));Pack(duplicate,"two",Basic("same","Two"));Pack(duplicate,"broken","{broken");
   var duplicates=ThemeCatalog.Discover(duplicate);Check(duplicates.Items.Count(p=>p.Id=="same")==1,"Duplicate theme IDs must be rejected");Check(duplicates.Errors.Count==2,"Bad packs must be reported and isolated");Check(duplicates.Find("neutral")==null,"Bad packs must not add a selectable plain theme beside a valid pack");
   for(int count=1;count<=65;count++)for(int item=0;item<count;item++)foreach(string key in new[]{"Left","Right","Up","Down"}){int next=ThemeNavigation.Move(count,item,key);Check(next>=-1&&next<count,"Picker navigation out of range");}
   Check(ThemeNavigation.Move(5,3,"Down")==4,"Partial last picker page must clamp column");Check(ThemeNavigation.Move(5,4,"Up")==2,"Picker must return to previous page");
   if(args.Length==1){var bundled=ThemeCatalog.Discover(args[0]);Check(bundled.Errors.Count==0,string.Join(";",bundled.Errors));Check(bundled.Items.Count>=4,"Bundled manifest packs were not found");Check(bundled.Find("neutral")==null,"Plain theme must not ship");}
   Console.WriteLine("Theme packs, motion bounds/lifecycle policy, preferences and picker pagination: "+checks+" checks passed.");
  }finally{Directory.Delete(root,true);}
 }
}
