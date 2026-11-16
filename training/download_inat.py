"""Download openly licensed iNaturalist research-grade photos for the feasibility set.

Writes images to data/images/<taxon_id>/<photo_id>.jpg and metadata to data/photos.csv.
Two passes per species: a general pass and a leafless pass (Leaves = No Live Leaves),
so winter identification can be evaluated.
"""
import csv
import os
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import requests

API = "https://api.inaturalist.org/v1"
UA = {"User-Agent": "tree-id-feasibility/0.1 (research prototype)"}
LICENSES = "cc0,cc-by,cc-by-nc"
# Texas, Oklahoma, Louisiana, Arkansas bounding box
BBOX = dict(swlat=25.8, swlng=-106.7, nelat=37.1, nelng=-89.0)
GENERAL_OBS = 800      # observations per species, general pass
LEAFLESS_OBS = 200     # observations per species, leafless pass
PHOTOS_PER_OBS = 3     # later photos in an observation are often bark or twig shots

ROOT = Path(__file__).resolve().parent
# Photos live outside OneDrive so gigabytes of images don't sync
DATA = Path(os.environ.get("TREE_DATA", Path.home() / "Tree ID data"))
IMG = DATA / "images"


def get(url, params):
    for attempt in range(5):
        r = requests.get(url, params=params, headers=UA, timeout=30)
        if r.status_code == 429:
            time.sleep(10 * (attempt + 1))
            continue
        r.raise_for_status()
        time.sleep(1.0)  # iNat asks for about 1 request per second
        return r.json()
    raise RuntimeError(f"rate limited: {url}")


def taxon_id(name):
    res = get(f"{API}/taxa", {"q": name, "rank": "species", "per_page": 30})["results"]
    for t in res:
        if t["name"].lower() == name.lower():
            return t["id"]
    for t in res:  # name was reclassified (e.g. Prosopis glandulosa -> Neltuma glandulosa)
        if (t.get("matched_term") or "").lower() == name.lower():
            print(f"  {name} is now {t['name']} on iNaturalist")
            return t["id"]
    raise ValueError(f"taxon not found: {name}")


def observations(tid, n, extra, use_bbox=True):
    params = {
        "taxon_id": tid, "quality_grade": "research", "photo_license": LICENSES,
        "per_page": 200, "order_by": "votes", "photos": "true", **extra,
    }
    if use_bbox:
        params.update(BBOX)
    out = []
    for page in range(1, n // 200 + 2):
        res = get(f"{API}/observations", {**params, "page": page})["results"]
        out += res
        if len(res) < 200 or len(out) >= n:
            break
    return out[:n]


def leafless(obs):
    for a in obs.get("annotations") or []:
        if a.get("controlled_attribute_id") == 36 and a.get("controlled_value_id") == 40:
            return True
    return False


def fetch(job):
    url, dest = job
    if dest.exists():
        return True
    try:
        r = requests.get(url, headers=UA, timeout=30)
        r.raise_for_status()
        dest.write_bytes(r.content)
        return True
    except Exception as e:  # noqa: BLE001
        print("  failed", url, e, file=sys.stderr)
        return False


def main():
    names = [l.strip() for l in (ROOT / "species.txt").read_text().splitlines()
             if l.strip() and not l.startswith("#")]
    IMG.mkdir(parents=True, exist_ok=True)
    rows, jobs, seen = [], [], set()
    for name in names:
        tid = taxon_id(name)
        obs = observations(tid, GENERAL_OBS, {})
        winter = observations(tid, LEAFLESS_OBS, {"term_id": 36, "term_value_id": 40})
        if len(winter) < 50:  # few regional leafless records; widen to anywhere
            winter = observations(tid, LEAFLESS_OBS, {"term_id": 36, "term_value_id": 40}, use_bbox=False)
        print(f"{name}: {len(obs)} general, {len(winter)} leafless obs")
        (IMG / str(tid)).mkdir(exist_ok=True)
        for o in obs + winter:
            if o["id"] in seen:
                continue
            seen.add(o["id"])
            month = (o.get("observed_on_details") or {}).get("month")
            lat, lng = (o.get("location") or ",").split(",") if o.get("location") else ("", "")
            for idx, p in enumerate(o["photos"][:PHOTOS_PER_OBS]):
                url = p["url"].replace("square", "medium")
                dest = IMG / str(tid) / f"{p['id']}.jpg"
                jobs.append((url, dest))
                rows.append({
                    "path": str(dest.relative_to(DATA)), "species": name, "taxon_id": tid,
                    "obs_id": o["id"], "photo_index": idx, "month": month or "",
                    "lat": lat, "lng": lng, "leafless": int(leafless(o)),
                    "license": p.get("license_code"), "attribution": p.get("attribution", ""),
                })
    print(f"downloading {len(jobs)} photos")
    with ThreadPoolExecutor(4) as ex:
        ok = list(ex.map(fetch, jobs))
    rows = [r for r, good in zip(rows, ok) if good]
    with open(DATA / "photos.csv", "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)
    print(f"done: {len(rows)} photos")


if __name__ == "__main__":
    main()
