using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {
partial class ProgramOfWorksForm {
 FlowLayoutPanel PremiumExcelToolbar(){
  var tools=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=true,Padding=new Padding(16,3,16,5),BackColor=Color.White};
  Button(tools,"Open / Link Excel",LinkWorksExcel);Button(tools,"Use open workbook",LinkActiveWorksExcel);Button(tools,"Sync now",()=>{Guard();SyncWorksExcel();});
  var menu=new ContextMenuStrip{Font=ModernUi.Body,ShowImageMargin=false};
  Action<string,Action> item=(caption,action)=>{var entry=new ToolStripMenuItem(caption);entry.Click+=(s,e)=>{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Program of Works");}};menu.Items.Add(entry);};
  item("Map BACKUP items",EditBackupMapping);item("Refresh BACKUP items",RefreshBackupBill);menu.Items.Add(new ToolStripSeparator());item("Review inputs",ShowWorksIssues);item("Disconnect Excel",DisconnectWorksExcel);
  item("Guide to quantities and source rows",()=>MessageBox.Show(this,"Linked BACKUP pay items appear in this table. Computed quantities use your measurements. Source rows show where a total comes from; do not add them again. Manual quantities, including LS, are edited in the Quantity column. Map an ambiguous/new specification once using Map BACKUP items. Save Program of Works retains the full project.","Bill of Quantities"));
  var more=new Button{Text="More  ▾",AutoSize=true};more.Click+=(s,e)=>menu.Show(more,new Point(0,more.Height));tools.Controls.Add(more);Disposed+=(s,e)=>menu.Dispose();return tools;
 }
}
}
