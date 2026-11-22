import ezdxf, math
from ezdxf.enums import TextEntityAlignment
class Sheet:
    def __init__(s):
        s.doc=ezdxf.new('R2010',setup=True);s.doc.units=4;s.msp=s.doc.modelspace()
        for n,lw in [('CONCRETE',35),('REBAR',50),('BARDOT',0),('TEXT',18),('DIM',13),('TITLE',35),('FRAME',25),('HIDDEN',18),('BREAK',18),('HATCH',0)]:
            s.doc.layers.add(n,color=7,lineweight=lw)
        s.doc.layers.get('HIDDEN').dxf.linetype='DASHED'
        if 'DASHED' not in s.doc.linetypes: s.doc.linetypes.add('DASHED',pattern=[6,4,-2])
        st=s.doc.styles.get('Standard');st.dxf.font='arial.ttf'
    def pl(s,pts,layer='CONCRETE',closed=False):s.msp.add_lwpolyline(pts,close=closed,dxfattribs={'layer':layer})
    def fill(s,pts,scale=0.14):
        # light gray, 60% transparent, sparse (owner: "lighter transparency")
        h=s.msp.add_hatch(color=253,dxfattribs={'layer':'HATCH'});h.set_pattern_fill('AR-CONC',scale=scale,color=253);h.transparency=0.6;h.paths.add_polyline_path(pts,is_closed=True)
    def ln(s,a,b,layer='CONCRETE'):s.msp.add_line(a,b,dxfattribs={'layer':layer})
    def bar(s,pts):s.msp.add_lwpolyline(pts,dxfattribs={'layer':'REBAR','const_width':0.9})
    def dot(s,x,y,r=1.5):
        h=s.msp.add_hatch(color=7,dxfattribs={'layer':'BARDOT'});h.paths.add_edge_path().add_arc((x,y),r,0,360);s.msp.add_circle((x,y),r,dxfattribs={'layer':'BARDOT'})
    def circ(s,x,y,r,layer='DIM'):s.msp.add_circle((x,y),r,dxfattribs={'layer':layer})
    def text(s,t,x,y,h=3.2,align='BL',layer='TEXT',rot=0,width=None):
        h=max(h,2.3);m=s.msp.add_mtext(t.replace('\n','\\P'),dxfattribs={'layer':layer,'char_height':h,'rotation':rot,'style':'Standard'})
        m.dxf.insert=(x,y);m.dxf.attachment_point={'TL':1,'TC':2,'TR':3,'ML':4,'MC':5,'MR':6,'BL':7,'BC':8,'BR':9}[align]
        if width:m.dxf.width=width
    def leader(s,t,p,q,align=None,h=3.0,width=None):
        s.ln(p,q,'DIM');s.dot(p[0],p[1],.7)
        if align is None: align='ML' if q[0]>=p[0] else 'MR'
        dx=1.5 if align.endswith('L') else -1.5
        s.ln(q,(q[0]+dx,q[1]),'DIM');s.text(t,q[0]+dx*1.5,q[1],h,align,width=width)
    def dim(s,a,b,off,t,h=2.8):
        ax,ay=a;bx,by=b;L=math.hypot(bx-ax,by-ay);ux,uy=(bx-ax)/L,(by-ay)/L;nx,ny=-uy,ux
        p=(ax+nx*off,ay+ny*off);q=(bx+nx*off,by+ny*off)
        s.ln(p,q,'DIM');s.ln(a,(p[0]+nx*1.5*math.copysign(1,off),p[1]+ny*1.5*math.copysign(1,off)),'DIM');s.ln(b,(q[0]+nx*1.5*math.copysign(1,off),q[1]+ny*1.5*math.copysign(1,off)),'DIM')
        for c in (p,q):s.ln((c[0]-1.4,c[1]-1.4),(c[0]+1.4,c[1]+1.4),'DIM')
        ang=math.degrees(math.atan2(uy,ux))
        if ang>90 or ang<-90:ang+=180
        mx,my=(p[0]+q[0])/2+nx*1.2*math.copysign(1,off),(p[1]+q[1])/2+ny*1.2*math.copysign(1,off)
        s.text(t,mx,my,h,'BC' if off>0 else 'TC',rot=ang)
    def brk(s,x,y0,y1):
        m=(y0+y1)/2;s.pl([(x,y0-2),(x,m-1.5),(x+1.5,m-.5),(x-1.5,m+.5),(x,m+1.5),(x,y1+2)],'BREAK')
    def cl(s,x,y0,y1):
        s.ln((x,y0),(x,y1),'HIDDEN');s.text('CL',x,y1+1,3,'BC')
    def title(s,code,t,x,y,w,scale='NOT TO SCALE'):
        n=len(t)*6*.62;lines=max(1,math.ceil(n/(w*.9)));s.text(t,x+w/2,y,6,'TC',layer='TITLE',width=w*.9);ul=y-lines*6*1.45-1.5;half=min(n,w*.9)/2
        s.msp.add_lwpolyline([(x+w/2-half,ul),(x+w/2+half,ul)],dxfattribs={'layer':'TITLE','const_width':0.5});s.text(code+'   ('+scale+')',x+w/2,ul-2,3,'TC')
    def frame(s,x,y,w,h):s.pl([(x,y),(x+w,y),(x+w,y-h),(x,y-h)],'FRAME',True)
    def notes(s,lines,x,y,w,h=2.7):
        s.text('NOTES:',x,y,3,'TL');yy=y-4
        for i,l in enumerate(lines):
            s.text(str(i+1)+'. '+l,x,yy,h,'TL',width=w);yy-=h*1.7*max(1,math.ceil(len(l)*h*.62/w))+1
        return yy
    def table(s,x,y,title,groups,heads,widths,rows=3,rh=7):
        W=sum(widths);s.pl([(x,y),(x+W,y),(x+W,y-10),(x,y-10)],'FRAME',True);s.text(title,x+W/2,y-5,4.6,'MC',layer='TITLE',width=W);y-=10
        cx=[x+sum(widths[:i]) for i in range(len(widths)+1)];c=0
        for g,span in groups:
            if g:
                s.pl([(cx[c],y),(cx[c+span],y),(cx[c+span],y-6),(cx[c],y-6)],'FRAME',True);s.text(g,(cx[c]+cx[c+span])/2,y-3,2.6,'MC',width=cx[c+span]-cx[c])
            c+=span
        grouped=set();c=0
        for g,span in groups:
            if g: grouped.update(range(c,c+span))
            c+=span
        for i,hd in enumerate(heads):
            top=y-6 if i in grouped else y
            s.pl([(cx[i],top),(cx[i+1],top),(cx[i+1],y-12),(cx[i],y-12)],'FRAME',True);s.text(hd,(cx[i]+cx[i+1])/2,(top+y-12)/2,2.6,'MC',width=widths[i]-1)
        y-=12
        for r in range(rows):
            for i in range(len(widths)):s.pl([(cx[i],y),(cx[i+1],y),(cx[i+1],y-rh),(cx[i],y-rh)],'FRAME',True)
            y-=rh
        return y
