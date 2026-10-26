using System;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

public sealed partial class QuietHome
{
 ImageView alternateTile;
 View alternateParent;
 int sceneLayerCount;
 readonly System.Collections.Generic.List<ImageView> sceneImages=new System.Collections.Generic.List<ImageView>();
 int SceneReadyCount(){int count=0;foreach(var image in sceneImages)if(image.LoadingStatus==ImageView.LoadingStatusType.Ready)count++;return count;}
 void ClearAlternateTile(){if(alternateTile!=null){alternateParent.Remove(alternateTile);alternateTile.Dispose();alternateTile=null;alternateParent=null;}}
 void BuildSceneMotion(){
  sceneLayerCount=0;
  if(page!=0||motion=="off")return;
  foreach(var layer in currentTheme.Motion.Layers){
   string path=currentTheme.ScenePath(layer);
   if(layer.Asset!=null&&path==null)continue;
   View visual;
   if(path!=null)visual=Add(new ImageView{ResourceUrl=path,ReleasePolicy=ReleasePolicyType.Destroyed,Position2D=new Position2D(layer.X,layer.Y),Size2D=new Size2D(layer.Width,layer.Height),DesiredWidth=layer.Width,DesiredHeight=layer.Height,FittingMode=FittingModeType.ScaleToFill,SamplingMode=Pixel?SamplingModeType.BoxThenNearest:SamplingModeType.BoxThenLanczos,Opacity=layer.Opacity});
   else visual=Panel(layer.X,layer.Y,layer.Width,layer.Height,Palette(layer.Color),Math.Min(12,layer.Height/2));
   if(visual is ImageView sceneImage){sceneImages.Add(sceneImage);sceneImage.ResourceReady+=(sender,args)=>WriteState("scene_ready");}
   var animation=new Animation(layer.Seconds*1000){Looping=true};ambientAnimations.Add(animation);
   using(var positions=new KeyFrames())using(var opacity=new KeyFrames()){
    positions.Add(0f,new Position(layer.X,layer.Y,0));
    if(layer.Kind=="travel")positions.Add(1f,new Position(layer.ToX,layer.ToY,0));
    else{positions.Add(.5f,new Position(layer.ToX,layer.ToY,0));positions.Add(1f,new Position(layer.X,layer.Y,0));}
    float strength=motion=="full"?1:.8f;
    if(layer.Kind=="travel"){opacity.Add(0f,0f);opacity.Add(.1f,layer.Opacity*strength);opacity.Add(.85f,layer.Opacity*strength);opacity.Add(1f,0f);}
    else{opacity.Add(0f,layer.Opacity*.35f*strength);opacity.Add(.5f,layer.Opacity*strength);opacity.Add(1f,layer.Opacity*.35f*strength);}
    animation.AnimateBetween(visual,"Position",positions);animation.AnimateBetween(visual,"Opacity",opacity);
   }
   if(foreground&&!motionPaused){animation.Play();animation.CurrentProgress=layer.Phase;}
   sceneLayerCount++;
  }
 }
 void AddTileAnimation(string key){
  string path=currentTheme.TileFramePath(key);if(path==null||floatingIcon==null)return;
  var profile=currentTheme.Motion.Tiles[key];
  alternateParent=slots[selected].Card;
  alternateTile=new ImageView{ResourceUrl=path,ReleasePolicy=ReleasePolicyType.Destroyed,Position2D=new Position2D(0,0),Size2D=new Size2D(272,272),DesiredWidth=544,DesiredHeight=544,FittingMode=FittingModeType.ShrinkToFit,SamplingMode=Pixel?SamplingModeType.BoxThenNearest:SamplingModeType.BoxThenLanczos,CornerRadius=new Vector4(Radius,Radius,Radius,Radius),Opacity=0};
  // Parent with the base artwork so character frames follow any selected-icon transform.
  alternateTile.ResourceReady+=(sender,args)=>WriteState("tile_frame_ready");
  floatingIcon.Add(alternateTile);alternateParent=floatingIcon;
  using(var frames=new KeyFrames()){
   if(profile.Kind=="pulse"){
    frames.Add(0f,0f);frames.Add(.5f,1f);frames.Add(1f,0f);
   }else if(profile.Kind=="flutter"){
    frames.Add(0f,0f);frames.Add(.20f,0f);frames.Add(.25f,1f);frames.Add(.45f,1f);frames.Add(.50f,0f);frames.Add(.70f,0f);frames.Add(.75f,1f);frames.Add(.95f,1f);frames.Add(1f,0f);
   }else{
    frames.Add(0f,0f);frames.Add(.70f,0f);frames.Add(.715f,1f);frames.Add(.745f,1f);frames.Add(.76f,0f);frames.Add(1f,0f);
   }
   iconAnimation.AnimateBetween(alternateTile,"Opacity",frames,0,profile.Seconds*1000);
  }
 }
}
