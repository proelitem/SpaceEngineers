"""Build original data, cost calculator and gallery; Pillow is used for read-only QA."""
from pathlib import Path
from decimal import Decimal as D
import json, csv, hashlib, html, zipfile
from PIL import Image
from prepare_catalogue import NEW

ROOT=Path(__file__).resolve().parent
V1=ROOT.parent/'2026-11-03-v1'
WIKI='https://spaceengineers.wiki.gg/wiki/'
DATE='2026-11-03'
MATERIALS={
 'Fe':('Fer','ingot-iron','ore-iron',.7),
 'Ni':('Nickel','ingot-nickel','ore-nickel',.4),
 'Co':('Cobalt','ingot-cobalt','ore-cobalt',.3),
 'Si':('Silicium','wafer-silicon','ore-silicon',.7),
 'Mg':('Magnésium','powder-magnesium','ore-magnesium',.007),
 'Ag':('Argent','ingot-silver','ore-silver',.1),
 'Au':('Or','ingot-gold','ore-gold',.01),
 'Pt':('Platine','ingot-platinum','ore-platinum',.005),
 'U':('Uranium','ingot-uranium','ore-uranium',.01),
 'Gr':('Gravier','gravel','ore-stone',.014),
}
# One finished component; amounts in kg of refined material, realistic x1.
COMPONENTS=[
 ('component-plates','Plaque d’acier','Steel_Plate',{'Fe':21}),
 ('component-construction','Éléments de construction','Construction_Components',{'Fe':8}),
 ('component-interior-plate','Plaque intérieure','Interior_Plate',{'Fe':3}),
 ('component-tubes','Petit tube','Small_Steel_Tube',{'Fe':5}),
 ('component-large-tube','Grand tube','Large_Steel_Tube',{'Fe':30}),
 ('component-metal-grid','Grille métallique','Metal_Grid',{'Fe':12,'Ni':5,'Co':3}),
 ('component-motor','Moteur électrique','Motor',{'Fe':20,'Ni':5}),
 ('component-circuits','Ordinateur','Computer',{'Fe':.5,'Si':.2}),
 ('component-display','Écran','Display',{'Fe':1,'Si':5}),
 ('component-girder','Poutrelle','Girder',{'Fe':6}),
 ('component-glass','Verre renforcé','Bulletproof_Glass',{'Si':15}),
 ('component-reactor','Composant de réacteur','Reactor_Components',{'Fe':15,'Ag':5,'Gr':20}),
 ('component-thruster','Composant de propulsion','Thruster_Components',{'Fe':30,'Co':10,'Au':1,'Pt':.4}),
 ('component-gravity','Composant gravitationnel','Gravity_Components',{'Fe':600,'Co':220,'Ag':5,'Au':10}),
 ('component-medical','Composant médical','Medical_Components',{'Fe':60,'Ni':70,'Ag':20}),
 ('component-radio','Composant radio','Radio_communication_Components',{'Fe':8,'Si':1}),
 ('component-detector','Composant de détection','Detector_Components',{'Fe':5,'Ni':15}),
 ('component-explosives','Charge explosive','Explosives',{'Si':.5,'Mg':2}),
 ('component-solar-cell','Cellule solaire','Solar_Cell',{'Ni':3,'Si':6}),
 ('component-power-cell','Cellule énergétique','Power_Cell',{'Fe':10,'Ni':2,'Si':1}),
 ('component-superconductor','Supraconducteur','Superconductor',{'Fe':10,'Au':2}),
]
TOOLS=[
 ('tool-drill','Foreuse à main','Hand_Drill',{'Fe':8,'Ni':3,'Si':3}),
 ('tool-welder','Soudeuse à main','Welder_(Tool)',{'Fe':5,'Ni':1,'Si':3}),
 ('tool-grinder','Meuleuse à main','Grinder_(Tool)',{'Fe':6,'Ni':2,'Si':2}),
]
# Full completed large-grid vanilla blocks: required + optional outer armor.
# These quantities describe SE reference variants, NOT the different 1m VoidForge blocks.
BLOCKS=[
 ('block-armor','Armure légère','Light_Armor_Block',[1,1,1],{'component-plates':25}),
 ('block-cockpit','Cockpit standard','Cockpit_1',[1,1,1],{'component-plates':30,'component-motor':1,'component-display':8,'component-construction':20,'component-circuits':100,'component-glass':60}),
 ('block-thruster','Petit propulseur ionique','Ion_Thruster',[1,1,2],{'component-thruster':80,'component-plates':25,'component-large-tube':8,'component-construction':60}),
 ('block-gyroscope','Gyroscope','Gyroscope',[1,1,1],{'component-plates':600,'component-motor':4,'component-metal-grid':50,'component-large-tube':4,'component-construction':40,'component-circuits':5}),
 ('block-battery','Batterie standard','Battery',[1,1,1],{'component-plates':80,'component-power-cell':80,'component-construction':30,'component-circuits':25}),
 ('block-refinery','Raffinerie complète','Refinery',[2,4,2],{'component-plates':1200,'component-motor':16,'component-metal-grid':20,'component-large-tube':20,'component-construction':40,'component-circuits':20}),
 ('block-assembler','Assembleur complet','Assembler',[1,1,2],{'component-plates':140,'component-motor':20,'component-metal-grid':10,'component-display':10,'component-construction':80,'component-circuits':160}),
 ('block-cargo','Petite soute','Small_Cargo_Container',[1,1,1],{'component-tubes':20,'component-motor':4,'component-metal-grid':4,'component-interior-plate':40,'component-display':1,'component-construction':40,'component-circuits':2}),
]
EXCLUDED=[
 {'family':'Prototech','items':['Prototech Scrap','Prototech Frame','Prototech Cooling Unit','Prototech Circuitry','Prototech Machinery','Prototech Propulsion Unit','Prototech Capacitor','Prototech Panel'],'reason':'Branche industrielle avancée postérieure au socle classique de la feuille de route ; non couverte par ce lot. Frame non fabricable.','source':WIKI+'Items'},
 {'family':'Autres fabrications SE','items':['Canvas','Datapad','Oxygen Bottle','Hydrogen Bottle','Medkit','Powerkit','outils améliorés','armes et munitions','alimentation et agriculture'],'reason':'Équipement, survie, combat et extensions hors des interfaces et de la chaîne industrielle classique demandées ici.','source':WIKI+'Items'},
 {'family':'Objets non fabricables','items':['Zone Chip','Space Credits','plushies','consommables commerciaux'],'reason':'Pas de recette minérale d’assemblage à attribuer.','source':WIKI+'Items'},
]

def write(name,data):
 (ROOT/name).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def fdecimal(v):return D(str(v))

def ore_cost(cost,eff):
 """Deterministic route: gravel from stone, credit its Fe/Ni/Si, dedicated ores for rest."""
 need={m:fdecimal(v)/D(eff) for m,v in cost.items()}
 ores={};credit={};surplus={}
 if need.get('Gr',0):
  stone=need.pop('Gr')/D('.014');ores['ore-stone']=stone
  for m,yield_ in [('Fe','.03'),('Ni','.0024'),('Si','.004')]:
   available=stone*D(yield_);required=need.get(m,D(0))
   credit[m]=min(available,required);surplus[m]=max(D(0),available-required)
   if required:need[m]=max(D(0),required-available)
 for m,v in need.items():
  if v:ores[MATERIALS[m][2]]=v/fdecimal(MATERIALS[m][3])
 return dict(ore_kg={k:float(v) for k,v in ores.items()},stone_coproduct_credit_kg={MATERIALS[k][1]:float(v) for k,v in credit.items()},surplus_refined_kg={MATERIALS[k][1]:float(v) for k,v in surplus.items()})

def main():
 old=json.loads((V1/'manifest.json').read_text(encoding='utf-8'))['assets']
 wanted={x[0] for x in COMPONENTS+TOOLS}|{x[0] for x in BLOCKS}|{v[1] for v in MATERIALS.values()}|{v[2] for v in MATERIALS.values()}|{'ore-ice'}
 assets={a['id']:dict(id=a['id'],label=a['label'],family=a['family'],file='../2026-11-03-v1/'+a['file'],origin='existing-v1',sha256=a['sha256']) for a in old if a['id'] in wanted}
 sources=json.loads((ROOT/'selected-sources.json').read_text(encoding='utf-8-sig')) if (ROOT/'selected-sources.json').exists() else []
 source_map={a['id']:a['source'] for a in sources}
 checks=[]
 for id,label,family,subject in NEW:
  p=ROOT/family/(id+'.png')
  entry=dict(id=id,label=label,family=family,file=f'{family}/{id}.png',origin='new-imagegen',available=p.exists(),source=source_map.get(id))
  if p.exists():
   raw=p.read_bytes();im=Image.open(p);alpha=im.getchannel('A') if im.mode=='RGBA' else None
   hist=alpha.histogram() if alpha else None
   entry.update(width=im.width,height=im.height,mode=im.mode,sha256=hashlib.sha256(raw).hexdigest(),bytes=len(raw),transparent_fraction=hist[0]/(im.width*im.height) if hist else 0,alpha_bbox=list(alpha.getbbox()) if alpha else None)
   # Generated alpha occasionally has an imperceptible value 1/255 at a corner.
   # Read-only analysis tolerates that; no generated pixels are changed.
   corners=[alpha.getpixel(pos) for pos in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]] if alpha else [255]
   visible_bbox=alpha.point(lambda v:255 if v>=16 else 0).getbbox() if alpha else None
   entry.update(alpha_corner_max=max(corners),visible_alpha_threshold=16,visible_bbox=visible_bbox)
   entry['qa_pass']=bool(alpha and .15<entry['transparent_fraction']<.95 and max(corners)<=1 and visible_bbox and visible_bbox[0]>im.width*.025 and visible_bbox[1]>im.height*.025 and visible_bbox[2]<im.width*.975 and visible_bbox[3]<im.height*.975)
   checks.append(dict(id=id,pass_=entry['qa_pass']))
  assets[id]=entry
 for id,label,_,_ in COMPONENTS+TOOLS:assets[id]['label']=label
 for id,label,_,_,_ in BLOCKS:assets[id]['label']=label+' — référence SE'
 recipes=[]
 def add(id,label,family,page,cost,**extra):
  profiles={}
  for e in [1,3,10]:profiles[str(e)]=dict(refined_kg={MATERIALS[m][1]:float(fdecimal(v)/D(e)) for m,v in cost.items()},**ore_cost(cost,e))
  recipes.append(dict(id='craft-'+id,output=id,label=label,family=family,output_quantity=1,output_unit='piece',source=WIKI+page,source_table=WIKI+'Component' if family=='component' else WIKI+page,materials_x1_kg={MATERIALS[m][1]:v for m,v in cost.items()},profiles=profiles,**extra))
 for id,label,page,cost in COMPONENTS:add(id,label,'component',page,cost)
 for id,label,page,cost in TOOLS:add(id,label,'tool',page,cost,variant='standard')
 by_id={c[0]:c[3] for c in COMPONENTS}
 for id,label,page,size,bill in BLOCKS:
  cost={}
  for comp,qty in bill.items():
   for m,v in by_id[comp].items():cost[m]=cost.get(m,D(0))+fdecimal(v)*qty
  add(id,label,'block',page,{k:float(v) for k,v in cost.items()},component_bill=bill,reference_grid='SE large grid, 2.5 m',reference_size_cells=size,completion='100%, required + optional',voidforge_balance_status='reference_only_not_adopted')
 refining=[]
 for m,(label,product,ore,yield_) in MATERIALS.items():
  if m=='Gr':continue
  refining.append(dict(id='refine-'+m,input={ore:1000},outputs={product:float(D(1000)*fdecimal(yield_))},unit='kg',yield_fraction=yield_,source=WIKI+'Refinery',machine='full_refinery_no_yield_module'))
 refining.extend([
  dict(id='refine-stone',input={'ore-stone':1000},outputs={'gravel':14,'ingot-iron':30,'ingot-nickel':2.4,'wafer-silicon':4},unit='kg',source=WIKI+'Refinery',machine='full_refinery_no_yield_module'),
  dict(id='recycle-scrap',input={'scrap-metal':1000},outputs={'ingot-iron':800},unit='kg',source=WIKI+'Refinery',machine='full_refinery_no_yield_module')])
 schema=dict(schema_version=1,profile_id='se1-classic-industrial-public-reference-20261103',title='VoidForge Odyssey — référence industrielle classique SE1',verified_date=DATE,reference='Current public official wiki values for classic items; not a verified 2016 game snapshot.',adoption_status='design_reference_not_runtime_economy',mass_unit='kg',default_assembler_efficiency=1,available_assembler_efficiencies=[1,3,10],refinery='Full refinery, no yield upgrades; not Basic Refinery.',ore_route='Dedicated ores; stone only for gravel. Stone Fe/Ni/Si coproducts credited once before dedicated ores. No stock, scrap or recycling assumed.',formula='refined_cost = sum(component_quantity * material_cost_x1) / assembler_efficiency; ore = remaining_refined / refinery_yield. Do not divide block component counts by efficiency.',rounding='Stored floats are display representations of Decimal calculations; display 3 decimals, never round intermediate requirements.',excluded_costs=['power','time','transport','mining losses'],sources=[dict(url=WIKI+'Component',role='21 classic component costs',retrieval='search-index excerpts; direct fetch 403'),dict(url=WIKI+'Refinery',role='refining yields and refinery block BOM',retrieval='search-index excerpts; direct fetch 403'),dict(url=WIKI+'World_Settings',role='efficiency x1/x3/x10'),dict(url=WIKI+'Items',role='scope and excluded classes')],assets=list(assets.values()),recipes=recipes,refining=refining,ice=dict(id='ore-ice',processing='O2/H2 generator; not a refinery ingot',conversion_not_in_this_profile=True,source=WIKI+'Ore'),excluded=EXCLUDED)
 write('catalogue.json',schema)
 write('manifest.json',dict(count=len(assets),new_requested=len(NEW),new_available=sum(a.get('available',False) for a in assets.values()),assets=list(assets.values())))
 write('validation.json',dict(status='complete' if len(checks)==len(NEW) and all(c['pass_'] for c in checks) else 'in_progress_or_failed',new_pngs=len(checks),expected_pngs=len(NEW),checks=checks))
 missing={family:[dict(id=id,label=label) for id,label,f,_ in NEW if f==family] for family in dict.fromkeys(x[2] for x in NEW)}
 write('audit.json',dict(before=dict(ore=3,refined=1,component=3,total_icons_all_categories=39),missing_before=missing,reused_semantics=[dict(id='component-plates',meaning='Steel Plate'),dict(id='component-tubes',meaning='Small Steel Tube'),dict(id='component-circuits',meaning='Computer')],after=dict(raw_materials=11,refined=10,components=21,salvage=1,tools=3,block_reference_variants=8),excluded=EXCLUDED))
 with (ROOT/'recipes.csv').open('w',encoding='utf-8-sig',newline='') as f:
  w=csv.writer(f);w.writerow(['recipe_id','produit','famille','quantite_sortie','efficacite_assembleur','ingredient','nature','quantite_kg','source'])
  for r in recipes:
   for e,p in r['profiles'].items():
    for typ,key in [('raffine','refined_kg'),('minerai_equivalent','ore_kg')]:
     for ingredient,qty in p[key].items():w.writerow([r['id'],r['label'],r['family'],1,e,ingredient,typ,format(qty,'.12g'),r['source_table']])
 with (ROOT/'refining.csv').open('w',encoding='utf-8-sig',newline='') as f:
  w=csv.writer(f);w.writerow(['recette','entree','kg_entree','sortie','kg_sortie','source'])
  for r in refining:
   for product,qty in r['outputs'].items():w.writerow([r['id'],next(iter(r['input'])),1000,product,qty,r['source']])
 gallery(schema)
 report(schema,missing)
 verify_data(schema)
 print(json.dumps(dict(assets=len(assets),new_available=len(checks),craft_recipes=len(recipes),refining_recipes=len(refining)),ensure_ascii=False))

def verify_data(data):
 ids={a['id'] for a in data['assets']};assert len(ids)==54
 assert len(data['recipes'])==32 and len(data['refining'])==11
 for r in data['recipes']:
  assert r['output'] in ids
  for e,p in r['profiles'].items():
   assert all(x in ids for x in p['refined_kg']) and all(x in ids for x in p['ore_kg'])
   for m,v in r['materials_x1_kg'].items():assert abs(p['refined_kg'][m]*int(e)-v)<1e-7
   # Verify the route supplies all required material; only stone has multiple outputs.
   for mat,need in p['refined_kg'].items():
    supply=0
    for ref in data['refining']:
     ore=next(iter(ref['input']));supply+=p['ore_kg'].get(ore,0)*ref['outputs'].get(mat,0)/1000
    assert supply+1e-7>=need,(r['id'],e,mat,supply,need)
 plate=next(r for r in data['recipes'] if r['output']=='component-plates')
 assert plate['profiles']['1']['ore_kg']['ore-iron']==30
 assert plate['profiles']['3']['ore_kg']['ore-iron']==10
 motor=next(r for r in data['recipes'] if r['output']=='component-motor')
 assert motor['profiles']['1']['ore_kg']['ore-nickel']==12.5
 assert abs(motor['profiles']['1']['ore_kg']['ore-iron']-200/7)<1e-10
 write('data-validation.json',dict(success=True,checks=['54 unique item IDs','32 recipes and 11 refining/recycling routes','all ingredient and icon references resolve','x1/x3/x10 scale once','raw routes supply all refined ingredients including credited stone coproducts','steel plate x1=30 kg ore, x3=10 kg ore','motor x1 nickel ore=12.5 kg'],runtime_balance_modified=False))

def report(data,missing):
 rows=['# Catalogue industriel — VoidForge Odyssey','',f'Profil public vérifié le {DATE}. **Référence de conception, pas économie active du jeu.**','',
 '## Inventaire des manques','',
 'Avant : 3 minerais (fer, nickel, silicium), fer raffiné, plaques, tubes génériques et circuits génériques. Les trois images de composants existantes sont réaffectées explicitement à plaque d’acier / petit tube / ordinateur, sans modifier les fichiers v1.','',
 '| Famille | Images à produire |','|---|---|']
 for family,items in missing.items():rows.append(f'| {family} ({len(items)}) | '+', '.join(x['label'] for x in items)+' |')
 rows+=['','Après ce lot : **11 matières brutes, 10 raffinées, 21 composants, 1 ferraille**, avec 3 outils et 8 variantes de blocs de référence ; 54 entrées et 36 images nouvelles.','',
 '## Coûts et réglages','',
 'Les quantités d’assemblage du wiki officiel correspondent au réglage réaliste ×1. À ×3 ou ×10, diviser les matières raffinées par ce multiplicateur. Les nombres de composants d’un bloc restent entiers et inchangés. Les masses de produits finis ne se déduisent pas des ingrédients. [Réglages officiels]('+WIKI+'World_Settings).','',
 'Raffinerie complète sans modules de rendement : Fe 70 %, Ni 40 %, Co 30 %, Si 70 %, Mg 0,7 %, Ag 10 %, Au 1 %, Pt 0,5 %, U 1 %. Pour 1000 kg de pierre : 14 kg de gravier, 30 kg Fe, 2,4 kg Ni, 4 kg Si. Ferraille : 80 % Fe. La glace se traite dans un générateur O₂/H₂, pas en lingots. [Raffinerie]('+WIKI+'Refinery), [minerais]('+WIKI+'Ore).','',
 'L’équivalent minerai est un parcours calculé : minerais dédiés pour les métaux, pierre pour le gravier. Les coproduits Fe/Ni/Si de cette pierre sont crédités une seule fois ; leurs surplus sont explicités dans JSON. Il ne s’agit pas d’une optimisation de tous les parcours possibles ni d’un coût avec stocks/récupération. Énergie, temps et pertes de minage exclus.','',
 '## Recettes de composants — une unité, ×1','',
 '| Produit | Matières raffinées (kg) | Minerais bruts équivalents (kg) |','|---|---|---|']
 labels={a['id']:a['label'] for a in data['assets']}
 for r in data['recipes']:
  if r['family']!='component':continue
  p=r['profiles']['1']
  fmt=lambda d:', '.join(f'{labels[k]} {v:.3f}'.rstrip('0').rstrip('.') for k,v in d.items())
  rows.append(f"| {r['label']} | {fmt(p['refined_kg'])} | {fmt(p['ore_kg'])} |")
 rows+=['','Source des ingrédients : [table des composants]('+WIKI+'Component). Les valeurs sont des faits de jeu transcrits dans un schéma original ; aucun fichier de jeu, code ou visuel Keen importé. Les pages directes ont renvoyé HTTP 403 : vérification via leurs extraits indexés, consultés avec recherche web.','',
 '## Blocs et périmètre','',
 'Les 8 recettes de blocs sont des comparaisons **SE grande grille 2,5 m, variante nommée, construction complète**. Les couches dites optional dans le wiki sont incluses pour atteindre 100 %. Les blocs VoidForge sont de tailles différentes : ces coûts ne sont pas adoptés automatiquement. Les totaux sont recalculés depuis la nomenclature et les coûts unitaires ; les résumés de masses de certains articles du wiki ne concordent pas avec leurs propres tableaux (notamment l’assembleur).','',
 'Le périmètre est la chaîne industrielle classique des interfaces actuelles, cohérente avec la référence autour de 2016 de docs/ROADMAP.md. Les chiffres vérifiés proviennent du wiki actuel ; **aucune certification d’un état exact du jeu en 2016**.','',
 'Différences modernes inventoriées mais hors de ce lot :']
 for e in EXCLUDED:rows+=['',f"- {e['family']} : {', '.join(e['items'])}. {e['reason']}"]
 rows+=['','## Images et utilisation','',
 '36 appels distincts au générateur intégré `image_gen.imagegen`, mode built-in, fond transparent demandé ; pas de spritesheet ni de découpage, pas de conversion de pixels. PNG originaux conservés. `generation-prompts.json` contient les prompts ; `selected-sources.json` les sorties sélectionnées ; `manifest.json` leurs tailles, alpha et SHA-256.','',
 'Galerie et calculateur : `index.html`, sans serveur ni dépendance réseau. Export : `catalogue.json`, `recipes.csv`, `refining.csv`. Les références v1 sont relatives ; le ZIP final contient les images v1 utilisées et leur copie locale avec références autonomes.','',
 'Bibliothèque locale consultée : index Jarvis et packs `2D_Icons_-_Space_Rank` (grades et emblèmes) / `2000_Fantasy_Icons` (fantasy). Inadaptés à ces matières industrielles et au style original demandé ; aucune ressource tierce copiée.','',
 'Aucune modification des Assets/Resources, de l’économie, de la scène ou des fichiers réservés au build équipement. Les recettes illustratives historiques du HUD restent identifiées comme telles jusqu’au branchement de l’économie.']
 (ROOT/'README.md').write_text('\n'.join(rows)+'\n',encoding='utf-8')

def gallery(data):
 payload=json.dumps(data,ensure_ascii=False).replace('</','<\\/')
 template='''<!doctype html><html lang="fr"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>VoidForge · Ressources & fabrication</title>
<style>*{box-sizing:border-box}body{margin:0;background:#091419;color:#d8e5e7;font:15px system-ui,sans-serif}header{padding:28px 4vw 16px;border-bottom:1px solid #24424b}small,.muted{color:#8aa6af}h1{font-size:28px;letter-spacing:1px;margin:4px 0 8px}h2{font-size:20px}a{color:#56d5df}button,input,select{font:inherit;color:inherit;background:#12272e;border:1px solid #35545c;border-radius:5px;padding:9px}button{cursor:pointer}button:hover,button.active{border-color:#59e1e9;background:#1c3a40}nav{display:flex;gap:8px;flex-wrap:wrap;padding:18px 4vw}.controls{display:flex;gap:14px;align-items:center;padding:0 4vw 20px;flex-wrap:wrap}input[type=number]{width:100px}main{display:grid;grid-template-columns:minmax(340px,1fr) minmax(390px,1fr);gap:24px;padding:0 4vw 40px}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(130px,1fr));gap:10px;align-content:start}.card{background:#102229;text-align:center;padding:8px;position:relative;min-height:175px}.card img{width:100%;height:112px;object-fit:contain}.card span{display:block;font-size:13px}.tag{font-size:10px!important;letter-spacing:.5px;color:#ffbd52;margin-top:4px}.detail{position:sticky;top:16px;background:#10232b;border:1px solid #2b4f59;padding:24px;align-self:start;border-radius:8px}.hero{height:190px;width:100%;object-fit:contain}table{width:100%;border-collapse:collapse}td,th{text-align:left;border-bottom:1px solid #27414a;padding:9px 4px;font-size:13px}td:last-child{text-align:right;font-variant-numeric:tabular-nums}td img{width:34px;height:34px;object-fit:contain;vertical-align:middle;margin-right:8px}p{line-height:1.6}.notice{border-left:3px solid #dfa24a;padding:10px 14px;background:#211e17;font-size:13px}.downloads{display:flex;gap:16px;flex-wrap:wrap}.light{background:#d4dde0}summary{cursor:pointer;color:#f9bf5d}footer{padding:20px 4vw;border-top:1px solid #24424b}@media(max-width:900px){main{grid-template-columns:1fr}.detail{position:static;grid-row:1}.grid{grid-template-columns:repeat(auto-fill,minmax(125px,1fr))}}@media print{nav,.controls,.downloads{display:none}.detail{position:static}body{background:white;color:black}}</style>
<header><small>VOIDFORGE ODYSSEY / CYBERDECK / CATALOGUE 02</small><h1>Ressources & fabrication</h1><p class="muted">54 entrées · 36 nouvelles images · 32 recettes de fabrication · 11 traitements de raffinage et recyclage</p><p class="notice">Référence industrielle classique Space Engineers 1. Les coûts sont documentés pour la conception ; l’économie du jeu n’est pas encore branchée. Blocs : variante SE grande grille 2,5 m, construite à 100 %.</p><div class="downloads"><a href="README.md">Inventaire et règles</a><a href="recipes.csv">Recettes CSV</a><a href="refining.csv">Raffinage CSV</a><a href="catalogue.json">Catalogue JSON</a><a href="generation-prompts.json">Prompts ImageGen</a><a href="VoidForge-Resources-v2.zip">Pack autonome ZIP</a></div></header>
<nav id="nav"></nav><div class="controls"><input id="search" aria-label="Rechercher" placeholder="Rechercher une ressource"><label>Assemblage <select id="eff"><option value="1">×1 · réaliste</option><option value="3">×3 · ingrédients ÷3</option><option value="10">×10 · ingrédients ÷10</option></select></label><label>Quantité <input id="qty" type="number" min="1" max="100000" step="1" value="1"></label><button id="bg">Fond clair / sombre des images</button></div>
<main><section id="grid" class="grid"></section><aside id="detail" class="detail"></aside></main><footer><details><summary>Périmètre et limites de la comparaison</summary><p>11 matières brutes, 10 produits raffinés et 21 composants classiques. Ferraille séparée. Trois outils standards et huit variantes de blocs déjà illustrés. Les ingrédients de blocs sont recalculés depuis leurs composants.</p><div id="excluded"></div><p>Glace : générateur O₂/H₂, pas raffinerie. Frais d’énergie, durée et pertes de minage exclus. Aucun visuel ou fichier de jeu Keen importé.</p></details><p class="muted">Images originales : générateur intégré ImageGen · inventaire vérifié le 3 novembre 2026 · <a href="validation.json">Contrôle des PNG</a> · <a href="data-validation.json">Contrôle des calculs</a></p></footer>
<script>const DATA=__DATA__;const $=s=>document.querySelector(s);const byId=Object.fromEntries(DATA.assets.map(a=>[a.id,a]));const categories=[['all','Tout'],['minerais','Matières brutes · 11'],['produits','Raffinés · 10'],['recettes','Composants · 21'],['outils','Outils · 3'],['blocs','Blocs · 8'],['recyclage','Recyclage · 1']];let family='all',selected='component-plates',light=false;
const fmt=v=>new Intl.NumberFormat('fr-CA',{maximumFractionDigits:3}).format(v);const escape=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
function draw(){const q=$('#search').value.toLocaleLowerCase('fr');$('#nav').innerHTML=categories.map(([id,l])=>`<button data-family="${id}" class="${family===id?'active':''}">${l}</button>`).join('');$('#grid').innerHTML=DATA.assets.filter(a=>(family==='all'||a.family===family)&&a.label.toLocaleLowerCase('fr').includes(q)).map(a=>`<button class="card ${a.id===selected?'active':''} ${light?'light':''}" data-id="${a.id}"><img src="${a.file}" alt="${escape(a.label)}" loading="lazy"><span>${escape(a.label)}</span><span class="tag">${a.origin==='new-imagegen'?'NOUVEAU · IMAGEGEN':'EXISTANT · V1'}</span></button>`).join('');detail();}
function table(title,dict,scale=1,unit='kg'){return `<h2>${title}</h2><table><tbody>${Object.entries(dict).map(([id,n])=>`<tr><td><img src="${byId[id].file}" alt="">${escape(byId[id].label)}</td><td>${fmt(n*scale)} ${unit}</td></tr>`).join('')}</tbody></table>`;}
function detail(){const a=byId[selected],r=DATA.recipes.find(x=>x.output===selected);const qty=Math.max(1,Math.min(100000,Math.floor(Number($('#qty').value)||1))),eff=$('#eff').value;let out=`<img class="hero ${light?'light':''}" src="${a.file}" alt="${escape(a.label)}"><small>${a.id}</small><h1>${escape(a.label)}</h1>`;if(r){const p=r.profiles[eff];out+=`<p>${qty} unité${qty>1?'s':''} · assemblage ×${eff}</p>`;if(r.component_bill)out+=`<p class="notice">Comparaison SE, grande grille 2,5 m · ${r.reference_size_cells.join(' × ')} cellules. Coût du bloc VoidForge de 1 m à équilibrer.</p>`+table('Composants à souder',r.component_bill,qty,'u.');out+=table('Matières raffinées nécessaires',p.refined_kg,qty)+table('Minerais à extraire — parcours calculé',p.ore_kg,qty);if(Object.keys(p.stone_coproduct_credit_kg).length)out+='<p class="muted">Les coproduits de la pierre destinée au gravier sont déduits une seule fois des autres besoins.</p>'+table('Surplus raffinés de la pierre',p.surplus_refined_kg,qty);out+=`<p><a href="${r.source_table}" target="_blank" rel="noreferrer">Source officielle des ingrédients</a>${r.component_bill?` · <a href="${r.source}" target="_blank" rel="noreferrer">Nomenclature du bloc</a>`:''}</p>`;}else{const routes=DATA.refining.filter(x=>selected in x.input||selected in x.outputs);if(routes.length){for(const ref of routes){out+=table('Entrée raffinage / recyclage',ref.input)+table('Sorties pour cette entrée',ref.outputs)+`<p><a href="${ref.source}" target="_blank" rel="noreferrer">Source des rendements</a></p>`;}out+='<p class="muted">Raffinerie complète sans module de rendement. Ce tableau reste sur un lot fixe de 1000 kg ; quantité et multiplicateur d’assemblage ne le modifient pas.</p>';}else if(selected==='ore-ice')out+='<p>La glace est destinée au générateur O₂/H₂, pas à la raffinerie. Les conversions de gaz seront définies avec le système de survie.</p>';}out+=`<p class="muted"><a href="${a.file}">PNG original</a> · ${a.origin==='new-imagegen'?'Nouvelle image ImageGen':'Illustration v1 conservée'}</p>`;$('#detail').innerHTML=out;}
document.addEventListener('click',e=>{const b=e.target.closest('[data-family]');if(b){family=b.dataset.family;draw();}const c=e.target.closest('[data-id]');if(c){selected=c.dataset.id;draw();}});$('#search').oninput=draw;$('#eff').onchange=detail;$('#qty').oninput=detail;$('#bg').onclick=()=>{light=!light;draw();};$('#excluded').innerHTML=DATA.excluded.map(x=>`<p><b>${escape(x.family)}</b> : ${x.items.map(escape).join(', ')}. ${escape(x.reason)}</p>`).join('');draw();</script></html>'''
 template=template.replace('</style>','.card.light{color:#132b34}.card.light .tag{color:#704000}</style>')
 (ROOT/'index.html').write_text(template.replace('__DATA__',payload),encoding='utf-8')

def package():
 data=json.loads((ROOT/'catalogue.json').read_text(encoding='utf-8'))
 assert all(a.get('available',True) for a in data['assets'])
 # Self-contained ZIP keeps new paths and bundles reused icons under v1/.
 replacement={a['file']:'v1/'+a['file'].removeprefix('../2026-11-03-v1/') for a in data['assets'] if a['origin']=='existing-v1'}
 archive=ROOT/'VoidForge-Resources-v2.zip'
 with zipfile.ZipFile(archive,'x',zipfile.ZIP_DEFLATED) as z:
  for p in sorted(ROOT.rglob('*')):
   if not p.is_file() or p.suffix=='.zip' or '__pycache__' in p.parts:continue
   rel=p.relative_to(ROOT).as_posix()
   if p.suffix in {'.json','.html','.md'}:
    content=p.read_text(encoding='utf-8-sig')
    for old,new in replacement.items():content=content.replace(old,new)
    z.writestr(rel,content)
   else:z.write(p,rel)
  for a in data['assets']:
   if a['origin']=='existing-v1':z.write(ROOT/a['file'],replacement[a['file']])
 print(archive)

if __name__=='__main__':
 import sys
 package() if '--package' in sys.argv else main()
