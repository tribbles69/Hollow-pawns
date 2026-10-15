"""Cross-checks the mod's XML against the game's own defs (and optionally CE's).
usage: python Source/tools/check_defs.py <RimWorld Data dir> <mod dir> [extra def dirs, e.g. CE Defs and Patches]
Reports unknown ParentNames, def-like references that resolve to nothing, field tags vanilla never uses,
and benches that recipes use but no WorkGiverDef lists. Enum values and the mod's own new fields show up
as noise: read the list, don't trust it blindly."""
import sys, os, re
import xml.etree.ElementTree as ET

data, mod = sys.argv[1], sys.argv[2]
extra = sys.argv[3:]

def xml_files(root):
    for d, _, fs in os.walk(root):
        for f in fs:
            if f.lower().endswith('.xml'):
                yield os.path.join(d, f)

def parse(p):
    try:
        return ET.parse(p).getroot()
    except ET.ParseError as e:
        print('PARSE ERROR', p, e)
        return None

game_defnames, game_abstract, game_tags = set(), set(), set()
game_classes = set()
def load_game(root):
    for p in xml_files(root):
        r = parse(p)
        if r is None:
            continue
        for el in r.iter():
            game_tags.add(el.tag)
            if el.get('Name'):
                game_abstract.add(el.get('Name'))
            if el.tag == 'defName' and el.text:
                game_defnames.add(el.text.strip())
            if el.get('Class'):
                game_classes.add(el.get('Class'))
            if el.text and re.fullmatch(r'[A-Za-z_][\w.]*', el.text.strip() or '') and el.tag.lower().endswith(('class', 'worker')):
                game_classes.add(el.text.strip())

for dlc in os.listdir(data):
    dd = os.path.join(data, dlc, 'Defs')
    if os.path.isdir(dd):
        load_game(dd)
for e in extra:
    load_game(e)

mod_defnames, mod_abstract, mod_tags = set(), set(), set()
mod_files = [p for p in xml_files(mod) if '.git' not in p and os.sep + 'About' + os.sep not in p and not p.endswith('LoadFolders.xml')]
roots = {}
for p in mod_files:
    r = parse(p)
    if r is None:
        continue
    roots[p] = r
    for el in r.iter():
        if el.get('Name'):
            mod_abstract.add(el.get('Name'))
        if el.tag == 'defName' and el.text:
            mod_defnames.add(el.text.strip())

known = game_defnames | mod_defnames
skip_vals = {'true', 'false', 'True', 'False', 'None', 'Always', 'Never'}
for p, r in roots.items():
    rel = os.path.relpath(p, mod)
    issues = []
    for el in r.iter():
        pn = el.get('ParentName')
        if pn and pn not in game_abstract and pn not in mod_abstract:
            issues.append(f'ParentName {pn!r} not found')
        if el.tag not in game_tags and el.tag not in ('Defs', 'Patch', 'Operation', 'li', 'value', 'xpath', 'match', 'nomatch', 'mods', 'operations', 'order') and not el.tag.startswith('HP_'):
            issues.append(f'tag <{el.tag}> never used in vanilla')
        t = (el.text or '').strip()
        if t and re.fullmatch(r'[A-Z][A-Za-z0-9_]+', t) and t not in skip_vals and el.tag not in ('defName', 'label', 'description'):
            if t not in known and t not in game_classes and not t.startswith('HP_'):
                issues.append(f'<{el.tag}>{t}</...> not a known defName/class')
    if issues:
        print('==', rel)
        for i in sorted(set(issues)):
            print('   ', i)

# ---- benches nobody can work: recipe users that no WorkGiverDef lists ----
def collect(root_iter):
    givers, users, pawns = set(), set(), set()
    for r in root_iter:
        for wg in r.iter('WorkGiverDef'):
            for li in wg.findall('fixedBillGiverDefs/li'):
                givers.add((li.text or '').strip())
            if (wg.findtext('billGiversAllHumanlikes') or '').strip().lower() == 'true':
                givers.add('*humanlike*')
        for el in r.iter():
            for li in el.findall('recipeUsers/li') + el.findall('recipeMaker/recipeUsers/li'):
                users.add((li.text or '').strip())
    return givers, users
game_roots = []
for dlc in os.listdir(data):
    dd = os.path.join(data, dlc, 'Defs')
    if os.path.isdir(dd):
        game_roots += [x for x in (parse(p) for p in xml_files(dd)) if x is not None]
g1, _ = collect(game_roots)
g2, u2 = collect(roots.values())
humans = {'Human'}
for u in sorted(u2 - g1 - g2 - humans):
    print('== recipe user with no WorkGiverDef:', u)
