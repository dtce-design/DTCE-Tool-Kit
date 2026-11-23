import sys,math;sys.path.insert(0,__import__('os').path.dirname(__import__('os').path.abspath(__file__)));from lib import Sheet
S=Sheet();CW,CH=440,250;c=3.0
def cell(i,j):return (i*CW,-j*CH)
def rect(x0,y0,x1,y1,fill=True):
    pts=[(x0,y0),(x1,y0),(x1,y1),(x0,y1)];S.pl(pts,closed=True)
    if fill:S.fill(pts)
def poly(pts,fill=True):
    S.pl(pts,closed=True)
    if fill:S.fill(pts)
# ---------------- SLAB-1 ----------------
x,y=cell(0,0)
yy=S.table(x+20,y-12,'ONE-WAY CONCRETE SLAB SCHEDULE',[('',2),('MAIN REINFORCEMENT',3),('TEMPERATURE REINFORCEMENT',2),('',1)],['MARK','DEPTH (mm)','TOP LEFT','BOTTOM','TOP RIGHT','TOP','BOT','REMARKS'],[30,30,52,52,52,52,52,80],rows=3)
t=18;top=yy-48;bot=top-t;sup=[x+30,x+200,x+370];bw=16
pts=[(x+20,top),(x+430,top),(x+430,bot)]
for k in range(2,-1,-1):pts+=[(sup[k]+bw,bot),(sup[k]+bw,bot-28),(sup[k],bot-28),(sup[k],bot)]
pts+=[(x+20,bot)];poly(pts);S.brk(x+430,bot,top)
for sx in sup:S.cl(sx+bw/2,bot-34,top+30)
S.bar([(x+23,bot+9),(x+23,top-c),(x+23+45,top-c)])
for sx in sup[1:]:S.bar([(sx-45,top-c),(min(x+428,sx+bw+45),top-c)])
S.bar([(x+26,bot+7),(x+26,bot+c),(sup[1]+bw-3,bot+c)]);S.bar([(sup[1]+3,bot+c+1.3),(sup[2]+bw-3,bot+c+1.3)]);S.bar([(sup[2]+3,bot+c),(x+428,bot+c)])
S.pl([(x+70,top-c),(sup[1]-45,top-c)],'HIDDEN');S.pl([(sup[1]+bw+45,top-c),(sup[2]-45,top-c)],'HIDDEN')
for d in range(int(x+34),int(x+426),14):S.dot(d,bot+c+3,1.3)
for d in range(int(x+280),int(x+426),14):S.dot(d,top-c-3,1.3)
S.dim((x+23,top),(x+23+45,top),9,'PER SCHED.');S.dim((sup[1]-45,top),(sup[1],top),9,'PER SCHED.');S.dim((sup[1]+bw,top),(sup[1]+bw+45,top),9,'PER SCHED.')
S.dim((sup[0]+bw,bot-28),(sup[1],bot-28),-6,'L1');S.dim((sup[1]+bw,bot-28),(sup[2],bot-28),-6,'L2')
S.leader('CONTINUOUS TOP BARS IF NOTED IN SCHEDULE;\nLAP 36 db MIN. AT MIDSPAN IF SPLICED',(sup[1]-80,top-c),(sup[1]-60,top+26))
S.leader('TOP TEMPERATURE AND\nDISTRIBUTION BARS PER SCHEDULE',(x+322,top-c-3),(x+330,top+26))
S.leader('TEMPERATURE BARS\nSEE SCHEDULE',(x+118,bot+c+3),(x+60,bot-22))
S.leader('EXTEND BARS INTO SUPPORT\n150 mm MIN.',(sup[1]+bw-3,bot+c),(sup[1]+30,bot-22))
S.leader('1/4 OF BOTTOM BARS\nCONTINUOUS',(sup[2]+bw+20,bot+c),(sup[2]+20,bot-40))
S.title('SLAB-1','ONE-WAY CONCRETE SLAB SCHEDULE',x,bot-55,CW)
# ---------------- SLAB-2_1 ----------------
x,y=cell(1,0)
yy=S.table(x+12,y-12,'TWO-WAY SLAB SCHEDULE - COLUMN STRIP',[('',2),('TOP',6),('BOTTOM',4),('',1)],['MARK','DEPTH (mm)','A','B','C','D','I','J','E','F','G','H','REMARKS'],[26,26,30,30,30,30,30,30,30,30,30,30,42],rows=3)
yy=S.table(x+12,yy-10,'TWO-WAY SLAB SCHEDULE - MIDDLE STRIP',[('',2),('TOP',4),('BOTTOM',6),('',1)],['MARK','DEPTH (mm)','K','L','M','N','N','P','Q','R','U','V','REMARKS'],[26,26,30,30,30,30,30,30,30,30,30,30,42],rows=3)
yy=S.notes(['PROVIDE TEMPERATURE AND SHRINKAGE REINFORCEMENT (NSCP 2015 / ACI 318 24.4).','WORK THIS SCHEDULE WITH THE SLAB COLUMN STRIP AND MIDDLE STRIP DETAILS (SLAB-2_2, SLAB-2_3).','#10 = 10 mm BAR; SPACING IN mm O.C.'],x+12,yy-8,410)
S.title('SLAB-2_1','TWO-WAY CONCRETE SLAB SCHEDULE',x,yy-6,CW)
# ---------------- strips ----------------
def strip(x,y,code,ttl,middle):
    t=18;top=y-70;bot=top-t;cols=[x+60,x+240,x+400];cw=16
    S.pl([(x+16,top),(x+425,top),(x+425,bot),(x+16,bot)],closed=True);S.fill([(x+16,top),(x+425,top),(x+425,bot),(x+16,bot)])
    for cx in cols:
        lay='HIDDEN' if middle else 'CONCRETE'
        for a in (cx,cx+cw):S.pl([(a,top),(a,top+26)],lay);S.pl([(a,bot),(a,bot-26)],lay)
        if not middle:S.fill([(cx,top),(cx+cw,top),(cx+cw,top+26),(cx,top+26)]);S.fill([(cx,bot),(cx+cw,bot),(cx+cw,bot-26),(cx,bot-26)])
        S.cl(cx+cw/2,bot-30,top+48)
    L1=cols[1]-cols[0]-cw;Li=cols[2]-cols[1]-cw
    S.dim((x+16,bot-26),(cols[0],bot-26),-6,'Lnc');S.dim((cols[0]+cw,bot-26),(cols[1],bot-26),-6,'Ln1');S.dim((cols[1]+cw,bot-26),(cols[2],bot-26),-6,'Lni')
    S.text('CANTILEVER SPAN',x+16,bot-46,3.4);S.text('INTERIOR SPAN',cols[0]+cw+L1/2,bot-46,3.4,'BC');S.text('EXTERIOR SPAN',cols[1]+cw+Li/2,bot-46,3.4,'BC')
    if not middle:
        S.bar([(x+22,bot+c),(x+19,bot+c),(x+19,top-c),(cols[0]+cw+.30*L1,top-c)]);S.bar([(cols[0]-22,top-c-1.6),(cols[0]+cw+.20*L1,top-c-1.6)])
        S.bar([(cols[1]-.30*L1,top-c),(cols[1]+cw+.33*Li,top-c)]);S.bar([(cols[1]-.20*L1,top-c-1.6),(cols[1]+cw+.20*Li,top-c-1.6)])
        S.bar([(cols[2]-.33*Li,top-c),(cols[2]+cw-3,top-c),(cols[2]+cw-3,bot+9)]);S.bar([(cols[2]-.20*Li,top-c-1.6),(cols[2]+cw-4.6,top-c-1.6)])
        S.bar([(x+22,bot+c),(cols[0]+cw+L1-18,bot+c)]);S.bar([(cols[1]-L1*.25,bot+c+1.6),(cols[2]+cw-5,bot+c+1.6),(cols[2]+cw-5,bot+11)])
        for a,b,o,tt in [(cols[0]+cw,cols[0]+cw+.30*L1,22,'0.30 Ln1* >= 5d'),(cols[0]+cw,cols[0]+cw+.20*L1,12,'0.20 Ln1*'),(cols[1]-.30*L1,cols[1],22,'0.30 Ln1* >= 5d'),(cols[1]-.20*L1,cols[1],12,'0.20 Ln1*'),(cols[1]+cw,cols[1]+cw+.33*Li,22,'0.33 Lni* >= 5d'),(cols[1]+cw,cols[1]+cw+.20*Li,12,'0.20 Lni*'),(cols[2]-.33*Li,cols[2],22,'0.33 Lni >= 5d'),(cols[2]-.20*Li,cols[2],12,'0.20 Lni')]:S.dim((a,top),(b,top),o,tt,2.5)
        for tg,px,py,qx in [('J',x+32,top-c,x+32),('I',cols[0]+cw+12,top-c-1.6,cols[0]+cw+12),('D',cols[1]+cw+14,top-c-1.6,cols[1]+cw+14),('C',cols[1]+cw+40,top-c,cols[1]+cw+40),('A',cols[2]-42,top-c,cols[2]-42),('B',cols[2]-20,top-c-1.6,cols[2]-20)]:S.leader(tg,(px,py),(qx,top+34),'BC')
        for tg,px,py,qx in [('E',cols[0]+cw+L1-35,bot+c,cols[0]+cw+L1-35),('G',cols[1]-18,bot+c+1.6,cols[1]-24),('F',cols[1]+cw+Li/2,bot+c+1.6,cols[1]+cw+Li/2)]:S.leader(tg,(px,py),(qx,bot-14),'TC')
        S.leader('MIN. 2 BARS WITH\nSTANDARD HOOK',(cols[2]+cw-5,bot+11),(cols[2]-70,bot-18))
        S.text('BOTTOM BAR SPLICES WITHIN THIS REGION',cols[1]-L1*.25,bot-58,2.8)
        nts=['* EXTENSION BASED ON THE LARGER OF THE TWO ADJACENT SPANS, AND NOT LESS THAN 5d.','SPLICES: MECHANICAL, WELDED OR CLASS B LAP SPLICE.','AT LEAST TWO BOTTOM BARS PASS WITHIN THE COLUMN BARS AND ARE ANCHORED AT THE EXTERIOR SUPPORT (STRUCTURAL INTEGRITY).','AT LEAST 50% OF THE TOP BARS EXTEND THE LONGER DISTANCE.']
    else:
        S.bar([(x+22,bot+c),(x+19,bot+c),(x+19,top-c),(cols[0]+cw+.22*L1,top-c)]);S.bar([(cols[1]-.22*L1,top-c),(cols[1]+cw+.22*Li,top-c)]);S.bar([(cols[2]-.22*Li,top-c),(cols[2]+cw-3,top-c),(cols[2]+cw-3,bot+9)])
        S.bar([(x+22,bot+c),(cols[1]+cw*.5,bot+c)]);S.bar([(cols[0]+cw+.15*L1,bot+c+1.6),(cols[1]-.15*L1,bot+c+1.6)]);S.bar([(cols[1]+cw*.5,bot+c+3.2),(cols[2]+cw-5,bot+c+3.2),(cols[2]+cw-5,bot+11)])
        for a,b,tt in [(cols[0]+cw,cols[0]+cw+.22*L1,'0.22 Ln1* >= Lnc'),(cols[1]-.22*L1,cols[1],'0.22 Ln1*'),(cols[1]+cw,cols[1]+cw+.22*Li,'0.22 Lni*'),(cols[2]-.22*Li,cols[2],'0.22 Lni')]:S.dim((a,top),(b,top),12,tt,2.5)
        for a,b,tt in [(cols[0]+cw,cols[0]+cw+.15*L1,'0.15 Ln1*'),(cols[1]-.15*L1,cols[1],'0.15 Ln1*'),(cols[2]-.15*Li,cols[2],'0.15 Lni')]:S.dim((a,bot),(b,bot),-10,tt,2.5)
        for tg,px,py in [('M',x+32,top-c),('L',cols[1]-22,top-c),('R',cols[1]+cw+22,top-c),('K',cols[2]-22,top-c)]:S.leader(tg,(px,py),(px,top+26),'BC')
        for tg,px,py in [('U',x+45,bot+c),('V',cols[0]+cw+L1/2,bot+c+1.6),('Q',cols[1]+cw+25,bot+c+3.2),('P',cols[1]+cw+Li/2,bot+c+3.2),('N',cols[2]-12,bot+c+3.2)]:S.leader(tg,(px,py),(px+6,bot-20),'TC')
        nts=['* EXTENSION BASED ON THE LARGER OF THE TWO ADJACENT SPANS.','SPLICES: MECHANICAL, WELDED OR CLASS B LAP SPLICE.']
    yy=S.notes(nts,x+16,bot-64,410)
    S.title(code,ttl,x,yy-4,CW)
x,y=cell(2,0);strip(x,y,'SLAB-2_2','SLAB COLUMN STRIP',False)
x,y=cell(3,0);strip(x,y,'SLAB-2_3','SLAB MIDDLE STRIP',True)
# ---------------- SLAB-100 ----------------
x,y=cell(0,1);t=18;top=y-60;bot=top-t;bx=x+150;bw=36;bd=48;drop=14;ex=x+400
poly([(x+20,top),(bx+bw,top),(bx+bw,top-5),(ex,top-drop),(ex,top-drop-t),(bx+bw,top-5-t),(bx+bw,top-bd),(bx,top-bd),(bx,bot),(x+20,bot)]);S.brk(x+20,bot,top);S.cl(bx+bw/2,top-bd-8,top+30)
S.bar([(x+22,top-c),(bx+bw-3,top-c),(bx+bw-3,top-c-10)]);S.bar([(x+22,bot+c),(bx+5,bot+c)])
S.bar([(bx+6,top-c-2),(ex-3,top-drop-c),(ex-3,top-drop-t+c),(ex-12,top-drop-t+c)]);S.bar([(bx+bw/2,top-5-t+c),(ex-10,top-drop-t+c+1.6)])
S.pl([(bx+3.5,top-3.5),(bx+bw-3.5,top-3.5),(bx+bw-3.5,top-bd+3.5),(bx+3.5,top-bd+3.5)],'REBAR',True)
for p in [(bx+6,top-6),(bx+bw-6,top-6),(bx+6,top-bd+6),(bx+bw-6,top-bd+6),(ex-6,top-drop-c-2.2),(ex-6,top-drop-t+c+2.2)]:S.dot(*p)
for d in range(int(bx+bw+15),int(ex-10),14):S.dot(d,top-5-c-2.6-(d-bx-bw)*(drop-5)/(ex-bx-bw),1.2)
S.leader('#X @ s O.C. (TOP)',(x+300,top-9.5),(x+300,top+22));S.leader('#X @ s O.C. (BOTTOM)',(x+300,top-drop-t+c+4.5),(x+300,top-drop-t-22));S.leader('SLAB EDGE 2-#X',(ex-6,top-drop-c-2.2),(ex,top+10))
S.leader('BEAM SIZE AND BARS\nPER BEAM SCHEDULE',(bx+bw-6,top-bd+6),(bx+60,top-bd-14));S.leader('FLOOR SLAB',(x+60,top),(x+60,top+18));S.text('SLOPE',x+255,top-3,3,rot=-2.5)
S.text('DRIP AT THE SLAB EDGE: OPTION 1 - BOTTOM BARS STOP SHORT OF THE DRIP AND ARE SPLICED; OPTION 2 - BOTTOM BARS SLOPED WITH 20 mm MIN. COVER AT THE DRIP (25 x 25 DRIP).',x+20,top-bd-34,2.8,width=400)
yy=S.notes(['BALCONY TOP SURFACE SLOPES AWAY FROM THE FLOOR SLAB; TOP COVER 50 mm AT THE CANTILEVER EDGE.','CANTILEVER BARS PARALLEL TO THE CANTILEVER SPAN; DEVELOPMENT AND LAPS PER NSCP / ACI 318 25.4 AND 25.5.','BEAM RESISTING TORSION WITH CLOSED TIES (NSCP / ACI 318 9.5.4, 9.7.6).'],x+20,top-bd-48,400)
S.title('SLAB-100','SLAB TO BEAM CONNECTION AT BALCONY',x,yy-4,CW)
# ---------------- SLAB-200/201 ----------------
def corner(x,y,opt):
    sz=125;S.pl([(x-8,y+8),(x+sz+20,y+8)],'HIDDEN');S.pl([(x-8,y+8),(x-8,y-sz-20)],'HIDDEN');S.ln((x,y),(x+sz+20,y));S.ln((x,y),(x,y-sz-20))
    if opt==1:
        q=100
        for a in range(0,90,14):S.bar([(x+a+2,y-2),(x+q,y-(q-a))]);S.bar([(x+2,y-a-2),(x+q-a,y-q)]) if a>0 else None
        for a in range(28,200,14):
            p0=(x+2,y-a) if a<=q else (x+a-q,y-q);p1=(x+a,y-2) if a<=q else (x+q,y-(a-q));S.pl([p0,p1],'HIDDEN')
        S.text('OPTION 1',x+sz/2,y-sz-30,3.6,'TC')
    else:
        for k in range(7):S.bar([(x+8+k*14,y-3),(x+8+k*14,y-96)])
        for k in range(7):S.bar([(x+3,y-8-k*14),(x+96,y-8-k*14)])
        S.text('OPTION 2',x+sz/2,y-sz-30,3.6,'TC')
    S.dim((x,y),(x+100,y),14,'L long / 5');S.dim((x,y),(x,y-100),14,'L long / 5')
x,y=cell(1,1);corner(x+40,y-35,1);corner(x+250,y-35,2)
S.leader('(n) #X @ s O.C. TOP\n(PARALLEL TO THE DIAGONAL)',(x+68,y-60),(x+150,y-70));S.leader('(n) #X @ s O.C. BOTTOM\n(DASHED, ACROSS THE DIAGONAL)',(x+60,y-100),(x+150,y-140))
yy=S.notes(['AT THE EXTERIOR CORNERS OF TWO-WAY SLABS (WALL OR COLUMN SUPPORT) PROVIDE CORNER BARS TOP AND BOTTOM EXTENDING L long / 5 (NSCP / ACI 318 8.7.3.1).','OPTION 1: TOP BARS PARALLEL TO THE DIAGONAL, BOTTOM BARS PERPENDICULAR TO IT. OPTION 2: BARS IN TWO LAYERS PARALLEL TO THE SIDES, TOP AND BOTTOM (EASIER TO PLACE, MAY CONGEST).'],x+20,y-200,400)
S.title('SLAB-200 / 201','TWO-WAY SLAB CORNER REINFORCEMENT',x,yy-4,CW)
# ---------------- SLAB-202/203 ----------------
x,y=cell(2,1);ox,oy=x+160,y-60;w,h=80,64
S.pl([(ox,oy),(ox+w,oy),(ox+w,oy-h),(ox,oy-h)],closed=True);S.ln((ox,oy),(ox+w,oy-h),'HIDDEN');S.ln((ox+w,oy),(ox,oy-h),'HIDDEN')
for k in range(3):
    S.bar([(ox-90,oy+8+k*3.5),(ox+w+90,oy+8+k*3.5)]);S.bar([(ox-90,oy-h-8-k*3.5),(ox+w+90,oy-h-8-k*3.5)]);S.bar([(ox-8-k*3.5,oy+45),(ox-8-k*3.5,oy-h-45)]);S.bar([(ox+w+8+k*3.5,oy+45),(ox+w+8+k*3.5,oy-h-45)])
for a,b in [((ox-12,oy+28),(ox+28,oy-12)),((ox+w-28,oy-12),(ox+w+12,oy+28)),((ox-12,oy-h-28),(ox+28,oy-h+12)),((ox+w-28,oy-h+12),(ox+w+12,oy-h-28))]:S.bar([a,b])
for k in range(2):
    xx=ox+24+k*32;S.bar([(xx,oy+42),(xx,oy+3),(xx+4,oy+3)]);S.bar([(xx,oy-h-42),(xx,oy-h-3),(xx+4,oy-h-3)])
S.leader('SLAB OPENING\nSIZE PER PLAN',(ox+w/2,oy-h/2),(x+80,oy-h/2-10));S.leader('MOVE 1/2 OF THE DISPLACED BARS TO EACH\nSIDE OF THE OPENING @ 75 O.C.,\n2 BARS MIN. EACH SIDE (TYP.)',(ox+w+90,oy+11.5),(ox+w+50,oy+40))
S.leader('#X x 1200 LONG AT 45°,\nCENTERED (TYP.)',(ox+w+5,oy+20),(ox+w+60,oy-30));S.leader('TOP BARS WITH STANDARD\nHOOK AT THE OPENING',(ox+24,oy+20),(x+80,oy+34))
yy=S.notes(['FOR OPENINGS 300 mm OR LESS THE SLAB BARS MAY BE RESPACED WITHOUT ADDED BARS.','DISPLACED BOTTOM BARS EXTEND THE FULL LENGTH OF THE BAY IN WHICH THEY OCCUR.','THE ADDED BARS ON EACH SIDE EQUAL THE DISPLACED BARS, NOT LESS THAN ONE #16, DEVELOPED BEYOND THE OPENING OR HOOKED (NSCP / ACI 318 8.5.4).'],x+20,oy-h-56,400)
S.title('SLAB-202 / 203','CONCRETE SLAB REINFORCEMENT AT OPENINGS',x,yy-4,CW)
# ---------------- SLAB-204 ----------------
x,y=cell(3,1);t=16
def slope(x0,y0,down):
    s=-0.42 if down else 0.42;k=180;L=150
    poly([(x0,y0),(x0+k,y0),(x0+k+L,y0+s*L),(x0+k+L,y0+s*L-t),(x0+k+6*(1 if down else -1)*0+ (6 if down else -6),y0-t),(x0,y0-t)]);S.brk(x0,y0-t,y0);S.brk(x0+k+L,y0+s*L-t,y0+s*L)
    S.bar([(x0+2,y0-c),(x0+k,y0-c),(x0+k+L-2,y0-c+s*(L-2))])
    if down:S.bar([(x0+2,y0-t+c),(x0+k+50,y0-t+c)]);S.bar([(x0+k-50,y0-t+c+1.8),(x0+k+5,y0-t+c+1.8),(x0+k+L-2,y0-t+c+1.8+s*(L-7))]);S.dot(x0+k+4,y0-t+c+3.4)
    else:S.bar([(x0+2,y0-t+c),(x0+k-6,y0-t+c),(x0+k+L-2,y0-t+c+s*(L+4))]);S.dot(x0+k-5,y0-t+c+1.8)
    S.dim((x0+k-50,y0),(x0+k,y0),10,'BASIC LAP',2.5);S.dim((x0+k,y0),(x0+k+50,y0+s*50),10,'BASIC LAP',2.5)
    S.leader('LAP THE BOTTOM BARS FOR SLOPES\nGREATER THAN 1:2' if down else 'BOTTOM BARS BENT CONTINUOUS;\n#16 MIN. AT THE BEND',(x0+k,y0-t+c+2),(x0+k-30,y0-t-24) if down else (x0+k-40,y0+26))
slope(x+20,y-40,True);slope(x+20,y-185,False)
yy=S.notes(['FLEXURAL BARS OF THE SLOPED SLAB EXTEND THROUGH THE SLOPE POINT WITH TENSION EMBEDMENT BEYOND IT (NSCP / ACI 318 25.4, 25.5).','SLAB DEPTH H PER PLAN IN THE FLAT AND SLOPED REGIONS.'],x+20,y-210,400)
S.title('SLAB-204','SLOPE IN ELEVATED SLAB',x,yy-4,CW)
# ---------------- SLAB-205 ----------------
x,y=cell(0,2);t=18;top=y-80;bot=top-t;cx=x+150;cw=24;chh=50
poly([(x+20,top),(cx,top),(cx,top+chh),(cx+cw-4,top+chh),(cx+cw,top+chh-4),(cx+cw,bot),(x+20,bot)]);S.brk(x+20,bot,top);S.ln((cx,top),(cx+cw,top),'HIDDEN')
S.bar([(x+22,top-c),(cx+cw-c,top-c),(cx+cw-c,top-c-8)]);S.bar([(x+22,bot+c),(cx+cw-c,bot+c),(cx+cw-c,bot+c+8)])
S.bar([(cx+cw-c-1.6,bot+c+1.6),(cx+cw-c-1.6,top+chh-c),(cx+c,top+chh-c),(cx+c,top+chh-c-8)]);S.bar([(cx+cw-7,top+chh-c-1.6),(cx+cw-7,bot+c+1.6),(cx-30,bot+c+1.6)])
for p in [(cx+cw-c-3.4,top-c-2),(cx+cw-c-3.4,bot+c+2),(cx+6,top+chh-c-3),(cx+6,top+22)]:S.dot(*p)
S.leader('1-#X CONTINUOUS TOP',(cx+6,top+chh-c-3),(cx+70,top+chh+8));S.leader('#X @ 300 HORIZONTAL',(cx+6,top+22),(cx+70,top+30));S.leader('1-#X TOP AND BOTTOM AT\nTHE SLAB EDGE (TYP.)',(cx+cw-c-3.4,bot+c+2),(cx+70,bot-14))
S.leader('#X @ s O.C. 90° BENT BAR,\nLAP WITH THE BOTTOM BARS',(cx-25,bot+c+1.6),(x+120,bot-24));S.leader('20 CHAMFER',(cx+cw-2,top+chh-2),(cx+70,top+chh+20));S.text('CONSTRUCTION JOINT',cx-74,top+3,2.8)
S.dim((cx,top+chh),(cx+cw,top+chh),8,'200 MIN.',2.5);S.text('WATERPROOF MEMBRANE AT THE\nEXTERIOR SIDE BY OTHERS',x+30,top+30,2.8)
S.text('INTERIOR CURB: 150 WIDE, 300 MAX. HIGH; 1-#X HORIZONTAL UP TO 150 HIGH, 2-#X UP TO 300 HIGH; 90° BENT DOWELS INTO THE SLAB. CURB 200 mm OR WIDER: 2 LAYERS OR A TIE (U-STIRRUP ALT. 1 / CLOSED TIE ALT. 2).',x+20,bot-44,2.8,width=400)
yy=S.notes(['SLAB TOP AND BOTTOM BARS END WITH A 90° HOOK NEAR THE SLAB EDGE; AT LEAST ONE #16 TOP AND BOTTOM AT THE EDGE.','CURB BARS: 90° BENT BARS PARALLEL TO THE BOTTOM SLAB BARS, LONGER THAN THE TENSION DEVELOPMENT LENGTH, LAP SPLICED WITH THE BOTTOM BARS (NSCP / ACI 318 25.4, 25.5).'],x+20,bot-62,400)
S.title('SLAB-205','CONCRETE CURB AT SLAB EDGE / INTERIOR CURB',x,yy-4,CW)
# ---------------- SLAB-206 ----------------
x,y=cell(1,2);t=30;top=y-80;bot=top-t;dx0,dx1=x+175,x+255
pts=[(x+40,top),(x+400,top),(x+400,bot),(x+40,bot)];S.pl(pts,closed=True);S.fill(pts);S.brk(x+40,bot,top);S.brk(x+400,bot,top)
S.pl([(dx0,top-9),(dx1,top-9),(dx1,bot+9),(dx0,bot+9)],closed=True);S.ln((dx0,top-9),(dx1,bot+9),'HIDDEN');S.ln((dx1,top-9),(dx0,bot+9),'HIDDEN')
S.bar([(x+42,top-c),(x+398,top-c)]);S.bar([(x+42,bot+c),(x+398,bot+c)]);S.bar([(dx0-38,top-c-1.8),(dx1+38,top-c-1.8)]);S.bar([(dx0-38,bot+c+1.8),(dx1+38,bot+c+1.8)])
for d in range(int(dx0-30),int(dx1+38),14):S.dot(d,top-c-3.6,1.2);S.dot(d,bot+c+3.6,1.2)
S.dim((dx0,top),(dx1,top),14,'DUCT WIDTH, 300 MAX.',2.5);S.dim((dx1,top),(dx1+38,top),14,'300 TYP.',2.5);S.dim((x+400,top),(x+400,top-9),-12,'65 MIN. CLR',2.3);S.dim((x+400,bot+9),(x+400,bot),-12,'65 MIN. CLR',2.3)
S.leader('(n) #X LONGITUDINAL',(dx0+8,bot+c+3.6),(x+110,bot-22));S.leader('#X @ s O.C.',(x+100,top-c),(x+80,top+20))
S.text('BAR TO DUCT: 40 MAX. (165 SLAB), 45 MAX. (170 SLAB), 50 MAX. (180 SLAB OR THICKER). OPTION: WELDED WIRE MESH, SIZE AND SPACING BY THE ENGINEER.',x+40,bot-34,2.8,width=360)
S.text('DUCT 750 MIN. CLEAR OF ALL SLAB SUPPORTS AND 600 MIN. CLEAR OF OTHER DUCTS; LOCATION, SIZE AND EXTENT PER THE MECHANICAL DRAWINGS.',x+40,bot-52,2.8,width=360)
S.title('SLAB-206','CONCRETE ELEVATED SLAB WITH EMBEDDED DUCTWORK',x,bot-72,CW)
# ---------------- SLAB-207 ----------------
x,y=cell(2,2);t=18;top1=y-70;bot1=top1-t;sx=x+170;sw=46;top2=bot1-14;bot2=top2-t
poly([(x+20,top1),(sx+sw-3,top1),(sx+sw,top1-3),(sx+sw,top2),(x+410,top2),(x+410,bot2),(sx,bot2),(sx,bot1),(x+20,bot1)]);S.brk(x+20,bot1,top1);S.brk(x+410,bot2,top2)
S.bar([(x+22,top1-c),(sx+sw-c,top1-c),(sx+sw-c,top1-c-8)]);S.bar([(x+40,bot1+c),(sx+sw-c-2,bot1+c)])
S.pl([(sx+3.5,top1-3.5),(sx+sw-3.5,top1-3.5),(sx+sw-3.5,bot2+3.5),(sx+3.5,bot2+3.5)],'REBAR',True)
S.bar([(sx+c,top2-c),(x+408,top2-c)]);S.bar([(sx+c+1.6,top2-c-1.6),(sx+c+1.6,bot2+c),(x+408,bot2+c)])
for p in [(sx+6,top1-6),(sx+sw-6,top1-6),(sx+6,bot2+6),(sx+sw-6,bot2+6)]:S.dot(*p)
S.dim((x+70,top1),(sx,top1),10,'Ld',2.6);S.dim((sx,top1),(sx+sw,top1),10,'TRANSITION',2.6);S.dim((sx+sw,top2),(sx+sw+90,top2),22,'Ld',2.6)
S.leader('(n) #X CONTINUOUS\nTOP AND BOTTOM',(sx+6,bot2+6),(x+60,bot2-16));S.leader('CLOSED TIES #X @ s',(sx+sw-3.5,bot2+14),(sx+90,bot2-18));S.leader('STANDARD HOOK BOTTOM\nREINFORCEMENT IF REQUIRED',(sx+sw-c-2,bot1+c),(x+40,bot1-26))
S.text('H1',x+14,(top1+bot1)/2,3.2,'MR');S.text('H2',x+416,(top2+bot2)/2,3.2,'ML');S.text('20 CHAMFER',sx+sw+4,top1+2,2.8);S.text('SLAB REINFORCEMENT NOT SHOWN FOR CLARITY',x+20,bot2-40,2.8)
yy=S.notes(['STEP LOCATION AND HEIGHT PER THE FLOOR PLAN, COORDINATED WITH THE ARCHITECTURAL DRAWINGS.','BARS OF THE UPPER AND LOWER SLABS EXTEND INTO THE TRANSITION WITH TENSION EMBEDMENT; ADDED TOP BARS END WITH A STANDARD HOOK NEAR THE VERTICAL FACE.','TOP AND BOTTOM BARS IN THE TRANSITION WITH CLOSED STIRRUPS FULL LENGTH (NSCP / ACI 318 25.4).'],x+20,bot2-50,400)
S.title('SLAB-207','STEP IN ELEVATED SLAB',x,yy-4,CW)
# ---------------- SLAB-208 ----------------
x,y=cell(3,2);t1=18;t2=34;top=y-80;sx=x+200
poly([(x+20,top),(x+410,top),(x+410,top-t2),(sx,top-t2),(sx,top-t1),(x+20,top-t1)]);S.brk(x+20,top-t1,top);S.brk(x+410,top-t2,top)
S.bar([(x+22,top-c),(x+408,top-c)]);S.bar([(x+22,top-t1+c),(sx+70,top-t1+c)]);S.bar([(sx+c,top-t2+c),(x+408,top-t2+c)])
S.bar([(sx-40,top-c-1.8),(sx+c+1.6,top-c-1.8),(sx+c+1.6,top-t2+c+1.8),(sx+70,top-t2+c+1.8)])
for d in range(int(x+32),int(x+408),16):S.dot(d,top-c-3.6,1.2)
S.dim((sx,top),(sx+70,top),10,'Ld OR SEE PLAN',2.6);S.text('H1',x+14,top-t1/2,3.2,'MR');S.text('H2\nSEE PLAN',x+416,top-t2/2,3.2,'ML')
S.leader('TRANSITION Z BARS: SIZE AND SPACING OF\nTHE BOTTOM BARS, LAPPED WITH THE TOP BARS',(sx+c+1.6,top-t1-6),(sx-40,top-t2-24));S.leader('STANDARD HOOK BOTTOM\nREINFORCEMENT',(sx+50,top-t2+c+1.8),(sx+100,top-t2-22))
S.leader('BOTTOM BARS OF THE THINNER SLAB\nEXTEND Ld INTO THE THICKER SLAB',(sx+45,top-t1+c),(sx+100,top+24))
yy=S.notes(['CHANGE IN SLAB DEPTH PER THE FLOOR PLAN; SLAB DEPTHS H1 AND H2 PER NSCP / ACI 318 7.3.1.','TRANSITION Z BARS LAP-SPLICED WITH THE TOP BARS (TENSION LAP, NSCP / ACI 318 25.5).'],x+20,top-t2-46,400)
S.title('SLAB-208','SLAB THICKNESS CHANGE - STEP IN BOTTOM OF ELEVATED ONE-WAY SLAB',x,yy-4,CW)
for i in range(4):
    for j in range(3):S.frame(i*CW,-j*CH,CW,CH)
S.text('TYPICAL SLAB DETAILS',0,34,12,'BL',layer='TITLE');S.text('REDRAWN AFTER THE ACI DETAILING MANUAL MNL-66(20), SECTION 2 - SLAB DETAILS, FOR PHILIPPINE PRACTICE: METRIC (mm), NSCP 2015 / ACI 318 REFERENCES. ALL DETAILS NOT TO SCALE. #10 = 10 mm BAR. THE ENGINEER OF RECORD REVIEWS AND COMPLETES EACH DETAIL FOR THE PROJECT.',0,18,3.6,'BL',width=1700)
S.doc.saveas(sys.argv[1]);print('saved')
