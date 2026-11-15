# Base ARES verification

43 tests pass; current preview build is OK. Desktop energy UI allocated 50/30/20 and started Sol 1; fixed-step movement crossed real tunnels into Observatory and through Airlock to Solar Park. Observatory unavailable/sample response read back through UI. Roof Off, Portuguese labels, isometric camera and mobile Low default checked. No page errors in final browser pass.

## Measured renderer counts

| Quality/view | Before calls | Before triangles | After calls | After triangles |
|---|---:|---:|---:|---:|
| High spawn |226|206469|171|56645|
| High interior |198|202785|179|58505|
| High overview |246|224953|231|68153|
| Low spawn |130|89016|107|26868|
| Low interior |84|74204|89|24084|
| Low overview |133|90168|147|33196|

Sol time 0.3; spawn and hub center captured. Before overview used distance 85/pitch 1; after overview uses distance 150/pitch 1.2 to include the larger base, so overview totals are not strictly equivalent views. Low exceeds the aspirational 100-call target at spawn/overview; triangle budget is met. Shadows are disabled on touch; High totals include shadow/postprocessing passes.

## Not verified
Live NASA and n8n: no supplied keys, authentic Sol Earth dates or picture snapshots. Source snapshot dates remain SAMPLE-DATE-1 through 5, apod fields null; no invented NASA images. Optional T11 stays disabled as before. Cache is per server instance and does not survive cold starts.
Real-device frame rate, touch feel and audible sound cannot be measured here: open preview on a phone, walk through tunnels, rotate/zoom, and enable sound to check them. Reference image and separate Context Skill document were not supplied.
