using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

// What the server needs from the launcher. Implementations run UI work on the
// launcher's main thread; every method may be called from a worker thread.
public interface ICompanionHost
{
 object State();
 string SetTheme(string id);
 string SetMotion(string mode);
 string SetLayout(LauncherLayout layout);
 string ThemePreview(string id);
 string PhotosDirectory {get;}
 string PageFile {get;}
 void PhotosChanged();
 void Paired(string name);
}

// A small HTTP API for the phone companion. It answers only private-network
// clients that address the TV by IP, and every /api call except pairing needs
// a token issued through a code shown on the TV.
public sealed class CompanionServer : IDisposable
{
 public const int DefaultPort=47801;
 const int MaxJson=16384, Workers=4;
 static readonly JsonSerializerOptions Json=new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
 readonly CompanionAccess access;
 readonly ICompanionHost host;
 readonly int port;
 readonly SemaphoreSlim slots=new SemaphoreSlim(Workers);
 HttpListener listener;
 Thread loop;
 public string Error {get;private set;}="";
 public bool Running=>listener!=null&&listener.IsListening;
 public int Port=>port;
 public CompanionServer(CompanionAccess access,ICompanionHost host,int port=DefaultPort){this.access=access;this.host=host;this.port=port;}
 public bool Start()
 {
  if(Running)return true;
  try{
   listener=new HttpListener();listener.Prefixes.Add("http://*:"+port+"/");listener.Start();
   loop=new Thread(Accept){IsBackground=true,Name="companion"};loop.Start();Error="";return true;
  }catch(Exception e){Error=e.GetType().Name+": "+e.Message;try{listener?.Close();}catch{}listener=null;return false;}
 }
 public void Dispose(){var l=listener;listener=null;try{l?.Close();}catch{}}
 void Accept()
 {
  var l=listener;
  while(l!=null&&l.IsListening){
   HttpListenerContext context;
   try{context=l.GetContext();}catch{break;}
   if(!slots.Wait(0)){Reply(context,503,new{error="The TV is busy. Try again."});continue;}
   ThreadPool.QueueUserWorkItem(_=>{try{Handle(context);}catch(Exception e)when(e is InvalidDataException||e is JsonException){TryReply(context,400,new{error=e is InvalidDataException?e.Message:"The request was not understood."});}catch(Exception e){TryReply(context,500,new{error="The TV could not finish that request.",detail=e.GetType().Name});}finally{slots.Release();}});
  }
 }
 public static bool PrivateAddress(IPAddress address)
 {
  if(address.IsIPv4MappedToIPv6)address=address.MapToIPv4();
  if(IPAddress.IsLoopback(address))return true;
  if(address.AddressFamily!=AddressFamily.InterNetwork)return address.IsIPv6LinkLocal||address.IsIPv6UniqueLocal;
  byte[] b=address.GetAddressBytes();
  return b[0]==10||b[0]==172&&b[1]>=16&&b[1]<=31||b[0]==192&&b[1]==168||b[0]==169&&b[1]==254;
 }
 void Handle(HttpListenerContext c)
 {
  var request=c.Request;
  // Name-based hosts are refused so a web page cannot reach the TV through DNS rebinding.
  if(!PrivateAddress(request.RemoteEndPoint.Address)||!IPAddress.TryParse(request.Url.Host,out _)){Reply(c,403,new{error="Open the address shown on the TV."});return;}
  string path=request.Url.AbsolutePath,method=request.HttpMethod;
  if(method=="GET"&&(path=="/"||path=="/index.html")){Page(c);return;}
  if(!path.StartsWith("/api/")){Reply(c,404,new{error="Not found."});return;}
  if(method=="POST"&&path=="/api/pair"){Pair(c);return;}
  string header=request.Headers["Authorization"]??"";
  var phone=header.StartsWith("Bearer ")?access.Authenticate(header.Substring(7).Trim()):null;
  if(phone==null){Reply(c,401,new{error="Pair this phone again from the Phone screen on the TV."});return;}
  if(method=="GET"&&path=="/api/state"){Reply(c,200,host.State());return;}
  if(method=="PUT"&&path=="/api/theme"){Result(c,host.SetTheme(Field(c,"id")));return;}
  if(method=="PUT"&&path=="/api/motion"){Result(c,host.SetMotion(Field(c,"mode")));return;}
  if(method=="PUT"&&path=="/api/layout"){Result(c,host.SetLayout(JsonSerializer.Deserialize<LauncherLayout>(Body(c,MaxJson),Json)));return;}
  if(method=="POST"&&path=="/api/unpair"){access.Forget(phone.Id);Reply(c,200,new{ok=true});return;}
  var preview=Regex.Match(path,@"\A/api/themes/([a-z0-9-]{1,40})/preview\z");
  if(method=="GET"&&preview.Success){string file=host.ThemePreview(preview.Groups[1].Value);if(file==null)Reply(c,404,new{error="No preview."});else Send(c,200,File.ReadAllBytes(file),"image/png","private, max-age=3600");return;}
  if(path=="/api/photos"&&method=="GET"){Photos(c);return;}
  if(path=="/api/photos"&&method=="POST"){Upload(c);return;}
  var image=Regex.Match(path,@"\A/api/photos/(images/[a-z0-9-]+\.jpg)\z");
  if(image.Success&&method=="GET"){string file=Path.Combine(host.PhotosDirectory,image.Groups[1].Value);if(PhotoStore.Existing(host.PhotosDirectory).Any(p=>p.File==image.Groups[1].Value))Send(c,200,File.ReadAllBytes(file),"image/jpeg","private, max-age=86400");else Reply(c,404,new{error="No such photo."});return;}
  if(image.Success&&method=="DELETE"){bool removed=PhotoStore.Delete(host.PhotosDirectory,image.Groups[1].Value);if(removed)host.PhotosChanged();Reply(c,removed?200:404,removed?(object)new{ok=true}:new{error="No such photo."});return;}
  Reply(c,404,new{error="Not found."});
 }
 void Pair(HttpListenerContext c)
 {
  JsonElement body;
  try{body=JsonDocument.Parse(Body(c,MaxJson)).RootElement;}catch(Exception e)when(e is JsonException||e is InvalidDataException){Reply(c,400,new{error="Send the code shown on the TV."});return;}
  string code=body.TryGetProperty("code",out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():null;
  string name=body.TryGetProperty("name",out var n)&&n.ValueKind==JsonValueKind.String?n.GetString():"";
  string token=access.Pair(code,name);
  if(token==null){Reply(c,403,new{error=access.Locked?"Too many wrong codes. Choose New code on the TV.":"That code is not showing on the TV."});return;}
  host.Paired(CompanionAccess.CleanName(name));
  Reply(c,200,new{token});
 }
 void Photos(HttpListenerContext c)
 {
  List<LocalPhoto> rows;
  try{rows=PhotoStore.Existing(host.PhotosDirectory);}catch(Exception e)when(e is InvalidDataException||e is IOException||e is JsonException||e is KeyNotFoundException||e is InvalidOperationException){Reply(c,409,new{error="The album on the TV could not be read."});return;}
  long bytes=rows.Sum(r=>new FileInfo(Path.Combine(host.PhotosDirectory,r.File)).Length);
  Reply(c,200,new{photos=rows.Select(r=>new{file=r.File,album=r.Album,caption=r.Caption}),bytes,maxBytes=PhotoStore.MaxAlbumBytes,maxPhotos=PhotoStore.MaxPhotos});
 }
 void Upload(HttpListenerContext c)
 {
  string album=c.Request.QueryString["album"],caption=c.Request.QueryString["caption"]??"";
  try{
   PhotoStore.CheckText(album,caption);
   string file=PhotoStore.Add(host.PhotosDirectory,Body(c,PhotoStore.MaxFileBytes),album,caption);
   host.PhotosChanged();Reply(c,200,new{file});
  }catch(InvalidDataException e){Reply(c,400,new{error=e.Message});}
 }
 string Field(HttpListenerContext c,string name)
 {
  var root=JsonDocument.Parse(Body(c,MaxJson)).RootElement;
  return root.ValueKind==JsonValueKind.Object&&root.TryGetProperty(name,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():"";
 }
 void Result(HttpListenerContext c,string error){if(error==null)Reply(c,200,new{ok=true});else Reply(c,400,new{error});}
 void Page(HttpListenerContext c)
 {
  c.Response.Headers["Content-Security-Policy"]="default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src blob: data:; connect-src 'self'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";
  Send(c,200,File.ReadAllBytes(host.PageFile),"text/html; charset=utf-8","no-store");
 }
 static byte[] Body(HttpListenerContext c,int limit)
 {
  if(c.Request.ContentLength64>limit)throw new InvalidDataException("The upload is too large.");
  var output=new MemoryStream();var buffer=new byte[65536];int read;
  while((read=c.Request.InputStream.Read(buffer,0,buffer.Length))>0){output.Write(buffer,0,read);if(output.Length>limit)throw new InvalidDataException("The upload is too large.");}
  return output.ToArray();
 }
 static void Reply(HttpListenerContext c,int status,object value)=>Send(c,status,JsonSerializer.SerializeToUtf8Bytes(value,Json),"application/json","no-store");
 static void TryReply(HttpListenerContext c,int status,object value){try{Reply(c,status,value);}catch{}}
 static void Send(HttpListenerContext c,int status,byte[] body,string type,string cache)
 {
  var r=c.Response;
  r.StatusCode=status;r.ContentType=type;r.ContentLength64=body.Length;
  r.Headers["Cache-Control"]=cache;r.Headers["X-Content-Type-Options"]="nosniff";r.Headers["Referrer-Policy"]="no-referrer";
  try{r.OutputStream.Write(body,0,body.Length);}finally{r.Close();}
 }
}
