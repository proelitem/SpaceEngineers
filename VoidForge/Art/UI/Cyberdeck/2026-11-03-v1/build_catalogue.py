"""Package ImageGen originals without editing their pixels."""
from pathlib import Path
from PIL import Image
import base64, hashlib, json, sys, zipfile

ROOT = Path(__file__).resolve().parent
def write_json(name, value):
    (ROOT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

if '--package-only' in sys.argv:
    manifest = json.loads((ROOT / 'manifest.json').read_text(encoding='utf-8'))
    files = [a['file'] for a in manifest['assets']]
    files += ['README.md', 'manifest.json', 'generation-prompts.json', 'selected-sources.json',
              'hud-usage.json', 'recipes-visuals.json', 'validation.json', 'index.html',
              'build_catalogue.py', 'Unity/VoidForgeGlyphMask.shader', 'Unity/Editor/HudMaskImportCheck.cs']
    files += [str(f.relative_to(ROOT)).replace('\\','/') for f in (ROOT/'Unity').rglob('*.meta')]
    for optional in ['unity-validation.json', 'qa-gallery-dark.png', 'qa-gallery-light.png', 'concept-raffinerie-bloc.png', 'concept-raffinerie-generation.json']:
        if (ROOT / optional).is_file(): files.append(optional)
    archive = ROOT / 'VoidForge-Cyberdeck-Graphics-v1.zip'
    with zipfile.ZipFile(archive, 'x', compression=zipfile.ZIP_DEFLATED) as z:
        for name in files: z.write(ROOT / name, name)
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None
    print(json.dumps({'archive':str(archive), 'files':len(files), 'bytes':archive.stat().st_size}))
    raise SystemExit(0)

selected = json.loads((ROOT / 'selected-sources.json').read_text(encoding='utf-8'))
assert len(selected) == 39 and len({a['id'] for a in selected}) == 39
assets, ui_data = [], []
for source in selected:
    path = ROOT / source['file']
    data = path.read_bytes()
    with Image.open(path) as im:
        is_mask = source['family'] == 'interface-masks'
        entry = {**source, 'width':im.width, 'height':im.height, 'mode':im.mode,
                 'bytes':len(data), 'sha256':hashlib.sha256(data).hexdigest(),
                 'usage_format':'luminance-mask' if is_mask else 'rgba-sprite'}
        assert im.width == im.height and im.width >= 1024
        if is_mask:
            rgb = im.convert('RGB')
            extrema = rgb.getextrema()
            assert all(lo <= 5 and hi >= 245 for lo,hi in extrema), source['id']
            corners = [rgb.getpixel(p) for p in [(0,0),(im.width-1,0),(0,im.height-1),(im.width-1,im.height-1)]]
            assert all(max(c) <= 12 for c in corners), (source['id'],corners)
            entry['corner_rgb_max'] = max(max(c) for c in corners)
            entry['coverage_channel'] = 'rgb-luminance'
        else:
            assert im.mode == 'RGBA', source['id']
            alpha = im.getchannel('A')
            assert alpha.getextrema() == (0,255), source['id']
            hist = alpha.histogram()
            entry['fully_transparent_fraction'] = round(hist[0] / (im.width * im.height), 4)
            assert hist[0] > 0.1 * im.width * im.height
    assets.append(entry)
    ui = {k:entry[k] for k in ['id','label','family','file','usage_format','width','height']}
    if is_mask: ui['maskData'] = 'data:image/png;base64,' + base64.b64encode(data).decode('ascii')
    ui_data.append(ui)
assert len({a['sha256'] for a in assets}) == 39
write_json('manifest.json', {'version':1,'date':'2026-11-03','tool':'image_gen.imagegen','count':39,
                           'rgba_sprites':21,'luminance_masks':18,'assets':assets})

usage = {
 'inventory': ['tool-drill','tool-welder','tool-grinder','ore-iron','ore-nickel','ore-silicon','ui-inventory','ui-battery','ui-chevron','ui-check'],
 'construction': ['block-armor','block-gyroscope','block-battery','block-cockpit','block-thruster','component-plates','ui-chevron','ui-construction'],
 'refinery': ['block-refinery','ore-iron','ingot-iron','ore-nickel','ui-power','ui-energy','ui-arrow','ui-pause','ui-inventory','ui-seat','ui-check'],
 'assembly': ['block-assembler','component-plates','component-tubes','component-circuits','ingot-iron','ui-plus','ui-minus','ui-pause','ui-inventory','ui-seat'],
 'map': ['map-asteroid','map-ship','map-outpost','ore-iron','ui-base','ui-ore','ui-gps','ui-recenter','ui-plus','ui-minus','ui-beacon','ui-chevron'],
 'new_blocks': ['block-refinery','block-assembler','block-cargo'],
 'shared_navigation': ['ui-inventory','ui-construction','ui-ship','ui-gps'],
 'rotations_clockwise_degrees': {'chevron_right':-90,'chevron_left':90,'arrow_left':180},
 'native_ui_primitives': ['text','quantity','button backgrounds','progress bars','carousel dots','map grid','range rings','guidance line'],
 'scope': 'visual_assets_only'
}
# In screen coordinates a downward chevron rotated -90 degrees points right.
write_json('hud-usage.json', usage)
recipes = [
 {'id':'refine-iron','label':'Raffiner le fer','icon':'ingot-iron','input_icons':['ore-iron'],'output_icon':'ingot-iron'},
 {'id':'assemble-plates','label':'Plaques métalliques','icon':'component-plates','input_icons':['ingot-iron'],'output_icon':'component-plates'},
 {'id':'assemble-tubes','label':'Tubes','icon':'component-tubes','input_icons':None,'output_icon':'component-tubes'},
 {'id':'assemble-circuits','label':'Circuits','icon':'component-circuits','input_icons':None,'output_icon':'component-circuits'},
 {'id':'build-armor','label':'Construire une armure','icon':'block-armor','input_icons':['component-plates'],'output_icon':'block-armor'}
]
write_json('recipes-visuals.json', {'status':'illustrative_ui_associations_not_game_recipes',
 'ratios_and_unknown_ingredients':None,'recipes':recipes})
write_json('validation.json', {'generated_final_assets':39, 'all_files_present':True,
 'all_square_minimum_1024':True,'rgba_sprites_alpha_range_verified':21,
 'opaque_luminance_masks_black_corners_verified':18,'unique_sha256':39,
 'all_final_assets_visually_inspected':True,'source_pngs_edited':False,
 'rejected_alpha_glyphs_excluded':True,'unity_shader_check':'see unity-validation.json when present',
 'xr_headset_validation':False})

template = '''<!doctype html><html lang="fr"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>VoidForge Odyssey — graphiques du cyberdeck</title><style>
:root{color-scheme:dark;font-family:Inter,Segoe UI,system-ui,sans-serif;color:#f0ece3;background:#080e14;--ink:#f0ece3;--preview:#10232b;--size:128px}*{box-sizing:border-box}body{margin:0;padding:clamp(18px,4vw,64px);max-width:1720px;margin-inline:auto}h1{font-size:clamp(28px,4vw,50px);margin:12px 0 16px;letter-spacing:-.035em}h2{font-size:22px}p{line-height:1.6;color:#aac2c9;max-width:92ch}.eyebrow{color:#6ddbe5;font-size:12px;letter-spacing:.2em}header{border-bottom:1px solid #28444d;padding-bottom:28px}.stats{display:flex;gap:24px;flex-wrap:wrap;margin-top:24px;color:#6ddbe5}.stats b{font-size:25px;color:#f0ece3;margin-right:6px}.hero{display:grid;grid-template-columns:repeat(3,1fr);gap:16px;margin:32px 0}.hero article{border:1px solid #37626b;background:radial-gradient(ellipse at center,#163942,#0b171e 75%);padding:18px 24px;border-radius:8px}.hero img{display:block;width:100%;height:210px;object-fit:contain}.hero small{color:#8da9b2}.hero h2{margin:10px 0}.controls,nav{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin:20px 0}button,input,select,a.download,a.action{font:inherit;border:1px solid #41646f;background:#10232b;color:#f0ece3;border-radius:5px;padding:10px 14px}button,a.download,a.action{cursor:pointer;text-decoration:none}button[aria-pressed=true]{background:#ffb43a;border-color:#ffb43a;color:#10151b;font-weight:700}input{flex:1;min-width:160px}label{display:flex;align-items:center;gap:8px;color:#b7ccd0}a{color:#6ddbe5}button:focus-visible,a:focus-visible,input:focus-visible,select:focus-visible{outline:3px solid #6ddbe5;outline-offset:4px}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(210px,1fr));gap:14px}.card{border:1px solid #294a53;border-radius:6px;overflow:hidden;background:#0c171e}.preview{height:194px;background:var(--preview);display:flex;align-items:center;justify-content:center}.preview img,.preview svg{width:var(--size);height:var(--size);max-width:100%;max-height:100%;object-fit:contain}.info{padding:15px}.info h3{margin:0 0 8px;font-size:17px}.info code{display:block;color:#8ba8b2;font-size:11px;overflow-wrap:anywhere}.info .format{font-size:12px;color:#7dbac8;display:block;margin:9px 0 14px}.download{display:inline-block;font-size:12px!important;padding:7px 10px!important}.recipes{display:grid;grid-template-columns:repeat(auto-fit,minmax(250px,1fr));gap:14px;margin-top:20px}.recipe{background:#102029;border:1px solid #30535e;padding:20px;border-radius:6px}.recipe img{width:74px;height:74px;object-fit:contain}.recipe .flow{display:flex;align-items:center;justify-content:center;gap:12px;font-size:28px;color:#ffb43a}.recipe small{display:block;color:#8faab3;line-height:1.5}.checker .preview{background-color:#e8e8e4;background-image:linear-gradient(45deg,#ccd1cf 25%,transparent 25%),linear-gradient(-45deg,#ccd1cf 25%,transparent 25%),linear-gradient(45deg,transparent 75%,#ccd1cf 75%),linear-gradient(-45deg,transparent 75%,#ccd1cf 75%);background-size:20px 20px;background-position:0 0,0 10px,10px -10px,-10px 0}.footer{margin-top:35px;border-top:1px solid #294a53;padding-top:20px;display:flex;gap:14px;flex-wrap:wrap}.hidden{display:none!important}#count{color:#84a5af;font-size:14px}#recipe-section{margin-top:40px}@media(max-width:700px){.hero{grid-template-columns:1fr}.hero img{height:180px}.grid{grid-template-columns:repeat(2,minmax(0,1fr))}.preview{height:150px}.info{padding:10px}}
</style><header><div class="eyebrow">VOIDFORGE ODYSSEY / CYBERDECK / GRAPHICS V1</div><h1>La bibliothèque graphique du HUD.</h1><p>Blocs industriels, ressources, recettes et commandes. Les objets sont détourés ; les pictogrammes se colorent à l'affichage à partir de leur masque. Chaque graphique est un élément séparé créé avec ImageGen.</p><div class="stats"><span><b>39</b>graphiques</span><span><b>21</b>objets RGBA</span><span><b>18</b>masques de commande</span></div></header>
<section class="hero" id="hero" aria-label="Blocs industriels"><article><img src="blocs/block-refinery.png" alt="Bloc vertical de raffinerie"><h2>Raffinerie</h2><small>Cuves contenues dans un carénage vertical.</small></article><article><img src="blocs/block-assembler.png" alt="Bloc caréné d'assemblage"><h2>Assembleur</h2><small>Bras et plateau de fabrication dans l'enveloppe.</small></article><article><img src="blocs/block-cargo.png" alt="Bloc cubique de soute"><h2>Soute</h2><small>Stockage cubique à grande trappe frontale.</small></article></section>
<nav id="filters" aria-label="Familles graphiques"></nav><div class="controls"><input id="search" type="search" aria-label="Chercher un graphique" placeholder="Rechercher un minerai, bloc, outil…"><label>Fond <select id="background"><option value="dark">Sombre</option><option value="light">Clair</option><option value="checker">Damier</option></select></label><label>Taille <select id="size"><option>32</option><option>64</option><option selected>128</option><option>192</option></select></label><label>Pictos <select id="tint"><option value="#f0ece3">Ivoire</option><option value="#6ddbe5">Cyan</option><option value="#ffb43a">Ambre</option><option value="#101820">Anthracite</option></select></label></div><p id="count" aria-live="polite"></p><main id="grid" class="grid"></main>
<section id="recipe-section"><h2>Graphiques des recettes</h2><p>Les mêmes éléments servent aux entrées, sorties et files de fabrication. Les ingrédients non définis et les ratios de jeu restent à préciser.</p><div class="recipes" id="recipes"></div></section><footer class="footer"><a class="action" href="VoidForge-Cyberdeck-Graphics-v1.zip" download>Télécharger le pack</a><a class="action" href="README.md">Notes et références MyrinoxPrime</a><a class="action" href="generation-prompts.json">Prompts ImageGen</a><a class="action" href="manifest.json">Catalogue des fichiers</a></footer><script>
const assets=__ASSETS__,recipes=__RECIPES__;
const families=[['all','Tous'],['minerais','Minerais'],['products','Produits / recettes'],['outils','Outils'],['blocs','Blocs'],['carte','Carte'],['interface-masks','Interface']];
const params=new URLSearchParams(location.search);let family=params.get('family')||'all';
const grid=document.querySelector('#grid'),nav=document.querySelector('#filters');
families.forEach(([id,label])=>{const b=document.createElement('button');b.textContent=label;b.dataset.family=id;b.addEventListener('click',()=>{family=id;render()});nav.append(b)});
function icon(a,key){if(a.usage_format!=='luminance-mask')return `<img src="${a.file}" alt="${a.label}" loading="lazy">`;return `<svg viewBox="0 0 100 100" role="img" aria-label="${a.label}"><defs><mask id="m-${key}" maskUnits="userSpaceOnUse" x="0" y="0" width="100" height="100" style="mask-type:luminance"><image href="${a.maskData}" width="100" height="100"/></mask></defs><rect width="100" height="100" fill="var(--ink)" mask="url(#m-${key})"/></svg>`}
function render(){grid.replaceChildren();const term=document.querySelector('#search').value.toLocaleLowerCase('fr');const filtered=assets.filter(a=>(family==='all'||a.family===family||(family==='products'&&['produits','recettes'].includes(a.family)))&&(a.label+' '+a.id).toLocaleLowerCase('fr').includes(term));filtered.forEach(a=>{const card=document.createElement('article');card.className='card';card.dataset.id=a.id;card.innerHTML=`<div class="preview">${icon(a,a.id)}</div><div class="info"><h3>${a.label}</h3><code>${a.file}</code><span class="format">${a.width} × ${a.height} · ${a.usage_format==='luminance-mask'?'Masque teintable':'PNG transparent'}</span><a class="download" href="${a.file}" download>Télécharger</a></div>`;grid.append(card)});document.querySelector('#count').textContent=filtered.length+' / 39 éléments';nav.querySelectorAll('button').forEach(b=>b.setAttribute('aria-pressed',b.dataset.family===family?'true':'false'));document.querySelector('#hero').classList.toggle('hidden',family!=='all'&&family!=='blocs');document.querySelector('#recipe-section').classList.toggle('hidden',family!=='all'&&family!=='products')}
document.querySelector('#search').addEventListener('input',render);document.querySelector('#size').addEventListener('change',e=>document.documentElement.style.setProperty('--size',e.target.value+'px'));document.querySelector('#background').addEventListener('change',e=>{document.body.classList.toggle('checker',e.target.value==='checker');document.documentElement.style.setProperty('--preview',e.target.value==='dark'?'#10232b':'#e8e8e4');if(e.target.value!=='dark'){document.querySelector('#tint').value='#101820';document.documentElement.style.setProperty('--ink','#101820')}});document.querySelector('#tint').addEventListener('change',e=>document.documentElement.style.setProperty('--ink',e.target.value));
recipes.forEach(r=>{const out=assets.find(a=>a.id===r.output_icon),input=r.input_icons&&assets.find(a=>a.id===r.input_icons[0]);const c=document.createElement('article');c.className='recipe';c.innerHTML=`<h3>${r.label}</h3><div class="flow">${input?`<img src="${input.file}" alt="${input.label}"> →`:''}<img src="${out.file}" alt="${out.label}"></div><small>${input?'Association visuelle montrée dans les concepts.':'Ingrédients à définir ; vignette de sortie disponible.'}</small>`;document.querySelector('#recipes').append(c)});
if(params.has('size')){document.querySelector('#size').value=params.get('size');document.querySelector('#size').dispatchEvent(new Event('change'))}if(params.has('bg')){document.querySelector('#background').value=params.get('bg');document.querySelector('#background').dispatchEvent(new Event('change'))}if(params.has('tint')){document.querySelector('#tint').value=params.get('tint');document.querySelector('#tint').dispatchEvent(new Event('change'))}render();window.assetCatalogueReady=true;
</script></html>'''
template = template.replace('__ASSETS__',json.dumps(ui_data,ensure_ascii=False)).replace('__RECIPES__',json.dumps(recipes,ensure_ascii=False))
(ROOT / 'index.html').write_text(template, encoding='utf-8')
print(json.dumps({'assets':len(assets),'rgba':21,'masks':18,'total_png_bytes':sum(a['bytes'] for a in assets),'gallery_bytes':(ROOT/'index.html').stat().st_size}))
