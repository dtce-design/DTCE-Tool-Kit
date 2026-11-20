using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;using System.Text.RegularExpressions;
namespace BeamScheduleFormatter {
/* Worksheet functions used by the DPWH V6.02 POW workbook (IF, SUMIF, INDEX/MATCH, ROUND, dates, text ...). */
sealed partial class WbCalc {
 static readonly HashSet<string> ArrayArgs=new HashSet<string>{"SUMPRODUCT"};
 object Arg(WbFuncNode f,int i,WbCtx ctx){if(i>=f.Args.Count||f.Args[i]==null)return null;return Eval(f.Args[i],ctx);}
 object S(WbFuncNode f,int i,WbCtx ctx){return Scalar(Arg(f,i,ctx),ctx);}
 bool Has(WbFuncNode f,int i){return i<f.Args.Count&&f.Args[i]!=null;}
 WbError D(WbFuncNode f,int i,WbCtx ctx,out double d){var v=S(f,i,ctx);if(v==null){d=0;return null;}return Num(v,out d);}
 WbError Bool(object v,out bool b){b=false;if(v==null)return null;if(v is bool){b=(bool)v;return null;}if(v is double){b=(double)v!=0;return null;}var e=v as WbError;if(e!=null)return e;var s=v as string;if(s!=null){if(s.Equals("TRUE",StringComparison.OrdinalIgnoreCase)){b=true;return null;}if(s.Equals("FALSE",StringComparison.OrdinalIgnoreCase))return null;}return WbError.Value;}
 WbCtx ArrayCtx(WbCtx c){return c.Array?c:new WbCtx{Sheet=c.Sheet,Row=c.Row,Col=c.Col,Array=true};}
 static readonly HashSet<string> Liftable=new HashSet<string>{"ROUND","ROUNDUP","ROUNDDOWN","INT","ABS","SQRT","SIN","COS","TAN","RADIANS","MOD","YEAR","MONTH","DAY","WEEKDAY","DATE","EOMONTH","EDATE","UPPER","LOWER","TRIM","LEN","LEFT","RIGHT","MID","VALUE","CHAR","CODE","ISERROR","ISNUMBER","ISTEXT","ISBLANK","ISNA","CEILING.MATH","CEILING","FLOOR","CONCATENATE","SEARCH","FIND","SUBSTITUTE","TEXT","N","NOT","DATEDIF","IFNA"};
 /* array formulas: a scalar function applied to a range works cell by cell */
 object Lift(WbFuncNode f,WbCtx ctx){var vals=f.Args.Select(x=>x==null?null:Eval(x,ctx)).ToList();if(!vals.Any(IsMulti))return null;var arrs=vals.Select(v=>v==null?null:ToArray(v)).ToList();int rows=arrs.Where(x=>x!=null).Max(x=>x.GetLength(0)),cols=arrs.Where(x=>x!=null).Max(x=>x.GetLength(1));
  var res=new object[rows,cols];var plain=new WbCtx{Sheet=ctx.Sheet,Row=ctx.Row,Col=ctx.Col,Array=false};
  for(int i=0;i<rows;i++)for(int j=0;j<cols;j++){var one=new WbFuncNode{Name=f.Name};foreach(var a in arrs)one.Args.Add(a==null?null:new WbConst{Value=Pick(a,i,j)});res[i,j]=Scalar(Call(one,plain),plain);}return res;}
 object Call(WbFuncNode f,WbCtx ctx){double a,b,c;WbError e;
  if(ctx.Array&&Liftable.Contains(f.Name)){var lifted=Lift(f,ctx);if(lifted!=null)return lifted;}
  switch(f.Name){
   case "IF":{var cond=S(f,0,ctx);if(ctx.Array&&IsMulti(Arg(f,0,ctx))){var arr=ToArray(Arg(f,0,ctx));var res=new object[arr.GetLength(0),arr.GetLength(1)];var t=Has(f,1)?ToArray(Arg(f,1,ctx)):null;var o=Has(f,2)?ToArray(Arg(f,2,ctx)):null;
     for(int i=0;i<arr.GetLength(0);i++)for(int j=0;j<arr.GetLength(1);j++){bool bb;var be=Bool(arr[i,j],out bb);res[i,j]=be!=null?(object)be:bb?(t==null?(object)true:Pick(t,i,j)):(f.Args.Count>2?(o==null?(object)0.0:Pick(o,i,j)):(object)false);}return res;}
    bool ok;e=Bool(cond,out ok);if(e!=null)return e;if(ok)return Has(f,1)?Arg(f,1,ctx):(f.Args.Count>1?(object)0.0:true);return f.Args.Count>2?(Has(f,2)?Arg(f,2,ctx):(object)0.0):false;}
   case "IFERROR":{var v=Arg(f,0,ctx);if(ctx.Array&&IsMulti(v)){var arr=ToArray(v);var alt=Arg(f,1,ctx);var res=new object[arr.GetLength(0),arr.GetLength(1)];for(int i=0;i<arr.GetLength(0);i++)for(int j=0;j<arr.GetLength(1);j++)res[i,j]=arr[i,j] is WbError?Scalar(alt,ctx)??0.0:arr[i,j];return res;}var s=Scalar(v,ctx);if(s is WbError)return Has(f,1)?Arg(f,1,ctx):0.0;return v is WbRange?v:s;}
   case "IFNA":{var v=Arg(f,0,ctx);var s=Scalar(v,ctx);if(s==WbError.NA)return Has(f,1)?Arg(f,1,ctx):0.0;return v is WbRange?v:s;}
   case "ISERROR":return S(f,0,ctx) is WbError;
   case "NA":return WbError.NA;
   case "AND":case "OR":{bool any=false,all=true;bool seen=false;for(int i=0;i<f.Args.Count;i++){foreach(var v in Values(Arg(f,i,ctx))){if(v==null||v is string)continue;bool bb;e=Bool(v,out bb);if(e!=null)return e;seen=true;if(bb)any=true;else all=false;}}if(!seen)return WbError.Value;return f.Name=="AND"?all:any;}
   case "NOT":{bool bb;e=Bool(S(f,0,ctx),out bb);if(e!=null)return e;return !bb;}
   case "SUM":{double s=0;for(int i=0;i<f.Args.Count;i++){var v=Arg(f,i,ctx);if(v is WbRange||v is object[,]){foreach(var x in Values(v)){if(x is double)s+=(double)x;else if(x is WbError)return x;}}else{v=Scalar(v,ctx);if(v==null)continue;e=Num(v,out a);if(e!=null)return e;s+=a;}}return s;}
   case "AVERAGE":case "MAX":case "MIN":case "COUNT":{var list=new List<double>();for(int i=0;i<f.Args.Count;i++){var v=Arg(f,i,ctx);if(v is WbRange||v is object[,]){foreach(var x in Values(v)){if(x is double)list.Add((double)x);else if(x is WbError&&f.Name!="COUNT")return x;}}else{v=Scalar(v,ctx);if(v==null)continue;if(f.Name=="COUNT"){double q;if(Num(v,out q)==null&&!(v is string&&((string)v).Trim()==""))list.Add(q);continue;}e=Num(v,out a);if(e!=null)return e;list.Add(a);}}
    if(f.Name=="COUNT")return (double)list.Count;if(list.Count==0)return f.Name=="AVERAGE"?(object)WbError.Div0:0.0;return f.Name=="AVERAGE"?list.Average():f.Name=="MAX"?list.Max():list.Min();}
   case "COUNTA":{double n=0;for(int i=0;i<f.Args.Count;i++){var v=Arg(f,i,ctx);if(v is WbRange||v is object[,]){foreach(var x in Values(v))if(x!=null)n++;}else if(Has(f,i))n++;}return n;}
   case "ROUND":case "ROUNDUP":case "ROUNDDOWN":{e=D(f,0,ctx,out a);if(e!=null)return e;double digits=0;if(Has(f,1)){e=D(f,1,ctx,out digits);if(e!=null)return e;}return Round(a,(int)Math.Truncate(digits),f.Name);}
   case "INT":e=D(f,0,ctx,out a);if(e!=null)return e;return Math.Floor(a);
   case "ABS":e=D(f,0,ctx,out a);if(e!=null)return e;return Math.Abs(a);
   case "SQRT":e=D(f,0,ctx,out a);if(e!=null)return e;if(a<0)return WbError.Num;return Math.Sqrt(a);
   case "SIN":e=D(f,0,ctx,out a);if(e!=null)return e;return Math.Sin(a);
   case "COS":e=D(f,0,ctx,out a);if(e!=null)return e;return Math.Cos(a);
   case "TAN":e=D(f,0,ctx,out a);if(e!=null)return e;return Math.Tan(a);
   case "RADIANS":e=D(f,0,ctx,out a);if(e!=null)return e;return a*Math.PI/180;
   case "PI":return Math.PI;
   case "MOD":e=D(f,0,ctx,out a)??D(f,1,ctx,out b);if(e!=null)return e;D(f,1,ctx,out b);if(b==0)return WbError.Div0;return a-b*Math.Floor(a/b);
   case "CEILING.MATH":case "CEILING":{e=D(f,0,ctx,out a);if(e!=null)return e;b=1;if(Has(f,1)){e=D(f,1,ctx,out b);if(e!=null)return e;}if(f.Name=="CEILING.MATH"&&!Has(f,1))b=1;if(b==0)return 0.0;b=Math.Abs(b);double mode=0;if(Has(f,2))D(f,2,ctx,out mode);
    double q=a/b;double r=(a<0&&mode!=0)?-Math.Ceiling(Math.Round(-q,9)):Math.Ceiling(Math.Round(q,9));return r*b;}
   case "FLOOR":{e=D(f,0,ctx,out a);if(e!=null)return e;D(f,1,ctx,out b);if(b==0)return 0.0;return Math.Floor(Math.Round(a/b,9))*b;}
   case "SUMPRODUCT":{var actx=ArrayCtx(ctx);object[,] acc=null;foreach(var arg in f.Args){var m=ToArray(Eval(arg,actx));if(acc==null){acc=new object[m.GetLength(0),m.GetLength(1)];for(int i=0;i<m.GetLength(0);i++)for(int j=0;j<m.GetLength(1);j++)acc[i,j]=m[i,j] is double?m[i,j]:(m[i,j] is WbError?m[i,j]:(object)0.0);continue;}
     if(m.GetLength(0)!=acc.GetLength(0)||m.GetLength(1)!=acc.GetLength(1))return WbError.Value;for(int i=0;i<m.GetLength(0);i++)for(int j=0;j<m.GetLength(1);j++){if(acc[i,j] is WbError)continue;if(m[i,j] is WbError){acc[i,j]=m[i,j];continue;}acc[i,j]=(double)acc[i,j]*(m[i,j] is double?(double)m[i,j]:0);}}
    double s=0;if(acc!=null)foreach(var x in acc){if(x is WbError)return x;s+=(double)x;}return s;}
   case "SUMIF":case "COUNTIF":case "SUMIFS":case "COUNTIFS":case "AVERAGEIF":return Conditional(f,ctx);
   case "INDEX":return Index(f,ctx);
   case "MATCH":return Match(f,ctx);
   case "VLOOKUP":case "HLOOKUP":return Lookup(f,ctx);
   case "SMALL":case "LARGE":{var list=new List<double>();foreach(var x in Values(Arg(f,0,ArrayCtx(ctx)))){if(x is double)list.Add((double)x);else if(x is WbError)return x;}e=D(f,1,ctx,out a);if(e!=null)return e;int k=(int)Math.Ceiling(a-1e-9);if(k<1||k>list.Count)return WbError.Num;list.Sort();return f.Name=="SMALL"?list[k-1]:list[list.Count-k];}
   case "ROW":case "COLUMN":{if(!Has(f,0))return (double)(f.Name=="ROW"?ctx.Row:ctx.Col);var r=Arg(f,0,ctx) as WbRange;if(r==null)return WbError.Value;
    if(ctx.Array&&(f.Name=="ROW"?r.Rows>1:r.Cols>1)){int n=f.Name=="ROW"?(r.R2>=WbFormula.MaxRow?Math.Min(r.Rows,Math.Max(1,r.Sheet.MaxRow-r.R1+1)):r.Rows):r.Cols;var res=f.Name=="ROW"?new object[n,1]:new object[1,n];for(int i=0;i<n;i++){if(f.Name=="ROW")res[i,0]=(double)(r.R1+i);else res[0,i]=(double)(r.C1+i);}return res;}return (double)(f.Name=="ROW"?r.R1:r.C1);}
   case "DATE":{double y,m,d;e=D(f,0,ctx,out y)??D(f,1,ctx,out m)??D(f,2,ctx,out d);if(e!=null)return e;D(f,1,ctx,out m);D(f,2,ctx,out d);return DateSerial((int)y,(int)m,(int)d);}
   case "YEAR":case "MONTH":case "DAY":{e=D(f,0,ctx,out a);if(e!=null)return e;var dt=FromSerial(a);return (double)(f.Name=="YEAR"?dt.Year:f.Name=="MONTH"?dt.Month:dt.Day);}
   case "WEEKDAY":{e=D(f,0,ctx,out a);if(e!=null)return e;double type=1;if(Has(f,1))D(f,1,ctx,out type);int dow=(int)FromSerial(a).DayOfWeek;/* Sunday=0 */
    switch((int)type){case 1:case 17:return (double)(dow+1);case 2:case 11:return (double)(dow==0?7:dow);case 3:return (double)(dow==0?6:dow-1);default:{int start=(int)type-10;/* 12=Tue..17=Sun */int mon=dow==0?7:dow;return (double)(((mon-start+7)%7)+1);}}}
   case "EOMONTH":case "EDATE":{e=D(f,0,ctx,out a)??D(f,1,ctx,out b);if(e!=null)return e;D(f,1,ctx,out b);var dt=FromSerial(a).AddMonths((int)Math.Truncate(b));if(f.Name=="EOMONTH")dt=new DateTime(dt.Year,dt.Month,DateTime.DaysInMonth(dt.Year,dt.Month));return Serial(dt);}
   case "WORKDAY":{e=D(f,0,ctx,out a)??D(f,1,ctx,out b);if(e!=null)return e;D(f,1,ctx,out b);var hol=new HashSet<double>();if(Has(f,2))foreach(var x in Values(Arg(f,2,ctx)))if(x is double)hol.Add(Math.Floor((double)x));
    double day=Math.Floor(a);int left=(int)Math.Truncate(b);int step=left<0?-1:1;while(left!=0){day+=step;var dt=FromSerial(day);if(dt.DayOfWeek==DayOfWeek.Saturday||dt.DayOfWeek==DayOfWeek.Sunday||hol.Contains(day))continue;left-=step;}return day;}
   case "DATEDIF":{e=D(f,0,ctx,out a)??D(f,1,ctx,out b);if(e!=null)return e;D(f,1,ctx,out b);var unit=Text(S(f,2,ctx)).ToUpperInvariant();var d1=FromSerial(a);var d2=FromSerial(b);if(d2<d1)return WbError.Num;
    int months=(d2.Year-d1.Year)*12+d2.Month-d1.Month;if(d2.Day<d1.Day)months--;switch(unit){case "D":return Math.Floor(b)-Math.Floor(a);case "M":return (double)months;case "Y":return (double)(months/12);case "YM":return (double)(months%12);case "MD":{var anchor=d1.AddMonths(months);return (d2-anchor).TotalDays;}case "YD":{var anchor=d1.AddYears(months/12);return (d2-anchor).TotalDays;}}return WbError.Num;}
   case "TODAY":return Serial(DateTime.Today);
   case "CONCATENATE":case "CONCAT":{var sb=new StringBuilder();for(int i=0;i<f.Args.Count;i++){var v=S(f,i,ctx);if(v is WbError)return v;sb.Append(Text(v));}return sb.ToString();}
   case "CHAR":e=D(f,0,ctx,out a);if(e!=null)return e;return ((char)(int)a).ToString();
   case "CODE":{var s=Text(S(f,0,ctx));if(s=="")return WbError.Value;return (double)Encoding.GetEncoding(1252).GetBytes(s.Substring(0,1))[0];}
   case "UPPER":{var v=S(f,0,ctx);if(v is WbError)return v;return Text(v).ToUpperInvariant();}
   case "LOWER":{var v=S(f,0,ctx);if(v is WbError)return v;return Text(v).ToLowerInvariant();}
   case "TRIM":{var v=S(f,0,ctx);if(v is WbError)return v;return Regex.Replace(Text(v).Trim()," +"," ");}
   case "LEN":{var v=S(f,0,ctx);if(v is WbError)return v;return (double)Text(v).Length;}
   case "LEFT":case "RIGHT":{var v=S(f,0,ctx);if(v is WbError)return v;var s=Text(v);double n=1;if(Has(f,1)){e=D(f,1,ctx,out n);if(e!=null)return e;}int k=Math.Max(0,Math.Min(s.Length,(int)n));return f.Name=="LEFT"?s.Substring(0,k):s.Substring(s.Length-k);}
   case "MID":{var v=S(f,0,ctx);if(v is WbError)return v;var s=Text(v);e=D(f,1,ctx,out a)??D(f,2,ctx,out b);if(e!=null)return e;D(f,2,ctx,out b);int st=(int)a-1;if(st<0)return WbError.Value;if(st>=s.Length)return "";return s.Substring(st,Math.Min((int)b,s.Length-st));}
   case "SEARCH":case "FIND":{var find=Text(S(f,0,ctx));var within=Text(S(f,1,ctx));double st=1;if(Has(f,2))D(f,2,ctx,out st);int start=(int)st-1;if(start<0||start>within.Length)return WbError.Value;
    if(f.Name=="FIND"){int p=within.IndexOf(find,start,StringComparison.Ordinal);return p<0?(object)WbError.Value:(double)(p+1);}var rx=new Regex(WildRegex(find,false),RegexOptions.IgnoreCase);var m=rx.Match(within,start);return m.Success?(object)(double)(m.Index+1):WbError.Value;}
   case "SUBSTITUTE":{var s=Text(S(f,0,ctx));var old=Text(S(f,1,ctx));var nw=Text(S(f,2,ctx));if(old=="")return s;if(!Has(f,3))return s.Replace(old,nw);D(f,3,ctx,out a);int inst=(int)a,idx=-1;for(int i=0;i<inst;i++){idx=s.IndexOf(old,idx+1,StringComparison.Ordinal);if(idx<0)return s;}return s.Substring(0,idx)+nw+s.Substring(idx+old.Length);}
   case "VALUE":{var v=S(f,0,ctx);if(v is double)return v;e=Num(Text(v),out a);if(e!=null)return e;return a;}
   case "TEXT":{var v=S(f,0,ctx);if(v is WbError)return v;double d;if(Num(v,out d)!=null)return Text(v);return WbFormat.Format(d,Text(S(f,1,ctx)));}
   case "ADDRESS":{e=D(f,0,ctx,out a)??D(f,1,ctx,out b);if(e!=null)return e;D(f,1,ctx,out b);double abs=1;if(Has(f,2))D(f,2,ctx,out abs);string col=WbBook.ColumnName((int)b),row=((int)a).ToString(CultureInfo.InvariantCulture);
    int ab=(int)abs;string txt=(ab==1||ab==3?"$":"")+col+(ab==1||ab==2?"$":"")+row;if(Has(f,4)){var sh=Text(S(f,4,ctx));txt=(Regex.IsMatch(sh,@"^[A-Za-z_][A-Za-z0-9_\.]*$")?sh:"'"+sh.Replace("'","''")+"'")+"!"+txt;}return txt;}
   case "INDIRECT":{var t=Text(S(f,0,ctx));try{var node=WbFormula.Parse(t);if(!(node is WbRefNode||node is WbNameNode||node is WbBinary))return WbError.Ref;return Eval(node,ctx);}catch(FormatException){return WbError.Ref;}}
   case "CONVERT":{e=D(f,0,ctx,out a);if(e!=null)return e;var from=Text(S(f,1,ctx));var to=Text(S(f,2,ctx));double fa,ta;if(!Units.TryGetValue(from,out fa)||!Units.TryGetValue(to,out ta))return WbError.NA;return a*fa/ta;}
   case "N":{var v=S(f,0,ctx);return v is double?v:v is bool?(object)((bool)v?1.0:0.0):0.0;}
   case "ISNUMBER":return S(f,0,ctx) is double;case "ISTEXT":return S(f,0,ctx) is string;case "ISBLANK":return S(f,0,ctx)==null;case "ISNA":return S(f,0,ctx)==WbError.NA;
   case "TRUE":return true;case "FALSE":return false;
  }
  if(Problems.Count<200)Problems.Add("Unsupported function "+f.Name+" at "+ctx.Sheet.Name+"!"+WbBook.ColumnName(ctx.Col)+ctx.Row);return WbError.Name;}
 static readonly Dictionary<string,double> Units=new Dictionary<string,double>{{"m",1},{"cm",0.01},{"mm",0.001},{"km",1000},{"in",0.0254},{"ft",0.3048},{"yd",0.9144},{"mi",1609.344},{"kg",1},{"g",0.001},{"lbm",0.45359237},{"ton",907.18474},{"l",0.001},{"L",0.001},{"gal",0.003785411784}};
 public static object Round(double x,int digits,string mode){try{decimal d=(decimal)x;decimal scale=1;for(int i=0;i<Math.Abs(digits);i++)scale*=10;
   if(mode=="ROUND"){if(digits>=0)return (double)Math.Round(d,Math.Min(digits,28),MidpointRounding.AwayFromZero);return (double)(Math.Round(d/scale,0,MidpointRounding.AwayFromZero)*scale);}
   decimal v=digits>=0?d*scale:d/scale;decimal t=mode=="ROUNDUP"?(v<0?Math.Floor(v):Math.Ceiling(v)):Math.Truncate(v);return (double)(digits>=0?t/scale:t*scale);}
  catch(OverflowException){double p=Math.Pow(10,digits);return mode=="ROUND"?Math.Round(x*p,MidpointRounding.AwayFromZero)/p:mode=="ROUNDUP"?(x<0?Math.Floor(x*p):Math.Ceiling(x*p))/p:Math.Truncate(x*p)/p;}}
 static double DateSerial(int y,int m,int d){if(y<1900)y+=1900;var dt=new DateTime(y,1,1).AddMonths(m-1).AddDays(d-1);return Serial(dt);}
 static string WildRegex(string pattern,bool whole){var sb=new StringBuilder();if(whole)sb.Append('^');for(int i=0;i<pattern.Length;i++){char ch=pattern[i];if(ch=='~'&&i+1<pattern.Length){sb.Append(Regex.Escape(pattern[++i].ToString()));continue;}if(ch=='*')sb.Append(".*");else if(ch=='?')sb.Append('.');else sb.Append(Regex.Escape(ch.ToString()));}if(whole)sb.Append('$');return sb.ToString();}
 /* criteria: "=x", "<>x", ">5", "x*" ... */
 Func<object,bool> Criteria(object crit){if(crit is WbError)return v=>false;string op="=";object target=crit;var s=crit as string;
  if(s!=null){foreach(var o in new[]{"<>",">=","<=","=",">","<"})if(s.StartsWith(o)){op=o;s=s.Substring(o.Length);break;}double d;if(s!=""&&double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out d))target=d;else if(s.Equals("TRUE",StringComparison.OrdinalIgnoreCase))target=true;else if(s.Equals("FALSE",StringComparison.OrdinalIgnoreCase))target=false;else target=s;}
  if(target==null)target=0.0;var ts=target as string;
  if(ts!=null&&(op=="="||op=="<>")){if(ts==""){return op=="="?(Func<object,bool>)(v=>v==null||(v is string&&(string)v=="")):(v=>!(v==null||(v is string&&(string)v=="")));}
   if(ts.IndexOfAny(new[]{'*','?','~'})>=0){var rx=new Regex(WildRegex(ts,true),RegexOptions.IgnoreCase|RegexOptions.Singleline);if(op=="=")return v=>v is string&&rx.IsMatch((string)v);return v=>!(v is string&&rx.IsMatch((string)v));}
   if(op=="=")return v=>v is string&&string.Equals((string)v,ts,StringComparison.OrdinalIgnoreCase);return v=>!(v is string&&string.Equals((string)v,ts,StringComparison.OrdinalIgnoreCase));}
  if(target is double){double t=(double)target;Func<object,double?> num=v=>v is double?(double?)(double)v:null;
   switch(op){case "=":return v=>{var x=num(v);return x.HasValue&&x.Value==t;};case "<>":return v=>{var x=num(v);return !(x.HasValue&&x.Value==t);};case ">":return v=>{var x=num(v);return x.HasValue&&x.Value>t;};case "<":return v=>{var x=num(v);return x.HasValue&&x.Value<t;};case ">=":return v=>{var x=num(v);return x.HasValue&&x.Value>=t;};case "<=":return v=>{var x=num(v);return x.HasValue&&x.Value<=t;};}}
  if(target is bool){bool t=(bool)target;if(op=="=")return v=>v is bool&&(bool)v==t;if(op=="<>")return v=>!(v is bool&&(bool)v==t);}
  if(ts!=null){switch(op){case ">":return v=>v is string&&Cmp(v,ts)>0;case "<":return v=>v is string&&Cmp(v,ts)<0;case ">=":return v=>v is string&&Cmp(v,ts)>=0;case "<=":return v=>v is string&&Cmp(v,ts)<=0;}}
  return v=>false;}
 object Conditional(WbFuncNode f,WbCtx ctx){bool multi=f.Name.EndsWith("S");var ranges=new List<WbRange>();var crits=new List<Func<object,bool>>();WbRange sumRange=null;
  if(multi){int start=f.Name=="COUNTIFS"?0:1;if(start==1)sumRange=Arg(f,0,ctx) as WbRange;for(int i=start;i+1<f.Args.Count;i+=2){var r=Arg(f,i,ctx) as WbRange;if(r==null)return WbError.Value;ranges.Add(r);crits.Add(Criteria(S(f,i+1,ctx)));}}
  else{var r=Arg(f,0,ctx) as WbRange;if(r==null)return WbError.Value;ranges.Add(r);crits.Add(Criteria(S(f,1,ctx)));if(f.Name!="COUNTIF"){sumRange=Has(f,2)?Arg(f,2,ctx) as WbRange:r;if(sumRange==null)return WbError.Value;sumRange=new WbRange{Sheet=sumRange.Sheet,R1=sumRange.R1,C1=sumRange.C1,R2=sumRange.R1+r.Rows-1,C2=sumRange.C1+r.Cols-1};}}
  var first=ranges[0];int rows=Math.Min(first.Rows,Math.Max(0,ranges.Max(x=>x.Sheet.MaxRow-x.R1+1))),cols=first.Cols;if(sumRange!=null)rows=Math.Min(first.Rows,Math.Max(rows,sumRange.Sheet.MaxRow-sumRange.R1+1));
  double sum=0,count=0;
  for(int i=0;i<rows;i++)for(int j=0;j<cols;j++){bool ok=true;for(int k=0;k<ranges.Count&&ok;k++){var r=ranges[k];if(r.R1+i>r.Sheet.MaxRow&&r.R1+i>0){if(!crits[k](null))ok=false;continue;}if(!crits[k](CellValue(r.Sheet,r.R1+i,r.C1+j)))ok=false;}
   if(!ok)continue;count++;if(sumRange!=null){var v=CellValue(sumRange.Sheet,sumRange.R1+i,sumRange.C1+j);if(v is double)sum+=(double)v;else if(v is WbError)return v;}}
  if(f.Name.StartsWith("COUNT"))return count;if(f.Name=="AVERAGEIF")return count==0?(object)WbError.Div0:sum/count;return sum;}
 object Index(WbFuncNode f,WbCtx ctx){var src=Arg(f,0,ArrayCtx(ctx));double r=0,c=0;WbError e;if(Has(f,1)){e=D(f,1,ctx,out r);if(e!=null)return e;}if(Has(f,2)){e=D(f,2,ctx,out c);if(e!=null)return e;}int ri=(int)r,ci=(int)c;
  var rg=src as WbRange;if(rg!=null){if(rg.Rows==1&&!Has(f,2)&&f.Args.Count<3){ci=ri;ri=1;if(rg.Cols==1){ci=1;}}else if(rg.Cols==1&&ci==0)ci=1;
   if(ri<0||ci<0||ri>rg.Rows||ci>rg.Cols)return WbError.Ref;int r1=ri==0?rg.R1:rg.R1+ri-1,r2=ri==0?rg.R2:r1,c1=ci==0?rg.C1:rg.C1+ci-1,c2=ci==0?rg.C2:c1;return new WbRange{Sheet=rg.Sheet,R1=r1,C1=c1,R2=r2,C2=c2};}
  var arr=src as object[,];if(arr==null)return ri<=1&&ci<=1?src:WbError.Ref;if(arr.GetLength(0)==1&&!Has(f,2)){ci=ri;ri=1;}if(ci==0&&arr.GetLength(1)==1)ci=1;if(ri==0&&arr.GetLength(0)==1)ri=1;if(ri<1||ci<1||ri>arr.GetLength(0)||ci>arr.GetLength(1))return WbError.Ref;return arr[ri-1,ci-1];}
 object Match(WbFuncNode f,WbCtx ctx){var look=S(f,0,ctx);if(look is WbError)return look;var src=Arg(f,1,ArrayCtx(ctx));double type=1;if(Has(f,2)){var e=D(f,2,ctx,out type);if(e!=null)return e;}
  IList<object> list;var rg=src as WbRange;if(rg!=null){if(rg.Rows>1&&rg.Cols>1)return WbError.NA;list=Values(rg).ToList();}else list=Values(src).ToList();
  if(look==null)look=0.0;
  if(type==0){Func<object,bool> eq;var ls=look as string;if(ls!=null&&ls.IndexOfAny(new[]{'*','?','~'})>=0){var rx=new Regex(WildRegex(ls,true),RegexOptions.IgnoreCase|RegexOptions.Singleline);eq=v=>v is string&&rx.IsMatch((string)v);}else eq=v=>v!=null&&Rank(v)==Rank(look)&&Cmp(v,look)==0;
   for(int i=0;i<list.Count;i++)if(eq(list[i]))return (double)(i+1);return WbError.NA;}
  int best=-1;for(int i=0;i<list.Count;i++){var v=list[i];if(v==null||Rank(v)!=Rank(look))continue;int c=Cmp(v,look);if(type>0){if(c<=0)best=i;else break;}else{if(c>=0)best=i;else break;}}return best<0?(object)WbError.NA:(double)(best+1);}
 object Lookup(WbFuncNode f,WbCtx ctx){var look=S(f,0,ctx);if(look is WbError)return look;var table=Arg(f,1,ctx);double idx;var e=D(f,2,ctx,out idx);if(e!=null)return e;bool approx=true;if(Has(f,3)){bool bb;e=Bool(S(f,3,ctx),out bb);if(e!=null)return e;approx=bb;}
  var arr=ToArray(table);bool v=f.Name=="VLOOKUP";int n=v?arr.GetLength(0):arr.GetLength(1);int k=(int)idx;if(k<1||k>(v?arr.GetLength(1):arr.GetLength(0)))return WbError.Ref;int found=-1;
  for(int i=0;i<n;i++){var x=v?arr[i,0]:arr[0,i];if(x==null)continue;if(!approx){var ls=look as string;bool hit=ls!=null&&ls.IndexOfAny(new[]{'*','?'})>=0?(x is string&&Regex.IsMatch((string)x,WildRegex(ls,true),RegexOptions.IgnoreCase)):(Rank(x)==Rank(look)&&Cmp(x,look)==0);if(hit){found=i;break;}}
   else{if(Rank(x)!=Rank(look))continue;if(Cmp(x,look)<=0)found=i;else break;}}
  if(found<0)return WbError.NA;return v?arr[found,k-1]:arr[k-1,found];}
}
static class WbFormat { public static string Format(double d,string fmt){try{if(fmt.Contains("yy")||fmt.Contains("mm")||fmt.Contains("dd"))return WbCalc.FromSerial(d).ToString(fmt.Replace("m","M").Replace("MMM","MMM"),CultureInfo.InvariantCulture);return d.ToString(fmt,CultureInfo.InvariantCulture);}catch(FormatException){return WbCalc.NumText(d);}}}
}
