using System;using System.Linq;using System.Drawing;using System.Windows.Forms;using System.Collections.Generic;
namespace BeamScheduleFormatter {
// Reuses the existing live settings controls and their persistence/change handlers.
partial class ProgramOfWorksForm {
 void RefineRCStart(){
  var page=tabs.TabPages.Cast<TabPage>().Single(p=>p.Text=="Reinforced Concrete Start");var start=page.Controls.OfType<FlowLayoutPanel>().Single();
  var original=start.Controls.OfType<PowCard>().Single();var memberButtons=original.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<Button>().ToList();memberButtons.AddRange(start.Controls.OfType<Button>());
  var coverFields=rcCovers.Values.Select(c=>c.Parent).Distinct().ToList();var defaults=rcDefaultsStatus.Parent;var supply=new TableLayoutPanel{AutoSize=true,ColumnCount=2,RowCount=4,Margin=Padding.Empty,Padding=Padding.Empty};supply.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));supply.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
  for(int i=0;i<ConcreteGroups.Length;i++){var box=readyMixGroups[ConcreteGroups[i]];box.AutoSize=true;box.MinimumSize=new Size(0,38);box.Margin=new Padding(4,4,18,4);supply.Controls.Add(box,i%2,i/2);if(i%2==0)supply.RowStyles.Add(new RowStyle(SizeType.AutoSize));}
  start.SuspendLayout();try{start.Controls.Clear();start.Padding=new Padding(20,16,20,20);start.WrapContents=false;start.FlowDirection=FlowDirection.TopDown;
   var title=new Label{Text="Reinforced concrete settings",AutoSize=true,Font=new Font("Segoe UI Semibold",19),Margin=new Padding(0,0,0,4)};start.Controls.Add(title);start.Controls.Add(new Label{Text="Set project defaults here. Member-specific details and commercial lengths are in each member's settings.",AutoSize=true,Margin=new Padding(0,0,0,16)});
   /* Owner (18 November 2026: "di maganda arrangement at user experience"): member buttons follow the subtab order
      (Beam, Column, Slab, Footing, Wall Footing, Canopy / Ledge, RC Wall, Miscellaneous Structures), use the same
      capitalisation and sit in an equal-width grid instead of a ragged wrap. */
   var captions=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"Beam Settings","Beam settings"},{"Column settings","Column settings"},{"Slab Settings","Slab settings"},{"Foundation / Footing Settings","Footing settings"},{"Wall Footing settings","Wall footing settings"},{"Canopy / Ledge Settings","Canopy / ledge settings"},{"RC Wall Settings","RC wall settings"},{"Miscellaneous Structures settings","Miscellaneous structures settings"}};
   var order=captions.Keys.ToList();memberButtons=memberButtons.OrderBy(b=>{int i=order.FindIndex(k=>string.Equals(k,b.Text,StringComparison.OrdinalIgnoreCase));return i<0?99:i;}).ToList();
   foreach(var button in memberButtons){string caption;if(captions.TryGetValue(button.Text,out caption))button.Text=caption;}
   var actions=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=true,Margin=Padding.Empty,Padding=Padding.Empty};int buttonWidth=220;bool sizingButtons=false;
   Action<Button> fitButton=b=>{if(sizingButtons)return;sizingButtons=true;try{b.AutoSize=false;b.MinimumSize=new Size(buttonWidth,40);b.MaximumSize=new Size(buttonWidth,40);b.Size=new Size(buttonWidth,40);}finally{sizingButtons=false;}};
   foreach(var button in memberButtons){var b=button;b.Margin=new Padding(0,0,10,10);b.TextAlign=ContentAlignment.MiddleCenter;fitButton(b);b.SizeChanged+=(s,e)=>{if(b.Width!=buttonWidth||b.Height!=40)fitButton(b);};actions.Controls.Add(b);}start.Controls.Add(new RCSettingsCard("Member settings","Materials, reinforcement and commercial lengths",actions));
   var covers=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,WrapContents=true,Margin=Padding.Empty};foreach(var field in coverFields){field.Width=152;field.Height=68;field.Margin=new Padding(0,0,14,8);foreach(Control c in field.Controls){c.Width=c is NumericUpDown?128:148;}covers.Controls.Add(field);}var lean=new FlowLayoutPanel{Width=162,Height=68,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0,0,0,8)};lean.Controls.Add(new Label{Text="Lean concrete (mm)",Width=156,Height=24});leanThickness.Width=128;lean.Controls.Add(leanThickness);covers.Controls.Add(lean);
   var coverBody=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty};coverBody.Controls.Add(covers);coverBody.Controls.Add(defaults);rcDefaultsStatus.MaximumSize=new Size(620,0);rcDefaultsStatus.Font=new Font("Segoe UI",9);rcDefaultsStatus.ForeColor=PowTheme.Muted;rcDefaultsStatus.Padding=Padding.Empty;rcDefaultsStatus.Margin=new Padding(6,12,0,0);
   /* The empty space under the cover buttons came from GrowOnly auto-size: the row grew while the window was laid out
      and never shrank back. */
   var defaultsFlow=defaults as FlowLayoutPanel;if(defaultsFlow!=null){defaultsFlow.AutoSizeMode=AutoSizeMode.GrowAndShrink;defaultsFlow.Padding=Padding.Empty;defaultsFlow.Margin=new Padding(0,6,0,0);defaultsFlow.WrapContents=true;}start.Controls.Add(new RCSettingsCard("Cover and foundation defaults","Millimetres · save once for future projects",coverBody));
   start.Controls.Add(new RCSettingsCard("Concrete supply","Check Ready Mix for the corresponding member group. Unchecked groups use ordinary mix.",supply));
   bool resizing=false;Action resize=()=>{if(resizing||start.IsDisposed)return;resizing=true;try{int width=Math.Max(520,Math.Min(1240,start.ClientSize.Width-start.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-4));foreach(var card in start.Controls.OfType<RCSettingsCard>()){card.Width=width;card.Arrange();}foreach(var label in start.Controls.OfType<Label>())label.MaximumSize=new Size(width,0);int bodyWidth=width-36;int columns=Math.Max(1,Math.Min(4,(bodyWidth+10)/250));int fitted=(bodyWidth-10*columns)/columns;if(fitted!=buttonWidth){buttonWidth=fitted;foreach(var b in memberButtons)fitButton(b);}/* Explicit sizes (auto-size left a clipped second button row and an empty gap under the cover buttons). */
   Func<Control,int> tall=c=>c.GetPreferredSize(new Size(bodyWidth,0)).Height;
   foreach(Control c in new Control[]{actions,covers,defaults,coverBody}){c.AutoSize=false;c.MaximumSize=Size.Empty;c.MinimumSize=Size.Empty;c.Width=bodyWidth;}
   actions.Height=((memberButtons.Count+columns-1)/columns)*50;
   int perRow=Math.Max(1,bodyWidth/166);covers.Height=((covers.Controls.Count+perRow-1)/perRow)*76;
   rcDefaultsStatus.MaximumSize=new Size(Math.Max(240,bodyWidth-12),0);defaults.Height=tall(defaults);
   coverBody.Height=covers.Height+defaults.Height+defaults.Margin.Vertical;supply.Width=bodyWidth;foreach(CheckBox box in supply.Controls)box.MaximumSize=new Size(Math.Max(200,bodyWidth/2-32),0);foreach(var card in start.Controls.OfType<RCSettingsCard>())card.Arrange();}finally{resizing=false;}};start.Resize+=(s,e)=>resize();Shown+=(s,e)=>resize();resize();
  }finally{start.ResumeLayout(true);}
 }
}
class RCSettingsCard:PowCard {
 readonly Label heading,description;readonly Control body;bool arranging;
 public RCSettingsCard(string title,string detail,Control content){heading=new Label{Text=title,AutoSize=true,Font=new Font("Segoe UI Semibold",12),Margin=Padding.Empty};description=new Label{Text=detail,AutoSize=true,ForeColor=PowTheme.Muted};body=content;Padding=new Padding(18);Margin=new Padding(0,0,0,14);Controls.Add(body);Controls.Add(description);Controls.Add(heading);body.SizeChanged+=(s,e)=>Arrange();SizeChanged+=(s,e)=>Arrange();}
 public void Arrange(){if(arranging||IsDisposed)return;arranging=true;try{int width=Math.Max(200,ClientSize.Width-Padding.Horizontal);heading.Location=new Point(Padding.Left,Padding.Top);heading.MaximumSize=new Size(width,0);description.Location=new Point(Padding.Left,heading.Bottom+5);description.MaximumSize=new Size(width,0);body.Location=new Point(Padding.Left,description.Bottom+14);body.Width=width;body.MaximumSize=new Size(width,0);body.PerformLayout();
  /* Owner (18 November 2026): on Windows the RC Start cards showed only their headings (body height 0). The body is
     never shorter than the height its contents need at this width. */
  if(!body.AutoSize){int needed=body.GetPreferredSize(new Size(width,0)).Height;if(needed>body.Height)body.Height=needed;}if(body.Height<8)body.Height=Math.Max(8,body.PreferredSize.Height);
  Height=body.Bottom+Padding.Bottom;}finally{arranging=false;}}
}
}
