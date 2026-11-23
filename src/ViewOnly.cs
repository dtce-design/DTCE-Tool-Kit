using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* View-only apps in the User edition (owner, 23 November 2026: "sa user pwede nila maaccess yung app pero di nila magagamit, pwede sila
   magscroll next tab, pero di sila makakapag input"): without an access code for an app, the app still opens so its tabs can be browsed
   and scrolled, but every input, table, dropdown, check box and command is locked, and a banner offers the access code. */
static class ViewOnly {
 static readonly ConditionalWeakTable<Control,object> locked=new ConditionalWeakTable<Control,object>();
 /* set while a locked app is being created, so its constructor does not refuse to open */
 [ThreadStatic]static int opening;
 internal static bool Opening{get{return opening>0;}}
 /* the user said no to the access-code prompt in this session: browse without asking again until they choose to enter one */
 internal static bool LoginDeclined;
 internal static bool Supported{get{
  #if ADMIN_APP
  return false;
  #else
  return true;
  #endif
 }}
 internal static Form Create(Func<Form> create){opening++;try{var form=create();Lock(form);return form;}finally{opening--;}}
 internal static bool IsLocked(Control c){object seen;return c!=null&&locked.TryGetValue(c,out seen);}
 static bool Allowed(Control c){var b=c as ButtonBase;if(b==null)return false;string t=(b.Text??"").Trim();return t=="Close"||t=="✕"||t=="Enter access code";}
 internal static void Lock(Form form){if(form==null||IsLocked(form))return;locked.Add(form,new object());form.AllowDrop=false;
  var banner=new Panel{Dock=DockStyle.Top,Height=44,BackColor=Color.FromArgb(255,244,214),Padding=new Padding(12,8,12,8),Name="ViewOnlyBanner"};
  var note=new Label{Dock=DockStyle.Fill,Text="View only - this app is not included in your access code. You can open the tabs and scroll, but inputs, calculations and exports are locked.",ForeColor=Color.FromArgb(110,70,0),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};
  var enter=new Button{Text="Enter access code",Dock=DockStyle.Right,Width=170};enter.Click+=(s,e)=>{LoginDeclined=false;using(var login=new LoginForm())if(login.ShowDialog(form)==DialogResult.OK)
   MessageBox.Show(form,AccessCodes.CanUseApp(AccessCodes.AppFor(form))?"Access accepted. Close this window and open the app again to use it.":"The access code does not include this app.","Access code");};
  banner.Controls.Add(note);banner.Controls.Add(enter);
  foreach(Control c in form.Controls)LockTree(c);form.Controls.Add(banner);banner.SendToBack();locked.Add(banner,new object());}
 static void LockTree(Control c){if(c==null||IsLocked(c))return;locked.Add(c,new object());LockOne(c);foreach(Control child in c.Controls)LockTree(child);
  c.ControlAdded+=(s,e)=>LockTree(e.Control);if(c.ContextMenuStrip!=null)foreach(ToolStripItem item in c.ContextMenuStrip.Items)item.Enabled=false;}
 static void LockOne(Control c){
  var text=c as TextBoxBase;if(text!=null){text.ReadOnly=true;text.AllowDrop=false;text.ReadOnlyChanged+=(s,e)=>{if(!text.ReadOnly)text.ReadOnly=true;};return;}
  var grid=c as DataGridView;if(grid!=null){grid.ReadOnly=true;grid.ReadOnlyChanged+=(s,e)=>{if(!grid.ReadOnly)grid.ReadOnly=true;};grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.AllowDrop=false;
   /* paste and delete shortcuts of the app's own grids do nothing either */grid.KeyDown+=(s,e)=>{if(e.Control||e.KeyCode==Keys.Delete||e.KeyCode==Keys.Back){e.Handled=true;e.SuppressKeyPress=true;}};return;}
  var strip=c as ToolStrip;if(strip!=null){foreach(ToolStripItem item in strip.Items)item.Enabled=false;strip.ItemAdded+=(s,e)=>e.Item.Enabled=false;return;}
  /* a list box only selects what is shown, so it stays usable for browsing */
  if(c is ComboBox||c is CheckBox||c is RadioButton||c is NumericUpDown||c is DateTimePicker||c is TrackBar||c is CheckedListBox||c is ButtonBase&&!Allowed(c)){c.Enabled=false;c.EnabledChanged+=(s,e)=>{if(c.Enabled)c.Enabled=false;};}}
}
}
