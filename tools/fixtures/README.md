# Synthetic fixtures

Run `dotnet run --project tools/FrameTheory.Fixtures -- tools/fixtures/generated` to create deterministic, synthetic test media:

- `test_chart.ppm` — small RGB color chart readable by common media tools.
- `tone_1khz.wav` — one second, mono, 48 kHz, 16-bit sine tone.
- `motion_counter.csv` — 90 frame counter at 30/1 fps.

Generated files are ignored by Git. The golden test generates two independent sets and compares their bytes before exercising a known mismatch.
