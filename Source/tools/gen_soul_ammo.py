"""Generates soul-charged variants of Combat Extended calibres, straight from CE's own defs.

Usage:  python3 Source/tools/gen_soul_ammo.py <path to a Combat Extended checkout or install>

For each calibre in CALIBRES it reads CE's standard round (FMJ, slug or charged), then writes:
  - a soul-charged ammo item (same texture, tinted soul cyan, listed under that calibre)
  - a soul-charged projectile: x1.25 damage, x1.5 sharp / x1.25 blunt penetration,
    soul blast damage (limb-destroying, adds soul shock) and a one-cell soul blast on impact
  - a crucible recipe: 100 standard rounds + 2 soul essence -> 100 soul-charged rounds
  - a patch adding it to that calibre's ammo set, so every gun using the calibre accepts it
Re-run after a CE update to pick up any stat changes.
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

# Calibres used by vanilla guns under CE. Value: substring picking the standard round in the
# ammo set (None = first listed). Shotguns use the slug: one pellet, one blast.
CALIBRES = {
    "AmmoSet_45ACP": None,
    "AmmoSet_44Magnum": None,
    "AmmoSet_556x45mmNATO": None,
    "AmmoSet_762x51mmNATO": None,
    "AmmoSet_303British": None,
    "AmmoSet_5x50mmCaseless": None,
    "AmmoSet_12Gauge": "Slug",
    "AmmoSet_5x35mmCharged": None,
    "AmmoSet_6x22mmCharged": None,
    "AmmoSet_6x24mmCharged": None,
    "AmmoSet_12x64mmCharged": None,
}

DMG, SHARP, BLUNT, BLAST = 1.25, 1.5, 1.25, 0.5
TINT = "(98,196,206)"


def load_defs(ce_root):
    things, sets = {}, {}
    for path in glob.glob(os.path.join(ce_root, "Defs", "Ammo", "**", "*.xml"), recursive=True):
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError:
            continue
        for el in root:
            name = el.findtext("defName") or el.get("Name")
            if not name:
                continue
            if el.tag == "CombatExtended.AmmoSetDef":
                sets[name] = el
            elif el.tag == "ThingDef":
                things[name] = el
    return things, sets


def inherited(things, el, path):
    """Find a value by walking up ParentName, as RimWorld's inheritance would."""
    while el is not None:
        v = el.findtext(path)
        if v is not None:
            return v
        el = things.get(el.get("ParentName"))
    return None


def main(ce_root):
    things, sets = load_defs(ce_root)
    defs, patches, report = [], [], []
    for set_name, pick in CALIBRES.items():
        s = sets.get(set_name)
        if s is None:
            report.append(f"  SKIPPED {set_name}: not found in this CE version")
            continue
        pairs = [(e.tag, e.text.strip()) for e in s.find("ammoTypes")]
        ammo_name, proj_name = next(((a, p) for a, p in pairs if pick is None or pick in a), pairs[0])
        ammo, proj = things[ammo_name], things[proj_name]
        if inherited(things, proj, "projectile/pelletCount") not in (None, "1"):
            report.append(f"  SKIPPED {set_name}: standard round is multi-pellet")
            continue

        dmg = float(inherited(things, proj, "projectile/damageAmountBase") or 10)
        sharp = float(inherited(things, proj, "projectile/armorPenetrationSharp") or 0)
        blunt = float(inherited(things, proj, "projectile/armorPenetrationBlunt") or 0)
        label = ammo.findtext("label") or ammo_name
        cal = label.split(" (")[0]
        tex = inherited(things, ammo, "graphicData/texPath")
        gclass = inherited(things, ammo, "graphicData/graphicClass") or "Graphic_StackCount"
        new_ammo, new_proj = f"HP_{ammo_name}_Soul", f"HP_{proj_name}_Soul"
        d, sh, bl, bs = round(dmg * DMG), round(sharp * SHARP, 2), round(blunt * BLUNT, 2), max(4, round(dmg * BLAST))
        report.append(f"  {cal:<24} {dmg:>5.0f} dmg {sharp:>6.2f}/{blunt:<6.2f} AP  ->  {d:>3} dmg {sh:>6.2f}/{bl:<6.2f} AP, blast {bs}")

        defs.append(f"""
  <!-- ===== {cal} (from {ammo_name}: {dmg:g} dmg, {sharp:g} sharp / {blunt:g} blunt) ===== -->
  <ThingDef Class="CombatExtended.AmmoDef" ParentName="{ammo.get('ParentName')}">
    <defName>{new_ammo}</defName>
    <label>{cal} (Soul)</label>
    <description>Standard {cal} rounds with a sliver of bound soul essence in the core. Every hit ends in a small soul blast.</description>
    <graphicData>
      <texPath>{tex}</texPath>
      <graphicClass>{gclass}</graphicClass>
      <color>{TINT}</color>
    </graphicData>
    <tradeTags Inherit="False" />
    <ammoClass>HP_SoulCharged</ammoClass>
    <generateAllowChance>0</generateAllowChance>
  </ThingDef>

  <ThingDef ParentName="{proj.get('ParentName')}">
    <defName>{new_proj}</defName>
    <label>{cal} bullet (Soul)</label>
    <projectile Class="CombatExtended.ProjectilePropertiesCE">
      <damageDef>HP_SoulBlast</damageDef>
      <damageAmountBase>{d}</damageAmountBase>
      <armorPenetrationSharp>{sh}</armorPenetrationSharp>
      <armorPenetrationBlunt>{bl}</armorPenetrationBlunt>
    </projectile>
    <comps>
      <li Class="CombatExtended.CompProperties_ExplosiveCE">
        <damageAmountBase>{bs}</damageAmountBase>
        <explosiveDamageType>HP_SoulBlast</explosiveDamageType>
        <explosiveRadius>0.5</explosiveRadius>
      </li>
    </comps>
  </ThingDef>

  <RecipeDef ParentName="AmmoRecipeBase">
    <defName>HP_MakeAmmo_{ammo_name}_Soul</defName>
    <label>soul-charge {cal} rounds x100</label>
    <description>Bind soul essence into 100 standard {cal} rounds.</description>
    <jobString>Soul-charging {cal} rounds.</jobString>
    <recipeUsers Inherit="False">
      <li>HP_SoulCrucible</li>
    </recipeUsers>
    <researchPrerequisite>HP_SoulWeaponry</researchPrerequisite>
    <skillRequirements>
      <Crafting>5</Crafting>
    </skillRequirements>
    <workAmount>1500</workAmount>
    <ingredients>
      <li>
        <filter><thingDefs><li>{ammo_name}</li></thingDefs></filter>
        <count>100</count>
      </li>
      <li>
        <filter><thingDefs><li>HP_SoulEssence</li></thingDefs></filter>
        <count>2</count>
      </li>
    </ingredients>
    <fixedIngredientFilter>
      <thingDefs>
        <li>{ammo_name}</li>
        <li>HP_SoulEssence</li>
      </thingDefs>
    </fixedIngredientFilter>
    <products>
      <{new_ammo}>100</{new_ammo}>
    </products>
  </RecipeDef>
""")
        patches.append(f"""
  <Operation Class="PatchOperationAdd">
    <xpath>Defs/CombatExtended.AmmoSetDef[defName="{set_name}"]/ammoTypes</xpath>
    <value>
      <{new_ammo}>{new_proj}</{new_ammo}>
    </value>
  </Operation>
""")

    head = '<?xml version="1.0" encoding="utf-8"?>\n'
    note = "  <!-- GENERATED by Source/tools/gen_soul_ammo.py from Combat Extended's own defs. Re-run it, don't hand-edit. -->\n"
    cat = """
  <CombatExtended.AmmoCategoryDef>
    <defName>HP_SoulCharged</defName>
    <label>soul-charged</label>
    <labelShort>soul</labelShort>
    <description>A standard round with a sliver of bound soul essence in its core. Hits harder, penetrates further, and ends in a small blast that takes limbs and rattles the soul.</description>
  </CombatExtended.AmmoCategoryDef>
"""
    out_defs = os.path.join(ROOT, "Mods/CombatExtended/Defs/HP_CE_SoulChargedAmmo.xml")
    out_patch = os.path.join(ROOT, "Mods/CombatExtended/Patches/HP_CE_SoulChargedAmmo.xml")
    with open(out_defs, "w", encoding="utf-8") as f:
        f.write(head + "<Defs>\n\n" + note + cat + "".join(defs) + "\n</Defs>\n")
    with open(out_patch, "w", encoding="utf-8") as f:
        f.write(head + "<Patch>\n\n" + note + "".join(patches) + "\n</Patch>\n")
    print("\n".join(report))


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(sys.argv[1])
