using System;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
static class DropdownCopy {
 static readonly ConditionalWeakTable<ComboBox,object> attached=new ConditionalWeakTable<ComboBox,object>();
 internal static string SelectedValue(ComboBox combo){return combo.SelectedItem==null?combo.Text:combo.GetItemText(combo.SelectedItem);}
 internal static void Copy(ComboBox combo){string text=combo.DropDownStyle!=ComboBoxStyle.DropDownList&&combo.SelectionLength>0?combo.SelectedText:SelectedValue(combo);if(!string.IsNullOrEmpty(text))Clipboard.SetText(text);}
 internal static void Attach(ComboBox combo){object old;if(attached.TryGetValue(combo,out old))return;attached.Add(combo,new object());combo.KeyDown+=(s,e)=>{if(e.KeyData!=(Keys.Control|Keys.C))return;try{Copy(combo);e.Handled=e.SuppressKeyPress=true;}catch(System.Runtime.InteropServices.ExternalException){}};
  if(combo is DataGridViewComboBoxEditingControl)return;
  ContextMenuStrip menu=null;combo.MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Right||combo.ContextMenuStrip!=null)return;menu=new ContextMenuStrip();var item=menu.Items.Add("Copy selected value",null,(sender,args)=>{try{Copy(combo);}catch(System.Runtime.InteropServices.ExternalException){}});menu.Opening+=(sender,args)=>item.Enabled=!string.IsNullOrEmpty(SelectedValue(combo));combo.ContextMenuStrip=menu;};combo.Disposed+=(s,e)=>{if(menu!=null)menu.Dispose();};
 }
}
}
