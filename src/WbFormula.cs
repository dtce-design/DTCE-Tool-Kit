using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text;
namespace BeamScheduleFormatter {
/* Formula tokenizer / parser for the POW worksheet engine (see WbModel.cs). Supports the syntax used by the DPWH V6.02 workbook:
   sheet-qualified references, whole rows / columns, defined names, structured table references, array constants and the usual
   operators. Shared formulas are moved to each cell by shifting their relative references. */
enum WbTok{Num,Str,Err,Ref,Ident,Func,Struct,Op,LParen,RParen,Comma,LBrace,RBrace,Semi,End}
sealed class WbToken{public WbTok Kind;public string Text;public string Sheet;public int Start,Length;public override string ToString(){return Kind+":"+Text;}}
abstract class WbNode{}
sealed class WbConst:WbNode{public object Value;}
sealed class WbArrayConst:WbNode{public object[,] Values;}
sealed class WbRefNode:WbNode{public string Sheet;public int R1,C1,R2,C2;public bool Bad;}
sealed class WbNameNode:WbNode{public string Sheet;public string Name;}
sealed class WbStructNode:WbNode{public string Table;public string Spec;}
sealed class WbFuncNode:WbNode{public string Name;public List<WbNode> Args=new List<WbNode>();}
sealed class WbUnary:WbNode{public string Op;public WbNode X;}
sealed class WbBinary:WbNode{public string Op;public WbNode A,B;}
static class WbFormula {
 public const int MaxRow=1048576,MaxCol=16384;
 static bool IsIdentStart(char c){return char.IsLetter(c)||c=='_'||c=='\\';}
 static bool IsIdentChar(char c){return char.IsLetterOrDigit(c)||c=='_'||c=='.'||c=='\\'||c=='?';}
 public static List<WbToken> Tokenize(string f){var list=new List<WbToken>();int i=0,n=f.Length;
  while(i<n){char c=f[i];if(c==' '||c=='\n'||c=='\r'||c=='\t'){i++;continue;}int start=i;
   if(c=='"'){var sb=new StringBuilder();i++;while(i<n){if(f[i]=='"'){if(i+1<n&&f[i+1]=='"'){sb.Append('"');i+=2;continue;}i++;break;}sb.Append(f[i++]);}list.Add(new WbToken{Kind=WbTok.Str,Text=sb.ToString(),Start=start,Length=i-start});continue;}
   if(c=='#'){int j=i+1;while(j<n&&(char.IsLetterOrDigit(f[j])||f[j]=='/'))j++;if(j<n&&(f[j]=='!'||f[j]=='?'))j++;string e=f.Substring(i,j-i);list.Add(new WbToken{Kind=WbTok.Err,Text=e,Start=i,Length=j-i});i=j;continue;}
   if(char.IsDigit(c)||(c=='.'&&i+1<n&&char.IsDigit(f[i+1]))){/* row range 1:5 */int j=i;while(j<n&&char.IsDigit(f[j]))j++;
    if(j<n&&f[j]==':'&&j+1<n&&(char.IsDigit(f[j+1])||f[j+1]=='$')&&IsRowRange(f,i)){int k=j+1;if(f[k]=='$')k++;while(k<n&&char.IsDigit(f[k]))k++;list.Add(new WbToken{Kind=WbTok.Ref,Text=f.Substring(i,k-i),Start=i,Length=k-i});i=k;continue;}
    j=i;while(j<n&&(char.IsDigit(f[j])||f[j]=='.'))j++;if(j<n&&(f[j]=='E'||f[j]=='e')&&j+1<n&&(char.IsDigit(f[j+1])||((f[j+1]=='+'||f[j+1]=='-')&&j+2<n&&char.IsDigit(f[j+2])))){j+=2;while(j<n&&char.IsDigit(f[j]))j++;}
    list.Add(new WbToken{Kind=WbTok.Num,Text=f.Substring(i,j-i),Start=i,Length=j-i});i=j;continue;}
   string sheet=null;
   if(c=='\''){var sb=new StringBuilder();int j=i+1;while(j<n){if(f[j]=='\''){if(j+1<n&&f[j+1]=='\''){sb.Append('\'');j+=2;continue;}j++;break;}sb.Append(f[j++]);}
    if(j<n&&f[j]=='!'){sheet=sb.ToString();i=j+1;var t=ReadRefAfterSheet(f,ref i);t.Sheet=sheet;t.Start=start;t.Length=i-start;list.Add(t);continue;}
    throw new FormatException("Unexpected quote in formula");}
   if(c=='$'||IsIdentStart(c)){int j=i;while(j<n&&(IsIdentChar(f[j])||f[j]=='$'))j++;string word=f.Substring(i,j-i);
    if(j<n&&f[j]=='!'){sheet=word;i=j+1;var t=ReadRefAfterSheet(f,ref i);t.Sheet=sheet;t.Start=start;t.Length=i-start;list.Add(t);continue;}
    /* cell / column ranges: A1, $A$1, A:A, $A:$B */
    int k;string refText;if(TryCellOrColumn(f,i,out k,out refText)&&!(k<n&&(f[k]=='('||f[k]=='['))){list.Add(new WbToken{Kind=WbTok.Ref,Text=refText,Start=i,Length=k-i});i=k;continue;}
    if(j<n&&f[j]=='('){list.Add(new WbToken{Kind=WbTok.Func,Text=word,Start=i,Length=j-i});i=j;continue;}
    if(j<n&&f[j]=='['){int depth=0,k2=j;for(;k2<n;k2++){if(f[k2]=='\''){k2++;continue;}if(f[k2]=='[')depth++;else if(f[k2]==']'){depth--;if(depth==0){k2++;break;}}}list.Add(new WbToken{Kind=WbTok.Struct,Text=word,Sheet=f.Substring(j,k2-j),Start=i,Length=k2-i});i=k2;continue;}
    list.Add(new WbToken{Kind=WbTok.Ident,Text=word,Start=i,Length=j-i});i=j;continue;}
   if(c=='['){/* structured reference without table name: [@Col] */int depth=0,k2=i;for(;k2<n;k2++){if(f[k2]=='\''){k2++;continue;}if(f[k2]=='[')depth++;else if(f[k2]==']'){depth--;if(depth==0){k2++;break;}}}list.Add(new WbToken{Kind=WbTok.Struct,Text="",Sheet=f.Substring(i,k2-i),Start=i,Length=k2-i});i=k2;continue;}
   if(c=='('){list.Add(new WbToken{Kind=WbTok.LParen,Text="(",Start=i,Length=1});i++;continue;}
   if(c==')'){list.Add(new WbToken{Kind=WbTok.RParen,Text=")",Start=i,Length=1});i++;continue;}
   if(c==','){list.Add(new WbToken{Kind=WbTok.Comma,Text=",",Start=i,Length=1});i++;continue;}
   if(c==';'){list.Add(new WbToken{Kind=WbTok.Semi,Text=";",Start=i,Length=1});i++;continue;}
   if(c=='{'){list.Add(new WbToken{Kind=WbTok.LBrace,Text="{",Start=i,Length=1});i++;continue;}
   if(c=='}'){list.Add(new WbToken{Kind=WbTok.RBrace,Text="}",Start=i,Length=1});i++;continue;}
   if(c=='<'||c=='>'){string op=c.ToString();if(i+1<n&&(f[i+1]=='='||(c=='<'&&f[i+1]=='>')))op+=f[i+1];list.Add(new WbToken{Kind=WbTok.Op,Text=op,Start=i,Length=op.Length});i+=op.Length;continue;}
   if("+-*/^&=:%".IndexOf(c)>=0){list.Add(new WbToken{Kind=WbTok.Op,Text=c.ToString(),Start=i,Length=1});i++;continue;}
   if(c=='@'){i++;continue;}/* implicit intersection marker */
   throw new FormatException("Unexpected character '"+c+"' in formula");}
  list.Add(new WbToken{Kind=WbTok.End,Text="",Start=n,Length=0});return list;}
 static bool IsRowRange(string f,int i){/* digits:digits not preceded by a letter (a number like 1:2 is only a row range) */return i==0||!(char.IsLetterOrDigit(f[i-1])||f[i-1]=='.');}
 static WbToken ReadRefAfterSheet(string f,ref int i){int n=f.Length;int k;string text;
  if(i<n&&f[i]=='#'){int j=i;while(j<n&&f[j]!='!'&&f[j]!=','&&f[j]!=')')j++;if(j<n&&f[j]=='!')j++;var e=new WbToken{Kind=WbTok.Err,Text="#REF!"};i=j;return e;}
  if(TryCellOrColumn(f,i,out k,out text)){i=k;return new WbToken{Kind=WbTok.Ref,Text=text};}
  int j2=i;if(f[j2]=='$')j2++;if(j2<n&&char.IsDigit(f[j2])){int q=j2;while(q<n&&char.IsDigit(f[q]))q++;if(q<n&&f[q]==':'){q++;if(q<n&&f[q]=='$')q++;while(q<n&&char.IsDigit(f[q]))q++;}var t=new WbToken{Kind=WbTok.Ref,Text=f.Substring(i,q-i)};i=q;return t;}
  int j3=i;while(j3<n&&IsIdentChar(f[j3]))j3++;var name=new WbToken{Kind=WbTok.Ident,Text=f.Substring(i,j3-i)};i=j3;return name;}
 /* A1, $A$1, A1:B2 (both parts cell refs), A:A */
 static bool TryCellOrColumn(string f,int i,out int end,out string text){end=i;text=null;int k;int r1,c1;bool col1;
  if(!ReadCellPart(f,i,out k,out r1,out c1,out col1))return false;int stop=k;
  if(k<f.Length&&f[k]==':'){int k2;int r2,c2;bool col2;if(ReadCellPart(f,k+1,out k2,out r2,out c2,out col2)&&col1==col2){stop=k2;}else if(col1)return false;}else if(col1)return false;
  if(stop<f.Length&&(char.IsLetterOrDigit(f[stop])||f[stop]=='_'||f[stop]=='.'))return false;end=stop;text=f.Substring(i,stop-i);return true;}
 static bool ReadCellPart(string f,int i,out int end,out int row,out int col,out bool columnOnly){end=i;row=0;col=0;columnOnly=false;int n=f.Length;int j=i;if(j<n&&f[j]=='$')j++;int ls=j;while(j<n&&j-ls<4&&f[j]>='A'&&f[j]<='Z'||(j<n&&j-ls<4&&f[j]>='a'&&f[j]<='z'))j++;int letters=j-ls;if(letters<1||letters>3)return false;
  if(j<n&&IsIdentChar(f[j])&&!char.IsDigit(f[j])&&f[j]!='$')return false;col=WbBook.ColumnNumber(f.Substring(ls,letters));if(col>MaxCol)return false;
  int k=j;if(k<n&&f[k]=='$')k++;int ds=k;while(k<n&&char.IsDigit(f[k]))k++;if(k==ds){if(k!=j)return false;columnOnly=true;end=j;return true;}
  if(!int.TryParse(f.Substring(ds,k-ds),out row)||row<1||row>MaxRow)return false;end=k;return true;}
 /* Shared formulas: move relative references by (dr,dc) */
 public static string Shift(string formula,int dr,int dc){if(dr==0&&dc==0)return formula;List<WbToken> toks;try{toks=Tokenize(formula);}catch(FormatException){return formula;}var sb=new StringBuilder();int pos=0;
  foreach(var t in toks){if(t.Kind!=WbTok.Ref)continue;string orig=formula.Substring(t.Start,t.Length);int bang=-1;if(t.Sheet!=null){bang=orig.LastIndexOf('!');}string prefix=bang>=0?orig.Substring(0,bang+1):"";string body=bang>=0?orig.Substring(bang+1):orig;
   sb.Append(formula,pos,t.Start-pos);sb.Append(prefix);sb.Append(string.Join(":",body.Split(':').Select(p=>ShiftPart(p,dr,dc))));pos=t.Start+t.Length;}
  sb.Append(formula,pos,formula.Length-pos);return sb.ToString();}
 static string ShiftPart(string p,int dr,int dc){int i=0;bool absC=false,absR=false;if(i<p.Length&&p[i]=='$'){absC=true;i++;}int ls=i;while(i<p.Length&&char.IsLetter(p[i]))i++;string letters=p.Substring(ls,i-ls);if(i<p.Length&&p[i]=='$'){absR=true;i++;}string digits=p.Substring(i);
  var sb=new StringBuilder();if(letters!=""){sb.Append(absC?"$":"");int c=WbBook.ColumnNumber(letters)+(absC?0:dc);sb.Append(c<1?"#REF":WbBook.ColumnName(c));}
  if(digits!=""){if(letters==""){sb.Append(absR||(p.StartsWith("$"))?"$":"");}else sb.Append(absR?"$":"");int r=int.Parse(digits)+((absR||(letters==""&&p.StartsWith("$")))?0:dr);sb.Append(r<1?"#REF":r.ToString(CultureInfo.InvariantCulture));}
  return sb.ToString();}
 /* parser */
 sealed class P{public List<WbToken> T;public int I;public WbToken Peek{get{return T[I];}}public WbToken Next(){return T[I++];}}
 public static WbNode Parse(string formula){var p=new P{T=Tokenize(formula)};var node=Compare(p);if(p.Peek.Kind!=WbTok.End)throw new FormatException("Unexpected '"+p.Peek.Text+"'");return node;}
 static WbNode Compare(P p){var a=Concat(p);while(p.Peek.Kind==WbTok.Op&&(p.Peek.Text=="="||p.Peek.Text=="<>"||p.Peek.Text=="<"||p.Peek.Text==">"||p.Peek.Text=="<="||p.Peek.Text==">=")){string op=p.Next().Text;a=new WbBinary{Op=op,A=a,B=Concat(p)};}return a;}
 static WbNode Concat(P p){var a=Add(p);while(p.Peek.Kind==WbTok.Op&&p.Peek.Text=="&"){p.Next();a=new WbBinary{Op="&",A=a,B=Add(p)};}return a;}
 static WbNode Add(P p){var a=Mul(p);while(p.Peek.Kind==WbTok.Op&&(p.Peek.Text=="+"||p.Peek.Text=="-")){string op=p.Next().Text;a=new WbBinary{Op=op,A=a,B=Mul(p)};}return a;}
 static WbNode Mul(P p){var a=Pow(p);while(p.Peek.Kind==WbTok.Op&&(p.Peek.Text=="*"||p.Peek.Text=="/")){string op=p.Next().Text;a=new WbBinary{Op=op,A=a,B=Pow(p)};}return a;}
 static WbNode Pow(P p){var a=Percent(p);while(p.Peek.Kind==WbTok.Op&&p.Peek.Text=="^"){p.Next();a=new WbBinary{Op="^",A=a,B=Percent(p)};}return a;}
 static WbNode Percent(P p){var a=Unary(p);while(p.Peek.Kind==WbTok.Op&&p.Peek.Text=="%"){p.Next();a=new WbBinary{Op="/",A=a,B=new WbConst{Value=100.0}};}return a;}
 static WbNode Unary(P p){if(p.Peek.Kind==WbTok.Op&&(p.Peek.Text=="-"||p.Peek.Text=="+")){string op=p.Next().Text;var x=Unary(p);return op=="-"?(WbNode)new WbUnary{Op="-",X=x}:x;}return Range(p);}
 static WbNode Range(P p){var a=Primary(p);while(p.Peek.Kind==WbTok.Op&&p.Peek.Text==":"){p.Next();a=new WbBinary{Op=":",A=a,B=Primary(p)};}return a;}
 static WbNode Primary(P p){var t=p.Next();switch(t.Kind){
  case WbTok.Num:return new WbConst{Value=double.Parse(t.Text,NumberStyles.Float,CultureInfo.InvariantCulture)};
  case WbTok.Str:return new WbConst{Value=t.Text};
  case WbTok.Err:return new WbConst{Value=WbError.Parse(t.Text)};
  case WbTok.Ref:return MakeRef(t.Sheet,t.Text);
  case WbTok.Struct:return new WbStructNode{Table=t.Text,Spec=t.Sheet};
  case WbTok.Ident:{string u=t.Text.ToUpperInvariant();if(t.Sheet==null&&(u=="TRUE"||u=="FALSE"))return new WbConst{Value=u=="TRUE"};return new WbNameNode{Name=t.Text,Sheet=t.Sheet};}
  case WbTok.Func:{var fn=new WbFuncNode{Name=NormalName(t.Text)};p.Next();/* ( */if(p.Peek.Kind==WbTok.RParen){p.Next();return fn;}
   while(true){if(p.Peek.Kind==WbTok.Comma||p.Peek.Kind==WbTok.RParen)fn.Args.Add(null);else fn.Args.Add(Compare(p));if(p.Peek.Kind==WbTok.Comma){p.Next();if(p.Peek.Kind==WbTok.RParen){fn.Args.Add(null);}continue;}if(p.Peek.Kind==WbTok.RParen){p.Next();break;}throw new FormatException("Expected ) in "+fn.Name);}return fn;}
  case WbTok.LParen:{var x=Compare(p);if(p.Peek.Kind==WbTok.Comma){/* union: keep the first area */while(p.Peek.Kind==WbTok.Comma){p.Next();Compare(p);}}if(p.Next().Kind!=WbTok.RParen)throw new FormatException("Expected )");return x;}
  case WbTok.LBrace:{var rows=new List<List<object>>{new List<object>()};while(p.Peek.Kind!=WbTok.RBrace){var k=p.Next();if(k.Kind==WbTok.Comma)continue;if(k.Kind==WbTok.Semi){rows.Add(new List<object>());continue;}bool neg=false;if(k.Kind==WbTok.Op&&k.Text=="-"){neg=true;k=p.Next();}
    object v=k.Kind==WbTok.Num?(object)((neg?-1:1)*double.Parse(k.Text,CultureInfo.InvariantCulture)):k.Kind==WbTok.Str?(object)k.Text:k.Kind==WbTok.Ident?(object)(k.Text.ToUpperInvariant()=="TRUE"):(object)WbError.Parse(k.Text);rows[rows.Count-1].Add(v);}p.Next();
   var arr=new object[rows.Count,rows.Max(r=>r.Count)];for(int r=0;r<rows.Count;r++)for(int c=0;c<rows[r].Count;c++)arr[r,c]=rows[r][c];return new WbArrayConst{Values=arr};}
  default:throw new FormatException("Unexpected '"+t.Text+"'");}}
 public static string NormalName(string name){string u=name.ToUpperInvariant();foreach(var pre in new[]{"_XLFN._XLWS.","_XLFN.","_XLWS."})if(u.StartsWith(pre))u=u.Substring(pre.Length);return u;}
 static WbNode MakeRef(string sheet,string text){var parts=text.Replace("$","").Split(':');int r1,c1,r2,c2;
  if(!ParsePart(parts[0],out r1,out c1)){return new WbRefNode{Sheet=sheet,Bad=true};}if(parts.Length==1){r2=r1;c2=c1;}else if(!ParsePart(parts[1],out r2,out c2))return new WbRefNode{Sheet=sheet,Bad=true};
  if(r1==0&&r2==0){r1=1;r2=MaxRow;}if(c1==0&&c2==0){c1=1;c2=MaxCol;}return new WbRefNode{Sheet=sheet,R1=Math.Min(r1,r2),C1=Math.Min(c1,c2),R2=Math.Max(r1,r2),C2=Math.Max(c1,c2)};}
 static bool ParsePart(string s,out int r,out int c){r=0;c=0;int i=0;while(i<s.Length&&char.IsLetter(s[i]))i++;if(i>0)c=WbBook.ColumnNumber(s.Substring(0,i));if(i<s.Length){if(!int.TryParse(s.Substring(i),out r))return false;}return i>0||r>0;}
}
}
