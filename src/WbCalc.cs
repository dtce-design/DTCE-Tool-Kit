using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using System.Text.RegularExpressions;using System.Threading;
namespace BeamScheduleFormatter {
/* Recalculation for the POW worksheet engine (see WbModel.cs). Every formula cell is evaluated on demand, following its references
   across sheets, defined names and tables, so a change in a rate (Labor, PERR, CMPD, Hauling) or a BACKUP quantity flows through
   the DUPA, DETAILED, ABC and SUM sheets the same way Excel computes them. */
sealed class WbRange{public WbSheet Sheet;public int R1,C1,R2,C2;public int Rows{get{return R2-R1+1;}}public int Cols{get{return C2-C1+1;}}}
sealed class WbCtx{public WbSheet Sheet;public int Row,Col;public bool Array;}
sealed partial class WbCalc {
 public readonly WbBook Book;readonly Dictionary<string,WbNode> nameAst=new Dictionary<string,WbNode>(StringComparer.OrdinalIgnoreCase);
 public int Evaluated,ParseErrors,Cycles;public readonly List<string> Problems=new List<string>();
 public WbCalc(WbBook book){Book=book;}
 /* Clears every computed value; constants stay. Run inside a large-stack thread: long reference chains recurse deeply. */
 public void Reset(){foreach(var s in Book.Sheets)foreach(var c in s.Cells.Values){if(c.Formula!=null||c.ArrayMaster!=null){c.State=0;c.Value=null;c.ArrayResult=null;}}}
 public static void RunWithLargeStack(Action action){Exception error=null;var t=new Thread(()=>{try{action();}catch(Exception x){error=x;}},512*1024*1024);t.Start();t.Join();if(error!=null)throw new Exception(error.Message,error);}
 public void CalculateAll(){foreach(var s in Book.Sheets)foreach(var c in s.Cells.Values.ToList())if(c.Formula!=null)CellValue(s,c);}
 public object CellValue(WbSheet sheet,int row,int col){var c=sheet.Get(row,col);return c==null?null:CellValue(sheet,c);}
 public object CellValue(WbSheet sheet,WbCell c){
  if(c.ArrayMaster!=null&&c.Formula==null){var m=c.ArrayMaster;CellValue(sheet,m);if(m.ArrayResult==null)return m.Value;int i=c.Row-m.Row,j=c.Col-m.Col;return i<m.ArrayResult.GetLength(0)&&j<m.ArrayResult.GetLength(1)?Norm(m.ArrayResult[i,j]):WbError.NA;}
  if(c.Formula==null)return c.Cached;
  if(c.State==2)return c.Value;if(c.State==1){Cycles++;return c.Value??c.Cached;}
  c.State=1;object v;try{if(c.Ast==null)c.Ast=WbFormula.Parse(c.Formula);var ctx=new WbCtx{Sheet=sheet,Row=c.Row,Col=c.Col,Array=c.IsArrayMaster};var raw=Eval(c.Ast,ctx);
   if(c.IsArrayMaster){var arr=ToArray(raw);c.ArrayResult=arr;v=Norm(arr[0,0]);}else v=Scalar(raw,ctx);if(v==null)v=0.0;/* a formula that returns an empty cell shows 0 */}
  catch(FormatException x){ParseErrors++;if(Problems.Count<200)Problems.Add(sheet.Name+"!"+WbBook.ColumnName(c.Col)+c.Row+": "+x.Message+" | "+c.Formula);v=WbError.Name;}
  c.Value=v;c.State=2;Evaluated++;return v;}
 static object Norm(object v){return v is WbRange?WbError.Value:v;}
 /* values */
 public object Eval(WbNode n,WbCtx ctx){
  var k=n as WbConst;if(k!=null)return k.Value;
  var r=n as WbRefNode;if(r!=null){if(r.Bad)return WbError.Ref;var sh=r.Sheet==null?ctx.Sheet:Book.Sheet(r.Sheet);if(sh==null)return WbError.Ref;return new WbRange{Sheet=sh,R1=r.R1,C1=r.C1,R2=r.R2,C2=r.C2};}
  var b=n as WbBinary;if(b!=null)return Binary(b,ctx);
  var u=n as WbUnary;if(u!=null){var x=Eval(u.X,ctx);return Map(x,ctx,v=>{double d;var e=Num(v,out d);return e??(object)(-d);});}
  var f=n as WbFuncNode;if(f!=null)return Call(f,ctx);
  var nm=n as WbNameNode;if(nm!=null)return Name(nm,ctx);
  var st=n as WbStructNode;if(st!=null)return Structured(st,ctx);
  var a=n as WbArrayConst;if(a!=null)return a.Values;
  return WbError.Value;}
 object Name(WbNameNode nm,WbCtx ctx){var sheet=nm.Sheet!=null?Book.Sheet(nm.Sheet):ctx.Sheet;string text=null;string key=null;
  if(sheet!=null&&Book.Names.TryGetValue(sheet.Index+"!"+nm.Name,out text))key=sheet.Index+"!"+nm.Name;else if(Book.Names.TryGetValue(nm.Name,out text))key=nm.Name;
  if(text==null){WbTable t;if(Book.Tables.TryGetValue(nm.Name,out t))return TableRange(t,"#Data",null,null,ctx);return WbError.Name;}
  WbNode ast;if(!nameAst.TryGetValue(key,out ast)){try{ast=WbFormula.Parse(text);}catch(FormatException){ast=new WbConst{Value=WbError.Ref};}nameAst[key]=ast;}
  return Eval(ast,new WbCtx{Sheet=sheet??ctx.Sheet,Row=ctx.Row,Col=ctx.Col,Array=ctx.Array});}
 /* structured references: Table[Col], Table[#All], Table[[#All],[Col]], Table[[#This Row],[Col]], Table[@Col], Table[[A]:[B]] */
 object Structured(WbStructNode s,WbCtx ctx){WbTable t=null;
  if(string.IsNullOrEmpty(s.Table)){t=Book.Tables.Values.FirstOrDefault(x=>x.Sheet==ctx.Sheet.Name&&ctx.Row>=x.R1&&ctx.Row<=x.R2&&ctx.Col>=x.C1&&ctx.Col<=x.C2);}else Book.Tables.TryGetValue(s.Table,out t);
  if(t==null)return WbError.Ref;string spec=s.Spec.Trim();if(spec.StartsWith("[")&&spec.EndsWith("]"))spec=spec.Substring(1,spec.Length-2).Trim();
  string area="#Data";string c1=null,c2=null;
  if(spec.StartsWith("@")){area="#This Row";spec=spec.Substring(1).Trim();if(spec.StartsWith("[")&&spec.EndsWith("]"))spec=spec.Substring(1,spec.Length-2);c1=Unescape(spec);if(c1=="")c1=null;}
  else if(!spec.Contains("[")){if(spec.StartsWith("#"))area=spec;else if(spec!="")c1=Unescape(spec);}
  else{var parts=Regex.Matches(spec,@"\[((?:'.|[^\]])*)\]").Cast<Match>().Select(m=>m.Groups[1].Value).ToList();bool colon=Regex.IsMatch(spec,@"\]\s*:\s*\[");var cols=new List<string>();
   foreach(var p in parts){if(p.StartsWith("#"))area=p;else cols.Add(Unescape(p));}if(cols.Count>0)c1=cols[0];if(cols.Count>1&&colon)c2=cols[cols.Count-1];}
  return TableRange(t,area,c1,c2??c1,ctx);}
 static string Unescape(string s){return Regex.Replace(s,"'(.)","$1").Trim();}
 object TableRange(WbTable t,string area,string c1,string c2,WbCtx ctx){var sh=Book.Sheet(t.Sheet);if(sh==null)return WbError.Ref;int a=t.C1,b=t.C2;
  if(c1!=null){a=t.ColumnIndex(c1);b=t.ColumnIndex(c2);if(a<0||b<0)return WbError.Ref;}
  int top=t.R1+t.HeaderRows,bottom=t.R2-t.TotalRows;string ar=area.ToUpperInvariant();
  if(ar=="#ALL"){top=t.R1;bottom=t.R2;}else if(ar=="#HEADERS"){top=t.R1;bottom=t.R1;}else if(ar=="#TOTALS"){top=t.R2;bottom=t.R2;}else if(ar=="#THIS ROW"){top=ctx.Row;bottom=ctx.Row;}
  return new WbRange{Sheet=sh,R1=top,C1=Math.Min(a,b),R2=bottom,C2=Math.Max(a,b)};}
 object Binary(WbBinary b,WbCtx ctx){
  if(b.Op==":"){var x=Eval(b.A,ctx) as WbRange;var y=Eval(b.B,ctx) as WbRange;if(x==null||y==null||x.Sheet!=y.Sheet)return WbError.Ref;return new WbRange{Sheet=x.Sheet,R1=Math.Min(x.R1,y.R1),C1=Math.Min(x.C1,y.C1),R2=Math.Max(x.R2,y.R2),C2=Math.Max(x.C2,y.C2)};}
  var av=Eval(b.A,ctx);var bv=Eval(b.B,ctx);
  if(ctx.Array&&(IsMulti(av)||IsMulti(bv))){var A=ToArray(av);var B=ToArray(bv);int rows=Math.Max(A.GetLength(0),B.GetLength(0)),cols=Math.Max(A.GetLength(1),B.GetLength(1));var res=new object[rows,cols];
   for(int i=0;i<rows;i++)for(int j=0;j<cols;j++){object p=Pick(A,i,j),q=Pick(B,i,j);res[i,j]=Op(b.Op,p,q);}return res;}
  return Op(b.Op,Scalar(av,ctx),Scalar(bv,ctx));}
 static object Pick(object[,] a,int i,int j){int r=a.GetLength(0),c=a.GetLength(1);if(r==1)i=0;if(c==1)j=0;if(i>=r||j>=c)return WbError.NA;return a[i,j];}
 static bool IsMulti(object v){var r=v as WbRange;if(r!=null)return r.Rows>1||r.Cols>1;var a=v as object[,];return a!=null&&(a.GetLength(0)>1||a.GetLength(1)>1);}
 object Map(object x,WbCtx ctx,Func<object,object> f){if(ctx.Array&&IsMulti(x)){var a=ToArray(x);var res=new object[a.GetLength(0),a.GetLength(1)];for(int i=0;i<a.GetLength(0);i++)for(int j=0;j<a.GetLength(1);j++)res[i,j]=f(a[i,j]);return res;}var s=Scalar(x,ctx);if(s is WbError)return s;return f(s);}
 static object Op(string op,object a,object b){if(a is WbError)return a;if(b is WbError)return b;
  switch(op){case "&":return Text(a)+Text(b);
   case "=":return Cmp(a,b)==0;case "<>":return Cmp(a,b)!=0;case "<":return Cmp(a,b)<0;case ">":return Cmp(a,b)>0;case "<=":return Cmp(a,b)<=0;case ">=":return Cmp(a,b)>=0;}
  double x,y;var e=Num(a,out x)??Num(b,out y);if(e!=null)return e;Num(b,out y);
  switch(op){case "+":return x+y;case "-":return x-y;case "*":return x*y;case "/":if(y==0)return WbError.Div0;return x/y;case "^":{var p=Math.Pow(x,y);return double.IsNaN(p)||double.IsInfinity(p)?(object)WbError.Num:p;}}
  return WbError.Value;}
 /* Excel comparison: numbers < text < logical; blank equals 0, "" and FALSE */
 public static int Cmp(object a,object b){if(a==null&&b==null)return 0;if(a==null)a=b is string?(object)"":b is bool?(object)false:0.0;if(b==null)b=a is string?(object)"":a is bool?(object)false:0.0;
  int ra=Rank(a),rb=Rank(b);if(ra!=rb)return ra.CompareTo(rb);if(a is double)return ((double)a).CompareTo((double)b);if(a is bool)return ((bool)a).CompareTo((bool)b);
  return string.Compare(((string)a).ToUpperInvariant(),((string)b).ToUpperInvariant(),StringComparison.Ordinal);}
 static int Rank(object v){return v is double?0:v is string?1:v is bool?2:3;}
 public object Scalar(object v,WbCtx ctx){var r=v as WbRange;if(r!=null){if(r.Rows==1&&r.Cols==1)return CellValue(r.Sheet,r.R1,r.C1);
   /* implicit intersection */if(r.Cols==1&&ctx.Row>=r.R1&&ctx.Row<=r.R2&&ctx.Sheet==r.Sheet)return CellValue(r.Sheet,ctx.Row,r.C1);if(r.Rows==1&&ctx.Col>=r.C1&&ctx.Col<=r.C2&&ctx.Sheet==r.Sheet)return CellValue(r.Sheet,r.R1,ctx.Col);
   if(ctx.Array)return CellValue(r.Sheet,r.R1,r.C1);return WbError.Value;}
  var a=v as object[,];if(a!=null)return a.GetLength(0)>0&&a.GetLength(1)>0?a[0,0]:WbError.Value;return v;}
 public object[,] ToArray(object v){var r=v as WbRange;if(r!=null){int rows=Math.Min(r.Rows,Math.Max(0,r.Sheet.MaxRow-r.R1+1)),cols=Math.Min(r.Cols,Math.Max(0,r.Sheet.MaxCol-r.C1+1));if(r.Rows==WbFormula.MaxRow&&rows<=0)rows=1;if(rows<=0)rows=1;if(cols<=0)cols=1;
   if(r.R2<WbFormula.MaxRow)rows=r.Rows;if(r.C2<WbFormula.MaxCol)cols=r.Cols;var res=new object[rows,cols];for(int i=0;i<rows;i++)for(int j=0;j<cols;j++)res[i,j]=CellValue(r.Sheet,r.R1+i,r.C1+j);return res;}
  var a=v as object[,];if(a!=null)return a;return new object[,]{{v}};}
 /* cells of a range in row order, stopping at the sheet's used area */
 public IEnumerable<object> Values(object v){var r=v as WbRange;if(r!=null){int r2=Math.Min(r.R2,r.Sheet.MaxRow),c2=Math.Min(r.C2,r.Sheet.MaxCol);for(int i=r.R1;i<=r2;i++)for(int j=r.C1;j<=c2;j++)yield return CellValue(r.Sheet,i,j);yield break;}
  var a=v as object[,];if(a!=null){foreach(var x in a)yield return x;yield break;}yield return v;}
 public static WbError Num(object v,out double d){d=0;if(v==null)return null;if(v is double){d=(double)v;return null;}if(v is bool){d=(bool)v?1:0;return null;}var e=v as WbError;if(e!=null)return e;
  var s=v as string;if(s!=null){if(s.Trim()==""){return WbError.Value;}if(double.TryParse(s.Trim().Replace(",",""),NumberStyles.Float,CultureInfo.InvariantCulture,out d))return null;DateTime dt;if(DateTime.TryParse(s,CultureInfo.InvariantCulture,DateTimeStyles.None,out dt)){d=Serial(dt);return null;}return WbError.Value;}return WbError.Value;}
 public static string Text(object v){if(v==null)return "";if(v is bool)return (bool)v?"TRUE":"FALSE";if(v is double)return NumText((double)v);return v.ToString();}
 public static string NumText(double d){if(d==Math.Floor(d)&&Math.Abs(d)<1e15)return d.ToString("0",CultureInfo.InvariantCulture);string s=d.ToString("G15",CultureInfo.InvariantCulture);if(s.Contains("E")){double a=Math.Abs(d);if(a>=1e-9&&a<1e15)s=((decimal)d).ToString(CultureInfo.InvariantCulture);else s=d.ToString("0.##############E+00",CultureInfo.InvariantCulture);}return s;}
 public static double Serial(DateTime d){return (d.Date-new DateTime(1899,12,30)).TotalDays;}
 public static DateTime FromSerial(double s){return new DateTime(1899,12,30).AddDays(Math.Floor(s));}
}
}
