from pathlib import Path
from decimal import Decimal
import json, shutil, uuid, hashlib
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
WORK=ROOT
SOURCE=ROOT/'Art/UI/Cyberdeck/2026-11-03-v2-resources'
NS=uuid.UUID('947325b9-cc43-43ce-9b86-ced186cb18f4')
def meta(path):
 p=Path(str(path)+'.meta')
 if p.exists():return
 rel=path.relative_to(WORK).as_posix()
 p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid5(NS,rel).hex+'\n'+('folderAsset: yes\n' if path.is_dir() else ''),encoding='utf-8')
def amount(id,n):return {'id':id,'amount':str(n)}

def main():
 data=json.loads((SOURCE/'catalogue.json').read_text(encoding='utf-8'))
 items=[]
 for a in data['assets']:
  if a['family']=='blocs':continue
  unit='u' if a['family'] in ('recettes','outils') else 'kg'
  litres='25' if a['family']=='outils' else '2' if unit=='u' else '0.37' if a['family'] in ('minerais','recyclage') else '0.12'
  items.append(dict(id=a['id'],name=a['label'],icon=a['id'],family=a['family'],unit=unit,litres=litres))
 recipes=[]
 for r in data['recipes']:
  if r['family']=='block':continue
  recipes.append(dict(id=r['id'],name=r['label'],icon=r['output'],machine='assembler',seconds=2 if r['family']=='component' else 5,
   inputs=[amount(k,v) for k,v in r['materials_x1_kg'].items()],outputs=[amount(r['output'],1)]))
 for r in data['refining']:
  recipes.append(dict(id=r['id'],name=next(a['label'] for a in data['assets'] if a['id']==next(iter(r['input']))),icon=next(iter(r['input'])),machine='refinery',seconds=2,
   inputs=[amount(k,Decimal(str(v))/100) for k,v in r['input'].items()],outputs=[amount(k,Decimal(str(v))/100) for k,v in r['outputs'].items()]))
 # Original prototype bills for 1m cells: deliberately separate from SE 2.5m comparisons.
 specs=[
 ('ArmorCube','Armure','block-armor',(1,1,1),{'plates':4}),
 ('ArmorSlope','Armure inclinée','block-armor',(1,1,1),{'plates':3}),
 ('ArmorCorner','Coin d’armure','block-armor',(1,1,1),{'plates':2}),
 ('Gyroscope','Gyroscope','block-gyroscope',(1,1,1),{'plates':8,'construction':2,'large-tube':1,'motor':2,'circuits':1}),
 ('Battery','Batterie','block-battery',(1,1,1),{'plates':6,'construction':2,'power-cell':4,'circuits':1}),
 ('Thruster','Propulseur','block-thruster',(1,1,2),{'plates':6,'construction':4,'large-tube':1,'thruster':1}),
 ('Cockpit','Cockpit','block-cockpit',(2,2,3),{'plates':10,'construction':4,'motor':1,'display':2,'circuits':2,'glass':4}),
 ('Refinery','Raffinerie','block-refinery',(1,2,1),{'plates':20,'construction':8,'large-tube':4,'motor':4,'circuits':2,'metal-grid':1}),
 ('Assembler','Assembleur','block-assembler',(2,1,1),{'plates':12,'construction':6,'motor':2,'display':2,'circuits':4,'metal-grid':1}),
 ('Cargo','Soute','block-cargo',(1,1,1),{'interior-plate':6,'construction':4,'tubes':2,'motor':1,'display':1,'circuits':1}),
 ('SolarPanel','Panneau solaire','component-solar-cell',(1,1,1),{'solar-cell':4,'construction':2,'girder':2,'circuits':1}),
 ]
 blocks=[]
 for id,name,icon,size,bill in specs:
  blocks.append(dict(id=id,name=name,icon=icon,x=size[0],y=size[1],z=size[2],components=[amount('component-'+k,v) for k,v in bill.items()],batteryKwh=4 if id=='Battery' else 0,solarKw=40 if id=='SolarPanel' else 0))
 definition=dict(version='voidforge-economy-1m-v1',sourceProfile=data['profile_id'],assemblerEfficiency=1,items=items,recipes=recipes,blocks=blocks)
 dest=WORK/'Assets/Resources/VoidForge/Economy';dest.mkdir(parents=True,exist_ok=True)
 (dest/'Catalogue.json').write_text(json.dumps(definition,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 art=WORK/'Assets/VoidForge/Art/UI/Cyberdeck'
 art.mkdir(parents=True,exist_ok=True)
 trims=json.loads((art/'icon-trim.json').read_text(encoding='utf-8'))['icons']
 trims={i['id']:i for i in trims}
 for a in data['assets']:
  if a['origin']!='new-imagegen':continue
  src=SOURCE/a['file'];target=art/(a['id']+'.png');shutil.copyfile(src,target)
  with Image.open(src) as im:
   bbox=im.getchannel('A').point(lambda v:255 if v>32 else 0).getbbox()
   x0,y0,x1,y1=bbox;pad=round(max(x1-x0,y1-y0)*.035)
   x0,y0=max(0,x0-pad),max(0,y0-pad);x1,y1=min(im.width,x1+pad),min(im.height,y1+pad)
   trims[a['id']]=dict(id=a['id'],x=x0/im.width,y=1-y1/im.height,w=(x1-x0)/im.width,h=(y1-y0)/im.height)
 (art/'icon-trim.json').write_text(json.dumps(dict(icons=list(trims.values())),indent=2),encoding='utf-8')
 baseline={}
 for rel in ['Assets/VoidForge/Scripts/UI/Cyberdeck/CyberdeckSession.cs','Assets/VoidForge/Scripts/UI/Cyberdeck/CyberdeckHud.cs','Assets/VoidForge/Scripts/UI/Cyberdeck/CyberdeckRuntime.cs','Assets/VoidForge/Art/UI/Cyberdeck/icon-trim.json']:
  baseline[rel]=hashlib.sha256((ROOT/rel).read_bytes()).hexdigest()
 # Existing serialized GUIDs are always retained; this command stages only this catalogue.
 owned=[art,dest,ROOT/'Assets/VoidForge/Scripts/Economy']
 for p in sorted(p for folder in owned for p in [folder,*folder.rglob('*')]):
  if p.suffix!='.meta':meta(p)
 print(json.dumps(dict(items=len(items),recipes=len(recipes),blocks=len(blocks),icons=len(trims))))

if __name__=='__main__':main()
