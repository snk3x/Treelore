"""Fetch how often each species is actually observed in every 1-degree cell of the region.

The training photos are capped per species, so they can't tell a common tree from a rare one.
This asks iNaturalist for real observation counts per cell (wild and planted), used for rarity in the app.
Writes abundance.json: {"labels": [...], "cells": {"lat,lng": [counts in label order]}}.
"""
import json
import math
from pathlib import Path

from download_inat import API, BBOX, get, taxon_id

ROOT = Path(__file__).resolve().parent

# Species most people call by the same name, counted together for rarity.
# Central Texas live oaks are mostly escarpment live oak, a separate species on iNaturalist.
ALIASES = {"Quercus fusiformis": "Quercus virginiana"}


def main():
    labels = (ROOT / "out" / "labels.txt").read_text().split("\n")
    ids = {taxon_id(name): i for i, name in enumerate(labels)}
    for alias, target in ALIASES.items():
        ids[taxon_id(alias)] = labels.index(target)
    cells = {}
    lat0, lat1 = math.floor(BBOX["swlat"]), math.ceil(BBOX["nelat"])
    lng0, lng1 = math.floor(BBOX["swlng"]), math.ceil(BBOX["nelng"])
    total = (lat1 - lat0) * (lng1 - lng0)
    done = 0
    for lat in range(lat0, lat1):
        for lng in range(lng0, lng1):
            # All quality grades: planted trees (crape myrtle, magnolia) are "casual" on iNaturalist,
            # but rarity should reflect what someone can actually find on a walk.
            res = get(f"{API}/observations/species_counts", {
                "taxon_id": ",".join(map(str, ids)), "quality_grade": "research,needs_id,casual", "per_page": 200,
                "swlat": lat, "swlng": lng, "nelat": lat + 1, "nelng": lng + 1,
            })["results"]
            counts = [0] * len(labels)
            for r in res:
                t = r["taxon"]
                # Varieties and subspecies roll up into their species
                for tid in [t["id"], *(t.get("ancestor_ids") or [])]:
                    if tid in ids:
                        counts[ids[tid]] += r["count"]
                        break
            if any(counts):
                cells[f"{lat},{lng}"] = counts
            done += 1
            if done % 20 == 0:
                print(f"{done}/{total} cells", flush=True)
    (ROOT / "abundance.json").write_text(json.dumps({"labels": labels, "cells": cells}, separators=(",", ":")))
    print(f"done: {len(cells)} cells with observations")


if __name__ == "__main__":
    main()
