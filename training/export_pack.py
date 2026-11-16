"""Build the app's region pack: pack.json (species, questions, range grid, model settings) plus model.onnx.

  python export_pack.py --model student --region "Texas and South Central" --dest "../Android/Resources/Raw/pack"
"""
import argparse
import csv
import json
import math
import shutil
from collections import defaultdict
from pathlib import Path

from common import DATA, MEAN, OUT, ROOT, STD

QUESTIONS = [
    {"trait": "arrangement", "text": "Look where the leaves (or buds) join the twig. How are they arranged?",
     "options": [["opposite", "In pairs, directly across from each other"], ["alternate", "Staggered, one at a time"],
                 ["needles", "Needles"], ["scale", "Tiny overlapping scales"]]},
    {"trait": "leaf", "text": "Is each leaf a single blade, or made of many small leaflets?",
     "options": [["simple", "A single blade"], ["pinnate", "One row of leaflets on each side of a stem"],
                 ["bipinnate", "Feathery: leaflets on branching stems"], ["needle", "Needles"], ["scale", "Tiny scales"]]},
    {"trait": "lobed", "text": "Do the leaves have deep lobes, like fingers or a star?",
     "options": [["yes", "Yes, deeply lobed"], ["no", "No, the edge is smooth or toothed"]]},
    {"trait": "evergreen", "text": "Does it keep green leaves all winter?",
     "options": [["yes", "Yes, green all year"], ["no", "No, it goes bare"]]},
    {"trait": "thorns", "text": "Are there thorns or spines on the twigs?",
     "options": [["yes", "Yes"], ["no", "No"]]},
    {"trait": "leafsize", "text": "How long are most of the leaves?",
     "options": [["small", "Under 2 inches"], ["large", "3 inches or longer"]]},
    {"trait": "nut", "text": "What do the nuts or their husks look like?",
     "options": [["splits", "Husk splits open into 4 sections"], ["round", "Round green ball that doesn't split"]]},
    {"trait": "trunks", "text": "Does it have one main trunk, or several from the base?",
     "options": [["one", "One straight trunk"], ["many", "Several trunks from the base"]]},
    {"trait": "bark", "text": "What does the bark look like?",
     "options": [["smooth", "Smooth, peeling in colored patches"], ["blocky", "Dark, broken into small square blocks"]]},
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--model", default="student")
    ap.add_argument("--region", default="Texas and South Central")
    ap.add_argument("--dest", required=True)
    a = ap.parse_args()

    labels = (OUT / "labels.txt").read_text().split("\n")
    info = json.loads((ROOT / "species_info.json").read_text(encoding="utf-8"))
    missing = [s for s in labels if s not in info]
    assert not missing, f"species_info.json is missing {missing}"

    # Raw observation counts per 1-degree cell; the app smooths over neighboring cells
    counts = defaultdict(lambda: [0] * len(labels))
    idx = {s: i for i, s in enumerate(labels)}
    seen = set()
    for r in csv.DictReader(open(DATA / "photos_tagged.csv", encoding="utf-8")):
        if not r["lat"] or r["obs_id"] in seen:
            continue
        seen.add(r["obs_id"])
        key = f"{math.floor(float(r['lat']))},{math.floor(float(r['lng']))}"
        counts[key][idx[r["species"]]] += 1

    import torch
    size = torch.load(OUT / f"{a.model}.pt", weights_only=False, map_location="cpu")["size"]
    pack = {
        "region": a.region, "version": 1, "model": "model.onnx", "inputSize": size,
        "resizeRatio": 1.14, "mean": MEAN, "std": STD, "priorWeight": 0.5,
        "species": [{"scientific": s, **info[s]} for s in labels],
        "questions": QUESTIONS,
        "range": dict(counts),
    }
    # Real observation counts per cell for rarity (fetch_abundance.py); training counts are capped per species
    abundance_file = ROOT / "abundance.json"
    if abundance_file.exists():
        ab = json.loads(abundance_file.read_text())
        assert ab["labels"] == labels, "abundance.json was made for a different species list"
        pack["abundance"] = ab["cells"]
    dest = Path(a.dest); dest.mkdir(parents=True, exist_ok=True)
    (dest / "pack.json").write_text(json.dumps(pack, ensure_ascii=False, separators=(",", ":")), encoding="utf-8")
    shutil.copy(OUT / f"{a.model}_fp32.onnx", dest / "model.onnx")
    print(f"pack: {len(labels)} species, {len(counts)} range cells -> {dest}")


if __name__ == "__main__":
    main()
