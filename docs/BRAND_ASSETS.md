# Frame Theory Official Brand Assets and Identity Policy

## Authoritative source
- Brand: **Frame Theory**.
- Instagram: **@frametheoryvisuals.ai** — never the superseded handle.
- Official logo: **only the original logo file provided in Frame Theory project sources** (current source attachment titled `Elegant Frame Theory Monogram.png`). Treat that source as authoritative; never redraw, invent, restyle, vector-trace, letter-substitute or recolor it.

## Asset availability
The approved logo exists in project source material, but **the GitHub repository was empty at documentation initialization**. It is **not yet included in this commit**. A maintainer must copy the exact approved bytes to `assets/brand/frame-theory-original.png` after verifying provenance; no reconstruction or stand-in logo. Development UI can use a neutral non-branded placeholder until available. The logo is RGB; do not assert transparent background.

## Use
- Preserve exact image content, aspect ratio, typography and brand colors; only lossless placement/scaling is permitted.
- Do not apply shadows, filters, blur, gradients or decorations unless project owner specifically approves.
- Use approved logo for splash screen, app icon, about dialog and social branding only after rights/size requirements are verified. An operating-system icon may need a logo-safe layout; obtain explicit approval before modifying the asset or padding it.
- Social-export watermarks: optional and opt-in; use approved source logo without altered content; Instagram follow CTA uses `@frametheoryvisuals.ai`.
- Test actual compiled assets for byte identity or digest against approved original.

## Releasing
Block *branded* build artifacts until the approved original logo is copied and verified. Lack of logo should not block unbranded editor engine development. Never present a generated image or UI mockup as the verified official logo.

## Required asset actions
1. Import exact image source into repository (owner-approved).
2. Record source file checksum, intended usage rights and approved path in this document.
3. Have a reviewer compare pixel appearance and byte fingerprint against source.
4. Package the original without destructive transformations.
