import sys,ezdxf
from ezdxf.addons.drawing import RenderContext, Frontend
from ezdxf.addons.drawing.matplotlib import MatplotlibBackend
from ezdxf.addons.drawing.config import Configuration, BackgroundPolicy, ColorPolicy
import matplotlib.pyplot as plt
doc=ezdxf.readfile(sys.argv[1]);msp=doc.modelspace()
fig=plt.figure(figsize=(float(sys.argv[3]),float(sys.argv[4])),dpi=int(sys.argv[9]) if len(sys.argv)>9 else 100);ax=fig.add_axes([0,0,1,1])
cfg=Configuration(background_policy=BackgroundPolicy.WHITE,color_policy=ColorPolicy.BLACK)
Frontend(RenderContext(doc),MatplotlibBackend(ax),config=cfg).draw_layout(msp,finalize=True)
if len(sys.argv)>5:
    x0,x1,y0,y1=map(float,sys.argv[5:9]);ax.set_xlim(x0,x1);ax.set_ylim(y0,y1)
fig.savefig(sys.argv[2],facecolor='white')
