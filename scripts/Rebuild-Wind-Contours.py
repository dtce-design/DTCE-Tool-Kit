"""Rebuild the workbook-derived wind TIN. Run with the owner-supplied RCB Wind v.3.3.xlsm.
This is raster interpolation, not an authoritative georeferenced wind dataset.
Dependencies: Pillow, numpy, scipy. No workbook macros are executed.
"""
from pathlib import Path
import sys,zipfile,re,io,json,hashlib
import numpy as np
from PIL import Image
from scipy import ndimage,optimize
from scipy.spatial import Delaunay
root=Path(__file__).resolve().parents[1];ref=root/'reference-review/wind';ref.mkdir(parents=True,exist_ok=True)
book=Path(sys.argv[1]);coast=Path(sys.argv[2]);binary=zipfile.ZipFile(book).read('xl/vbaProject.bin');images={}
for k,m in enumerate(re.finditer(b'\xff\xd8\xff',binary)):
 if k in [27,28,29]:
  raw=binary[m.start():binary.find(b'\xff\xd9',m.start())+2];im=Image.open(io.BytesIO(raw));im.load();assert im.size==(3179,4801);images[k]=np.array(im.convert('RGB'));(root/'src'/f'WindMap{k}.jpg').write_bytes(raw)
a=images[27];sea=(a[:,:,2].astype(int)-a[:,:,0]>45)&(a[:,:,1].astype(int)-a[:,:,0]>20)&(a[:,:,2]>220);labels,n=ndimage.label(~sea);sizes=np.bincount(labels.ravel());land=(sizes[labels]>700)&(labels>0);distance=ndimage.distance_transform_edt(~(land^ndimage.binary_erosion(land)))
world=json.loads(coast.read_text());ph=next(f for f in world['features'] if f['properties'].get('ADM0_A3')=='PHL');points=[]
for polygon in ph['geometry']['coordinates']:
 ring=np.array(polygon[0]);
 if len(ring)>35:points.extend(ring[::2])
points=np.array(points);points[:,1]=np.degrees(np.log(np.tan(np.pi/4+np.radians(points[:,1])/2)))
def residual(t):return np.minimum(ndimage.map_coordinates(distance,[t[2]*(points[:,1]-13)+t[3],t[0]*(points[:,0]-121)+t[1]],order=1,mode='constant',cval=100),45)
fit=optimize.least_squares(residual,[300,1430,-300,2500],diff_step=.0001,max_nfev=400,loss='soft_l1',f_scale=5);transform=fit.x
levels={27:[290,320,300,310,280,270,260,250,240],28:[300,290,310,330,320,280,270,260,250],29:[310,300,320,340,330,290,280,260,270,250,240]};contours={}
for number,a in images.items():
 a=a.astype(int);red=(a[:,:,0]>130)&(a[:,:,0]-a[:,:,1]>65)&(a[:,:,0]-a[:,:,2]>55);components,count=ndimage.label(ndimage.binary_dilation(red,iterations=6));items=[]
 for component,sl in enumerate(ndimage.find_objects(components),1):
  if sl is None:continue
  ys,xs=np.nonzero(red[sl]&(components[sl]==component))
  if len(xs)<100 or ys.max()-ys.min()<50:continue
  items.append(np.column_stack([xs+sl[1].start,ys+sl[0].start]))
 assert len(items)==len(levels[number]),'Contour geometry changed; review labels before regenerating.'
 contours[number]=items
lines=['DTCE-WIND-TIN-1',','.join(str(x) for x in transform)];manifest=[]
for category,number in [('I',29),('II',28),('III-V',27)]:
 nodes=[];items=list(zip(contours[number],levels[number]))
 if number!=29:items.append((contours[29][-1],240)) # Explicit 240 labels on Palawan in all three maps.
 for curve,speed in items:
  grid={}
  for x,y in curve:grid.setdefault((int(x)//12,int(y)//12),[]).append([x,y])
  nodes.extend([*np.mean(group,axis=0),speed] for group in grid.values())
 nodes=np.array(nodes);triangles=Delaunay(nodes[:,:2]).simplices;lines.append(f'SURFACE,{category},{number},{len(nodes)},{len(triangles)}');lines.extend(f'{x:.3f},{y:.3f},{speed:.0f}' for x,y,speed in nodes);lines.extend(','.join(str(i) for i in t) for t in triangles);manifest.append({'category':category,'image':number,'nodes':len(nodes),'triangles':len(triangles),'contourSpeeds':levels[number]})
(root/'src/WindSurfaces.txt').write_text('\n'.join(lines)+'\n');audit={'workbookSHA256':hashlib.sha256(book.read_bytes()).hexdigest(),'source':'Owner-supplied RCB Wind v.3.3.xlsm, embedded NSCP2015 maps; I essential/highest, II intermediate, III-V lower','method':'Delaunay TIN/barycentric interpolation of traced contours;12px grid sampling; Web Mercator fitted to Natural Earth 1:10m coastline','registration':dict(zip(['sx','cx','sy','cy'],transform)),'medianResidualPixels':float(np.median(residual(transform))),'p90ResidualPixels':float(np.percentile(residual(transform),90)),'surfaces':manifest,'limitations':'Approximate raster digitization, not exact site wind speed. Coastal continuation outside hull must be flagged. Special wind regions require review.'};(ref/'manifest.json').write_text(json.dumps(audit,indent=2));print(json.dumps(audit,indent=2))
