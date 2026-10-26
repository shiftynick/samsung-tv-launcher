using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

// Change only the Home operation and four recorded startup preferences.
// Native value setters preserve the existing Buxton access privileges.
public static class HomeRoute
{
 const string Operation="http://tizen.org/appcontrol/operation/main";
 const string HomeKey="db/appfw/home_appid", StartupKey="db/appfw/startup_appid";
 const string BackupKey="db/appfw/booting_appid_backup", AutoKey="db/menu/smart_hub/ontvsetting/auto_start";
 [DllImport("libcapi-appfw-app-operation-tv.so.0")] static extern int app_operation_get_current_app(string op,out IntPtr app);
 [DllImport("libcapi-appfw-app-operation-tv.so.0")] static extern int app_operation_set_current_app(string op,string app);
 [DllImport("libvconf.so.0")] static extern IntPtr vconf_get_str(string key);
 [DllImport("libvconf.so.0")] static extern int vconf_set_str(string key,string value);
 [DllImport("libvconf.so.0")] static extern int vconf_get_int(string key,out int value);
 [DllImport("libvconf.so.0")] static extern int vconf_set_int(string key,int value);
 [DllImport("libc.so.6")] static extern void free(IntPtr pointer);
 public sealed class State
 {
  public string OperationApp {get;set;}
  public string Home {get;set;}
  public string Startup {get;set;}
  public string BootBackup {get;set;}
  public int AutoStart {get;set;}
 }
 static string Take(IntPtr value){if(value==IntPtr.Zero)throw new InvalidOperationException("Native getter returned null");try{return Marshal.PtrToStringAnsi(value);}finally{free(value);}}
 static State Read()
 {
  IntPtr app;if(app_operation_get_current_app(Operation,out app)!=0)throw new InvalidOperationException("Cannot read Home operation");
  string op=Take(app);int auto;if(vconf_get_int(AutoKey,out auto)!=0)throw new InvalidOperationException("Cannot read autostart");
  return new State{OperationApp=op,Home=Take(vconf_get_str(HomeKey)),Startup=Take(vconf_get_str(StartupKey)),BootBackup=Take(vconf_get_str(BackupKey)),AutoStart=auto};
 }
 static bool Same(State a,State b)=>a.OperationApp==b.OperationApp&&a.Home==b.Home&&a.Startup==b.Startup&&a.BootBackup==b.BootBackup&&a.AutoStart==b.AutoStart;
 static State Desired(string app)=>new State{OperationApp=app,Home=app,Startup=app,BootBackup=app,AutoStart=1};
 static void SetString(string key,string value){if(vconf_set_str(key,value)!=0||Take(vconf_get_str(key))!=value)throw new InvalidOperationException("Value/readback failed: "+key);}
 static void Set(State value)
 {
  if(app_operation_set_current_app(Operation,value.OperationApp)!=0)throw new InvalidOperationException("Home operation setter failed");
  SetString(HomeKey,value.Home);SetString(StartupKey,value.Startup);SetString(BackupKey,value.BootBackup);
  if(vconf_set_int(AutoKey,value.AutoStart)!=0||!Same(Read(),value))throw new InvalidOperationException("Home routing readback failed");
 }
 public static int Main(string[] args)
 {
  if(IntPtr.Size!=4){Console.Error.WriteLine("This helper requires the inventoried 32-bit TV runtime.");return 2;}
  try{
   if(args.Length==1&&args[0]=="query"){Console.WriteLine(JsonSerializer.Serialize(Read()));return 0;}
   if(args.Length==2&&args[0]=="snapshot"){
    using(var stream=new FileStream(args[1],FileMode.CreateNew,FileAccess.Write))JsonSerializer.Serialize(stream,Read(),new JsonSerializerOptions{WriteIndented=true});
    Console.WriteLine("Original Home routing saved.");return 0;
   }
   if(args.Length!=3||(args[0]!="apply"&&args[0]!="restore")||!System.Text.RegularExpressions.Regex.IsMatch(args[2],@"\A[A-Za-z0-9_.-]+\z"))return 2;
   State saved=JsonSerializer.Deserialize<State>(File.ReadAllText(args[1]));
   if(saved==null||string.IsNullOrEmpty(saved.OperationApp)||string.IsNullOrEmpty(saved.Home)||string.IsNullOrEmpty(saved.Startup)||string.IsNullOrEmpty(saved.BootBackup))throw new InvalidDataException("Incomplete routing snapshot");
   State current=Read(), desired=Desired(args[2]);
   State before=args[0]=="apply"?saved:desired, after=args[0]=="apply"?desired:saved;
   if(Same(current,after)){Console.WriteLine("Already selected; readback verified.");return 0;}
   if(!Same(current,before))throw new InvalidOperationException("Routing changed since the snapshot; inspect before proceeding.");
   try{Set(after);}catch{Set(before);throw;}
   Console.WriteLine(JsonSerializer.Serialize(Read()));return 0;
  }catch(Exception error){Console.Error.WriteLine(error.Message);return 1;}
 }
}
