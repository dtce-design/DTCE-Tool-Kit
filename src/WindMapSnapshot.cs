using System;using System.Drawing;using System.Threading.Tasks;using System.Linq;
namespace BeamScheduleFormatter {
partial class WindSiteMap {
 void ReportCamera(){if(double.IsNaN(siteLat))return;latitude=siteLat;longitude=siteLon;zoom=WindMapViewport.ZoomForRadius(latitude,terrain==null?1200:terrain.Radius,Width,Height);}
 bool CompleteVisibleTiles(){double left=X(longitude,zoom)-Width/2.0,top=Y(latitude,zoom)-Height/2.0;int max=1<<zoom;for(int row=(int)Math.Floor(top/256);row<=Math.Floor((top+Height)/256);row++)for(int col=(int)Math.Floor(left/256);col<=Math.Floor((left+Width)/256);col++){if(row<0||row>=max)continue;int x=((col%max)+max)%max;if(!tiles.ContainsKey(Key(zoom,x,row)))return false;}return true;}
 public async Task PrepareReportAsync(){if(double.IsNaN(siteLat))return;ReportCamera();Invalidate();RequestTiles();DateTime deadline=DateTime.UtcNow.AddSeconds(8);while(!IsDisposed&&!CompleteVisibleTiles()&&DateTime.UtcNow<deadline)await Task.Delay(90);}
 public Bitmap Snapshot(){double oldLatitude=latitude,oldLongitude=longitude;int oldZoom=zoom;try{ReportCamera();var image=new Bitmap(Math.Max(1,Width),Math.Max(1,Height));using(var g=Graphics.FromImage(image)){g.Clear(ModernUi.Surface);DrawMap(g,new Rectangle(0,0,Width,Height),false,true);}return image;}finally{latitude=oldLatitude;longitude=oldLongitude;zoom=oldZoom;}}
}
partial class WindDesignForm {protected override async Task PrepareDocumentAsync(){if(ValidCoordinates())await siteMap.PrepareReportAsync();}}
}
