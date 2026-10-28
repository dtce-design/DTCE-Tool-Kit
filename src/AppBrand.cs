using System;using System.Drawing;using System.Windows.Forms;
namespace BeamScheduleFormatter {static class AppBrand {
 static Icon icon;static Image logo;
 public static Icon Icon{get{if(icon==null)using(var s=typeof(AppBrand).Assembly.GetManifestResourceStream("DtceIcon.ico"))icon=new Icon(s);return icon;}}
 public static Image Logo{get{if(logo==null)using(var s=typeof(AppBrand).Assembly.GetManifestResourceStream("DtceLogo.png"))using(var original=Image.FromStream(s))logo=new Bitmap(original);return logo;}}
 public static void Apply(Form form){form.Icon=Icon;form.Shown+=(s,e)=>PowTheme.Apply(form);}
}}
