using System;
public static class ThemeNavigation
{
 // Two columns, two rows per page. -1 returns focus to navigation tabs.
 public static int Move(int count,int item,string key)
 {
  if(count<=0)return -1;
  item=Math.Clamp(item,0,count-1);
  if(key=="Left")return Math.Max(0,item-1);
  if(key=="Right")return Math.Min(count-1,item+1);
  if(key=="Up")return item<2?-1:item-2;
  if(key=="Down")return item+2<count?item+2:(item/4+1)*4<count?count-1:item;
  return item;
 }
}
