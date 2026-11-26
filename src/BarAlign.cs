using System;using System.Linq;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* Toolbars and input rows (FlowLayoutPanel with buttons, labels, text boxes, check boxes, dropdowns) keep their controls centred on one
   line (owner, 26 November 2026: "paki check yung alignment ng buttons, pati na rin sa iba, always laging ganyan ang problem"): a
   flow lines its controls up at the top, so a 38 px button stands lower than a 23 px text box and its caption. Each control's top and
   bottom margins are set so every control of a row shares the row's centre line. */
static class BarAlign {
 static readonly ConditionalWeakTable<FlowLayoutPanel,object> attached=new ConditionalWeakTable<FlowLayoutPanel,object>();
 static bool Simple(Control c){return c is ButtonBase||c is Label||c is TextBox||c is ComboBox||c is NumericUpDown||c is DateTimePicker;}
 internal static void Attach(FlowLayoutPanel flow){if(flow==null||flow.FlowDirection!=FlowDirection.LeftToRight)return;object seen;if(attached.TryGetValue(flow,out seen))return;attached.Add(flow,new object());
  bool busy=false;LayoutEventHandler handler=(s,e)=>{if(busy)return;busy=true;try{Align(flow);}finally{busy=false;}};flow.Layout+=handler;flow.ControlAdded+=(s,e)=>{if(!busy)flow.PerformLayout();};flow.PerformLayout();}
 /* only bars: small, plain controls (or small label + box groups) on one line */
 static bool Item(Control c){if(c.Height>60)return false;if(c is TextBox&&((TextBox)c).Multiline&&c.Height>40)return false;if(Simple(c))return true;var f=c as FlowLayoutPanel;return f!=null&&f.Controls.Count>0&&f.Controls.Cast<Control>().All(x=>Simple(x)&&x.Height<=60);}
 static bool IsBar(FlowLayoutPanel flow){var items=flow.Controls.Cast<Control>().ToList();return items.Count>1&&items.All(Item);}
 internal static void Align(FlowLayoutPanel flow){if(!IsBar(flow))return;foreach(var inner in flow.Controls.OfType<FlowLayoutPanel>())Attach(inner);var items=flow.Controls.Cast<Control>().ToList();int row=items.Max(c=>c.Height);
  foreach(var c in items){var l=c as Label;if(l!=null&&l.AutoSize&&(l.Padding.Top!=0||l.Padding.Bottom!=0))l.Padding=new Padding(l.Padding.Left,0,l.Padding.Right,0);if(l!=null)l.TextAlign=System.Drawing.ContentAlignment.MiddleLeft;}
  row=items.Max(c=>c.Height);foreach(var c in items){int top=Math.Max(0,(row-c.Height)/2),bottom=Math.Max(0,row-c.Height-top);var m=c.Margin;if(m.Top!=top||m.Bottom!=bottom)c.Margin=new Padding(m.Left,top,m.Right,bottom);}}
}
}
