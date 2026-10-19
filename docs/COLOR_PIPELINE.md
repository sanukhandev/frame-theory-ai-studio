# Unified Color Pipeline — Images + Video

## Non-negotiable goal

A look authored once can be reused on both photos and video **without color-space mistakes**. “Same preset” means identical canonical _intent_ represented as normalized parameters, not blindly identical encoded RGB operations.

## V1 color contract

1. Read source metadata: image ICC profile, video primaries/transfer/matrix/range, bit depth, orientation and alpha.
2. Decode into documented working representation (initial SDR baseline): linear-light Rec.709/sRGB primaries with sufficient float precision for transforms where applicable; distinguish sRGB and Rec.709 transfer.
3. Evaluate a versioned, ordered adjustment graph: exposureEV -> temperature/tint -> contrast/pivot -> curves -> HSL -> saturation -> LUT -> output mapping.
4. Map to preview display/output space, and ensure export uses equivalent transforms.
5. Guard clipping, invalid LUTs, alpha handling, gamut and transform precision; never apply a display-referred LUT in the wrong space.

## UI controls (P0)

Exposure (EV), contrast, saturation, white balance (temperature/tint), shadows/highlights when verified, HSL and RGB curves, grade enable/disable, stack reorder, bypass, keyframe where supported, split-screen before/after, presets and film-emulation looks using original licensed transforms.

## LUT import/export

- Support common `.cube` 3D LUT syntax `LUT_3D_SIZE`, `DOMAIN_MIN`, `DOMAIN_MAX`, comments; reject malformed/oversize inputs and enforce memory limits. Implement trilinear initially; tetrahedral interpolation optional with parity tests.
- Support generated 17/33/65 grid sizes based on quality/performance; clearly communicate that LUTs cannot represent spatial masks, text, cropping, keyframes, transforms or local retouching.
- Bake only _color-representable_ operations; document input/output profile and domain with exported look.
- Verify round-trip against identity, ramps, known charts and reference transforms; tolerance stored with golden tests.

## Shared workflows

Photo adjustment -> save look JSON -> apply to video clip -> compare sample charts under declared profiles -> export .cube. Video grade -> export preset -> apply to photo. Always maintain originals.

## AI color plan

Gemini suggests bounded values and creative rationale; it does **not** invent unsupported physics or remove color profiles. Validate value ranges, report color profile assumptions, allow manual correction. Reference matching uses profile-normalized image statistics plus model creative plan; differentiate statistical match from semantic look.

## Instrumentation and quality

Build RGB patch charts, exposure/gray wedge, skin tone sample synthetic set, known Rec.709 frame and sRGB PNG, alpha edge and out-of-gamut fixture. Test deterministic identical results across preview/export within measured tolerances and document unavoidable hardware differences.

## Later

Waveform, vectorscope, histogram, scopes measurement, LOG transforms and camera profile packs, HDR/PQ/HLG, ACES/OCIO optional integration only after license + accuracy review.
