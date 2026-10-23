"""Full-kit apparel sets (ROADMAP 4.22).

    python gen_apparel_sets.py <RimWorld Data dir> <Combat Extended dir>

Only the conscript kit is left: the line forge's slot-filling set for thralls (4.24). The six
soul-essence sets (gravecloth, ossuary, mourner, soulsteel, wraithweave, reliquary) were cut on
23 Oct: copies of vanilla gear a shade weaker than the original weren't worth the essence.
Soulweave (a fabric, Defs/ThingDefs_Items/HP_Items.xml) replaces them: any vanilla clothing
made from it is the soul gear now. The copy/belt/eyes helpers stay for any future set.

A set can mix pieces copied from a real vanilla piece (its layers, body coverage, art, recipe
and stats, armour scaled down a little), pieces written out here, and existing defs. Under
Combat Extended a set can add a webbing, a backpack and a shield, and every CE patch on a
vanilla original is mirrored onto its copy.

The script refuses to write anything unless, both without and with CE:
  - no two pieces in a set clash, by vanilla's own rule (ApparelUtility.CanWearTogether: a
    shared layer plus overlapping body-part groups on the Human body, read from the game), and
  - every layer is filled (vanilla: OnSkin, Middle, Shell, Belt, Overhead, EyeCover; CE adds
    Webbing, Backpack, Shield, except for a vanillaOnly set) and the skin layer covers both
    torso and legs.

Writes:
  Defs/HP_ApparelSets.xml
  Mods/CombatExtended/Defs/HP_CE_ApparelSets.xml
  Mods/CombatExtended/Patches/HP_CE_ApparelSets.xml
Never edit those by hand: change the tables below and run this again.
"""
import copy
import glob
import os
import re
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

VANILLA_LAYERS = ["OnSkin", "Middle", "Shell", "Belt", "Overhead", "EyeCover"]
CE_LAYERS = ["Webbing", "Backpack", "Shield"]

# Shared custom pieces: a belt and an eye piece per set. Stats per tier stay small.
def belt(set_id, label, desc, tech, color, parent="ApparelMakeableBase", stuff=("Leathery", "Fabric"), essence=2, offsets=None):
    return {"kind": "custom", "id": "Belt", "label": label, "desc": desc, "parent": parent, "tech": tech,
            "layers": ["Belt"], "groups": ["Waist"], "tex": "Things/Item/Equipment/HP_KitBelt", "worn": None,
            "stuff": list(stuff), "costStuff": 25, "cost": {"HP_SoulEssence": essence},
            "stats": {"MaxHitPoints": 80, "WorkToMake": 2500, "Mass": 0.5, "EquipDelay": 1},
            "offsets": offsets or {"WorkSpeedGlobal": 0.03}, "color": color}


def eyes(label, desc, tech, color, cost=None, offsets=None, parent="ApparelMakeableBase"):
    return {"kind": "custom", "id": "Eyes", "label": label, "desc": desc, "parent": parent, "tech": tech,
            "layers": ["EyeCover"], "groups": ["Eyes"], "tex": "Things/Item/Equipment/HP_KitGoggles", "worn": None,
            "stuff": None, "costStuff": 0, "cost": cost or {"Leather_Plain": 10, "HP_SoulEssence": 1},
            "stats": {"MaxHitPoints": 60, "WorkToMake": 2000, "Mass": 0.2, "ArmorRating_Sharp": 0.04, "ArmorRating_Blunt": 0.02, "EquipDelay": 1},
            "offsets": offsets or {"AimingDelayFactor": -0.05}, "color": color}


def copy_of(piece_id, ref, label, desc, armour=0.9, essence=2, color=None, research="keep", ce=False):
    return {"kind": "copy", "id": piece_id, "ref": ref, "label": label, "desc": desc, "armour": armour,
            "essence": essence, "color": color, "research": research, "ce": ce}


def external(defname):
    return {"kind": "external", "def": defname}


SETS = [
    {"id": "Conscript", "label": "conscript kit", "tier": "line forge", "vanillaOnly": True, "pieces": [
        # Made only by the line forge (4.24): no bench recipe, no stuff, near-worthless.
        {"kind": "custom", "id": "Fatigues", "label": "conscript fatigues",
         "desc": "Coarse grey fatigues pressed out by the line forge. One size fits nobody. Part of the conscript kit.",
         "parent": "ApparelBase", "tech": "Industrial", "layers": ["OnSkin"], "groups": ["Torso", "Shoulders", "Arms", "Legs"],
         "tex": "Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic", "worn": "Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic",
         "stuff": None, "costStuff": 0, "cost": {"Steel": 10},
         "stats": {"MaxHitPoints": 80, "Mass": 0.6, "MarketValue": 3, "ArmorRating_Sharp": 0.03, "ArmorRating_Blunt": 0.02, "Insulation_Cold": 6, "Insulation_Heat": 2, "EquipDelay": 1.5},
         "offsets": {}, "color": (0.45, 0.46, 0.44)},
        external("HP_ConscriptVest"),
        {"kind": "custom", "id": "Greatcoat", "label": "conscript greatcoat",
         "desc": "A long, stiff coat with steel thread through it. It turns a little cold and very little else. Part of the conscript kit.",
         "parent": "ApparelBase", "tech": "Industrial", "layers": ["Shell"], "groups": ["Torso", "Neck", "Shoulders", "Arms", "Legs"],
         "tex": "Things/Pawn/Humanlike/Apparel/Duster/Duster", "worn": "Things/Pawn/Humanlike/Apparel/Duster/Duster",
         "stuff": None, "costStuff": 0, "cost": {"Steel": 20},
         "stats": {"MaxHitPoints": 120, "Mass": 1.8, "MarketValue": 4, "ArmorRating_Sharp": 0.12, "ArmorRating_Blunt": 0.06,
                   "Insulation_Cold": 18, "EquipDelay": 2.5},
         "offsets": {"MoveSpeed": -0.03}, "color": (0.35, 0.36, 0.34)},
        external("HP_ConscriptHelmet"),
        {"kind": "custom", "id": "Belt", "label": "conscript belt",
         "desc": "A webbing belt with two empty pouches. Part of the conscript kit.",
         "parent": "ApparelBase", "tech": "Industrial", "layers": ["Belt"], "groups": ["Waist"],
         "tex": "Things/Item/Equipment/HP_KitBelt", "worn": None,
         "stuff": None, "costStuff": 0, "cost": {"Steel": 4},
         "stats": {"MaxHitPoints": 60, "Mass": 0.3, "MarketValue": 1, "ArmorRating_Sharp": 0.02, "ArmorRating_Blunt": 0.02, "EquipDelay": 1},
         "offsets": {}, "color": (0.40, 0.40, 0.36)},
        {"kind": "custom", "id": "Eyes", "label": "conscript goggles",
         "desc": "Scratched goggles on an elastic strap. Part of the conscript kit.",
         "parent": "ApparelBase", "tech": "Industrial", "layers": ["EyeCover"], "groups": ["Eyes"],
         "tex": "Things/Item/Equipment/HP_KitGoggles", "worn": None,
         "stuff": None, "costStuff": 0, "cost": {"Steel": 3},
         "stats": {"MaxHitPoints": 40, "Mass": 0.2, "MarketValue": 1, "ArmorRating_Sharp": 0.04, "ArmorRating_Blunt": 0.02, "EquipDelay": 1},
         "offsets": {}, "color": (0.40, 0.40, 0.36)},
    ], "ce": []},
]

ARMOUR_STATS = ("ArmorRating_Sharp", "ArmorRating_Blunt", "ArmorRating_Heat", "StuffEffectMultiplierArmor")


# ---------------------------------------------------------------- loading


def parse(path):
    try:
        return ET.parse(path).getroot()
    except ET.ParseError:
        return None


def load_defs(paths):
    """ThingDefs by defName and by Name, from every xml under the given Defs dirs."""
    by_def, by_name = {}, {}
    for p in paths:
        for f in glob.glob(os.path.join(p, "**", "*.xml"), recursive=True):
            root = parse(f)
            if root is None:
                continue
            for t in root.findall("ThingDef"):
                if t.get("Name"):
                    by_name[t.get("Name")] = t
                if t.findtext("defName"):
                    by_def[t.findtext("defName")] = t
    return by_def, by_name


def merge(parent, child):
    """RimWorld-style XML inheritance, close enough for apparel: lists append, named nodes merge."""
    out = copy.deepcopy(parent)
    for c in child:
        if c.tag == "li" or c.get("Inherit") == "False":
            if c.get("Inherit") == "False":
                for old in out.findall(c.tag):
                    out.remove(old)
            out.append(copy.deepcopy(c))
            continue
        old = out.find(c.tag)
        if old is None:
            out.append(copy.deepcopy(c))
        elif len(c) == 0 or len(old) == 0:
            out.remove(old)
            out.append(copy.deepcopy(c))
        else:
            out.remove(old)
            out.append(merge(old, c))
    for k, v in child.attrib.items():
        if k not in ("Name", "ParentName", "Abstract"):
            out.set(k, v)
    return out


def resolve(elem, by_name):
    p = elem.get("ParentName")
    if not p:
        return copy.deepcopy(elem)
    if p not in by_name:
        raise SystemExit("unknown parent " + p)
    return merge(resolve(by_name[p], by_name), elem)


def human_parts(data):
    root = parse(os.path.join(data, "Core", "Defs", "Bodies", "Bodies_Humanlike.xml"))
    body = next(b for b in root.findall("BodyDef") if b.findtext("defName") == "Human")
    parts = []

    def walk(node):
        parts.append({g.text for g in node.findall("groups/li")})
        for li in node.findall("parts/li"):
            walk(li)

    walk(body.find("corePart"))
    return parts


# ---------------------------------------------------------------- CE patch mirroring

STEP = re.compile(r'([A-Za-z_]+)(?:\[(@Class|\.)="([^"]+)"\])?')
TARGET = re.compile(r'^Defs/ThingDef\[(.*?)\](/.*)?$')


def targets_of(selector):
    return set(re.findall(r'defName="([^"]+)"', selector)), set(re.findall(r'@Name="([^"]+)"', selector))


def find_path(elem, tail):
    """Nodes under elem matching a simple xpath tail like /statBases/Mass or /comps/li[@Class="X"]/y."""
    nodes = [elem]
    for step in [s for s in (tail or "").split("/") if s]:
        m = STEP.fullmatch(step)
        if not m:
            return []
        tag, kind, val = m.groups()
        nxt = []
        for n in nodes:
            for c in n.findall(tag):
                if kind == "@Class" and c.get("Class") != val:
                    continue
                if kind == "." and (c.text or "").strip() != val:
                    continue
                nxt.append(c)
        nodes = nxt
    return nodes


def apply_op(op, elem, tail):
    cls = op.get("Class")
    value = op.find("value")
    if cls == "PatchOperationConditional":
        branch = op.find("match") if find_path(elem, tail) else op.find("nomatch")
        if branch is not None:
            m = TARGET.match(branch.findtext("xpath").strip())
            apply_op(branch, elem, m.group(2))
        return
    hits = find_path(elem, tail)
    if cls == "PatchOperationAdd":
        for h in hits:
            for v in value:
                h.append(copy.deepcopy(v))
    elif cls == "PatchOperationReplace":
        for h in hits:
            parent = next(p for p in elem.iter() if h in list(p))
            i = list(parent).index(h)
            parent.remove(h)
            for k, v in enumerate(value):
                parent.insert(i + k, copy.deepcopy(v))
    elif cls == "PatchOperationRemove":
        for h in hits:
            parent = next(p for p in elem.iter() if h in list(p))
            parent.remove(h)
    elif cls == "PatchOperationAddModExtension":
        for h in hits:
            me = h.find("modExtensions")
            if me is None:
                me = ET.SubElement(h, "modExtensions")
            for v in value:
                me.append(copy.deepcopy(v))


def load_ce_ops(ce):
    ops = []
    for f in glob.glob(os.path.join(ce, "Patches", "**", "*.xml"), recursive=True):
        root = parse(f)
        if root is None:
            continue
        for op in root.findall("Operation"):
            xp = op.findtext("xpath")
            if xp is None:
                continue
            m = TARGET.match(xp.strip())
            if m:
                defs, names = targets_of(m.group(1))
                ops.append((defs, names, m.group(2), op))
    return ops


def patch_names(by_name, ops):
    """CE's patches on abstract parents (ApparelArmorPowerBase...), applied for the CE-state check."""
    out = {k: copy.deepcopy(v) for k, v in by_name.items()}
    for defs, names, tail, op in ops:
        for n in names:
            if n in out:
                apply_op(op, out[n], tail)
    return out


# ---------------------------------------------------------------- building defs


def sub(parent, tag, text=None, **attrs):
    e = ET.SubElement(parent, tag, attrs)
    if text is not None:
        e.text = str(text)
    return e


def fmt(x):
    return ("%.3f" % x).rstrip("0").rstrip(".")


def color_gen(t, color):
    cg = sub(t, "colorGenerator", Class="ColorGenerator_Options")
    li = sub(sub(cg, "options"), "li")
    sub(li, "weight", 10)
    sub(li, "only", "(%s,%s,%s)" % tuple(fmt(c) for c in color))


def def_name(s, p):
    return "HP_Kit_%s_%s" % (s["id"], p["id"])


def set_ext(t, s):
    me = t.find("modExtensions")
    if me is None:
        me = sub(t, "modExtensions")
    li = sub(me, "li", Class="HollowPawns.ApparelSetExtension")
    sub(li, "set", s["id"])
    sub(li, "setLabel", s["label"])


def build_copy(s, p, by_def, by_name):
    ref = by_def[p["ref"]]
    resolved = resolve(ref, by_name)
    t = ET.Element("ThingDef")
    if ref.get("ParentName"):
        t.set("ParentName", ref.get("ParentName"))
    if ref.get("Class"):
        t.set("Class", ref.get("Class"))
    sub(t, "defName", def_name(s, p))
    sub(t, "label", p["label"])
    sub(t, "description", p["desc"])
    for c in ref:
        if c.tag in ("defName", "label", "description", "thingSetMakerTags", "colorGenerator", "statBases", "costList", "modExtensions"):
            continue
        t.append(copy.deepcopy(c))
    # Stats: the original's, with armour scaled for the set.
    sb = sub(t, "statBases")
    for st in resolved.find("statBases") if resolved.find("statBases") is not None else []:
        v = st.text
        if st.tag in ARMOUR_STATS:
            v = fmt(float(st.text) * p["armour"])
        if ref.find("statBases/" + st.tag) is not None or st.tag in ARMOUR_STATS:
            sub(sb, st.tag, v)
    cl = sub(t, "costList")
    for c in (ref.find("costList") if ref.find("costList") is not None else []):
        sub(cl, c.tag, c.text)
    sub(cl, "HP_SoulEssence", p["essence"])
    # Kit pieces are made, not found: keep them off generated pawns and out of reward pools.
    ap = t.find("apparel")
    if ap is None:
        ap = sub(t, "apparel")
    for old in ap.findall("tags"):
        ap.remove(old)
    sub(ap, "tags", Inherit="False")
    sub(t, "thingSetMakerTags", Inherit="False")
    gc = t.find("generateCommonality")
    if gc is None:
        sub(t, "generateCommonality", 0)
    else:
        gc.text = "0"
    if p["research"] is None:
        rm = t.find("recipeMaker")
        if rm is not None:
            for tag in ("researchPrerequisite", "researchPrerequisites"):
                for old in rm.findall(tag):
                    rm.remove(old)
            sub(rm, "researchPrerequisites", Inherit="False")
    if p["color"] is not None and resolved.find("stuffCategories") is None:
        color_gen(t, p["color"])
    me = sub(t, "modExtensions")
    for c in (ref.find("modExtensions") if ref.find("modExtensions") is not None else []):
        me.append(copy.deepcopy(c))
    set_ext(t, s)
    return t


def build_custom(s, p):
    t = ET.Element("ThingDef", ParentName=p["parent"])
    sub(t, "defName", def_name(s, p))
    sub(t, "label", p["label"])
    sub(t, "description", p["desc"])
    sub(t, "techLevel", p["tech"])
    gd = sub(t, "graphicData")
    sub(gd, "texPath", p["tex"])
    sub(gd, "graphicClass", "Graphic_Single")
    if p["stuff"]:
        sc = sub(t, "stuffCategories")
        for st in p["stuff"]:
            sub(sc, "li", st)
        sub(t, "costStuffCount", p["costStuff"])
    cl = sub(t, "costList")
    for k, v in p["cost"].items():
        sub(cl, k, v)
    if not p["stuff"] and not any(p["stats"].get(a, 0) > 0 for a in ARMOUR_STATS[:3]):
        # Combat Extended logs an error every time a stuffless piece with no armour at all is hit.
        raise SystemExit("%s has no stuff and no armour stat: give it a token ArmorRating_Sharp" % p["id"])
    sb = sub(t, "statBases")
    for k, v in p["stats"].items():
        sub(sb, k, v)
    if p["offsets"]:
        eo = sub(t, "equippedStatOffsets")
        for k, v in p["offsets"].items():
            sub(eo, k, v)
    tc = sub(t, "thingCategories")
    sub(tc, "li", "ApparelMisc")
    ap = sub(t, "apparel")
    bg = sub(ap, "bodyPartGroups")
    for g in p["groups"]:
        sub(bg, "li", g)
    if p["worn"]:
        sub(ap, "wornGraphicPath", p["worn"])
    ly = sub(ap, "layers")
    for l in p["layers"]:
        sub(ly, "li", l)
    sub(ap, "tags", Inherit="False")
    sub(ap, "canBeGeneratedToSatisfyWarmth", "false")
    sub(t, "generateCommonality", 0)
    if p["color"] is not None and not p["stuff"]:
        color_gen(t, p["color"])
    set_ext(t, s)
    return t


# ---------------------------------------------------------------- checks


def wear_info(elem):
    layers = [l.text for l in elem.findall("apparel/layers/li")]
    groups = [g.text for g in elem.findall("apparel/bodyPartGroups/li")]
    return layers, groups


def interfering(groups, parts, relevant):
    out = set()
    for pg in parts:
        if pg & set(groups):
            out |= pg
    return out & relevant


def check_set(name, pieces, parts, relevant, layers_needed):
    """pieces: [(label, layers, groups)]. Returns a list of problems."""
    problems = []
    for i in range(len(pieces)):
        for j in range(i + 1, len(pieces)):
            a, b = pieces[i], pieces[j]
            if not set(a[1]) & set(b[1]):
                continue
            ia, ib = interfering(a[2], parts, relevant), interfering(b[2], parts, relevant)
            if set(a[2]) & ib or set(b[2]) & ia:
                problems.append("%s: %s and %s clash (layer %s)" % (name, a[0], b[0], ",".join(sorted(set(a[1]) & set(b[1])))))
    have = {l for p in pieces for l in p[1]}
    for l in layers_needed:
        if l not in have:
            problems.append("%s: nothing on layer %s" % (name, l))
    skin = {g for p in pieces if "OnSkin" in p[1] for g in p[2]}
    for g in ("Torso", "Legs"):
        if g not in skin:
            problems.append("%s: skin layer misses %s" % (name, g))
    return problems


# ---------------------------------------------------------------- main


def write(path, root, header):
    ET.indent(root, space="  ")
    body = ET.tostring(root, encoding="unicode")
    body = body.replace("<Defs>", "<Defs>\n\n  <!-- " + header + " -->\n", 1).replace("<Patch>", "<Patch>\n\n  <!-- " + header + " -->\n", 1)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n' + body + "\n")
    print("wrote", os.path.relpath(path, ROOT))


def main():
    data, ce = sys.argv[1], sys.argv[2]
    v_def, v_name = load_defs([os.path.join(data, "Core", "Defs")])
    ce_def, ce_name = load_defs([os.path.join(ce, "Defs")])
    own_def, own_name = load_defs([os.path.join(ROOT, "Defs")])
    all_def = {**v_def, **ce_def, **own_def}
    all_name = {**v_name, **ce_name, **own_name}
    parts = human_parts(data)
    ops = load_ce_ops(ce)
    ce_named = patch_names(all_name, ops)

    relevant = set()
    for t in list(v_def.values()) + list(ce_def.values()):
        if t.get("Abstract") != "True":
            relevant |= {g.text for g in resolve(t, all_name).findall("apparel/bodyPartGroups/li")}

    defs_out, ce_defs_out, patch_out = ET.Element("Defs"), ET.Element("Defs"), ET.Element("Patch")
    problems = []
    for s in SETS:
        vanilla_state, ce_state = [], []
        for p in s["pieces"] + s["ce"]:
            if p["kind"] == "external":
                el = resolve(all_def[p["def"]], all_name)
                vanilla_state.append((p["def"],) + wear_info(el))
                ce_state.append((p["def"],) + wear_info(el))
                continue
            if p["kind"] == "copy":
                if p["armour"] > 1:
                    problems.append("%s: %s scales armour above its original" % (s["id"], p["label"]))
                t = build_copy(s, p, all_def, all_name)
            else:
                t = build_custom(s, p)
            (ce_defs_out if p.get("ce") else defs_out).append(t)
            dn = t.findtext("defName")
            el_v = resolve(t, all_name)
            # Mirror every CE patch on the original onto the copy.
            el_ce = resolve(t, ce_named)
            if p["kind"] == "copy" and not p.get("ce"):
                for defs, names, tail, op in ops:
                    # CE renames some originals ("armor vest"); our pieces keep their own names.
                    if p["ref"] in defs and tail not in ("/label", "/description"):
                        apply_op(op, el_ce, tail)
                        mirrored = copy.deepcopy(op)
                        for x in mirrored.iter("xpath"):
                            x.text = re.sub(r'^Defs/ThingDef\[[^\]]*\]', 'Defs/ThingDef[defName="%s"]' % dn, x.text.strip())
                        for st in ARMOUR_STATS:
                            for v in mirrored.iter(st):
                                if re.fullmatch(r"[0-9.]+", (v.text or "").strip()):
                                    v.text = fmt(float(v.text) * p["armour"])
                        patch_out.append(mirrored)
            if p["kind"] == "custom":
                op = sub(patch_out, "Operation", Class="PatchOperationAdd")
                sub(op, "xpath", 'Defs/ThingDef[defName="%s"]/statBases' % dn)
                val = sub(op, "value")
                sub(val, "Bulk", 0.5 if "EyeCover" in p["layers"] else 1)
                sub(val, "WornBulk", 0 if "EyeCover" in p["layers"] else 0.5)
            if not p.get("ce"):
                vanilla_state.append((p["label"],) + wear_info(el_v))
            ce_state.append((p["label"],) + wear_info(el_ce))
        problems += check_set(s["label"], vanilla_state, parts, relevant, VANILLA_LAYERS)
        # Under CE some vanilla layers move (hats to OnHead), so CE only has to add its own three.
        # A vanilla-only set (the conscript kit) is still checked for clashes under CE, but needs no CE pieces.
        ce_needed = [] if s.get("vanillaOnly") else CE_LAYERS
        problems += check_set(s["label"] + " (CE)", ce_state, parts, relevant, ce_needed + ["OnSkin", "Shell", "Belt"])
    if problems:
        print("Refusing to write:")
        for pr in problems:
            print("  " + pr)
        sys.exit(1)
    hdr = "GENERATED by Source/tools/gen_apparel_sets.py: edit the tables there, not this file."
    write(os.path.join(ROOT, "Defs", "HP_ApparelSets.xml"), defs_out, hdr)
    write(os.path.join(ROOT, "Mods", "CombatExtended", "Defs", "HP_CE_ApparelSets.xml"), ce_defs_out, hdr)
    write(os.path.join(ROOT, "Mods", "CombatExtended", "Patches", "HP_CE_ApparelSets.xml"), patch_out, hdr)
    for s in SETS:
        print("  %-14s %-17s %d pieces, +%d with CE" % (s["label"], s["tier"], len(s["pieces"]), len(s["ce"])))


if __name__ == "__main__":
    main()
