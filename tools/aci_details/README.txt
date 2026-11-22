ACI typical details for Philippine practice (owner, 22 November 2026: "based from the aci detailing manual, can you give me all the
drawings in 1 dxf file. only what is applicable in the ph"; "gawin mong maganda"; "use 2mm min text height").
Redrawn after the ACI Detailing Manual MNL-66(20) Section 2 in mm with NSCP 2015 / ACI 318 references; not copies of the manual.
  python3 slab.py "ACI Typical Slab Details (PH).dxf"    (needs ezdxf)
  python3 render.py <dxf> <png> <w in> <h in>            (preview)
Each detail sits in a 440 x 250 frame (plot each on A3, fit); text height 2.3 minimum at 1:1.
Batches: slab (done), beam / column, foundation / wall, slab-on-ground.
