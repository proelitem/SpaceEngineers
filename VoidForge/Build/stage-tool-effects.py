"""Stage only the referenced Sci-Fi Arsenal particles into the ignored project-local vendor folder."""
from pathlib import Path
import hashlib,json,re,shutil

project=Path(__file__).resolve().parents[1]
bank=Path('G:/UnityAssets/asset_manager/store/Sci-Fi_Arsenal/Files/Archanor/Sci-Fi Arsenal')
dest=project/'Assets/ThirdParty/SciFiArsenal'
lookup={}
for meta in bank.rglob('*.meta'):
    match=re.search(r'^guid: ([a-f0-9]{32})',meta.read_text(encoding='utf-8-sig',errors='ignore'),re.M)
    if match:lookup[match[1]]=Path(str(meta)[:-5])
stack=[bank/('Sci-Fi Effects/Prefabs/Environment/Sparks/'+name+'.prefab') for name in ('SparksYellow','SparksBlue')]
seen=set();missing=set()
while stack:
    path=stack.pop()
    if path in seen:continue
    seen.add(path)
    if path.suffix.lower() in ('.prefab','.mat','.asset','.shader','.shadergraph'):
        for guid in re.findall(r'guid: ([a-f0-9]{32})',path.read_text(encoding='utf-8-sig',errors='ignore')):
            if guid in lookup:stack.append(lookup[guid])
            elif not guid.startswith('0000000000000000'):missing.add(guid)
if missing:raise RuntimeError('Unresolved vendor GUIDs: '+str(sorted(missing)))
for path in sorted(seen):
    target=dest/path.relative_to(bank);target.parent.mkdir(parents=True,exist_ok=True)
    for source,out in ((path,target),(Path(str(path)+'.meta'),Path(str(target)+'.meta'))):
        if not source.exists():continue
        if out.exists() and out.read_bytes()!=source.read_bytes():raise RuntimeError('Local vendor asset differs: '+str(out))
        shutil.copy2(source,out)
report={'source':str(bank),'license':'Asset Store; local ignored derivatives only','files':[{'path':str(p.relative_to(bank)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(seen)]}
out=project/'Library/Recette04Publication/arsenal-provenance.json';out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(report,indent=2),encoding='utf-8')
print('Sci-Fi Arsenal staged:',len(seen),'assets; zero unresolved GUIDs')
