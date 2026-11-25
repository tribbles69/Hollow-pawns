"""Pull the grim-industry art (Higgsfield) and sounds (ElevenLabs) into the mod.

The assets were generated in Nov 2026 and packed into one zip on Higgsfield's file storage:
  Textures/Things/Building/HP_BatteryCage, HP_BatteryPen, HP_FeedHopper, HP_RenderingPod, HP_MechDynamo
  Textures/Things/Item/HP_NutrientSlurry
  Sounds/HP/CageCollect, RenderSquelch, MechBreakout (.ogg)

Until this runs, the repo carries flat placeholder sprites and silent-ish placeholder sounds under the
same names. Run from anywhere: python Source/tools/fetch_grim_art.py
It checks the zip's hash, overwrites the placeholder PNGs and swaps each placeholder .wav for the .ogg.
Commit the result.
"""
import hashlib
import io
import os
import sys
import urllib.request
import zipfile

URL = "https://d2ol7oe51mr4n9.cloudfront.net/user_3LvvG7LFkCqxoLayPAS5HuAf6k9/63617530-650a-4d86-9551-9dd59accb51c.zip"
SHA256 = "ee9b8c9876717e3fc0cf4d198eb3fe4f766206d70fc87a9fc962baa3a96a3eed"
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def main():
    data = urllib.request.urlopen(URL, timeout=60).read()
    digest = hashlib.sha256(data).hexdigest()
    if digest != SHA256:
        sys.exit("Hash mismatch: got %s, expected %s. Not touching anything." % (digest, SHA256))
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        for name in z.namelist():
            if name.endswith("/"):
                continue
            target = os.path.join(ROOT, *name.split("/"))
            os.makedirs(os.path.dirname(target), exist_ok=True)
            with open(target, "wb") as f:
                f.write(z.read(name))
            print("wrote", name)
            if name.endswith(".ogg"):
                wav = target[:-4] + ".wav"
                if os.path.exists(wav):
                    os.remove(wav)
                    print("removed placeholder", os.path.relpath(wav, ROOT))


if __name__ == "__main__":
    main()
