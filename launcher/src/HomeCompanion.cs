using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

// The Phone screen and the launcher side of the companion API.
public sealed partial class QuietHome : ICompanionHost
{
 List<LibraryApp> bundledLibrary=new List<LibraryApp>(),homeApps=new List<LibraryApp>();
 LauncherLayout layout=new LauncherLayout();
 string layoutPath="",companionPath="",companionPhonesPath="";
 bool companionEnabled;
 CompanionAccess companionAccess;
 CompanionServer companionServer;
 SynchronizationContext uiContext;
 TextLabel companionCode,companionStatus;
 View companionQr;
 string companionUrl="";

 void LoadCompanion(string data)
 {
  uiContext=SynchronizationContext.Current;
  layoutPath=System.IO.Path.Combine(data,"launcher-layout.json");
  companionPath=System.IO.Path.Combine(data,"launcher-companion.txt");
  companionPhonesPath=System.IO.Path.Combine(data,"companion-phones.json");
  bundledLibrary=library;ApplyLayout(LauncherLayout.Load(layoutPath,bundledLibrary));
  companionAccess=new CompanionAccess(companionPhonesPath);
  try{companionEnabled=File.ReadAllText(companionPath).Trim()=="on";}catch(IOException){}catch(UnauthorizedAccessException){}
  if(companionEnabled)StartCompanion();
 }
 void ApplyLayout(LauncherLayout next){layout=next;library=layout.Ordered(bundledLibrary);homeApps=layout.Home(bundledLibrary);}
 void StartCompanion(){companionServer??=new CompanionServer(companionAccess,this);companionServer.Start();}
 void StopCompanion(){companionServer?.Dispose();companionServer=null;companionAccess.ClearCode();}
 protected override void OnTerminate(){StopCompanion();base.OnTerminate();}

 void OpenCompanion(){page=5;area=1;selected=0;editing="";if(companionEnabled)companionAccess.NewCode();BuildPage();WriteState("companion_opened");}
 void BuildCompanion()
 {
  Text("Phone",80,150,1600,52,Pixel?23:36,ink,"Semibold");
  if(!companionEnabled){
   Text("Set up this launcher from your phone: themes, tile order, the HDMI shortcut and photos.",80,260,1700,50,Pixel?17:28,ink);
   Text("Turn on phone access to show an address and a pairing code. Only phones on your home network can connect.",80,320,1700,50,Pixel?15:24,muted);
  }else{
   Text("On a phone connected to the same Wi-Fi, open",80,250,1700,44,Pixel?16:26,muted);
   companionUrl=CompanionUrl();
   Text(companionUrl,80,300,1300,90,Pixel?30:56,ink,"Semibold");
   companionQr=Panel(1430,240,370,370,white,12);
   var scan=Text("Or scan with the phone camera",1380,622,470,36,Pixel?13:21,muted);scan.HorizontalAlignment=HorizontalAlignment.Center;
   Text("Then enter this code",80,430,1700,44,Pixel?16:26,muted);
   companionCode=Text("",80,480,1700,140,Pixel?52:110,accent,"Semibold");
   companionStatus=Text("",80,640,1280,44,Pixel?15:24,muted);
   UpdateCompanionText();
  }
  HomeTile("Phone access: "+(companionEnabled?"On":"Off"),"companion-toggle",null,80,760,380,72,0);
  if(companionEnabled){
   HomeTile("New code","companion-code",null,490,760,260,72,0);
   HomeTile("Forget phones","companion-forget",null,780,760,300,72,0);
  }
 }
 void UpdateCompanionText()
 {
  if(companionCode==null)return;
  string code=companionAccess.Code;
  companionCode.Text=code==null?"------":code.Substring(0,3)+" "+code.Substring(3);
  DrawQr(code==null?companionUrl:companionUrl+"/#"+code);
  int phones=companionAccess.PhoneCount;
  string status=companionServer==null||!companionServer.Running?"Phone access could not start"+(companionServer==null||companionServer.Error==""?"":": "+companionServer.Error):
   companionAccess.Locked?"Too many wrong codes. Choose New code.":
   code==null?"Code used. Choose New code to pair another phone.":
   "The code works once and only while this screen is open.";
  if(phones>0)status+="   ·   "+phones+(phones==1?" phone paired":" phones paired")+(companionAccess.LastPaired==""?"":" (latest: "+companionAccess.LastPaired+")");
  companionStatus.Text=status;
 }
 // The QR code carries the pairing code in the URL fragment, which the phone
 // page reads locally; fragments are never sent to the server.
 void DrawQr(string text)
 {
  if(companionQr==null)return;
  while(companionQr.ChildCount>0){var child=companionQr.GetChildAt(0);companionQr.Remove(child);child.Dispose();}
  bool[,] modules;try{modules=QrCode.Encode(text);}catch(ArgumentException){return;}
  const int module=10,quiet=4;var dark=new Color(0,0,0,1);
  for(int r=0;r<QrCode.Size;r++)for(int c=0;c<QrCode.Size;){
   if(!modules[r,c]){c++;continue;}
   int start=c;while(c<QrCode.Size&&modules[r,c])c++;
   companionQr.Add(new View{Position2D=new Position2D((quiet+start)*module,(quiet+r)*module),Size2D=new Size2D((c-start)*module,module),BackgroundColor=dark});
  }
 }
 bool CompanionAction(string a)
 {
  if(a=="companion"){OpenCompanion();return true;}
  if(a=="companion-toggle"){
   bool next=!companionEnabled;
   try{File.WriteAllText(companionPath,next?"on\n":"off\n");}catch(Exception){ShowError("Phone access could not be saved.");return true;}
   companionEnabled=next;if(next){StartCompanion();companionAccess.NewCode();}else StopCompanion();
   BuildPage();WriteState("companion_"+(next?"on":"off"));return true;
  }
  if(a=="companion-code"){companionAccess.NewCode();UpdateCompanionText();WriteState("companion_code");return true;}
  if(a=="companion-forget"){companionAccess.ForgetAll();UpdateCompanionText();ShowError("Paired phones were forgotten. Pair again to use the phone page.");WriteState("companion_forget");return true;}
  return false;
 }
 bool HandleCompanionKey(string key)
 {
  if(key=="XF86Back"||key=="Back"||key=="Escape"||key=="XF86Exit"){Home("companion_back");return true;}
  if(key=="Left"||key=="Right"){selected=(selected+(key=="Left"?-1:1)+slots.Count)%slots.Count;UpdateFocus();return true;}
  if(key=="Return"||key=="KP_Enter"){Activate();return true;}
  return key=="Up"||key=="Down";
 }
 string CompanionUrl()
 {
  string address="this TV's address";
  try{
   // Connecting a UDP socket only selects a route; no packet is sent.
   using(var probe=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp)){probe.Connect("192.0.2.1",9);address=((IPEndPoint)probe.LocalEndPoint).Address.ToString();}
  }catch(SocketException){}
  return "http://"+address+":"+CompanionServer.DefaultPort;
 }

 // ICompanionHost: called on server worker threads; launcher state is touched only on the UI thread.
 T OnUi<T>(Func<T> work)
 {
  if(uiContext==null)throw new InvalidOperationException("Launcher is not ready.");
  T result=default;Exception error=null;
  using(var done=new ManualResetEventSlim()){
   uiContext.Post(_=>{try{result=work();}catch(Exception e){error=e;}finally{done.Set();}},null);
   if(!done.Wait(5000))throw new TimeoutException("Launcher did not respond.");
  }
  if(error!=null)throw new InvalidOperationException(error.Message,error);
  return result;
 }
 string ICompanionHost.PhotosDirectory=>photosDirectory;
 string ICompanionHost.PageFile=>Root+"companion/index.html";
 object ICompanionHost.State()=>OnUi(()=>(object)new{
  theme,motion,
  themes=themes.Items.Select(t=>new{id=t.Id,name=t.Name,preview=t.PreviewPath!=null}).ToArray(),
  apps=library.Select(a=>new{id=a.Id,name=a.Name,hidden=layout.Hidden.Contains(a.Id)}).ToArray(),
  homeSlots=LauncherLayout.HomeSlots,
  hdmiLabel=layout.HdmiLabel,hdmiSource=layout.HdmiSource,
  hdmiInputs=LauncherLayout.HdmiSources.Select(s=>new{source=s,name=LauncherLayout.HdmiName(s)}).ToArray()});
 string ICompanionHost.SetTheme(string id)=>OnUi(()=>{
  if(themes.Find(id)==null)return "That theme is not installed.";
  SetTheme(id);return theme==id?null:"The theme could not be saved.";});
 string ICompanionHost.SetMotion(string mode)=>OnUi(()=>{
  if(!MotionPreference.Valid(mode))return "Unknown motion setting.";
  SetMotion(mode);return motion==mode?null:"The motion setting could not be saved.";});
 string ICompanionHost.SetLayout(LauncherLayout next)=>OnUi(()=>{
  try{LauncherLayout.Save(layoutPath,next,bundledLibrary);}catch(InvalidDataException e){return e.Message;}catch(IOException){return "The layout could not be saved.";}
  ApplyLayout(next);if(page==0||page==1){selected=0;BuildPage();}WriteState("layout_changed");return null;});
 string ICompanionHost.ThemePreview(string id)=>OnUi(()=>themes.Find(id)?.PreviewPath);
 void ICompanionHost.PhotosChanged()=>OnUi(()=>{if(page==4)OpenPhotos();return true;});
 void ICompanionHost.Paired(string name)=>OnUi(()=>{UpdateCompanionText();if(page==5)ShowError(name+" is paired. You can put the remote down.");WriteState("companion_paired");return true;});
}
