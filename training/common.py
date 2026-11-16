"""Shared data loading, split, evaluation and report code for the training scripts."""
import csv
import math
import os
import random
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np
from PIL import Image
from torch.utils.data import Dataset
from torchvision import transforms as T

ROOT = Path(__file__).resolve().parent
DATA = Path(os.environ.get("TREE_DATA", Path.home() / "Tree ID data"))
OUT = ROOT / "out"
OUT.mkdir(exist_ok=True)
MEAN, STD = (0.485, 0.456, 0.406), (0.229, 0.224, 0.225)
PARTS = ["leaf", "bark", "buds_twig", "fruit_flower", "whole"]


def train_tf(size):
    return T.Compose([
        T.RandomResizedCrop(size, scale=(0.3, 1.0)), T.RandomHorizontalFlip(),
        T.TrivialAugmentWide(), T.ToTensor(), T.Normalize(MEAN, STD), T.RandomErasing(0.2)])


def test_tf(size):
    return T.Compose([T.Resize(int(size * 1.14)), T.CenterCrop(size), T.ToTensor(), T.Normalize(MEAN, STD)])


class Photos(Dataset):
    def __init__(self, rows, classes, tf):
        self.rows, self.cls, self.tf = rows, classes, tf

    def __len__(self):
        return len(self.rows)

    def __getitem__(self, i):
        r = self.rows[i]
        return self.tf(Image.open(DATA / r["path"]).convert("RGB")), self.cls[r["species"]]


def load_split(seed=7):
    """Observation-grouped 80/20 split, saved so every model uses the same test set."""
    rows = list(csv.DictReader(open(DATA / "photos_tagged.csv", encoding="utf-8")))
    species = sorted({r["species"] for r in rows})
    split_file = OUT / "test_obs.txt"
    if split_file.exists():
        test_obs = set(split_file.read_text().split())
    else:
        obs = sorted({r["obs_id"] for r in rows})
        random.Random(seed).shuffle(obs)
        test_obs = set(obs[: len(obs) // 5])
        split_file.write_text("\n".join(sorted(test_obs)))
    train = [r for r in rows if r["obs_id"] not in test_obs]
    test = [r for r in rows if r["obs_id"] in test_obs]
    return species, {s: i for i, s in enumerate(species)}, train, test


def _rank(lp, y):
    return int((lp > lp[y]).sum())


def _acc(ranks, k):
    return 100.0 * np.mean([r < k for r in ranks]) if ranks else float("nan")


def location_prior(train, species):
    grid = defaultdict(Counter)
    cell = lambda r: (math.floor(float(r["lat"])), math.floor(float(r["lng"]))) if r["lat"] else None
    for r in train:
        c = cell(r)
        if c:
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    grid[(c[0] + dy, c[1] + dx)][r["species"]] += 1

    def prior(r):
        c = cell(r)
        if not c or not grid[c]:
            return np.zeros(len(species))
        p = np.array([grid[c][s] + 1.0 for s in species])
        return np.log(p / p.sum())
    return prior


def evaluate(name, logps, train, test, species, cls):
    """Returns a dict of metrics and a markdown section."""
    prior = location_prior(train, species)
    single, single_p, fused, fused_p = (defaultdict(list) for _ in range(4))
    by_obs = defaultdict(list)
    conf = Counter()
    for r, lp in zip(test, logps):
        y = cls[r["species"]]
        rk, rkp = _rank(lp, y), _rank(lp + 0.5 * prior(r), y)
        for k in ["all", f"part:{r['part']}", "leafless" if r["leafless"] == "1" else "with leaves"]:
            single[k].append(rk); single_p[k].append(rkp)
        by_obs[r["obs_id"]].append((r, lp))
        if rk:
            conf[(r["species"], species[int(lp.argmax())])] += 1
    for items in by_obs.values():
        r0 = items[0][0]; y = cls[r0["species"]]
        s = sum(lp for _, lp in items)
        n = "1 photo" if len(items) == 1 else f"{min(len(items), 3)} photos"
        for k in ["all", n, "leafless" if r0["leafless"] == "1" else "with leaves"]:
            fused[k].append(_rank(s, y)); fused_p[k].append(_rank(s + 0.5 * prior(r0), y))

    def table(title, d, dp, order):
        lines = [f"#### {title}", "", "| Slice | Items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |",
                 "|---|---|---|---|---|---|"]
        for k in order:
            if k in d:
                lines.append(f"| {k} | {len(d[k])} | {_acc(d[k], 1):.1f}% | {_acc(d[k], 3):.1f}% | "
                             f"{_acc(dp[k], 1):.1f}% | {_acc(dp[k], 3):.1f}% |")
        return "\n".join(lines)

    md = [f"### {name}", "",
          table("Single photo", single, single_p, ["all"] + [f"part:{p}" for p in PARTS] + ["with leaves", "leafless"]),
          "", table("Whole observation (photos fused)", fused, fused_p,
                    ["all", "1 photo", "2 photos", "3 photos", "with leaves", "leafless"]),
          "", "#### Top confusions (single photo)", "", "| True | Predicted | Count |", "|---|---|---|"]
    md += [f"| {a} | {b} | {n} |" for (a, b), n in conf.most_common(10)]
    summary = {k: _acc(v, 1) for k, v in single.items()}
    summary.update({f"fused {k}": _acc(v, 1) for k, v in fused.items()})
    return summary, "\n".join(md)
