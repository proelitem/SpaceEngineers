"""Original catalogue definitions; no game files or third-party art imported."""
from pathlib import Path
import json

ROOT = Path(__file__).resolve().parent
STYLE = ('Use case: stylized-concept. Asset: single original 2D inventory sprite for VoidForge Odyssey cyberdeck. '
 'Premium coherent semi-realistic industrial sci-fi 3D-rendered icon, crisp silhouette readable at 64px, '
 'view from slightly above three-quarter, soft neutral upper-left light, realistic metal/rock texture. '
 'Center one object or compact cohesive cluster filling about 72% of square canvas with generous transparent margins. '
 'GENUINELY TRANSPARENT alpha background, no floor, no cast shadow outside object, no panel, no frame, '
 'no text, no letters, no symbols, no logo, no watermark, no additional items. Original shapes; do not reproduce any other game art. ')

# IDs are VoidForge data IDs, not imported game definitions.
NEW = [
 ('ore-cobalt','Minerai de cobalt','minerais','Cobalt-bearing ore sample, one angular dark charcoal rock with clearly visible cobalt-blue crystalline mineral veins and small steel-gray flecks, raw unprocessed natural rock, no glow.'),
 ('ore-magnesium','Minerai de magnésium','minerais','One angular pale chalky beige-gray ore rock with silver-white crystalline inclusions, porous fractured surface, coarse magnesium-bearing mineral, raw natural rock, no glow.'),
 ('ore-silver','Minerai d’argent','minerais','One angular dark slate ore rock with branching bright silver native metallic veins and sparkling white facets, raw silver-bearing mineral. Not an ingot, not gold.'),
 ('ore-gold','Minerai d’or','minerais','One angular dark basalt rock with irregular rich warm yellow gold metallic veins, gold embedded in raw black ore, no glow.'),
 ('ore-platinum','Minerai de platine','minerais','One compact angular deep graphite ore rock with broad dense pale platinum-white metallic nodules and a subtle cool blue metallic sheen, raw unprocessed ore, distinct from gold and silver veins.'),
 ('ore-uranium','Minerai d’uranium','minerais','One angular near-black pitchblende ore rock with restrained olive-yellow mineral crust in a few deep fissures, heavy raw rock, no cartoon radioactive green glow, no symbols.'),
 ('ore-stone','Pierre brute','minerais','One angular unprocessed rough gray basalt stone with neutral medium gray mineral grain, fractured surfaces, no colored metal veins, distinguishable simple low-value raw rock.'),
 ('ore-ice','Glace','minerais','A single compact cluster of three adjoining broken pale cyan water-ice chunks, frosted edges, transparent ice cores, crisp chunky facets, visible silhouette, no water puddle, no snow background.'),
 ('ingot-nickel','Lingot de nickel','produits','Two short stacked cast nickel ingots, rounded trapezoid bars, brushed warm silver with subtle pale champagne cast, small industrial casting recess but no markings, clean solid refined metal.'),
 ('ingot-cobalt','Lingot de cobalt','produits','Two short stacked cast cobalt ingots, chunky trapezoid bars, dark steel blue metal with a subtle satin blue sheen, small casting recess but no markings, refined solid metal, no rocks.'),
 ('wafer-silicon','Plaquettes de silicium','produits','Three offset thin circular semiconductor silicon wafers, charcoal blue mirror surfaces with a restrained violet iridescent reflection and subtle etched grid, one flat notch, no text, clearly flat refined wafer discs, not rock.'),
 ('powder-magnesium','Poudre de magnésium','produits','A small shallow open charcoal industrial sample cup filled with fine pale silver magnesium powder, a tiny coherent spill of sparkling powder against cup lip, no floating loose dust, material focus, no labels.'),
 ('ingot-silver','Lingot d’argent','produits','Two stacked chunky cast silver ingots, brilliant cool white silver clean reflective metal, trapezoid shape with unmarked casting recess, no gold tint, refined solid metal.'),
 ('ingot-gold','Lingot d’or','produits','Two stacked chunky cast gold ingots, rich warm yellow gold metal, trapezoid shape with unmarked casting recess, premium believable brushed gold metal, no embossing.'),
 ('ingot-platinum','Lingot de platine','produits','Two stacked dense refined platinum ingots, crisp beveled rectangular bars slightly longer and flatter than conventional casting bars, pale cool platinum satin finish, no markings, no gold tint.'),
 ('ingot-uranium','Pastilles d’uranium','produits','A coherent compact cluster of three short dense refined uranium fuel cylinders in a small graphite holder with one restrained amber safety latch, dark metallic charcoal satin surface, original industrial fuel product, no labels or symbols, no radioactive glow.'),
 ('gravel','Gravier','produits','A compact mound of crushed medium gray stone aggregate, many small angular uniform gravel pebbles, clearly processed crushed rock, strong compact silhouette, no container, no large raw ore boulder.'),
 ('scrap-metal','Ferraille','recyclage','A compact pile of three bent torn steel offcuts, one curled metal strip and a dented bracket, weathered gray metal with a little ochre rust, salvaged scrap, no intact manufactured component or background.'),
 ('component-construction','Éléments de construction','recettes','Original industrial construction fastener assembly: two short machined steel mounting brackets joined by three chunky bolts and one clamp, ivory protective collar with tiny amber detail, simple readable solid silhouette, no labels.'),
 ('component-interior-plate','Plaque intérieure','recettes','Two offset thin ivory enamel interior wall panels, shallow beveled corners, one subtle inset rib and visible thin steel edge, small amber corner tab, clearly lighter thinner panels than raw heavy armor steel plate.'),
 ('component-large-tube','Grand tube','recettes','One large hollow steel conduit section with thick wide circular bore facing diagonally toward viewer, machined flange collar and a thin ivory band with one amber latch, large diameter, not a group of thin pipes.'),
 ('component-metal-grid','Grille métallique','recettes','One square industrial heavy expanded steel reinforcement grid panel, thick charcoal steel lattice diamond openings within a stout silver rim, tiny ivory corner braces with amber screws, holes genuinely transparent.'),
 ('component-motor','Moteur électrique','recettes','One compact cylindrical industrial electric motor, charcoal ribbed barrel, silver shaft projecting diagonally toward viewer, ivory end housing, tiny amber connector cover, visible copper winding detail at back, original compact assembly.'),
 ('component-display','Écran','recettes','One compact rectangular blank display component, wide horizontal black glass screen in slim ivory beveled industrial frame, graphite back casing and small amber corner latch, screen completely empty, no HUD, no letters.'),
 ('component-girder','Poutrelle','recettes','One short steel I-beam girder segment diagonally oriented, clearly readable capital-I cross section at one end, dark brushed industrial steel, sharp intact flanges, one tiny amber end safety stripe, no text.'),
 ('component-glass','Verre renforcé','recettes','Three offset thick square sheets of reinforced transparent glass, pale cyan beveled edges, subtle transparent gray blue tint, no frame, no reflective background, clear overlapping glass layers and compact silhouette.'),
 ('component-reactor','Composant de réacteur','recettes','One original compact reactor core component: short armored graphite cylinder with ivory segmented outer collar and three thick silver connector studs, small recessed amber ceramic insulators, clean modular industrial part, no radioactive symbols or glow.'),
 ('component-thruster','Composant de propulsion','recettes','One original engine ion acceleration component, stout silver concentric metal rings surrounding a compact charcoal cylindrical core with visible copper coil, ivory mounting lugs and a tiny amber connector, no flame, a component rather than complete thruster block.'),
 ('component-gravity','Composant gravitationnel','recettes','One original heavy gravitic field component, nested graphite toroidal rings around a dense silver spherical center, supported in an ivory triangular open mounting bracket, tiny amber connector, no floating magical effects, readable compact ring assembly.'),
 ('component-medical','Composant médical','recettes','One original compact life-support medical cartridge, ivory rounded rectangular housing with three small clear cyan fluid chambers, graphite couplings, one amber retaining clip, functional sci-fi industrial component, no cross symbol, no text.'),
 ('component-radio','Composant radio','recettes','One original radio communications component: small charcoal rectangular electronics box with silver coaxial connectors, short copper wound coil and one ivory finned plate, amber connector clip, no antenna mast, no labels.'),
 ('component-detector','Composant de détection','recettes','One compact original sensor component, ivory hexagonal housing around a single dark cyan optical lens, graphite rear connector and small amber edge clip, distinct readable large circular lens, not a camera scene or full block.'),
 ('component-explosives','Charge explosive','recettes','One sealed original industrial mining explosive cartridge, squat dark graphite rectangular canister with ivory ribbed sides and amber locking cap, two small recessed connectors, safe inert item icon, no wires extending beyond silhouette, no symbols or text.'),
 ('component-solar-cell','Cellule solaire','recettes','One small rectangular photovoltaic cell component, deep navy blue silicon divided into a clean 3 by 4 grid with fine silver contacts, thin silver edge and ivory back plate, no support legs, one component not a complete solar array.'),
 ('component-power-cell','Cellule énergétique','recettes','One original compact high-capacity power cell component: paired squat graphite cylinders in an ivory open cradle, two copper terminal contacts, narrow amber separator strip, no glowing energy, distinct from a complete battery block.'),
 ('component-superconductor','Supraconducteur','recettes','One original superconducting coil component: tight flat spiral of silver ribbon conductor wound around a charcoal spool, gold terminal contacts and tiny ivory ceramic spacers, small amber latch, no glow, clear metallic winding silhouette.'),
]

def main():
    entries=[]
    for id,label,family,subject in NEW:
        (ROOT/family).mkdir(exist_ok=True)
        entries.append(dict(id=id,label=label,family=family,file=f'{family}/{id}.png',prompt=STYLE+'Subject: '+subject,tool='image_gen.imagegen',transparent_background=True))
    (ROOT/'generation-prompts.json').write_text(json.dumps(dict(mode='built-in',date='2026-11-03',entries=entries),ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print(f'{len(entries)} original missing images specified')

if __name__=='__main__':main()
