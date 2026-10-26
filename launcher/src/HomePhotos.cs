using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

public sealed partial class QuietHome
{
 PhotoCatalog photos;
 string photosDirectory="",photoError="";
 int photoIndex,photoAlbum; // album 0 = all albums
 bool photoPlaying=true,photoPending,photoTimerRunning;
 int pendingPhotoIndex;
 Tizen.NUI.Timer photoTimer,photoChromeTimer;
 ImageView photoFront,photoBack;
 TextLabel photoTitle,photoHint;
 View photoFooter;
 Animation photoFade;
 List<LocalPhoto> PhotoItems=>photos==null?new List<LocalPhoto>():photos.Photos.Where(p=>photoAlbum==0||p.Album==photos.Albums[photoAlbum-1]).ToList();
 bool PhotoReady=>photoFront!=null&&photoFront.LoadingStatus==ImageView.LoadingStatusType.Ready;
 void OpenPhotos(){
  try{photos=PhotoCatalog.Load(photosDirectory);photoError="";photoAlbum=Math.Clamp(photoAlbum,0,photos.Albums.Count);photoIndex=Math.Clamp(photoIndex,0,Math.Max(0,PhotoItems.Count-1));}
  catch{photos=null;photoError="No readable local album is installed.";}
  page=4;area=1;editing="";BuildPage();WriteState("photos_opened");
 }
 void PausePhotos(){photoTimer?.Stop();photoChromeTimer?.Stop();photoTimerRunning=false;CancelPhotoTransition();}
 void ResumePhotos(){if(page==4&&foreground&&photoPlaying&&photos!=null&&photoTimer!=null){photoTimer.Start();photoTimerRunning=true;}}
 void CancelPhotoTransition(){
  if(photoFade!=null){photoFade.Finished-=PhotoFadeFinished;photoFade.Stop();photoFade.Dispose();photoFade=null;}
  photoPending=false;if(photoBack!=null)photoBack.Opacity=0;if(photoFront!=null)photoFront.Opacity=1;
 }
 void BuildPhotoPage(){
  photoFront=null;photoBack=null;photoTitle=null;photoHint=null;
  Panel(0,0,1920,1080,new Color(0,0,0,1),0);
  var items=PhotoItems;
  if(items.Count==0){
   Text("Travel Photos",90,140,1700,90,48,white,"Semibold");
   Text(photoError.Length==0?"This album is empty.":photoError,90,270,1700,70,32,white);
   Text("Back to Home",90,920,1700,70,26,white);return;
  }
  photoIndex=Math.Clamp(photoIndex,0,items.Count-1);
  photoFront=PhotoView();photoBack=PhotoView();photoBack.Opacity=0;
  var first=photoFront;var second=photoBack;
  first.ResourceReady+=(s,e)=>PhotoLoaded(first);second.ResourceReady+=(s,e)=>PhotoLoaded(second);
  photoFront.ResourceUrl=items[photoIndex].Path;
  photoFooter=Panel(0,930,1920,150,new Color(0,0,0,.72f),0);
  photoTitle=Text("",65,940,1790,52,Pixel?20:32,white,"Semibold");
  photoHint=Text("",65,1004,1790,42,Pixel?13:22,new Color(.83f,.87f,.90f,1));
  UpdatePhotoCaption();
  if(photoTimer==null){photoTimer=new Tizen.NUI.Timer(12000);photoTimer.Tick+=(s,e)=>{if(page==4&&foreground&&photoPlaying)NextPhoto(1);return true;};}
  ResumePhotos();
 }
 ImageView PhotoView(){return (ImageView)Add(new ImageView{Size2D=new Size2D(1920,1080),DesiredWidth=1920,DesiredHeight=1080,FittingMode=FittingModeType.ShrinkToFit,ReleasePolicy=ReleasePolicyType.Destroyed});}
 void UpdatePhotoCaption(){
  if(photoTitle==null)return;
  var items=PhotoItems;var item=items[photoIndex];
  photoTitle.Text=item.Album+(item.Caption.Length>0?" · "+item.Caption:"");
  ShowPhotoChrome();
  photoHint.Text=(photoPlaying?"Playing":"Paused")+" · "+(photoIndex+1)+" / "+items.Count+"   Left / Right: photo   Up / Down: album   Select: pause   Back: Home"+(photoError.Length>0?" · "+photoError:"");
 }
 void ShowPhotoChrome(){
  if(photoTitle==null)return;
  photoFooter.Opacity=1;photoTitle.Opacity=1;photoHint.Opacity=1;
  if(photoChromeTimer==null){photoChromeTimer=new Tizen.NUI.Timer(5000);photoChromeTimer.Tick+=(s,e)=>{if(page==4&&foreground&&photoPlaying){photoFooter.Opacity=0;photoTitle.Opacity=0;photoHint.Opacity=0;}return false;};}
  photoChromeTimer.Stop();if(photoPlaying&&foreground)photoChromeTimer.Start();
 }
 void PhotoRequest(string action){
  if(page!=4)OpenPhotos();
  if(action=="next")NextPhoto(1);else if(action=="previous")NextPhoto(-1);
  else if(action=="album-next")HandlePhotoKey("Down");else if(action=="album-previous")HandlePhotoKey("Up");
  else if(action=="all"){photoAlbum=0;photoIndex=0;BuildPage();}
  else if(action=="pause"){photoPlaying=false;photoTimer?.Stop();photoTimerRunning=false;UpdatePhotoCaption();}
  else if(action=="play"){photoPlaying=true;ResumePhotos();UpdatePhotoCaption();}
  WriteState("photo_request");
 }
 void PhotoLoaded(ImageView image){
  if(page!=4||!foreground)return;
  if(image==photoBack&&photoPending){
   if(image.LoadingStatus==ImageView.LoadingStatusType.Ready)StartPhotoTransition();
   else{photoPending=false;photoError="This photo could not be displayed.";UpdatePhotoCaption();}
  }
  WriteState("photo_ready");
 }
 void NextPhoto(int delta){
  var items=PhotoItems;if(items.Count<2||photoPending||photoFade!=null)return;
  pendingPhotoIndex=PhotoCatalog.Move(items.Count,photoIndex,delta);photoPending=true;
  photoBack.ResourceUrl=items[pendingPhotoIndex].Path;
  if(photoBack.LoadingStatus==ImageView.LoadingStatusType.Ready)StartPhotoTransition();
 }
 void StartPhotoTransition(){
  if(!photoPending||photoFade!=null)return;
  if(motion=="off"){CompletePhotoTransition();return;}
  photoBack.RaiseToTop();photoFooter.RaiseToTop();photoTitle.RaiseToTop();photoHint.RaiseToTop();
  photoFade=new Animation(500);photoFade.AnimateTo(photoBack,"Opacity",1f);photoFade.Finished+=PhotoFadeFinished;photoFade.Play();
 }
 void PhotoFadeFinished(object sender,EventArgs args){
  var finished=photoFade;photoFade=null;if(finished!=null){finished.Finished-=PhotoFadeFinished;finished.Dispose();}CompletePhotoTransition();
 }
 void CompletePhotoTransition(){
  var previous=photoFront;photoFront=photoBack;photoBack=previous;photoFront.Opacity=1;photoBack.Opacity=0;
  // Both reusable views have readiness listeners; their roles swap after each fade.
  photoIndex=pendingPhotoIndex;photoPending=false;photoError="";UpdatePhotoCaption();WriteState("photo_changed");
 }
 bool HandlePhotoKey(string key){
  if(key=="XF86Back"||key=="Back"||key=="Escape"||key=="XF86Exit"){Home("photos_back");return true;}
  if(key=="Left"||key=="Right"){NextPhoto(key=="Left"?-1:1);return true;}
  if(key=="Up"||key=="Down"){
   if(photos!=null){photoAlbum=PhotoCatalog.Move(photos.Albums.Count+1,photoAlbum,key=="Up"?-1:1);photoIndex=0;BuildPage();}return true;
  }
  if(key=="Return"||key=="KP_Enter"||key=="MediaPlayPause"){
   photoPlaying=!photoPlaying;if(photoPlaying)ResumePhotos();else {photoTimer?.Stop();photoTimerRunning=false;}UpdatePhotoCaption();WriteState("photo_playback");return true;
  }
  return true; // Photo mode never dispatches selection into the Home tile list.
 }
}
