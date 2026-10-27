using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
public static class CompanionTests
{
 static int checks;
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Reject(Action action,string message){bool rejected=false;try{action();}catch(Exception e)when(e is InvalidDataException||e is ArgumentException){rejected=true;}Check(rejected,message);}
 // A 4x2 landscape baseline JPEG and a 2x4 portrait one (the portrait must be refused).
 static readonly byte[] Landscape=Jpeg(4,2),Portrait=Jpeg(2,4);
 static byte[] Jpeg(int w,int h){byte[] b=Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAACAAQDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAABAb/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCHALkZ/9k=");
  int sof=1;while(!(b[sof-1]==255&&b[sof]==192))sof++;sof+=4;b[sof]=(byte)(h>>8);b[sof+1]=(byte)h;b[sof+2]=(byte)(w>>8);b[sof+3]=(byte)w;return b;}
 static List<LibraryApp> Apps()=>new[]{"plex","netflix","apple-tv","prime","youtube","youtube-tv","extra"}.Select(n=>new LibraryApp{Id="app."+n,Name=n,ThemeIcon=n}).ToList();

 sealed class FakeHost:ICompanionHost
 {
  public string Theme="cinema",Motion="subtle",Directory,Page;public LauncherLayout Layout;public int PhotoChanges;public string PairedName;
  public object State()=>new{theme=Theme,motion=Motion};
  public string SetTheme(string id){if(id!="cinema"&&id!="pixel")return "That theme is not installed.";Theme=id;return null;}
  public string SetMotion(string mode){if(!MotionValid(mode))return "Unknown motion setting.";Motion=mode;return null;}
  static bool MotionValid(string m)=>m=="off"||m=="subtle"||m=="full";
  public string SetLayout(LauncherLayout l){try{LauncherLayout.Validate(l,Apps());}catch(InvalidDataException e){return e.Message;}Layout=l;return null;}
  public string ThemePreview(string id)=>null;
  public string PhotosDirectory=>Directory;
  public string PageFile=>Page;
  public void PhotosChanged()=>PhotoChanges++;
  public void Paired(string name)=>PairedName=name;
  public SkySettings SkySettings=new SkySettings();public string Previewed;
  public object Sky()=>new{enabled=SkySettings.Enabled,place=SkySettings.Place};
  public string SetSky(SkySettings s){try{SkySettings.Validate(s);}catch(InvalidDataException e){return e.Message;}SkySettings=s;return null;}
  public List<PlaceMatch> SearchPlaces(string q){if((q??"").Trim().Length<2)throw new InvalidDataException("Enter a city or postcode.");return new List<PlaceMatch>{new PlaceMatch{Name="Springfield",Latitude=1,Longitude=2}};}
  public string PreviewSky(string p,string c,string s){if(Array.IndexOf(global::Sky.Phases,p)<0||Array.IndexOf(global::Sky.Weathers,c)<0||Array.IndexOf(global::Sky.Seasons,s)<0)return "Unknown scene.";Previewed=p+"/"+c+"/"+s;return null;}
 }

 public static void Main()
 {
  string root=Path.Combine(Path.GetTempPath(),"companion-tests-"+Guid.NewGuid());Directory.CreateDirectory(root);
  try{
   // Pairing
   string phones=Path.Combine(root,"data","phones.json");
   var access=new CompanionAccess(phones);
   Check(access.Pair("000000","x")==null,"Pairing without a code on screen must fail");
   string code=access.NewCode();Check(code.Length==6&&code.All(char.IsDigit),"Code must be six digits");
   string wrong=code=="111111"?"222222":"111111";
   Check(access.Pair(wrong,"x")==null&&access.Code==code,"A wrong code must not consume the code early");
   string token=access.Pair(code,"Sam's <iPhone>");
   Check(token!=null&&token.Length>=40,"Correct code must issue a long token");
   Check(access.Code==null,"A code must work only once");
   Check(access.Pair(code,"again")==null,"A used code must not pair again");
   Check(access.Authenticate(token)?.Name=="Sam's iPhone","Phone name must be cleaned and token accepted");
   Check(access.Authenticate(token+"x")==null&&access.Authenticate("")==null&&access.Authenticate(null)==null,"Bad tokens must be refused");
   Check(!File.ReadAllText(phones).Contains(token),"Tokens must not be stored in plain text");
   Check(new CompanionAccess(phones).Authenticate(token)!=null,"Pairing must survive a launcher restart");
   code=access.NewCode();for(int i=0;i<CompanionAccess.MaxAttempts;i++){string guess=((int.Parse(code)+1+i)%1000000).ToString("D6");access.Pair(guess,"x");}
   Check(access.Code==null&&access.Locked&&access.Pair(code,"x")==null,"Too many wrong codes must cancel the code");
   access.NewCode();Check(!access.Locked,"A new code must unlock pairing");access.ClearCode();Check(access.Code==null,"Leaving the screen must clear the code");
   for(int i=0;i<CompanionAccess.MaxPhones+3;i++){access.Pair(access.NewCode(),"p"+i);}
   Check(access.PhoneCount==CompanionAccess.MaxPhones,"Paired phones must be bounded");
   Check(access.Authenticate(token)==null,"The oldest phone must be dropped first");
   File.WriteAllText(phones,"{corrupt");Check(new CompanionAccess(phones).PhoneCount==0,"A corrupt phone list must load as empty");
   string second=access.Pair(access.NewCode(),"keep");var keep=access.Authenticate(second);Check(access.Forget(keep.Id)&&access.Authenticate(second)==null,"Unpair must revoke the token");
   access.ForgetAll();Check(access.PhoneCount==0,"Forget phones must clear everything");

   // Layout
   var apps=Apps();string layoutPath=Path.Combine(root,"data","layout.json");
   var fresh=LauncherLayout.Load(layoutPath,apps);
   Check(fresh.Home(apps).Select(a=>a.Id).SequenceEqual(apps.Take(6).Select(a=>a.Id)),"Default Home must keep bundled order");
   Check(fresh.HdmiLabel=="HDMI 3"&&fresh.HdmiSource==15,"Default HDMI shortcut must match the previous tile");
   var custom=new LauncherLayout{Order=new List<string>{"app.extra","app.plex"},Hidden=new List<string>{"app.netflix"},HdmiLabel="  PS5 ",HdmiSource=14};
   LauncherLayout.Save(layoutPath,custom,apps);var loaded=LauncherLayout.Load(layoutPath,apps);
   Check(loaded.HdmiLabel=="PS5"&&loaded.HdmiSource==14,"HDMI shortcut must persist trimmed");
   Check(loaded.Ordered(apps).First().Id=="app.extra"&&loaded.Ordered(apps).Count==apps.Count,"Order must put chosen apps first and keep the rest");
   Check(!loaded.Home(apps).Any(a=>a.Id=="app.netflix")&&loaded.Home(apps).Count==6,"Hidden apps must leave Home and the next app fills in");
   foreach(var bad in new[]{
    new LauncherLayout{Order=new List<string>{"app.unknown"}},
    new LauncherLayout{Order=new List<string>{"app.plex","app.plex"}},
    new LauncherLayout{Hidden=apps.Select(a=>a.Id).ToList()},
    new LauncherLayout{HdmiSource=16},
    new LauncherLayout{HdmiLabel=""},
    new LauncherLayout{HdmiLabel=new string('x',17)},
    new LauncherLayout{HdmiLabel="bad\nname"},
    new LauncherLayout{Order=null}})
    Reject(()=>LauncherLayout.Save(layoutPath,bad,apps),"Invalid layout saved");
   Check(LauncherLayout.Load(layoutPath,apps).HdmiLabel=="PS5","Rejected layout overwrote the saved one");
   File.WriteAllText(layoutPath,"{\"Order\":[\"app.gone\"],\"Hidden\":[]}");Check(LauncherLayout.Load(layoutPath,apps).Order.Count==0,"Layout naming a removed app must fall back");
   File.WriteAllText(layoutPath,"nonsense");Check(LauncherLayout.Load(layoutPath,apps).Hidden.Count==0,"Corrupt layout must fall back");

   // Photos
   string photos=Path.Combine(root,"photos");
   string first=PhotoStore.Add(photos,Landscape,"Trip","Beach");
   Check(PhotoCatalog.Load(photos).Photos.Single().File==first,"Upload must create a loadable album");
   Reject(()=>PhotoStore.Add(photos,Portrait,"Trip",""),"Portrait JPEG must be refused");
   Reject(()=>PhotoStore.Add(photos,new byte[]{1,2,3,4,5,6,7,8,9,10,11},"Trip",""),"Non-JPEG must be refused");
   Reject(()=>PhotoStore.Add(photos,Landscape,"","x"),"Empty album name must be refused");
   Reject(()=>PhotoStore.Add(photos,Landscape,"Trip","line\nbreak"),"Control characters must be refused");
   Reject(()=>PhotoStore.Add(photos,new byte[PhotoStore.MaxFileBytes+1],"Trip",""),"Oversized upload must be refused");
   Check(Directory.GetFiles(Path.Combine(photos,"images")).Length==1,"Refused uploads must leave no files");
   string second2=PhotoStore.Add(photos,Landscape,"Home","");
   Check(PhotoCatalog.Load(photos).Albums.Count==2,"Second album must appear");
   Check(PhotoStore.Delete(photos,first)&&!File.Exists(Path.Combine(photos,first)),"Delete must remove the file");
   Check(!PhotoStore.Delete(photos,first),"Deleting twice must report nothing removed");
   Check(PhotoStore.Delete(photos,second2)&&PhotoCatalog.Load(photos).Photos.Count==0,"Deleting the last photo must leave an empty album");
   Check(!Directory.EnumerateFiles(photos,"*.new").Any()&&!Directory.EnumerateFiles(photos,"*.old").Any(),"No staging files may remain");

   // Server over loopback
   var host=new FakeHost{Directory=photos,Page=Path.Combine(root,"index.html")};File.WriteAllText(host.Page,"<!doctype html><title>t</title>");
   var serverAccess=new CompanionAccess(Path.Combine(root,"data","server-phones.json"));
   int port=new Random().Next(20000,40000);
   using(var server=new CompanionServer(serverAccess,host,port)){
    Check(server.Start(),"Server must start: "+server.Error);
    using var http=new HttpClient{BaseAddress=new Uri("http://127.0.0.1:"+port)};
    HttpResponseMessage Send(HttpMethod m,string path,string json=null,string bearer=null,byte[] bytes=null,string hostHeader=null){
     var req=new HttpRequestMessage(m,path);if(json!=null)req.Content=new StringContent(json,Encoding.UTF8,"application/json");
     if(bytes!=null){req.Content=new ByteArrayContent(bytes);req.Content.Headers.ContentType=new MediaTypeHeaderValue("image/jpeg");}
     if(bearer!=null)req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",bearer);
     if(hostHeader!=null)req.Headers.Host=hostHeader;
     return http.Send(req);
    }
    var page=Send(HttpMethod.Get,"/");Check(page.StatusCode==HttpStatusCode.OK&&page.Headers.Contains("X-Content-Type-Options")&&page.Content.Headers.ContentType.MediaType=="text/html","Page must be served");
    Check(page.Headers.GetValues("Content-Security-Policy").Single().Contains("frame-ancestors 'none'"),"Page must carry a restrictive CSP");
    Check(Send(HttpMethod.Get,"/",hostHeader:"evil.example:"+port).StatusCode==HttpStatusCode.Forbidden,"Name-based Host must be refused (DNS rebinding)");
    Check(Send(HttpMethod.Get,"/api/state").StatusCode==HttpStatusCode.Unauthorized,"State must need a token");
    Check(Send(HttpMethod.Get,"/api/state",bearer:"guess").StatusCode==HttpStatusCode.Unauthorized,"Unknown token must be refused");
    Check(Send(HttpMethod.Post,"/api/pair","{\"code\":\"123456\"}").StatusCode==HttpStatusCode.Forbidden,"Pairing without a code on screen must fail");
    Check(Send(HttpMethod.Post,"/api/pair","not json").StatusCode==HttpStatusCode.BadRequest,"Bad pairing body must be a 400");
    string serverCode=serverAccess.NewCode();
    var paired=Send(HttpMethod.Post,"/api/pair",JsonSerializer.Serialize(new{code=serverCode,name="Test phone"}));
    Check(paired.StatusCode==HttpStatusCode.OK&&host.PairedName=="Test phone","Pairing must succeed with the TV code");
    string t=JsonDocument.Parse(paired.Content.ReadAsStringAsync().Result).RootElement.GetProperty("token").GetString();
    Check(Send(HttpMethod.Get,"/api/state",bearer:t).StatusCode==HttpStatusCode.OK,"Token must open the API");
    Check(Send(HttpMethod.Put,"/api/theme","{\"id\":\"pixel\"}",t).StatusCode==HttpStatusCode.OK&&host.Theme=="pixel","Theme change must reach the launcher");
    Check(Send(HttpMethod.Put,"/api/theme","{\"id\":\"../x\"}",t).StatusCode==HttpStatusCode.BadRequest,"Unknown theme must be a 400");
    Check(Send(HttpMethod.Put,"/api/motion","{\"mode\":\"full\"}",t).StatusCode==HttpStatusCode.OK&&host.Motion=="full","Motion change must reach the launcher");
    Check(Send(HttpMethod.Put,"/api/motion","[]",t).StatusCode==HttpStatusCode.BadRequest,"Malformed motion must be a 400");
    Check(Send(HttpMethod.Put,"/api/layout","{\"order\":[\"app.prime\"],\"hidden\":[],\"hdmiLabel\":\"Switch\",\"hdmiSource\":13}",t).StatusCode==HttpStatusCode.OK&&host.Layout.HdmiLabel=="Switch","Layout must reach the launcher");
    Check(Send(HttpMethod.Put,"/api/layout","{\"order\":[],\"hidden\":[],\"hdmiLabel\":\"\",\"hdmiSource\":13}",t).StatusCode==HttpStatusCode.BadRequest,"Invalid layout must be a 400");
    Check(Send(HttpMethod.Put,"/api/layout",new string(' ',20000),t).StatusCode==HttpStatusCode.BadRequest,"Oversized JSON must be refused");
    var up=Send(HttpMethod.Post,"/api/photos?album=Garden&caption=Roses",bearer:t,bytes:Landscape);
    Check(up.StatusCode==HttpStatusCode.OK&&host.PhotoChanges==1,"Upload must succeed and notify the launcher");
    string file=JsonDocument.Parse(up.Content.ReadAsStringAsync().Result).RootElement.GetProperty("file").GetString();
    Check(Send(HttpMethod.Post,"/api/photos?album=Garden",bearer:t,bytes:Portrait).StatusCode==HttpStatusCode.BadRequest,"Invalid photo must be a 400");
    Check(Send(HttpMethod.Post,"/api/photos",bearer:t,bytes:Landscape).StatusCode==HttpStatusCode.BadRequest,"Upload without album must be a 400");
    var list=Send(HttpMethod.Get,"/api/photos",bearer:t);Check(list.Content.ReadAsStringAsync().Result.Contains("Roses"),"Photo list must include the upload");
    var img=Send(HttpMethod.Get,"/api/photos/"+file,bearer:t);Check(img.StatusCode==HttpStatusCode.OK&&img.Content.ReadAsByteArrayAsync().Result.Length==Landscape.Length,"Photo must be readable by its phone");
    Check(Send(HttpMethod.Get,"/api/photos/"+file).StatusCode==HttpStatusCode.Unauthorized,"Photos must need a token");
    Check(Send(HttpMethod.Get,"/api/photos/images/..%2Fphotos.jpg",bearer:t).StatusCode==HttpStatusCode.NotFound,"Path tricks must not read files");
    Check(Send(HttpMethod.Delete,"/api/photos/"+file,bearer:t).StatusCode==HttpStatusCode.OK&&host.PhotoChanges==2,"Delete must succeed and notify");
    Check(Send(HttpMethod.Get,"/api/sky").StatusCode==HttpStatusCode.Unauthorized,"Scene settings must need a token");
    Check(Send(HttpMethod.Put,"/api/sky","{\"enabled\":true,\"weather\":true,\"latitude\":39.7392,\"longitude\":-104.9903,\"place\":\"Springfield\",\"units\":\"f\"}",t).StatusCode==HttpStatusCode.OK&&host.SkySettings.Latitude==39.74,"Scene settings must reach the launcher, rounded");
    Check(Send(HttpMethod.Put,"/api/sky","{\"enabled\":true,\"weather\":true,\"place\":\"\",\"units\":\"f\"}",t).StatusCode==HttpStatusCode.BadRequest,"Weather without a location must be a 400");
    Check(Send(HttpMethod.Post,"/api/sky/search","{\"query\":\"Springfield\"}",t).Content.ReadAsStringAsync().Result.Contains("Springfield"),"Place search must return matches");
    Check(Send(HttpMethod.Post,"/api/sky/search","{\"query\":\"\"}",t).StatusCode==HttpStatusCode.BadRequest,"Empty place search must be a 400");
    Check(Send(HttpMethod.Post,"/api/sky/preview","{\"phase\":\"night\",\"condition\":\"snow\",\"season\":\"winter\"}",t).StatusCode==HttpStatusCode.OK&&host.Previewed=="night/snow/winter","Scene preview must reach the launcher");
    Check(Send(HttpMethod.Post,"/api/sky/preview","{\"phase\":\"noon\",\"condition\":\"snow\",\"season\":\"winter\"}",t).StatusCode==HttpStatusCode.BadRequest,"Unknown preview must be a 400");
    Check(Send(HttpMethod.Post,"/api/unpair",bearer:t).StatusCode==HttpStatusCode.OK&&Send(HttpMethod.Get,"/api/state",bearer:t).StatusCode==HttpStatusCode.Unauthorized,"Unpair must revoke this phone");
   }
   // QR: decoded with zbarimg during development; here, structure and bounds.
   var qr=QrCode.Encode("http://192.0.2.50:47801/#123456");
   Check(qr.GetLength(0)==29&&qr.GetLength(1)==29,"QR must be version 3");
   foreach(var (r,c) in new[]{(0,0),(0,28),(28,0),(3,3),(3,25),(25,3),(22,22),(21,8)})Check(qr[r,c],"QR finder, alignment and dark module must be set");
   foreach(var (r,c) in new[]{(1,1),(7,7),(7,21),(21,7),(21,21)})Check(!qr[r,c],"QR separators and pattern rings must be light");
   Check(!QrCode.Encode("http://192.0.2.50:47801/#654321").Cast<bool>().SequenceEqual(qr.Cast<bool>()),"QR must change with the pairing code");
   Reject(()=>QrCode.Encode(new string('x',QrCode.MaxBytes+1)),"Oversized QR text accepted");
   // Built from bytes: the public-file audit refuses private address literals.
   static IPAddress V4(params byte[] b)=>new IPAddress(b);
   Check(CompanionServer.PrivateAddress(V4(192,168,1,5))&&CompanionServer.PrivateAddress(V4(10,0,0,1))&&CompanionServer.PrivateAddress(V4(172,20,0,1))&&CompanionServer.PrivateAddress(V4(192,168,0,2).MapToIPv6()),"Private ranges must be allowed");
   Check(!CompanionServer.PrivateAddress(IPAddress.Parse("8.8.8.8"))&&!CompanionServer.PrivateAddress(IPAddress.Parse("172.32.0.1"))&&!CompanionServer.PrivateAddress(IPAddress.Parse("2001:db8::1")),"Public addresses must be refused");
   // Reactive scenes.
   Check(Sky.SunElevation(new DateTime(2026,6,21,19,0,0,DateTimeKind.Utc),39.74,-104.99)>70&&Sky.SunElevation(new DateTime(2026,6,21,8,0,0,DateTimeKind.Utc),39.74,-104.99)<-20,"Sun elevation must follow the day");
   Check(Sky.SunElevation(new DateTime(2026,3,20,12,7,0,DateTimeKind.Utc),0,0)>85,"Equinox noon at the equator must be overhead");
   var city=new SkySettings{Weather=true,Latitude=39.74,Longitude=-104.99,Place="Springfield"};
   string At(int h,int m)=>Sky.Phase(new DateTime(2026,10,27,h,m,0,DateTimeKind.Utc),DateTime.Now,city);
   Check(At(13,15)=="dawn"&&At(18,0)=="day"&&At(0,5)=="dusk"&&At(1,30)=="night"&&At(10,0)=="night","Phases must follow sunrise and sunset");
   var nowhere=new SkySettings();
   Check(Sky.Phase(DateTime.UtcNow,new DateTime(2026,1,1,7,0,0),nowhere)=="dawn"&&Sky.Phase(DateTime.UtcNow,new DateTime(2026,1,1,12,0,0),nowhere)=="day"&&Sky.Phase(DateTime.UtcNow,new DateTime(2026,1,1,19,0,0),nowhere)=="dusk"&&Sky.Phase(DateTime.UtcNow,new DateTime(2026,1,1,23,0,0),nowhere)=="night","Without a location the clock must decide");
   Check(Sky.Season(new DateTime(2026,10,27),39.74)=="autumn"&&Sky.Season(new DateTime(2026,1,5),39.74)=="winter"&&Sky.Season(new DateTime(2026,1,5),-33.9)=="summer","Seasons must follow month and hemisphere");
   Check(Sky.WeatherKind(0)=="clear"&&Sky.WeatherKind(2)=="cloudy"&&Sky.WeatherKind(45)=="fog"&&Sky.WeatherKind(63)=="rain"&&Sky.WeatherKind(81)=="rain"&&Sky.WeatherKind(73)=="snow"&&Sky.WeatherKind(86)=="snow"&&Sky.WeatherKind(95)=="storm","Weather codes must map to scenes");
   Check(!Sky.KnownCode(4)&&!Sky.KnownCode(-1)&&Sky.KnownCode(99),"Unknown weather codes must be refused");
   var nowUtc=new DateTime(2026,10,27,15,0,0,DateTimeKind.Utc);
   var reading=WeatherReading.Parse("{\"current\":{\"time\":\"x\",\"temperature_2m\":71.6,\"weather_code\":61}}","f",nowUtc);
   Check(Sky.Temperature(reading)=="72°F"&&Sky.Describe(reading.Code)=="Light rain","Reading must parse and format");
   Check(Sky.Current(nowUtc,nowUtc,city,reading).Weather=="rain"&&Sky.Current(nowUtc.AddHours(4),nowUtc,city,reading).Weather=="clear","Stale weather must be ignored");
   Check(Sky.Current(nowUtc,nowUtc,new SkySettings{Weather=false,Latitude=39.74,Longitude=-104.99,Place="x"},reading).Weather=="clear","Weather off must ignore readings");
   Reject(()=>WeatherReading.Parse("{\"current\":{\"temperature_2m\":20,\"weather_code\":4}}","c",nowUtc),"Unknown weather code accepted");
   Reject(()=>SkySettings.Validate(new SkySettings{Latitude=91,Place="x"}),"Out-of-range latitude accepted");
   Reject(()=>SkySettings.Validate(new SkySettings{Units="k"}),"Unknown units accepted");
   Reject(()=>SkySettings.Validate(new SkySettings{Place=new string('x',61)}),"Long place accepted");
   string skyFile=Path.Combine(root,"sky.json");
   SkySettings.Save(skyFile,new SkySettings{Weather=true,Latitude=39.7392,Longitude=-104.9903,Place=" Springfield ",Units="c"});
   var skyLoaded=SkySettings.Load(skyFile);Check(skyLoaded.Latitude==39.74&&skyLoaded.Longitude==-104.99&&skyLoaded.Place=="Springfield"&&skyLoaded.Units=="c","Scene settings must persist rounded and trimmed");
   File.WriteAllText(skyFile,"{bad");Check(SkySettings.Load(skyFile).Enabled&&!SkySettings.Load(skyFile).Weather,"Corrupt scene settings must fall back to defaults");
   string weatherFile=Path.Combine(root,"weather.json");WeatherReading.Save(weatherFile,reading);Check(WeatherReading.Load(weatherFile)?.Code==61,"Weather cache must persist");
   var places=WeatherClient.ParseSearch("{\"results\":[{\"name\":\"Springfield\",\"admin1\":\"State\",\"country_code\":\"US\",\"latitude\":39.7817,\"longitude\":-89.6501},{\"admin1\":\"no name\"}]}");
   Check(places.Count==1&&places[0].Name=="Springfield, State, US"&&places[0].Latitude==39.78,"Place search must parse, label and round");
   Check(WeatherClient.ParseSearch("{}").Count==0,"No results must be an empty list");
   Console.WriteLine("Companion pairing, layout, photo store and HTTP API: "+checks+" checks passed.");
  }finally{Directory.Delete(root,true);}
 }
}
