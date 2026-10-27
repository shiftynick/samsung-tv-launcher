using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

// Reactive scenes: a Home overlay that follows the time of day, the season and,
// once a location is set, the local weather. It sits above the theme's own
// scene layers and below the tiles.
public sealed partial class QuietHome
{
 SkySettings skySettings=new SkySettings();
 WeatherReading weather;
 string skyPath="",weatherPath="",skyKey="",weatherError="";
 View skyLayer;
 TextLabel weatherLabel;
 readonly List<Animation> skyAnimations=new List<Animation>();
 SkyState skyPreview;
 DateTime skyPreviewUntil,lastWeatherAttempt;
 bool weatherFetching;
 int skyVersion,skyParticles;

 void LoadSky(string data)
 {
  skyPath=System.IO.Path.Combine(data,"launcher-sky.json");
  weatherPath=System.IO.Path.Combine(data,"launcher-weather.json");
  skySettings=SkySettings.Load(skyPath);
  weather=WeatherReading.Load(weatherPath);
  if(weather!=null&&weather.Units!=skySettings.Units)weather=null;
 }
 SkyState CurrentSky()=>DateTime.UtcNow<skyPreviewUntil?skyPreview:Sky.Current(DateTime.UtcNow,DateTime.Now,skySettings,weather);
 bool WeatherShown=>skySettings.Weather&&weather!=null&&weather.Fresh(DateTime.UtcNow);

 // Called from BuildPage on Home, after the theme's scene layers.
 void BuildSky()
 {
  skyLayer=null;weatherLabel=null;skyKey="";
  if(page!=0||!skySettings.Enabled)return;
  skyLayer=Add(new View{Position2D=new Position2D(0,0),Size2D=new Size2D(1920,1080)});
  FillSky();
 }
 void BuildWeatherLabel()
 {
  if(page!=0||!skySettings.Enabled)return;
  weatherLabel=Text("",900,66,580,45,Pixel?17:24,muted);weatherLabel.HorizontalAlignment=HorizontalAlignment.End;
  UpdateWeatherLabel();
 }
 void UpdateWeatherLabel()
 {
  if(weatherLabel==null)return;
  weatherLabel.Text=WeatherShown?Sky.Temperature(weather)+"  ·  "+Sky.Describe(weather.Code):"";
 }
 void ClearSkyMotion(){foreach(var a in skyAnimations){a.Stop();a.Dispose();}skyAnimations.Clear();skyParticles=0;}
 void FillSky()
 {
  ClearSkyMotion();
  if(skyLayer==null)return;
  while(skyLayer.ChildCount>0){var child=skyLayer.GetChildAt(0);skyLayer.Remove(child);child.Dispose();}
  var state=CurrentSky();skyKey=state.Key;
  try{DrawSky(state);}catch(Exception e){ClearSkyMotion();motionError="sky:"+e.GetType().Name;}
 }
 View SkyPanel(float x,float y,float w,float h,Color color,float radius)
 {
  var v=new View{Position=new Position(x,y,0),Size=new Size(w,h),BackgroundColor=color,CornerRadius=new Vector4(radius,radius,radius,radius)};
  skyLayer.Add(v);return v;
 }
 static Color Rgba(float r,float g,float b,float a)=>new Color(r,g,b,a);
 Animation SkyLoop(int ms)
 {
  var a=new Animation(Math.Max(500,ms)){Looping=true};skyAnimations.Add(a);return a;
 }
 void StartSkyLoop(Animation a,float phase)
 {
  if(foreground&&!motionPaused&&motion!="off"){a.Play();a.CurrentProgress=phase;}
 }
 int SkyCount(int subtle,int full)=>motion=="off"?0:motion=="full"?full:subtle;

 void DrawSky(SkyState s)
 {
  // Colour grade first: phase, then weather. These stay even with motion off.
  switch(s.Phase){
   case "dawn":SkyPanel(0,0,1920,1080,Rgba(1f,.62f,.55f,.12f),0);break;
   case "dusk":SkyPanel(0,0,1920,1080,Rgba(1f,.45f,.2f,.16f),0);break;
   case "night":SkyPanel(0,0,1920,1080,Rgba(.03f,.06f,.18f,.40f),0);break;
  }
  switch(s.Weather){
   case "cloudy":SkyPanel(0,0,1920,1080,Rgba(.55f,.6f,.66f,.12f),0);break;
   case "fog":SkyPanel(0,0,1920,1080,Rgba(.75f,.78f,.8f,.22f),0);break;
   case "rain":SkyPanel(0,0,1920,1080,Rgba(.25f,.3f,.38f,.18f),0);break;
   case "snow":SkyPanel(0,0,1920,1080,Rgba(.85f,.9f,1f,.10f),0);break;
   case "storm":SkyPanel(0,0,1920,1080,Rgba(.08f,.1f,.15f,.30f),0);break;
  }
  if(motion=="off")return;
  var random=new Random(s.Key.GetHashCode()&0x7fffffff);
  switch(s.Weather){
   case "rain":Rain(random,SkyCount(24,48));return;
   case "storm":Rain(random,SkyCount(30,56));if(motion=="full")Lightning();return;
   case "snow":Snow(random,SkyCount(30,60));return;
   case "fog":Bands(random,3,.10f,.16f,150,900);return;
   case "cloudy":Bands(random,2,.05f,.08f,150,300);break;
  }
  if(s.Phase=="night"&&s.Weather=="clear")Stars(random,SkyCount(10,18));
  if(s.Season=="summer"&&(s.Phase=="night"||s.Phase=="dusk"))Fireflies(random,SkyCount(8,14));
  else if(s.Season=="autumn"&&s.Phase!="night")Falling(random,SkyCount(6,12),new[]{Rgba(.85f,.38f,.12f,.8f),Rgba(.72f,.2f,.1f,.8f),Rgba(.9f,.62f,.18f,.8f)},14,9);
  else if(s.Season=="spring"&&s.Phase!="night")Falling(random,SkyCount(6,12),new[]{Rgba(1f,.75f,.82f,.75f),Rgba(1f,.88f,.92f,.75f)},9,7);
 }
 void Rain(Random random,int count)
 {
  for(int i=0;i<count;i++){
   float x=random.Next(-100,1960),y=-60;
   var drop=SkyPanel(x,y,Pixel?3:2,Pixel?24:28,Rgba(.8f,.88f,1f,motion=="full"?.38f:.28f),0);
   var a=SkyLoop(900+random.Next(700));
   using(var p=new KeyFrames()){p.Add(0f,new Position(x,y,0));p.Add(1f,new Position(x-70,1100,0));a.AnimateBetween(drop,"Position",p);}
   StartSkyLoop(a,(float)random.NextDouble());skyParticles++;
  }
 }
 void Lightning()
 {
  var flash=SkyPanel(0,0,1920,1080,Rgba(1f,1f,1f,1f),0);flash.Opacity=0;
  var a=SkyLoop(11000);
  using(var o=new KeyFrames()){o.Add(0f,0f);o.Add(.55f,0f);o.Add(.56f,.18f);o.Add(.58f,0f);o.Add(.61f,.12f);o.Add(.63f,0f);o.Add(1f,0f);a.AnimateBetween(flash,"Opacity",o);}
  StartSkyLoop(a,0);
 }
 void Snow(Random random,int count)
 {
  for(int i=0;i<count;i++){
   float x=random.Next(0,1920),size=Pixel?6:5+random.Next(5);
   var flake=SkyPanel(x,-20,size,size,Rgba(1f,1f,1f,.55f+(float)random.NextDouble()*.3f),Pixel?0:size/2);
   float sway=20+random.Next(30);
   var a=SkyLoop(11000+random.Next(9000));
   using(var p=new KeyFrames()){
    p.Add(0f,new Position(x,-20,0));p.Add(.25f,new Position(x+sway,260,0));p.Add(.5f,new Position(x,540,0));p.Add(.75f,new Position(x-sway,820,0));p.Add(1f,new Position(x,1100,0));
    a.AnimateBetween(flake,"Position",p);
   }
   StartSkyLoop(a,(float)random.NextDouble());skyParticles++;
  }
 }
 void Bands(Random random,int count,float subtle,float full,int top,int range)
 {
  for(int i=0;i<count;i++){
   float y=top+random.Next(range),x=-400+random.Next(300);
   var band=SkyPanel(x,y,2600,240,Rgba(.88f,.9f,.94f,1f),Pixel?0:120);band.Opacity=motion=="full"?full:subtle;
   var a=SkyLoop(50000+random.Next(30000));
   using(var p=new KeyFrames()){p.Add(0f,new Position(x,y,0));p.Add(.5f,new Position(x+260,y+20,0));p.Add(1f,new Position(x,y,0));a.AnimateBetween(band,"Position",p);}
   StartSkyLoop(a,(float)random.NextDouble());skyParticles++;
  }
 }
 void Stars(Random random,int count)
 {
  for(int i=0;i<count;i++){
   var star=SkyPanel(80+random.Next(1760),160+random.Next(260),3,3,Rgba(1f,.97f,.88f,1f),Pixel?0:1.5f);
   var a=SkyLoop(4000+random.Next(5000));
   using(var o=new KeyFrames()){o.Add(0f,.15f);o.Add(.5f,.8f);o.Add(1f,.15f);a.AnimateBetween(star,"Opacity",o);}
   StartSkyLoop(a,(float)random.NextDouble());skyParticles++;
  }
 }
 void Fireflies(Random random,int count)
 {
  for(int i=0;i<count;i++){
   float x=100+random.Next(1720),y=380+random.Next(420),size=Pixel?6:7;
   var fly=SkyPanel(x,y,size,size,Rgba(.85f,1f,.45f,1f),Pixel?0:size/2);fly.Opacity=0;
   var a=SkyLoop(7000+random.Next(6000));
   using(var p=new KeyFrames())using(var o=new KeyFrames()){
    p.Add(0f,new Position(x,y,0));p.Add(.5f,new Position(x+random.Next(-60,60),y-30-random.Next(40),0));p.Add(1f,new Position(x,y,0));
    o.Add(0f,0f);o.Add(.3f,.9f);o.Add(.45f,.2f);o.Add(.6f,.85f);o.Add(1f,0f);
    a.AnimateBetween(fly,"Position",p);a.AnimateBetween(fly,"Opacity",o);
   }
   StartSkyLoop(a,(float)random.NextDouble());skyParticles++;
  }
 }
 void Falling(Random random,int count,Color[] colors,int w,int h)
 {
  for(int i=0;i<count;i++){
   float x=random.Next(0,1920),drift=80+random.Next(120);
   var leaf=SkyPanel(x,-30,w,h,colors[i%colors.Length],Pixel?0:h/2f);
   var a=SkyLoop(15000+random.Next(8000));
   using(var p=new KeyFrames())using(var r=new KeyFrames()){
    p.Add(0f,new Position(x,-30,0));p.Add(.33f,new Position(x+drift*.6f,340,0));p.Add(.66f,new Position(x+drift*.2f,720,0));p.Add(1f,new Position(x+drift,1100,0));
    r.Add(0f,new Rotation(new Radian(0),Vector3.ZAxis));r.Add(.5f,new Rotation(new Radian(3.1f),Vector3.ZAxis));r.Add(1f,new Rotation(new Radian(6.2f),Vector3.ZAxis));
    a.AnimateBetween(leaf,"Position",p);a.AnimateBetween(leaf,"Orientation",r);
   }
   StartSkyLoop(a,(float)random.NextDouble());skyParticles++;
  }
 }

 // Clock tick: redraw only when the phase, weather or season changes.
 void SkyTick()
 {
  if(page==0&&skyLayer!=null&&CurrentSky().Key!=skyKey){FillSky();WriteState("sky_changed");}
  UpdateWeatherLabel();
  FetchWeatherIfDue(false);
 }
 void FetchWeatherIfDue(bool force)
 {
  if(!skySettings.Weather||weatherFetching||!foreground)return;
  var now=DateTime.UtcNow;
  if(!force&&(now-lastWeatherAttempt<TimeSpan.FromMinutes(10)||weather!=null&&now-weather.FetchedUtc<TimeSpan.FromMinutes(30)))return;
  weatherFetching=true;lastWeatherAttempt=now;
  var request=new SkySettings{Weather=true,Latitude=skySettings.Latitude,Longitude=skySettings.Longitude,Place=skySettings.Place,Units=skySettings.Units};
  int version=skyVersion;var context=uiContext;
  Task.Run(()=>{
   WeatherReading reading=null;string error="";
   try{reading=WeatherClient.Forecast(request);}catch(Exception e){error=e.GetType().Name+": "+e.Message;}
   context?.Post(_=>{
    weatherFetching=false;
    if(version!=skyVersion)return;
    weatherError=error;
    if(reading!=null){weather=reading;try{WeatherReading.Save(weatherPath,reading);}catch(Exception){}}
    SkyTick();WriteState(reading!=null?"weather_updated":"weather_failed");
   },null);
  });
 }
 void SkyChanged()
 {
  skyVersion++;weatherError="";
  if(weather!=null&&(weather.Units!=skySettings.Units||!skySettings.Weather))weather=null;
  if(page==0)BuildPage();
  FetchWeatherIfDue(true);
 }

 // ICompanionHost
 object ICompanionHost.Sky()=>OnUi(()=>{
  var now=CurrentSky();
  return (object)new{
   enabled=skySettings.Enabled,weather=skySettings.Weather,place=skySettings.Place,units=skySettings.Units,latitude=skySettings.Latitude,longitude=skySettings.Longitude,
   phase=now.Phase,condition=now.Weather,season=now.Season,preview=DateTime.UtcNow<skyPreviewUntil,
   reading=WeatherShown?Sky.Temperature(weather)+" · "+Sky.Describe(weather.Code):"",
   updated=weather==null?null:(DateTime?)weather.FetchedUtc,error=weatherError,
   phases=Sky.Phases,conditions=Sky.Weathers,seasons=Sky.Seasons};
 });
 string ICompanionHost.SetSky(SkySettings next)=>OnUi(()=>{
  try{SkySettings.Save(skyPath,next);}catch(System.IO.InvalidDataException e){return e.Message;}catch(System.IO.IOException){return "Scene settings could not be saved.";}
  bool moved=next.Latitude!=skySettings.Latitude||next.Longitude!=skySettings.Longitude;
  skySettings=next;if(moved)weather=null;
  SkyChanged();WriteState("sky_settings");return null;
 });
 System.Collections.Generic.List<PlaceMatch> ICompanionHost.SearchPlaces(string query)=>WeatherClient.Search(query);
 string ICompanionHost.PreviewSky(string phase,string condition,string season)=>OnUi(()=>{
  if(Array.IndexOf(Sky.Phases,phase)<0||Array.IndexOf(Sky.Weathers,condition)<0||Array.IndexOf(Sky.Seasons,season)<0)return "Unknown scene.";
  if(!skySettings.Enabled)return "Turn on reactive scenes first.";
  skyPreview=new SkyState(phase,condition,season);skyPreviewUntil=DateTime.UtcNow.AddSeconds(60);
  if(page!=0)Home("sky_preview");
  FillSky();WriteState("sky_preview");return null;
 });
}
