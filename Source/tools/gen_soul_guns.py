"""Generates the soul gun family: vanilla defs, and the Combat Extended ammo/beams/patches.

Edit the GUNS table and re-run:  python3 Source/tools/gen_soul_guns.py
Writes:
  Defs/ThingDefs_Weapons/HP_SoulGuns.xml                  (vanilla guns + one-cell blast bolts)
  Mods/CombatExtended/Defs/HP_CE_SoulCharge.xml           (soul charge ammo, per-gun ammo sets + beams)
  Mods/CombatExtended/Patches/HP_CE_SoulGuns.xml          (CE conversion of each gun)
"""
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

# key, label, description
# vanilla: dmg, ap, range, burst, ticksBetween, warmup, cooldown, mass
# cost: soulsteel, components, essence, crafting skill
# CE: dmg, apSharp, apBlunt, blast, range, mag, reload, recoil, spread, sway, sights, bulk, aimedBurst
GUNS = [
    dict(key="Vesper", label="vesper pistol", kind="pistol",
         desc="A soulsteel sidearm. Quick to draw and brutal up close: each shot ends in a small soul blast that takes fingers, hands and worse.",
         dmg=20, ap=0.35, range=25.9, burst=1, tbb=0, warmup=0.3, cooldown=1.4, mass=1.4,
         steel=40, comps=2, essence=10, skill=6,
         ce=dict(dmg=16, sharp=12, blunt=25, blast=8, range=20, mag=12, reload=3, recoil=1.4, spread=0.12, sway=1.4, sights=0.7, bulk=2.5, aimed=1)),
    dict(key="Whisper", label="whisper SMG", kind="pistol",
         desc="A compact soulsteel machine pistol. Each round makes only a small blast, but there are a lot of them.",
         dmg=16, ap=0.30, range=21.9, burst=4, tbb=6, warmup=0.6, cooldown=1.2, mass=2.8,
         steel=55, comps=3, essence=15, skill=7,
         ce=dict(dmg=13, sharp=10, blunt=25, blast=6, range=28, mag=30, reload=3.5, recoil=1.6, spread=0.12, sway=1.6, sights=1.0, bulk=5, aimed=2)),
    dict(key="Dirge", label="dirge rifle", kind="rifle",
         desc="The soul gun family's all-rounder: a burst-fire soulsteel assault rifle.",
         dmg=22, ap=0.40, range=30.9, burst=3, tbb=8, warmup=1.0, cooldown=1.7, mass=3.4,
         steel=65, comps=4, essence=20, skill=8,
         ce=dict(dmg=18, sharp=18, blunt=70, blast=9, range=55, mag=30, reload=4, recoil=1.5, spread=0.07, sway=1.3, sights=1.1, bulk=8, aimed=3)),
    dict(key="Requiem", label="requiem rifle", kind="rifle",
         desc="A single-shot soulsteel marksman rifle. Every bolt hits like a hammer and leaves a crater where it lands.",
         dmg=30, ap=0.55, range=38.9, burst=1, tbb=0, warmup=1.3, cooldown=1.6, mass=3.6,
         steel=70, comps=5, essence=25, skill=8,
         ce=dict(dmg=26, sharp=28, blunt=90, blast=12, range=62, mag=10, reload=4, recoil=1.6, spread=0.04, sway=1.3, sights=1.6, bulk=9, aimed=1)),
    dict(key="LastRites", label="last rites rifle", kind="rifle",
         desc="A soulsteel sniper rifle. Slow to aim. What it hits, it removes.",
         dmg=42, ap=0.75, range=45.9, burst=1, tbb=0, warmup=3.0, cooldown=1.5, mass=6.5,
         steel=90, comps=6, essence=30, skill=10,
         ce=dict(dmg=38, sharp=45, blunt=160, blast=18, range=80, mag=5, reload=4.5, recoil=2.2, spread=0.02, sway=1.8, sights=3.5, bulk=13, aimed=1)),
    dict(key="Litany", label="litany machine gun", kind="rifle",
         desc="A belt-fed soulsteel machine gun. Long bursts of small blasts that chew through cover and whoever is behind it.",
         dmg=18, ap=0.35, range=26.9, burst=6, tbb=7, warmup=1.8, cooldown=2.2, mass=9.0,
         steel=90, comps=6, essence=30, skill=9,
         ce=dict(dmg=16, sharp=16, blunt=70, blast=8, range=60, mag=100, reload=7.8, recoil=1.8, spread=0.08, sway=3.0, sights=1.0, bulk=13, aimed=5)),
    dict(key="Wake", label="wake shotgun", kind="rifle",
         desc="A soulsteel shotgun firing one heavy soul charge. Short range, enormous blast.",
         dmg=35, ap=0.30, range=15.9, burst=1, tbb=0, warmup=0.9, cooldown=1.4, mass=3.5,
         steel=60, comps=3, essence=20, skill=7,
         ce=dict(dmg=30, sharp=12, blunt=110, blast=16, range=18, mag=6, reload=4, recoil=2.6, spread=0.12, sway=1.4, sights=1.0, bulk=8, aimed=1)),
]

HEAD = '<?xml version="1.0" encoding="utf-8"?>\n'


def vanilla_tools(kind):
    if kind == "pistol":
        return """    <tools>
      <li>
        <label>grip</label>
        <capacities><li>Blunt</li></capacities>
        <power>9</power>
        <cooldownTime>2</cooldownTime>
      </li>
      <li>
        <label>barrel</label>
        <capacities><li>Blunt</li><li>Poke</li></capacities>
        <power>9</power>
        <cooldownTime>2</cooldownTime>
      </li>
    </tools>"""
    return """    <tools>
      <li>
        <label>stock</label>
        <capacities><li>Blunt</li></capacities>
        <power>9</power>
        <cooldownTime>2</cooldownTime>
      </li>
      <li>
        <label>barrel</label>
        <capacities><li>Blunt</li><li>Poke</li></capacities>
        <power>9</power>
        <cooldownTime>2</cooldownTime>
      </li>
    </tools>"""


def ce_tools(kind):
    first = ("grip", "Grip") if kind == "pistol" else ("stock", "Stock")
    return f"""        <li Class="CombatExtended.ToolCE">
          <label>{first[0]}</label>
          <capacities><li>Blunt</li></capacities>
          <power>{2 if kind == 'pistol' else 8}</power>
          <cooldownTime>{1.54 if kind == 'pistol' else 1.55}</cooldownTime>
          <armorPenetrationBlunt>{0.555 if kind == 'pistol' else 2.755}</armorPenetrationBlunt>
          <linkedBodyPartsGroup>{first[1]}</linkedBodyPartsGroup>
        </li>
        <li Class="CombatExtended.ToolCE">
          <label>barrel</label>
          <capacities><li>Blunt</li></capacities>
          <power>{2 if kind == 'pistol' else 5}</power>
          <cooldownTime>{1.54 if kind == 'pistol' else 2.02}</cooldownTime>
          <armorPenetrationBlunt>{0.555 if kind == 'pistol' else 1.630}</armorPenetrationBlunt>
          <linkedBodyPartsGroup>Barrel</linkedBodyPartsGroup>
        </li>"""


def vanilla():
    out = [HEAD, "<Defs>\n\n  <!-- GENERATED by Source/tools/gen_soul_guns.py: edit the table there, not this file. -->\n"]
    for g in GUNS:
        k = g["key"]
        burst_tbb = f"\n        <ticksBetweenBurstShots>{g['tbb']}</ticksBetweenBurstShots>" if g["burst"] > 1 else ""
        out.append(f"""
  <!-- ===== {g['label']} ===== -->
  <ThingDef ParentName="BaseHumanMakeableGun">
    <defName>HP_Gun_{k}</defName>
    <label>{g['label']}</label>
    <description>{g['desc']}</description>
    <graphicData>
      <texPath>Things/Item/Equipment/HP_{k}</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <soundInteract>Interact_Rifle</soundInteract>
    <techLevel>Industrial</techLevel>
    <weaponTags Inherit="False">
      <li>HP_SoulWeapon</li>
    </weaponTags>
    <statBases>
      <WorkToMake>{20000 + g['steel'] * 300}</WorkToMake>
      <Mass>{g['mass']}</Mass>
      <AccuracyTouch>0.70</AccuracyTouch>
      <AccuracyShort>0.75</AccuracyShort>
      <AccuracyMedium>0.70</AccuracyMedium>
      <AccuracyLong>{0.85 if k == 'LastRites' else 0.60}</AccuracyLong>
      <RangedWeapon_Cooldown>{g['cooldown']}</RangedWeapon_Cooldown>
    </statBases>
    <costList>
      <HP_Soulsteel>{g['steel']}</HP_Soulsteel>
      <ComponentIndustrial>{g['comps']}</ComponentIndustrial>
      <HP_SoulEssence>{g['essence']}</HP_SoulEssence>
    </costList>
    <recipeMaker>
      <researchPrerequisite>HP_SoulWeaponry</researchPrerequisite>
      <skillRequirements>
        <Crafting>{g['skill']}</Crafting>
      </skillRequirements>
      <recipeUsers Inherit="False">
        <li>HP_SoulCrucible</li>
      </recipeUsers>
    </recipeMaker>
    <verbs>
      <li>
        <verbClass>Verb_Shoot</verbClass>
        <hasStandardCommand>true</hasStandardCommand>
        <defaultProjectile>HP_Bolt_{k}</defaultProjectile>
        <warmupTime>{g['warmup']}</warmupTime>
        <range>{g['range']}</range>
        <burstShotCount>{g['burst']}</burstShotCount>{burst_tbb}
        <soundCast>Shot_ChargeRifle</soundCast>
        <soundCastTail>GunTail_Medium</soundCastTail>
        <muzzleFlashScale>9</muzzleFlashScale>
      </li>
    </verbs>
{vanilla_tools(g['kind'])}
  </ThingDef>

  <!-- One-cell soul blast on impact: takes the limb it hits, craters what it misses into. -->
  <ThingDef ParentName="BaseBullet">
    <defName>HP_Bolt_{k}</defName>
    <label>{g['label']} bolt</label>
    <thingClass>Projectile_Explosive</thingClass>
    <graphicData>
      <texPath>Things/Projectile/HP_SoulBolt</texPath>
      <graphicClass>Graphic_Single</graphicClass>
      <shaderType>TransparentPostLight</shaderType>
      <drawSize>(0.6, 2.4)</drawSize>
    </graphicData>
    <projectile>
      <damageDef>HP_SoulBlast</damageDef>
      <damageAmountBase>{g['dmg']}</damageAmountBase>
      <armorPenetrationBase>{g['ap']}</armorPenetrationBase>
      <stoppingPower>1.5</stoppingPower>
      <speed>110</speed>
      <explosionRadius>0.9</explosionRadius>
      <postExplosionSpawnThingDef>Filth_RubbleRock</postExplosionSpawnThingDef>
      <postExplosionSpawnChance>0.3</postExplosionSpawnChance>
      <postExplosionSpawnThingCount>1</postExplosionSpawnThingCount>
    </projectile>
  </ThingDef>
""")
    out.append("\n</Defs>\n")
    return "".join(out)


def ce_defs():
    out = [HEAD, """<Defs>

  <!-- GENERATED by Source/tools/gen_soul_guns.py: edit the table there, not this file.
       Soul guns under CE work like CE's own lasers: one shared ammo item, and a separate
       ammo set per gun mapping it to that gun's own beam. -->

  <ThingCategoryDef>
    <defName>HP_AmmoSoulCharge</defName>
    <label>soul charges</label>
    <parent>AmmoAdvanced</parent>
  </ThingCategoryDef>

  <CombatExtended.AmmoCategoryDef>
    <defName>HP_SoulCharge</defName>
    <label>soul charge</label>
    <description>A cell of bound soul essence. Soul guns turn each one into a beam that ends in a small, vicious blast.</description>
  </CombatExtended.AmmoCategoryDef>

  <ThingDef Class="CombatExtended.AmmoDef" ParentName="SpacerSmallAmmoBase">
    <defName>HP_Ammo_SoulCharge</defName>
    <label>soul charge</label>
    <description>A cell of bound soul essence. Shared by every soul gun.</description>
    <graphicData>
      <texPath>Things/Item/HP_SoulCharge</texPath>
      <graphicClass>Graphic_Single</graphicClass>
    </graphicData>
    <techLevel>Industrial</techLevel>
    <tradeTags Inherit="False" />
    <statBases>
      <Mass>0.01</Mass>
      <Bulk>0.01</Bulk>
      <MarketValue>1.5</MarketValue>
    </statBases>
    <thingCategories>
      <li>HP_AmmoSoulCharge</li>
    </thingCategories>
    <stackLimit>1000</stackLimit>
    <ammoClass>HP_SoulCharge</ammoClass>
    <generateAllowChance>0</generateAllowChance>
  </ThingDef>

  <!-- 100 charges from 6 soulsteel + 4 essence: one fresh corpse (8 essence) is about 200 shots. -->
  <RecipeDef ParentName="AmmoRecipeBase">
    <defName>HP_MakeAmmo_SoulCharge</defName>
    <label>make soul charges x100</label>
    <description>Bind soul essence into 100 soul charges.</description>
    <jobString>Making soul charges.</jobString>
    <recipeUsers Inherit="False">
      <li>HP_SoulCrucible</li>
    </recipeUsers>
    <researchPrerequisite>HP_SoulWeaponry</researchPrerequisite>
    <skillRequirements>
      <Crafting>6</Crafting>
    </skillRequirements>
    <workAmount>4000</workAmount>
    <ingredients>
      <li>
        <filter><thingDefs><li>HP_Soulsteel</li></thingDefs></filter>
        <count>6</count>
      </li>
      <li>
        <filter><thingDefs><li>HP_SoulEssence</li></thingDefs></filter>
        <count>4</count>
      </li>
    </ingredients>
    <fixedIngredientFilter>
      <thingDefs>
        <li>HP_Soulsteel</li>
        <li>HP_SoulEssence</li>
      </thingDefs>
    </fixedIngredientFilter>
    <products>
      <HP_Ammo_SoulCharge>100</HP_Ammo_SoulCharge>
    </products>
  </RecipeDef>
"""]
    for g in GUNS:
        k, c = g["key"], g["ce"]
        out.append(f"""
  <!-- ===== {g['label']} ===== -->
  <CombatExtended.AmmoSetDef>
    <defName>HP_AmmoSet_{k}</defName>
    <label>soul charge</label>
    <ammoTypes>
      <HP_Ammo_SoulCharge>HP_Beam_{k}</HP_Ammo_SoulCharge>
    </ammoTypes>
  </CombatExtended.AmmoSetDef>

  <!-- CE's own laser beam, in its blue preset, with a one-cell soul blast on impact. -->
  <ThingDef Class="CombatExtended.Lasers.LaserBeamDefCE" ParentName="LaserBulletBlue">
    <defName>HP_Beam_{k}</defName>
    <label>{g['label']} beam</label>
    <projectile Class="CombatExtended.ProjectilePropertiesCE">
      <damageDef>HP_SoulBlast</damageDef>
      <damageAmountBase>{c['dmg']}</damageAmountBase>
      <armorPenetrationSharp>{c['sharp']}</armorPenetrationSharp>
      <armorPenetrationBlunt>{c['blunt']}</armorPenetrationBlunt>
    </projectile>
    <comps>
      <li Class="CombatExtended.CompProperties_ExplosiveCE">
        <damageAmountBase>{c['blast']}</damageAmountBase>
        <explosiveDamageType>HP_SoulBlast</explosiveDamageType>
        <explosiveRadius>0.5</explosiveRadius>
      </li>
    </comps>
  </ThingDef>
""")
    out.append("\n</Defs>\n")
    return "".join(out)


def ce_patches():
    out = [HEAD, "<Patch>\n\n  <!-- GENERATED by Source/tools/gen_soul_guns.py: edit the table there, not this file. -->\n"]
    for g in GUNS:
        k, c = g["key"], g["ce"]
        burst = f"\n      <burstShotCount>{g['burst']}</burstShotCount>\n      <ticksBetweenBurstShots>{g['tbb']}</ticksBetweenBurstShots>" if g["burst"] > 1 else ""
        fire = (f"""      <aimedBurstShotCount>{c['aimed']}</aimedBurstShotCount>
      <aiUseBurstMode>TRUE</aiUseBurstMode>
      <aiAimMode>AimedShot</aiAimMode>""" if g["burst"] > 1 else "      <aiAimMode>AimedShot</aiAimMode>")
        out.append(f"""
  <!-- ===== {g['label']} ===== -->
  <Operation Class="CombatExtended.PatchOperationMakeGunCECompatible">
    <defName>HP_Gun_{k}</defName>
    <statBases>
      <Mass>{g['mass']}</Mass>
      <RangedWeapon_Cooldown>{round(g['cooldown'] * 0.35, 2)}</RangedWeapon_Cooldown>
      <SightsEfficiency>{c['sights']}</SightsEfficiency>
      <ShotSpread>{c['spread']}</ShotSpread>
      <SwayFactor>{c['sway']}</SwayFactor>
      <Bulk>{c['bulk']}</Bulk>
      <WorkToMake>{20000 + g['steel'] * 300}</WorkToMake>
    </statBases>
    <costList>
      <HP_Soulsteel>{g['steel']}</HP_Soulsteel>
      <ComponentIndustrial>{g['comps']}</ComponentIndustrial>
      <HP_SoulEssence>{g['essence']}</HP_SoulEssence>
    </costList>
    <Properties>
      <recoilAmount>{c['recoil']}</recoilAmount>
      <verbClass>CombatExtended.Verb_ShootCE</verbClass>
      <hasStandardCommand>true</hasStandardCommand>
      <defaultProjectile>HP_Beam_{k}</defaultProjectile>
      <warmupTime>{g['warmup']}</warmupTime>
      <range>{c['range']}</range>{burst}
      <soundCast>Shot_ChargeRifle</soundCast>
      <soundCastTail>GunTail_Medium</soundCastTail>
      <muzzleFlashScale>9</muzzleFlashScale>
    </Properties>
    <AmmoUser>
      <magazineSize>{c['mag']}</magazineSize>
      <reloadTime>{c['reload']}</reloadTime>
      <ammoSet>HP_AmmoSet_{k}</ammoSet>
    </AmmoUser>
    <FireModes>
{fire}
    </FireModes>
    <weaponTags>
      <li>HP_SoulWeapon</li>
    </weaponTags>
    <researchPrerequisite>HP_SoulWeaponry</researchPrerequisite>
  </Operation>

  <Operation Class="PatchOperationReplace">
    <xpath>Defs/ThingDef[defName="HP_Gun_{k}"]/tools</xpath>
    <value>
      <tools>
{ce_tools(g['kind'])}
      </tools>
    </value>
  </Operation>
""")
    out.append("\n</Patch>\n")
    return "".join(out)


def write(rel, text):
    path = os.path.join(ROOT, rel)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(text)
    print("wrote", rel)


if __name__ == "__main__":
    write("Defs/ThingDefs_Weapons/HP_SoulGuns.xml", vanilla())
    write("Mods/CombatExtended/Defs/HP_CE_SoulCharge.xml", ce_defs())
    write("Mods/CombatExtended/Patches/HP_CE_SoulGuns.xml", ce_patches())
