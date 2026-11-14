using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class WindDesignForm {
 readonly WindSiteMap siteMap=new WindSiteMap{Dock=DockStyle.Fill};
 void ConfigureSiteLayout(FlowLayoutPanel input){
  var page=(TabPage)input.Parent;page.Controls.Remove(input);
  var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(12)};
  layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,365));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  input.Dock=DockStyle.Fill;input.FlowDirection=FlowDirection.TopDown;input.WrapContents=false;input.Padding=new Padding(8);input.Margin=new Padding(0,0,12,0);input.BackColor=Color.White;
  var note=input.Controls.OfType<Label>().First();note.Text="PROJECT SITE\r\nEnter coordinates to locate the site and calculate basic wind speed from the NSCP contour map.";note.Height=82;note.ForeColor=PowTheme.Muted;
  foreach(Control child in input.Controls){
   var field=child as FlowLayoutPanel;
   if(field!=null&&Convert.ToString(field.Tag)!="FullWidth"){
    field.Height=76;field.Margin=new Padding(0,0,0,4);
    foreach(var label in field.Controls.OfType<Label>())label.Height=30;
   }
  }
  speed.Height=88;speed.Font=new Font("Segoe UI",12,FontStyle.Bold);speed.Padding=new Padding(8);speed.BackColor=ModernUi.Selection;
  status.Height=78;status.Padding=new Padding(0,8,0,0);status.ForeColor=PowTheme.Muted;
  Action fit=()=>{int width=Math.Max(220,input.ClientSize.Width-input.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-6);foreach(Control child in input.Controls){child.Width=width;var field=child as FlowLayoutPanel;if(field!=null)foreach(Control inner in field.Controls)if(!(inner is Button))inner.Width=Math.Max(180,width-10);}};
  input.Resize+=(s,e)=>fit();layout.Controls.Add(input,0,0);
  var mapHost=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};var tools=new FlowLayoutPanel{Dock=DockStyle.Top,Padding=new Padding(4),Height=40};
  var center=new Button{Text="Center on project",AutoSize=true};center.Click+=(s,e)=>siteMap.Recenter();tools.Controls.Add(center);var zones=new CheckBox{Text="Roughness zones",Checked=true,AutoSize=true,Padding=new Padding(8,6,0,0)};zones.CheckedChanged+=(s,e)=>{siteMap.ShowRoughness=zones.Checked;siteMap.Invalidate();};tools.Controls.Add(zones);var directions=new CheckBox{Text="8 directions",Checked=true,AutoSize=true,Padding=new Padding(8,6,0,0)};directions.CheckedChanged+=(s,e)=>{siteMap.ShowDirections=directions.Checked;siteMap.Invalidate();};tools.Controls.Add(directions);
  var contours=new CheckBox{Text="Wind contours",Checked=true,AutoSize=true,Padding=new Padding(8,6,0,0)};contours.CheckedChanged+=(s,e)=>{siteMap.ShowContours=contours.Checked;siteMap.Invalidate();};tools.Controls.Add(contours);var region=new Button{Text="View wind region",AutoSize=true};region.Click+=(s,e)=>siteMap.ViewWindRegion();tools.Controls.Add(region);
  tools.Controls.Add(new Label{Text="Drag to pan · scroll to zoom",AutoSize=true,Padding=new Padding(10,7,0,0),ForeColor=PowTheme.Muted});
  mapHost.Controls.Add(siteMap);mapHost.Controls.Add(new Label{Dock=DockStyle.Bottom,Height=56,Padding=new Padding(8),Text=WindColors.Legend+"\r\nLight pie sectors: suggested exposure. Rings show height-dependent fetch and assessment radius. Wind contours are interpolated from the supplied source map.",ForeColor=PowTheme.Muted});mapHost.Controls.Add(tools);
  layout.Controls.Add(mapHost,1,0);page.Controls.Add(layout);fit();
 }
}
}
