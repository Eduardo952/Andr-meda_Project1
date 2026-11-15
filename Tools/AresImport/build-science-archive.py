"""Curate downloaded OFFICIAL responses. No random measurements or silent fallback.
Input: Temp/Science/{flares,cmes,osdr}.json. Re-run only after reviewing source records.
"""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = root / 'Temp/Science'
output = root / 'Assets/ARES/Resources/ARES/Science'
output.mkdir(parents=True, exist_ok=True)
flr = json.loads((source / 'flares.json').read_text(encoding='utf-8-sig'))
cme = json.loads((source / 'cmes.json').read_text(encoding='utf-8-sig'))
osdr = json.loads((source / 'osdr.json').read_text(encoding='utf-8-sig'))
records = []
for f in flr:
    records.append(dict(kind='flare', id=f['flrID'], date=f['beginTime'], label=f['classType'], value=0, url=f['link'], preliminary=False))
for c in cme:
    analyses = [a for a in c.get('cmeAnalyses') or [] if a.get('isMostAccurate') and a.get('speed', 0) > 0]
    if not analyses:
        continue
    a = sorted(analyses, key=lambda x: x.get('submissionTime', ''))[-1]
    records.append(dict(kind='cme', id=c['activityID'], date=c['startTime'], label='Nuvem solar', value=a['speed'], url=a['link'], preliminary=a['levelOfData'] == 0))
studies = [dict(id=h['_source']['Identifiers'], title=h['_source']['Study Title'], url='https://osdr.nasa.gov/bio/repo/data/studies/OSD-'+h['_source']['Identifiers'].split('-')[-1]) for h in osdr['hits']['hits']]
assert records and studies
payload = dict(provider='NASA CCMC / DONKI + NASA OSDR', mode='archive', retrievedUtc=datetime.now(timezone.utc).isoformat(), records=records, studies=studies)
(output/'archive.json').write_text(json.dumps(payload,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
audit=root/'Tools/ScienceEvidence'
audit.mkdir(exist_ok=True)
urls={'flares':'https://ccmc.gsfc.nasa.gov/DONKI-API/get/FLR?startDate=2024-05-10&endDate=2024-05-10', 'cmes':'https://ccmc.gsfc.nasa.gov/DONKI-API/get/CME?startDate=2024-05-10&endDate=2024-05-10', 'osdr':'https://osdr.nasa.gov/osdr/data/search?term=plants&from=0&size=3&type=cgene'}
manifest=[]
for name,url in urls.items():
    raw=(source/(name+'.json')).read_bytes();(audit/(name+'.json')).write_bytes(raw)
    manifest.append(dict(file=name+'.json',url=url,sha256=hashlib.sha256(raw).hexdigest()))
(audit/'manifest.json').write_text(json.dumps(dict(retrievedUtc=payload['retrievedUtc'],sources=manifest),indent=2)+'\n',encoding='utf-8')
print(f'Curated {len(records)} solar records and {len(studies)} study citations; raw evidence and SHA-256 preserved.')
