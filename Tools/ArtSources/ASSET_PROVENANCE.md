# Shadow Garden 6.1 asset provenance

Date: 2026-08-05

All 6.1 raster additions were generated specifically for this project with OpenAI image generation, then locally chroma-keyed, despilled, cropped, resized, and visually inspected. No third-party game asset or artist-named style prompt was used.

## Moa action poses

- References: the project-owned `Moa_ActionPoses_Source.png` and canonical `Moa_front_source.png`.
- Purpose: replace the cream-cloak identity drift with one navy-cloak, brass-trimmed master.
- Post-process: border chroma key, soft matte, one-pixel edge contraction, Lanczos crop to six 384×384 runtime poses.

## Gameplay VFX additions

- Reference: the project-owned `GameplayFx_source.png`.
- Added as distinct assets: danger pulse, door glow, flower petals, cliff-fall dust.
- Post-process: border chroma key, soft matte, despill, quadrant crop to 256×256.

## World foreground and reactions

- References: the project-owned W01/W02/W03 prop source sheets.
- Added per world: three independent foreground sprites and one environment reaction overlay.
- The former reversed-background foreground fallback is no longer used.
- W01 avoids a tall foliage strip; W03 avoids free-floating solid crystal debris.

## Deterministic vector source

- `UI/ShadowGardenUiMaster.svg` is a project-authored vector source.
- Korean copy remains dynamic TextMeshPro content and is not embedded in the raster UI.
