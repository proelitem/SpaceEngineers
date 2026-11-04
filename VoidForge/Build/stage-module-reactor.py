"""Reuse the local Sci-Fi Base generator as a reactor casing; vendor source stays read-only."""
from pathlib import Path
import re,json,shutil,hashlib
project=Path(__file__).resolve().parents[1]
bank=Path('G:/UnityAssets/asset_manager/store/Sci-Fi_base/Files/Sci-Fi Base')
dest=project/'Assets/ThirdParty/VoidForgeReactor'
lookup={}
for meta in bank.rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})',meta.read_text(encoding='utf-8-sig',errors='ignore'),re.M)
    if match:lookup[match[1]]=Path(str(meta)[:-5])
seen=set();stack=[bank/'Prefabs/Generator_01.prefab'];missing=set()
while stack:
    source=stack.pop()
    if source in seen:continue
    seen.add(source)
    reads=[source] if source.suffix.lower() in ('.prefab','.mat','.asset') else []
    if Path(str(source)+'.meta').exists():reads.append(Path(str(source)+'.meta'))
    for read in reads:
        for guid in re.findall(r'guid: ([a-f0-9]{32})',read.read_text(encoding='utf-8-sig',errors='ignore')):
            if guid in lookup:stack.append(lookup[guid])
            elif not guid.startswith('0000000000000000'):missing.add(guid)
legacy={'3202a9e6d9c81a640a5fa4bc85f56971','da692e001514ec24dbc4cca1949ff7e8'}
if missing-legacy:raise RuntimeError('Unresolved vendor GUIDs: '+str(sorted(missing-legacy)))
for source in sorted(seen):
    target=dest/source.relative_to(bank);target.parent.mkdir(parents=True,exist_ok=True)
    for a,b in ((source,target),(Path(str(source)+'.meta'),Path(str(target)+'.meta'))):
        if not a.exists():continue
        content=a.read_bytes()
        if a.suffix.lower()=='.meta':
            content=content.replace(b'{fileID: 2100000, guid: 3202a9e6d9c81a640a5fa4bc85f56971, type: 2}',b'{fileID: 0}')
        elif a.suffix.lower()=='.mat':
            text=content.decode('utf-8-sig')
            text=re.sub(r'--- !u!114 &[^\n]+\nMonoBehaviour:.*?(?=--- !u!|\Z)',lambda m:'' if 'da692e001514ec24dbc4cca1949ff7e8' in m[0] else m[0],text,flags=re.S)
            content=text.encode('utf-8')
        if b.exists() and b.read_bytes()!=content:raise RuntimeError('Concurrent local derivative: '+str(b))
        b.write_bytes(content)
report={'source':str(bank),'prefab':'Prefabs/Generator_01.prefab','license':'Asset Store; ignored local derivative only','legacy_adaptation':{'3202a9e6d9c81a640a5fa4bc85f56971':'Missing No Name material remap on physics-only FBX; cleared locally','da692e001514ec24dbc4cca1949ff7e8':'Obsolete URP editor material-version MonoBehaviour; removed locally, material texture properties preserved'},'files':[{'path':str(p.relative_to(bank)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(seen)]}
out=project/'Library/Recette04Publication/reactor-provenance.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Reactor casing staged:',len(seen),'assets')
