using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* View-only apps in the User edition (owner, 23 November 2026: "sa user pwede nila maaccess yung app pero di nila magagamit, pwede sila
   magscroll next tab, pero di sila makakapag input"): without an access code for an app, the app still opens so its tabs can be browsed
   and scrolled, but every input, table, dropdown, check box and command is locked, and a banner offers the access code. */
static class ViewOnly {
 static readonly ConditionalWeakTable<Control,object> locked=new ConditionalWeakTable<Control,object>();
 /* what each locked control was before (owner, 27 November 2026: "bat kapag nag input na ako ng code e, kailangan pang iclose ulit yung window"):
    a code entered later unlocks the open windows in place (Unlock) - no closing and reopening */
 static readonly ConditionalWeakTable<Control,Action> restore=new ConditionalWeakTable<Control,Action>();
 static void Remember(Control c,Action undo){Action old;if(restore.TryGetValue(c,out old)){restore.Remove(c);var both=old;undo=(Action)Delegate.Combine(both,undo);}restore.Add(c,undo);}
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
  var enter=new Button{Text="Enter access code",Dock=DockStyle.Right,Width=170};enter.Click+=(s,e)=>{LoginDeclined=false;using(var login=new LoginForm())if(login.ShowDialog(form)==DialogResult.OK){
   int n=UnlockAllowed();if(!IsLocked(form))MessageBox.Show(form,"Access accepted. This app can now be used"+(n>1?" (with the other open apps your code includes)":"")+".","Access code");else MessageBox.Show(form,"The access code does not include this app.","Access code");}};
  banner.Controls.Add(note);banner.Controls.Add(enter);
  foreach(Control c in form.Controls)LockTree(c);form.Controls.Add(banner);banner.SendToBack();locked.Add(banner,new object());
  Remember(form,()=>{form.AllowDrop=true;if(!banner.IsDisposed){form.Controls.Remove(banner);banner.Dispose();}});}
 /* every open window (and window inside a window) that is locked but now allowed by the access code is unlocked; returns how many */
 internal static int UnlockAllowed(){int n=0;var forms=new List<Form>();Action<Control> walk=null;walk=c=>{var f=c as Form;if(f!=null&&IsLocked(f)&&!forms.Contains(f))forms.Add(f);foreach(Control ch in c.Controls)walk(ch);};
  foreach(Form f in Application.OpenForms.Cast<Form>().ToList())walk(f);
  foreach(var f in forms){bool ok;try{ok=AccessCodes.CanUseApp(AccessCodes.AppFor(f));}catch(Exception){ok=false;}if(ok){Unlock(f);n++;}}return n;}
 /* controls of a form shown outside it (pages moved to another tab control) follow the form's lock */
 static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Form,List<Control>> parts=new System.Runtime.CompilerServices.ConditionalWeakTable<Form,List<Control>>();
 internal static void AddPart(Form form,Control part){if(form==null||part==null)return;parts.GetOrCreateValue(form).Add(part);if(IsLocked(form))LockTree(part);}
 internal static void Unlock(Form form){if(form==null||!IsLocked(form))return;Action<Control> undo=null;undo=c=>{foreach(Control ch in c.Controls.Cast<Control>().ToList())if(!(ch is Form))undo(ch);
   if(IsLocked(c)){locked.Remove(c);Action a;if(restore.TryGetValue(c,out a)){restore.Remove(c);try{a();}catch(Exception){}}}};
  undo(form);List<Control> extra;if(parts.TryGetValue(form,out extra))foreach(var part in extra)if(!part.IsDisposed)undo(part);/* windows hosted inside this one (design sheets in tabs) follow their own access */form.Invalidate(true);}
 static void LockTree(Control c){if(c==null||IsLocked(c))return;locked.Add(c,new object());LockOne(c);foreach(Control child in c.Controls)LockTree(child);
  c.ControlAdded+=(s,e)=>{if(IsLocked(c))LockTree(e.Control);};
  if(c.ContextMenuStrip!=null){var menu=c.ContextMenuStrip;var was=menu.Items.Cast<ToolStripItem>().Select(i=>new{I=i,E=i.Enabled}).ToList();foreach(ToolStripItem item in menu.Items)item.Enabled=false;Remember(c,()=>{foreach(var w in was)w.I.Enabled=w.E;});}}
 static void LockOne(Control c){
  var text=c as TextBoxBase;if(text!=null){bool ro=text.ReadOnly,drop=text.AllowDrop;text.ReadOnly=true;text.AllowDrop=false;text.ReadOnlyChanged+=(s,e)=>{if(IsLocked(text)&&!text.ReadOnly)text.ReadOnly=true;};Remember(text,()=>{text.ReadOnly=ro;text.AllowDrop=drop;});return;}
  var grid=c as DataGridView;if(grid!=null){bool ro=grid.ReadOnly,add=grid.AllowUserToAddRows,del=grid.AllowUserToDeleteRows,drop=grid.AllowDrop;
   grid.ReadOnly=true;grid.ReadOnlyChanged+=(s,e)=>{if(IsLocked(grid)&&!grid.ReadOnly)grid.ReadOnly=true;};grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.AllowDrop=false;
   /* paste and delete shortcuts of the app's own grids do nothing either */grid.KeyDown+=(s,e)=>{if(IsLocked(grid)&&(e.Control||e.KeyCode==Keys.Delete||e.KeyCode==Keys.Back)){e.Handled=true;e.SuppressKeyPress=true;}};
   Remember(grid,()=>{grid.ReadOnly=ro;grid.AllowUserToAddRows=add;grid.AllowUserToDeleteRows=del;grid.AllowDrop=drop;});return;}
  var strip=c as ToolStrip;if(strip!=null){var was=strip.Items.Cast<ToolStripItem>().Select(i=>new{I=i,E=i.Enabled}).ToList();foreach(ToolStripItem item in strip.Items)item.Enabled=false;strip.ItemAdded+=(s,e)=>{if(IsLocked(strip))e.Item.Enabled=false;};
   Remember(strip,()=>{foreach(var w in was)w.I.Enabled=w.E;});return;}
  /* a list box only selects what is shown, so it stays usable for browsing */
  if(c is ComboBox||c is CheckBox||c is RadioButton||c is NumericUpDown||c is DateTimePicker||c is TrackBar||c is CheckedListBox||c is ButtonBase&&!Allowed(c)){bool en=c.Enabled;c.Enabled=false;c.EnabledChanged+=(s,e)=>{if(IsLocked(c)&&c.Enabled)c.Enabled=false;};Remember(c,()=>c.Enabled=en);}}
}
}
