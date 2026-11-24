using System;using System.Drawing;using System.Threading.Tasks;using System.Linq;
namespace BeamScheduleFormatter {
partial class WindSiteMap {
 void ReportCamera(){if(double.IsNaN(siteLat))return;latitude=siteLat;longitude=siteLon;zoom=WindMapViewport.ZoomForRadius(latitude,terrain==null?1200:terrain.Radius,VW,VH);}
 bool CompleteVisibleTiles(){double left=X(longitude,zoom)-VW/2.0/mapScale,top=Y(latitude,zoom)-VH/2.0/mapScale;int max=1<<zoom;for(int row=(int)Math.Floor(top/256);row<=Math.Floor((top+VH/mapScale)/256);row++)for(int col=(int)Math.Floor(left/256);col<=Math.Floor((left+VW/mapScale)/256);col++){if(row<0||row>=max)continue;int x=((col%max)+max)%max;if(!tiles.ContainsKey(Key(zoom,x,row)))return false;}return true;}
 /* the report map is rendered at a fixed size, not the window size (owner, 24 November 2026: "nawala yung map at yung contour"): a wide
    window needed many tiles, the 8 s wait ran out and the whole basemap was dropped */
 static readonly Size ReportSize=new Size(1046,590);Size viewOverride;double userLatitude=double.NaN,userLongitude;int userZoom;
 int VW{get{return viewOverride.Width>0?viewOverride.Width:Width;}}int VH{get{return viewOverride.Height>0?viewOverride.Height:Height;}}
 /* owner ("mas zoomed in"): tiles exist only at whole zoom levels, so the circle could fill only half the height. The report view is
    rendered at the whole zoom just below the exact one with the map tiles enlarged (mapScale) and the labels at their normal size, so the assessment circle fills
    about 86 % of the map height */
 double reportEnlarge=1;
 void EnterReportView(){if(double.IsNaN(userLatitude)){userLatitude=latitude;userLongitude=longitude;userZoom=zoom;}latitude=siteLat;longitude=siteLon;
  double radius=terrain==null?1200:terrain.Radius;double exact=Math.Log(.86*ReportSize.Height*156543.033928*Math.Cos(siteLat*Math.PI/180)/(2*radius),2);int z=Math.Max(3,Math.Min(16,(int)Math.Floor(exact)));
  reportEnlarge=Math.Max(1,Math.Min(2,Math.Pow(2,exact-z)));zoom=z;mapScale=reportEnlarge;viewOverride=ReportSize;}
 void LeaveReportView(){viewOverride=Size.Empty;mapScale=1;if(!double.IsNaN(userLatitude)){latitude=userLatitude;longitude=userLongitude;zoom=userZoom;userLatitude=double.NaN;}Invalidate();}
 public async Task PrepareReportAsync(){if(double.IsNaN(siteLat))return;EnterReportView();try{RequestTiles();DateTime deadline=DateTime.UtcNow.AddSeconds(20);while(!IsDisposed&&!CompleteVisibleTiles()&&DateTime.UtcNow<deadline){await Task.Delay(120);RequestTiles();}}finally{LeaveReportView();}}
 public Bitmap Snapshot(){EnterReportView();try{var image=new Bitmap(VW,VH);using(var g=Graphics.FromImage(image)){g.Clear(ModernUi.Surface);g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;DrawMap(g,new Rectangle(0,0,VW,VH),false,true);}return image;}finally{LeaveReportView();}}
}
partial class WindDesignForm {protected override async Task PrepareDocumentAsync(){if(ValidCoordinates())await siteMap.PrepareReportAsync();}}
}
