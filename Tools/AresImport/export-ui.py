"""Export only presentation strings; never modify the reference or its snapshots."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
source = root / 'a896f420-3053-49eb-b6f2-a97f9b2dc6c8 (4)' / 'src'
dest = root / 'Assets/ARES/Resources/ARES/UI'
dest.mkdir(parents=True, exist_ok=True)

def flatten(value, prefix=''):
    for key, item in value.items():
        path = f'{prefix}.{key}' if prefix else key
        if isinstance(item, dict):
            yield from flatten(item, path)
        elif isinstance(item, str):
            yield {'key': path, 'value': item}

for lang in ['pt-BR', 'en']:
    data = json.loads((source / f'locales/{lang}.json').read_text(encoding='utf-8'))
    (dest / f'{lang}.json').write_text(json.dumps({'entries': list(flatten(data))}, ensure_ascii=False), encoding='utf-8')
