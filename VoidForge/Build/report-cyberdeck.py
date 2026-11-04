"""Build the native/reference review gallery. Pixel error is not a fidelity percentage."""
import json
from pathlib import Path
from PIL import Image, ImageChops, ImageStat

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT/'docs/validation/cyberdeck-2026-11-03'
VIEWS = [
    ('inventory', 'Inventaire', 'docs/concepts/2026-11-03-cyberdeck-ui-v1/01-inventaire.png'),
    ('construction', 'Construction', 'docs/concepts/2026-11-03-cyberdeck-ui-v1/02-construction.png'),
    ('refinery', 'Raffinerie', 'Art/UI/Cyberdeck/2026-11-03-v1/concept-raffinerie-bloc.png'),
    ('assembly', 'Assemblage', 'docs/concepts/2026-11-03-cyberdeck-ui-v1/03b-vaisseau-assemblage.png'),
    ('map', 'GPS / Carte', 'docs/concepts/2026-11-03-cyberdeck-ui-v1/04-gps-carte.png'),
]


def main():
    import os
    rows, records = [], []
    for id, label, relative in VIEWS:
        native, reference = OUT/(id+'.png'), ROOT/relative
        with Image.open(native) as a, Image.open(reference) as b:
            assert a.size == b.size == (1914, 822)
            mae = sum(ImageStat.Stat(ImageChops.difference(a.convert('RGB'), b.convert('RGB'))).mean)/3
        records.append(dict(view=id, resolution=[1914, 822], mean_absolute_rgb_error_0_255=round(mae, 3)))
        rows.append(dict(id=id, label=label, native=native.name, reference=os.path.relpath(reference, OUT).replace('\\', '/')))
    report = dict(target_fidelity_percent=98, certified=False,
                  explanation='Native live UI, independent ImageGen assets, free typography and actual game states differ from the raster references. RGB error is a diagnostic, not a certified perceptual fidelity score.',
                  views=records)
    (OUT/'visual-comparison.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    data = json.dumps(rows, ensure_ascii=False)
    html = '''<!doctype html><html lang="fr"><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<title>VoidForge — HUD natif Unity</title><style>
*{box-sizing:border-box}body{margin:0;background:#041116;color:#f5f3ea;font:16px system-ui,sans-serif}header{padding:24px 3vw 12px}h1{font-size:24px;margin:0 0 8px}p{max-width:1100px;line-height:1.5;color:#b5c7cb;margin:8px 0}nav,.tools{display:flex;flex-wrap:wrap;gap:10px;margin:18px 0}button,a{font:inherit;color:inherit}button{border:1px solid #58d9e8;background:#06313a;padding:12px 20px;cursor:pointer}button[aria-pressed=true]{background:#ffb329;color:#041116;border-color:#ffb329}main{padding:0 3vw 32px}.frame{position:relative;width:100%;aspect-ratio:1914/822;border:1px solid #3d9bac;background:#041116;overflow:hidden}.frame img{position:absolute;width:100%;height:100%;object-fit:contain}#reference{opacity:0}input{accent-color:#ffb329;width:250px}label{display:flex;align-items:center;gap:12px}.state{color:#58d9e8;font-size:13px}.files{display:flex;gap:24px;flex-wrap:wrap;margin-top:20px}a{color:#58d9e8}a:focus-visible,button:focus-visible{outline:3px solid white;outline-offset:3px}</style>
<header><h1>VOIDFORGE ODYSSEY / HUD CYBERDECK</h1>
<p>Captures du Canvas uGUI rendu par Unity 6000.4. Les boutons, textes, jauges, listes et repères sont des éléments natifs. Les valeurs de la scène de recette sont illustratives.</p>
<nav id="tabs" aria-label="Interfaces"></nav>
<div class="tools"><button id="native" aria-pressed="true">Rendu Unity</button><button id="concept" aria-pressed="false">Concept</button><label>Superposition du concept <input id="blend" type="range" min="0" max="100" value="0"><output id="percent">0 %</output></label></div>
<p class="state">Cible demandée : 98 % de fidélité. Ce pourcentage n'est pas certifié ; la superposition permet d'examiner directement les écarts.</p></header>
<main><div class="frame"><img id="render" alt="HUD natif Unity"><img id="reference" alt="Concept de référence"></div>
<div class="files"><a href="device-preview.png">Sur le cyberdeck 3D</a><a href="unity-validation.json">Contrôles Unity</a><a href="game-validation.json">Attachement dans le jeu</a><a href="visual-comparison.json">Mesures et limites</a><a href="ship-disconnected.png">Cockpit déconnecté</a><a href="inventory-disconnected.png">Soute déconnectée</a><a href="assembly-insufficient.png">Composants insuffisants</a><a href="../../CYBERDECK_HUD.md">Intégration et commandes</a></div></main>
<script>const views=DATA;let current=0;const tabs=document.querySelector('#tabs'),render=document.querySelector('#render'),ref=document.querySelector('#reference'),range=document.querySelector('#blend');function select(i){current=i;render.src=views[i].native;ref.src=views[i].reference;tabs.querySelectorAll('button').forEach((b,n)=>b.setAttribute('aria-pressed',n===i));render.alt='Rendu Unity : '+views[i].label;ref.alt='Concept : '+views[i].label}views.forEach((v,i)=>{let b=document.createElement('button');b.textContent=v.label;b.onclick=()=>select(i);tabs.append(b)});function blend(value){range.value=value;ref.style.opacity=value/100;document.querySelector('#percent').value=value+' %';document.querySelector('#native').setAttribute('aria-pressed',value==0);document.querySelector('#concept').setAttribute('aria-pressed',value==100)}range.oninput=()=>blend(range.value);document.querySelector('#native').onclick=()=>blend(0);document.querySelector('#concept').onclick=()=>blend(100);select(0);</script></html>'''.replace('DATA', data)
    (OUT/'index.html').write_text(html, encoding='utf-8')
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    main()
