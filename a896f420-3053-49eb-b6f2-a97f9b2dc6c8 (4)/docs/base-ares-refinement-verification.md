# Base ARES visual refinement verification

Build OK. All 48 tests pass. Desktop (1280×1800) and touch phone (390×844): allocated 50/30/20 through the real Energy Panel, started Sol, selected Greenhouse map guidance, asked Bubble about solar flares, read the answer, opened Observatory fallback cards, disabled Bubble toasts, and switched to Portuguese. One canvas per game, no page errors in the final pass. The existing campaign, energy engine, data and narrator service files were not modified.

## Comparable rendering measurements
Fresh runs at Sol fraction 0.3; spawn (0,4), interior (0,0), and overview distance 150 / pitch 1.2; 45 rendered frames per state. Desktop includes all active render passes. Counts vary slightly with culling and camera convergence.

| Quality / view | Before calls | After calls | Before triangles | After triangles |
|---|---:|---:|---:|---:|
| Low spawn | 90 | 89 | 24,240 | 26,388 |
| Low interior | 89 | 85 | 24,084 | 25,908 |
| Low overview | 148 | 151 | 33,436 | 41,904 |
| High spawn | 171 | 169 | 56,645 | 66,233 |
| High interior | 179 | 173 | 58,505 | 68,041 |
| High overview | 240 | 241 | 70,253 | 86,649 |

Low close views meet the approximately 100-call target; full-base overview exceeds it. All views remain below 100,000 triangles. New rounded hulls, tiled floors, arches and levers add geometry; merging and instancing remain. Shadows remain disabled on touch.

## Limitations
Actual NASA picture fields are empty/sample. Honest unavailable cards were checked; authentic live images, real video content, image download failures, and live NASA/n8n service provenance remain unverified without their existing inputs/credentials. No data or backend changes were made. NASA captions are bilingual framing, not newly generated scientific summaries.

First walk is presentation-only and yields to mission targets; manual map guidance does not change campaign tasks, and alarms override manual targets. Real GPU frame rate, audible sound, and touch feel cannot be measured here: open the preview on a phone, walk to the wall Energy Panel, drag the joystick, rotate the camera, and ask Bubble with sound enabled.
