# Desktop recovery checkpoint — 14 November 2026

Source checkpoint for the next desktop update; the downloadable release remains v1.4.47. No Store, TestFlight or Pages publication.

Implemented in source:
- Earthworks summary and gravel quantities on Earthworks Start; top-level caption Earthworks.
- Editable auto-growing calculation grids use a left row-header delete cross, preserving data-column indices. The blank input row and computed results have no delete cross. Add/Remove Entry commands removed from POW, Beam and Slab estimating sheets.
- Sheet guides accessible through an information button in the worksheet heading.
- Folder-shaped top tabs with full captions and separate main/sub/nested colors.
- Pavement result headings explicitly show Total area (sq.m.) or Total volume (cu.m.) rather than Pay quantity / Pay unit. Full PCCP descriptions are inside the sheet; short tab captions retained.
- Subgrade preparation is area only, with no thickness or swell/shrink factor. Legacy volume-mode project settings migrate to square metres. Roadway excavation remains in-place bank volume without swell/shrink multiplication.
- Built-in subgrade catalogue unit corrected to sq.m. A linked workbook whose item is still cu.m. must be reviewed/corrected; exact unit identity is retained, so no square metres are silently written as cubic metres.
- Disconnect no longer forces an unchanged BACKUP write. It finishes pending writes/save, suppresses automatic timer requeue during disconnection, and prevents repeated disconnect/reconnect races. Excel save and COM release may still take time; actual Windows timing remains unverified.
- Earlier draft work: lazy POW tab styling and embedded estimator visibility; roofing dropdown validation; mixed roofing material/thickness routing and profile schema 2 / POW schema 21; responsive project/admin layouts; beam formatter presentation; automatic always-included Tree IDs; Excel metadata caching and lazy anchors; wind contour and circular roughness overlays and live building model.

Both Admin and User require compilation after edits. Pure quantity tests can run on Linux; actual Windows native layout, first-open timing, deletion interaction and Excel COM behavior remain unverified here. Existing native tests need updates for project schema 21 and removed wind-map tab. Terrain scaling and interactive steel-section previews remain pending. Wind land-cover suggestions remain preliminary; no claim of full NSCP compliance from raster data alone.

Never commit owner private keys, certificates or real mobile activation keys.
