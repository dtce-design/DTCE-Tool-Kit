using System;using System.Drawing;using System.Threading.Tasks;using System.Linq;
namespace BeamScheduleFormatter {
partial class WindSiteMap {
 void ReportCamera(){if(double.IsNaN(siteLat))return;latitude=siteLat;longitude=siteLon;zoom=WindMapViewport.ZoomForRadius(latitude,terrain==null?1200:terrain.Radius,VW,VH);}
 bool CompleteVisibleTiles(){double left=X(longitude,zoom)-VW/2.0,top=Y(latitude,zoom)-VH/2.0;int max=1<<zoom;for(int row=(int)Math.Floor(top/256);row<=Math.Floor((top+VH)/256);row++)for(int col=(int)Math.Floor(left/256);col<=Math.Floor((left+VW)/256);col++){if(row<0||row>=max)continue;int x=((col%max)+max)%max;if(!tiles.ContainsKey(Key(zoom,x,row)))return false;}return true;}
 /* the report map is rendered at a fixed size, not the window size (owner, 24 November 2026: "nawala yung map at yung contour"): a wide
    window needed many tiles, the 8 s wait ran out and the whole basemap was dropped */
 static readonly Size ReportSize=new Size(1046,590);Size viewOverride;double userLatitude=double.NaN,userLongitude;int userZoom;
 int VW{get{return viewOverride.Width>0?viewOverride.Width:Width;}}int VH{get{return viewOverride.Height>0?viewOverride.Height:Height;}}
 void EnterReportView(){if(double.IsNaN(userLatitude)){userLatitude=latitude;userLongitude=longitude;userZoom=zoom;}viewOverride=ReportSize;ReportCamera();}
 void LeaveReportView(){viewOverride=Size.Empty;if(!double.IsNaN(userLatitude)){latitude=userLatitude;longitude=userLongitude;zoom=userZoom;userLatitude=double.NaN;}Invalidate();}
 public async Task PrepareReportAsync(){if(double.IsNaN(siteLat))return;EnterReportView();try{RequestTiles();DateTime deadline=DateTime.UtcNow.AddSeconds(20);while(!IsDisposed&&!CompleteVisibleTiles()&&DateTime.UtcNow<deadline){await Task.Delay(120);RequestTiles();}}finally{LeaveReportView();}}
 public Bitmap Snapshot(){EnterReportView();try{var image=new Bitmap(VW,VH);using(var g=Graphics.FromImage(image)){g.Clear(ModernUi.Surface);DrawMap(g,new Rectangle(0,0,VW,VH),false,true);}return image;}finally{LeaveReportView();}}
}
partial class WindDesignForm {protected override async Task PrepareDocumentAsync(){if(ValidCoordinates())await siteMap.PrepareReportAsync();}}
}
