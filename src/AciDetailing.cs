using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Windows.Forms;
namespace BeamScheduleFormatter {
/* ACI detailing and schedule option of the Formatters (owner, 22 November 2026: "Add an option sa formatters to use aci detailing and
   schedule"; no notes could be read from the shared GoodNotes link, owner chose the typical ACI SP-66 conventions): bars in ACI
   metric bar notation (#16 = 16 mm bar) and the ACI 318M-14 / ACI SP-66 detailing notes under the schedule. The bar data, marks and
   layout stay the same, so the revised DXF still loads in the Bar Bending Schedule. */
static class AciDetailing {
 public const string Standard="DTCE standard (Ø bars)",Aci="ACI 318M / SP-66 (# bars)";
 internal static ComboBox Combo(){var c=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=230,Margin=new Padding(6,6,10,0)};c.Items.AddRange(new object[]{Standard,Aci});c.SelectedIndex=0;return c;}
 internal static bool IsAci(ComboBox c){return c!=null&&Convert.ToString(c.SelectedItem)==Aci;}
 /* only a bar size (Ø16, %%C16) changes; a column header that is the symbol alone stays */
 internal static string Notation(string text){return System.Text.RegularExpressions.Regex.Replace(text??"",@"(%%[cC]|\u00d8)(?=\s*\d)","#");}
 /* ACI wording of the spacing (owner, 22 November 2026: "automatic convert the standard naming of spacing of ties"):
    beam stirrups "1 - 50mm, 15 - 75mm, AND REST AT 125mm C/C" > "1 @ 50, 15 @ 75, REST @ 125"; slab bars "#10 @ 175mm C/C" >
    "#10 @ 175 O.C."; column hoops "#10 @ 75 (Lo = 585)" > "#10 @ 75 O.C. (Lo = 585)" */
 internal static string Notation(string text,string member){string t=Notation(text);
  if(member=="Beam"){t=System.Text.RegularExpressions.Regex.Replace(t,@"AND\s+REST\s+AT\s+(\d+)\s*mm,?\s*C/C","REST @ $1",System.Text.RegularExpressions.RegexOptions.IgnoreCase);t=System.Text.RegularExpressions.Regex.Replace(t,@"(?<![#\d])(\d+)\s*-\s*(\d+)\s*mm","$1 @ $2",System.Text.RegularExpressions.RegexOptions.IgnoreCase);}
  else if(member=="Slab")t=System.Text.RegularExpressions.Regex.Replace(t,@"@\s*(\d+(?:\.\d+)?)\s*mm\s*C/C","@ $1 O.C.",System.Text.RegularExpressions.RegexOptions.IgnoreCase);
  else if(member=="Column"){t=System.Text.RegularExpressions.Regex.Replace(t,@"^(#\d+\s*@\s*\d+(?:\s*/\s*\d+)?)\s*\(Lo","$1 O.C. (Lo");t=System.Text.RegularExpressions.Regex.Replace(t,@"^(#\d+\s*@\s*\d+)\s+REST$","$1 O.C. REST");}
  return t;}
 /* reading: the ACI beam stirrup wording back to the form the schedule readers know (the DXF stays as written) */
 internal static string ToStandard(string text){if(string.IsNullOrEmpty(text)||text.IndexOf('#')>=0)return text;if(text.IndexOf("REST @",StringComparison.OrdinalIgnoreCase)<0&&text.IndexOf("FREE END",StringComparison.OrdinalIgnoreCase)<0)return text;if(!System.Text.RegularExpressions.Regex.IsMatch(text,@"\d+\s*@\s*\d+"))return text;
  string t=System.Text.RegularExpressions.Regex.Replace(text,@"REST\s*@\s*(\d+)","AND REST AT $1mm C/C",System.Text.RegularExpressions.RegexOptions.IgnoreCase);return System.Text.RegularExpressions.Regex.Replace(t,@"(\d+)\s*@\s*(\d+)(?!\d|mm)","$1 - $2mm");}
 internal static string[] Notes(string member){var common=new List<string>{"DETAILING PER ACI 318M-14 AND ACI SP-66 (ACI DETAILING MANUAL). BAR NOTATION: #16 = 16 mm BAR."};
  if(member=="Beam")common.AddRange(new[]{
   "1. STANDARD HOOKS: 90° WITH 12db EXTENSION (25.3.1). STIRRUPS AND HOOPS: 135° SEISMIC HOOKS WITH 6db, NOT LESS THAN 75 mm, EXTENSION (25.3.2).",
   "2. LAP SPLICES: CLASS B, 1.3 ld (25.5.2). IN SPECIAL MOMENT FRAMES NOT WITHIN A JOINT OR 2h FROM ITS FACE, WITH HOOPS AT d/4 OR 100 mm MAXIMUM OVER THE LAP (18.6.3.3).",
   "3. HOOPS OVER 2h FROM EACH SUPPORT FACE: FIRST HOOP 50 mm FROM THE FACE, SPACING NOT MORE THAN d/4, 6db OF THE SMALLEST LONGITUDINAL BAR OR 150 mm (18.6.4.4).",
   "4. AT LEAST 1/4 OF THE POSITIVE MOMENT BARS EXTEND 150 mm INTO THE SUPPORT (9.7.3.8.2); AT LEAST 1/3 OF THE NEGATIVE MOMENT BARS EXTEND BEYOND THE POINT OF INFLECTION BY d, 12db OR ln/16, WHICHEVER IS LARGER (9.7.3.8.4)."});
  else if(member=="Slab")common.AddRange(new[]{
   "1. AT LEAST 1/4 OF THE BOTTOM BARS EXTEND 150 mm INTO THE SUPPORT (7.7.3.8.2, 8.7.4.1.3). TOP BARS EXTEND BEYOND THE POINT OF INFLECTION BY d, 12db OR ln/16, WHICHEVER IS LARGER (7.7.3.8.4).",
   "2. SPACING OF FLEXURAL BARS NOT MORE THAN 3h OR 450 mm IN ONE-WAY SLABS (7.7.2.3) AND 2h OR 450 mm AT CRITICAL SECTIONS OF TWO-WAY SLABS (8.7.2.2); DISTRIBUTION (SHRINKAGE AND TEMPERATURE) BARS NOT MORE THAN 5h OR 450 mm (24.4.3.3).",
   "3. LAP SPLICES: CLASS B, 1.3 ld (25.5.2). STANDARD HOOKS: 90° WITH 12db EXTENSION (25.3.1)."});
  else if(member=="Column")common.AddRange(new[]{
   "1. LAP SPLICES ONLY IN THE CENTER HALF OF THE CLEAR HEIGHT, CLASS B TENSION LAP SPLICES (18.7.4.3).",
   "2. HOOPS OVER Lo FROM EACH JOINT FACE: Lo NOT LESS THAN THE COLUMN DEPTH, ln/6 OR 450 mm (18.7.5.1); SPACING NOT MORE THAN 1/4 OF THE LEAST DIMENSION, 6db OF THE SMALLEST LONGITUDINAL BAR OR so = 100 + (350 - hx)/3, BETWEEN 100 AND 150 mm (18.7.5.3).",
   "3. HOOPS AS WITHIN Lo CONTINUE THROUGH THE BEAM-COLUMN JOINT (18.8.3.1). HOOPS 135° SEISMIC HOOKS WITH 6db (75 mm MINIMUM) EXTENSION; CROSSTIES 135° / 90°, ALTERNATE ENDS (25.3.2, 18.7.5.2)."});
  return common.ToArray();}
 /* the Column Formatter draws its own template: notation and notes are applied to the drawing items (the preview and the DXF stay alike) */
 internal static void Apply(ColumnSheet sh){foreach(var it in sh.Items)if(it.Kind=="TEXT")it.Text=Notation(it.Text,"Column");
  var note=sh.Items.LastOrDefault(i=>i.Kind=="TEXT"&&(i.Text??"").StartsWith("NOTES:"));double y=note!=null?note.P[1]-(note.W>0?Math.Ceiling(note.Text.Length*note.H*.62/note.W):1)*note.H*1.7-3:sh.Bottom;double width=sh.Right-sh.Left;foreach(var t in Notes("Column")){sh.Text(t,sh.Left,y,1.8,7,false,"TEXT",width);y-=4.2;}sh.Bottom=y-6;}
 /* the Beam and Slab Formatters fill the owner's templates: notation and notes are applied to the DXF */
 internal static void Apply(Dxf d,string member){var es=d.ModelEntities();var texts=es.Where(e=>e.Type=="MTEXT"||e.Type=="TEXT").ToList();if(texts.Count==0)return;
  foreach(var e in texts)foreach(var p in e.P)if(p.Code==1||p.Code==3)p.Value=Notation(p.Value,member);
  var ys=new List<double>();var xs=new List<double>();foreach(var e in es)foreach(var p in e.P){double v;if(!double.TryParse(p.Value,NumberStyles.Float,CultureInfo.InvariantCulture,out v))continue;if(p.Code==10||p.Code==11)xs.Add(v);if(p.Code==20||p.Code==21)ys.Add(v);}
  if(xs.Count==0||ys.Count==0)return;double left=xs.Min(),right=xs.Max(),bottom=ys.Min();var proto=texts.Where(e=>e.Type=="MTEXT").OrderBy(e=>e.Num(40)).FirstOrDefault();if(proto==null)return;
  double h=Math.Max(proto.Num(40),(right-left)/260);long next=1;foreach(var e in d.Records){long handle;if(long.TryParse(e.Get(5),NumberStyles.HexNumber,CultureInfo.InvariantCulture,out handle))next=Math.Max(next,handle+1);}
  int end=d.Records.FindIndex(e=>e.Type=="SECTION"&&e.Get(2)=="ENTITIES");while(end<d.Records.Count&&d.Records[end].Type!="ENDSEC")end++;double y=bottom-h*3;var added=new List<Entity>();
  foreach(var t in Notes(member)){var n=proto.Copy(true);n.P.RemoveAll(p=>p.Code==1||p.Code==3||p.Code==5||p.Code==102);n.P.Insert(1,new Pair(5,(next++).ToString("X")));n.Set(1,t);n.Set(10,left);n.Set(20,y);n.Set(40,h);n.Set(41,right-left);n.Set(71,"1");added.Add(n);y-=h*2.6;}
  d.Records.InsertRange(end,added);foreach(var e in d.Records)for(int n=0;n<e.P.Count-1;n++)if(e.P[n].Code==9&&e.P[n].Value=="$HANDSEED")e.P[n+1].Value=next.ToString("X");}
}
}
