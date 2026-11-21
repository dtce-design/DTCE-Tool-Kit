using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
/* Excel number formats for the POW worksheet view: sections, literals, accounting fill ("* "), currency [$₱-3409], percent,
   thousands, decimals, colours and dates. Fill text is returned split so the renderer can push the number to the right edge. */
sealed class WbFormatted{public string Left="",Text="";public Color? Color;public bool Number;public bool Fill;}
static class WbNumberFormat {
 static readonly Dictionary<string,List<string>> cache=new Dictionary<string,List<string>>();
 static List<string> Sections(string code){List<string> list;if(cache.TryGetValue(code,out list))return list;list=new List<string>();var sb=new StringBuilder();bool q=false,br=false;
  for(int i=0;i<code.Length;i++){char c=code[i];if(c=='"'&&!br)q=!q;else if(c=='['&&!q)br=true;else if(c==']'&&!q)br=false;else if(c=='\\'&&!q&&i+1<code.Length){sb.Append(c).Append(code[++i]);continue;}
   if(c==';'&&!q&&!br){list.Add(sb.ToString());sb.Clear();continue;}sb.Append(c);}list.Add(sb.ToString());cache[code]=list;return list;}
 public static WbFormatted Format(object value,string code){var r=new WbFormatted();if(value==null)return r;
  if(value is WbError){r.Text=value.ToString();return r;}if(value is bool){r.Text=(bool)value?"TRUE":"FALSE";return r;}
  var secs=Sections(string.IsNullOrEmpty(code)?"General":code);
  if(value is string){var s=(string)value;if(secs.Count>=4)return Apply(secs[3],0,s,r,false);if(secs.Count==1&&secs[0].Contains("@"))return Apply(secs[0],0,s,r,false);r.Text=s;return r;}
  double d=(double)value;r.Number=true;string sec;bool abs=false;
  if(secs.Count==1)sec=secs[0];else if(d>0||(d==0&&secs.Count<3))sec=secs[0];else if(d<0){sec=secs[1];abs=true;}else sec=secs[2];
  if(abs)d=Math.Abs(d);return Apply(sec,d,null,r,!abs&&secs.Count>1&&d<0);}
 static WbFormatted Apply(string sec,double d,string text,WbFormatted r,bool keepSign){
  /* brackets: colours and currency/locale */
  var lit=new StringBuilder();var sb=new StringBuilder();int fillAt=-1;
  if(sec.Trim().Equals("General",StringComparison.OrdinalIgnoreCase)||sec.Trim()==""){if(text!=null){r.Text=text;return r;}r.Text=General(d);return r;}
  bool isDate=text==null&&IsDate(sec);
  var tokens=new List<string>();int i=0;string numberPattern="";bool inNumber=false;bool numberDone=false;var prefix=new StringBuilder();var suffix=new StringBuilder();
  Action<string> emit=s=>{if(inNumber||numberDone){if(!numberDone&&inNumber){/* literal inside pattern ends it */inNumber=false;numberDone=true;}suffix.Append(s);}else prefix.Append(s);};
  while(i<sec.Length){char c=sec[i];
   if(c=='"'){int j=sec.IndexOf('"',i+1);if(j<0)j=sec.Length;emit(sec.Substring(i+1,j-i-1));i=j+1;continue;}
   if(c=='\\'&&i+1<sec.Length){emit(sec[i+1].ToString());i+=2;continue;}
   if(c=='_'&&i+1<sec.Length){emit(" ");i+=2;continue;}
   if(c=='*'&&i+1<sec.Length){if(!inNumber&&!numberDone)fillAt=prefix.Length;i+=2;r.Fill=true;continue;}
   if(c=='['){int j=sec.IndexOf(']',i);if(j<0)j=sec.Length-1;string b=sec.Substring(i+1,j-i-1);if(b.StartsWith("$")){var cur=b.Substring(1);int dash=cur.IndexOf('-');if(dash>=0)cur=cur.Substring(0,dash);emit(cur);}else{var col=ColorName(b);if(col.HasValue)r.Color=col;}i=j+1;continue;}
   if(text!=null){if(c=='@'){emit(text);i++;continue;}emit(c.ToString());i++;continue;}
   if(isDate){emit(c.ToString());i++;continue;}
   if("0#?.,".IndexOf(c)>=0||((c=='E'||c=='e')&&inNumber)){if(numberDone){suffix.Append(c);i++;continue;}inNumber=true;numberPattern+=c;if((c=='E'||c=='e')&&i+1<sec.Length&&(sec[i+1]=='+'||sec[i+1]=='-')){numberPattern+=sec[i+1];i++;}i++;continue;}
   if(c=='%'){emit("%");d*=100;i++;continue;}
   emit(c.ToString());i++;}
  if(text!=null){r.Text=prefix.ToString()+suffix.ToString();SplitFill(r,fillAt);return r;}
  if(isDate){r.Text=FormatDate(d,StripForDate(sec));return r;}
  if(numberPattern==""){r.Text=prefix.ToString()+suffix.ToString();SplitFill(r,fillAt);return r;}
  string num=FormatNumber(d,numberPattern);if(keepSign&&d<0&&!num.StartsWith("-"))num="-"+num;
  r.Text=prefix.ToString()+num+suffix.ToString();SplitFill(r,fillAt);return r;}
 static void SplitFill(WbFormatted r,int fillAt){if(fillAt<0||fillAt>r.Text.Length)return;r.Left=r.Text.Substring(0,fillAt);r.Text=r.Text.Substring(fillAt);}
 static string FormatNumber(double d,string pat){int e=pat.IndexOfAny(new[]{'E','e'});if(e>=0){int decs=pat.Substring(0,e).Contains(".")?pat.Substring(pat.IndexOf('.')+1,e-pat.IndexOf('.')-1).Length:0;return d.ToString((decs>0?"0."+new string('0',decs):"0")+"E+00",CultureInfo.InvariantCulture);}
  /* trailing commas scale by 1000 */
  while(pat.EndsWith(",")){pat=pat.Substring(0,pat.Length-1);d/=1000;}
  int dot=pat.IndexOf('.');string ip=dot<0?pat:pat.Substring(0,dot),fp=dot<0?"":pat.Substring(dot+1);bool thousands=ip.Contains(",");ip=ip.Replace(",","");
  int minFrac=fp.Count(ch=>ch=='0'),maxFrac=fp.Count(ch=>ch=='0'||ch=='#'||ch=='?'),qFrac=fp.Count(ch=>ch=='?');int minInt=ip.Count(ch=>ch=='0');
  bool neg=d<0;double a=Math.Abs(d);double rounded=(double)WbCalc.Round(a,maxFrac,"ROUND");string s=rounded.ToString("F"+maxFrac,CultureInfo.InvariantCulture);string intPart=s,frac="";int p=s.IndexOf('.');if(p>=0){intPart=s.Substring(0,p);frac=s.Substring(p+1);}
  while(frac.Length>minFrac&&frac.EndsWith("0")&&frac.Length>0){frac=frac.Substring(0,frac.Length-1);}if(qFrac>0)frac=frac.PadRight(minFrac+qFrac);
  if(intPart=="0"&&minInt==0)intPart="";if(intPart.Length<minInt)intPart=intPart.PadLeft(minInt,'0');
  if(thousands&&intPart.Length>3){var sb=new StringBuilder();for(int i=0;i<intPart.Length;i++){if(i>0&&(intPart.Length-i)%3==0)sb.Append(',');sb.Append(intPart[i]);}intPart=sb.ToString();}
  string res=intPart+(dot>=0&&(frac.Length>0||fp.Length>0&&minFrac>0)?"."+frac:(dot>=0&&maxFrac>0&&frac.Length==0&&minFrac==0?".":""));if(res=="")res=dot>=0?".":"";return (neg&&res.Trim('0','.',',')!=""?"-":"")+res;}
 static string General(double d){if(d==0)return "0";double a=Math.Abs(d);if(a>=1e11||a<1e-9){return d.ToString("0.#####E+00",CultureInfo.InvariantCulture);}
  int intDigits=a>=1?(int)Math.Floor(Math.Log10(a))+1:1;int decs=Math.Max(0,10-intDigits);var s=Math.Round(d,decs,MidpointRounding.AwayFromZero).ToString("0."+new string('#',Math.Max(1,decs)),CultureInfo.InvariantCulture);return s.EndsWith(".")?s.TrimEnd('.'):s;}
 static Color? ColorName(string b){switch(b.ToLowerInvariant()){case "red":return Color.Red;case "blue":return Color.Blue;case "green":return Color.Green;case "black":return Color.Black;case "white":return Color.White;case "yellow":return Color.Yellow;case "magenta":return Color.Magenta;case "cyan":return Color.Cyan;}return null;}
 static bool IsDate(string sec){bool q=false;for(int i=0;i<sec.Length;i++){char c=sec[i];if(c=='"')q=!q;if(q)continue;if(c=='['){int j=sec.IndexOf(']',i);if(j<0)break;i=j;continue;}if(c=='\\'){i++;continue;}if("yYdDhHsS".IndexOf(c)>=0)return true;if((c=='m'||c=='M')&&!sec.Contains("0"))return true;}return false;}
 static string StripForDate(string sec){var sb=new StringBuilder();bool q=false;for(int i=0;i<sec.Length;i++){char c=sec[i];if(c=='"'){q=!q;sb.Append('\u0001');continue;}if(q){sb.Append(c);continue;}if(c=='['){int j=sec.IndexOf(']',i);if(j<0)break;i=j;continue;}if(c=='\\'&&i+1<sec.Length){sb.Append('\u0001').Append(sec[++i]).Append('\u0001');continue;}sb.Append(c);}return sb.ToString();}
 static string FormatDate(double serial,string f){var dt=new DateTime(1899,12,30).AddDays(serial);var sb=new StringBuilder();bool lit=false;string lower=f.ToLowerInvariant();bool hasH=lower.Contains("h");
  for(int i=0;i<f.Length;){char c=f[i];if(c=='\u0001'){lit=!lit;i++;continue;}if(lit){sb.Append(c);i++;continue;}char lc=char.ToLowerInvariant(c);int n=1;while(i+n<f.Length&&char.ToLowerInvariant(f[i+n])==lc)n++;
   switch(lc){case 'y':sb.Append(n<=2?dt.ToString("yy",CultureInfo.InvariantCulture):dt.Year.ToString("0000"));break;
    case 'm':{bool minute=hasH&&(PrevToken(lower,i)=='h'||NextToken(lower,i+n)=='s');if(minute)sb.Append(n==1?dt.Minute.ToString():dt.Minute.ToString("00"));else sb.Append(n==1?dt.Month.ToString():n==2?dt.Month.ToString("00"):n==3?dt.ToString("MMM",CultureInfo.InvariantCulture):n==5?dt.ToString("MMMM",CultureInfo.InvariantCulture).Substring(0,1):dt.ToString("MMMM",CultureInfo.InvariantCulture));break;}
    case 'd':sb.Append(n==1?dt.Day.ToString():n==2?dt.Day.ToString("00"):n==3?dt.ToString("ddd",CultureInfo.InvariantCulture):dt.ToString("dddd",CultureInfo.InvariantCulture));break;
    case 'h':{int h=dt.Hour;if(lower.Contains("am/pm")){h%=12;if(h==0)h=12;}sb.Append(n==1?h.ToString():h.ToString("00"));break;}
    case 's':sb.Append(n==1?dt.Second.ToString():dt.Second.ToString("00"));break;
    case 'a':if(lower.Substring(i).StartsWith("am/pm")){sb.Append(dt.Hour<12?"AM":"PM");n=5;}else sb.Append(c);break;
    default:sb.Append(f,i,n);break;}i+=n;}return sb.ToString();}
 static char PrevToken(string f,int i){for(int k=i-1;k>=0;k--)if(char.IsLetter(f[k]))return f[k];return ' ';}
 static char NextToken(string f,int i){for(int k=i;k<f.Length;k++)if(char.IsLetter(f[k]))return f[k];return ' ';}
}
}
