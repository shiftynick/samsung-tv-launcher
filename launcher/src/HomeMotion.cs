using System;
using System.Collections.Generic;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

public sealed partial class QuietHome
{
 readonly List<Animation> ambientAnimations=new List<Animation>();
 Animation iconAnimation;
 ImageView floatingIcon;
 Position iconRest;
 string motion="subtle",motionPath="",motionError="";
 bool foreground=true,motionPaused;
 int particleCount;
 bool MotionActive=>MotionPreference.Active(motion,foreground,page)&&!motionPaused;
 string MotionName=>char.ToUpperInvariant(motion[0])+motion.Substring(1);
 void SetMotion(string mode){
  if(!MotionPreference.Valid(mode)){ShowError("Unknown motion setting.");return;}
  try{MotionPreference.Save(motionPath,mode);motionError="";}catch(Exception ex){motionError=ex.GetType().Name;ShowError("Motion setting could not be saved.");WriteState("motion_save_failed");return;}
  motion=mode;BuildPage();WriteState("motion_changed");
 }
 void StopIconMotion(){
  if(iconAnimation!=null){iconAnimation.Stop();iconAnimation.Dispose();iconAnimation=null;}
  ClearAlternateTile();
  if(floatingIcon!=null){floatingIcon.Position=iconRest;floatingIcon=null;}
 }
 void ClearMotion(){StopIconMotion();foreach(var animation in ambientAnimations){animation.Stop();animation.Dispose();}ambientAnimations.Clear();ClearSkyMotion();particleCount=0;sceneLayerCount=0;sceneImages.Clear();}
 void PauseMotion(){
  motionPaused=true;
  foreach(var animation in ambientAnimations)animation.Pause();foreach(var animation in skyAnimations)animation.Pause();
  StopIconMotion();
  if(focusAnimation!=null){focusAnimation.Stop();focusAnimation.Dispose();focusAnimation=null;}
 }
 void ResumeMotion(){motionPaused=false;if(MotionActive){foreach(var animation in ambientAnimations)animation.Play();foreach(var animation in skyAnimations)animation.Play();}UpdateIconMotion();}
 void BuildAmbientMotion(){try{BuildSceneMotion();BuildAmbientMotionCore();}catch(Exception error){ClearMotion();motionError=error.GetType().Name;}}
 void BuildAmbientMotionCore(){
  if(page!=0||motion=="off"||currentTheme.Motion.Effect=="none")return;
  var profile=currentTheme.Motion;
  particleCount=MotionPreference.ParticleCount(motion,profile.Count);
  var random=new Random(1759); // Stable placement, no per-frame managed timer or allocation.
  for(int i=0;i<particleCount;i++){
   int x=90+random.Next(1720),y=155+random.Next(230);
   bool rain=profile.Effect=="rain",square=Pixel||profile.Effect=="stars";
   int size=square?4:profile.Effect=="dust"?3:5;
   var mote=Panel(x,y,rain?2:size,rain?20:size,new Color(accent.R,accent.G,accent.B,motion=="full"?.55f:.30f),square||rain?0:size);
   int duration=(profile.Seconds+i%5)*1000;
   var animation=new Animation(duration){Looping=true};ambientAnimations.Add(animation);
   using(var positions=new KeyFrames())using(var opacity=new KeyFrames()){
    positions.Add(0f,new Position(x,y,0));
    positions.Add(.5f,new Position(x+(rain?-12:18+i%3*8),y+(rain?140:-22-i%4*8),0));
    positions.Add(1f,new Position(x,y,0));
    opacity.Add(0f,0f);opacity.Add(.2f,.8f);opacity.Add(.75f,.5f);opacity.Add(1f,0f);
    animation.AnimateBetween(mote,"Position",positions);
    animation.AnimateBetween(mote,"Opacity",opacity);
   }
   if(foreground&&!motionPaused)animation.Play();
  }
 }
 void UpdateIconMotion(){try{UpdateIconMotionCore();}catch(Exception error){StopIconMotion();motionError=error.GetType().Name;}}
 void UpdateIconMotionCore(){
  StopIconMotion();
  if(!MotionActive||area!=1||selected<0||selected>=slots.Count||!slots[selected].Action.StartsWith("app:"))return;
  foreach(var cached in artworkCache.Values)if(cached.Parent==slots[selected].Card){floatingIcon=cached.Image;break;}
  if(floatingIcon==null)return;
  iconRest=new Position(floatingIcon.Position.X,floatingIcon.Position.Y,floatingIcon.Position.Z);
  string iconKey=library[selected].ThemeIcon;
  if(!currentTheme.Motion.IconFloat&&currentTheme.TileFramePath(iconKey)==null){floatingIcon=null;return;}
  float amount=currentTheme.Motion.IconFloat?(motion=="full"?4:2):0;
  iconAnimation=new Animation(5000){Looping=true};
  using(var frames=new KeyFrames()){
   frames.Add(0f,iconRest);frames.Add(.5f,new Position(iconRest.X,iconRest.Y-amount,iconRest.Z));frames.Add(1f,iconRest);
   iconAnimation.AnimateBetween(floatingIcon,"Position",frames);
  }
  AddTileAnimation(iconKey);
  iconAnimation.Play();
 }
}
