# Shadow Garden production art provenance

These are project-specific production assets created on 2026-08-05 for **Shadow Garden**. The generated raster art uses only the project's own concept documents and the already approved Moa reference as visual inputs; no living artist or commercial game style was requested or copied.

## Master workflow

1. Establish the shared hand-painted top-down 3/4 camera, silhouettes, materials, and palette from the project's concept and in-game asset planning documents.
2. Generate each asset family independently on a uniform chroma background.
3. Remove chroma with the official ImageGen helper, despill, crop, normalize, and inspect alpha edges.
4. Create simple channel and UI icons deterministically from vector-like geometric forms.
5. Keep generated master sheets under `Assets/Game/Art/Source`; import only separated runtime sprites through the presentation catalog.

## Adopted asset families

- Moa: six movement frames, six expression portraits, six story/action poses.
- Common gameplay: brass sun lamp, three silhouette-distinct pillar heights, four channel marks.
- Worlds: individual orchard, canyon, and greenhouse backgrounds; six tiles, three props, two door states, and two night-flower states per world.
- FX: single shadow, overlap hazard, cliff, rotation sweep, time vacuum, and completion glow.
- UI: deterministic panels, buttons, focus frame, key cap, pause/retry/world-map/status icons.

Every runtime image was checked for generated text, watermarks, subject cropping, magenta spill, and visual drift. The source files remain separated from the replaceable runtime assets.
