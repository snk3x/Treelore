"""Tag each photo with the plant part it shows, using zero-shot CLIP.

iNaturalist has no "which part" label, so this gives the evaluation a way to report
accuracy separately for leaf, bark, buds and twig, fruit or flower, and whole-tree shots.
Reads data/photos.csv, writes data/photos_tagged.csv.
"""
import csv
import os
from pathlib import Path

import open_clip
import torch
from PIL import Image
from tqdm import tqdm

DATA = Path(os.environ.get("TREE_DATA", Path.home() / "Tree ID data"))

PROMPTS = {
    "leaf": ["a close-up photo of a tree leaf", "a photo of green leaves on a branch",
             "a photo of pine needles", "a photo of a single leaf held in a hand"],
    "bark": ["a close-up photo of tree bark", "a photo of a tree trunk"],
    "buds_twig": ["a close-up photo of a bare winter twig with buds", "a photo of bare branches"],
    "fruit_flower": ["a photo of a flower on a tree", "a photo of fruit or seeds",
                     "a photo of acorns or nuts", "a photo of a pine cone or berries"],
    "whole": ["a photo of a whole tree", "a landscape photo with a tree in it"],
}


def main():
    dev = "cuda"
    model, _, preprocess = open_clip.create_model_and_transforms(
        "ViT-B-32", pretrained="laion2b_s34b_b79k", device=dev)
    tok = open_clip.get_tokenizer("ViT-B-32")
    parts = list(PROMPTS)
    with torch.no_grad():
        text_feats = []
        for p in parts:
            f = model.encode_text(tok(PROMPTS[p]).to(dev))
            f = f / f.norm(dim=-1, keepdim=True)
            m = f.mean(0)
            text_feats.append(m / m.norm())
        text = torch.stack(text_feats)

    rows = list(csv.DictReader(open(DATA / "photos.csv", encoding="utf-8")))
    batch = 128
    for i in tqdm(range(0, len(rows), batch)):
        chunk = rows[i:i + batch]
        imgs = torch.stack([preprocess(Image.open(DATA / r["path"]).convert("RGB")) for r in chunk]).to(dev)
        with torch.no_grad(), torch.autocast("cuda"):
            f = model.encode_image(imgs)
            f = f / f.norm(dim=-1, keepdim=True)
            probs = (100 * f.float() @ text.T.float()).softmax(-1)
        for r, pr in zip(chunk, probs):
            k = int(pr.argmax())
            r["part"] = parts[k]
            r["part_conf"] = f"{float(pr[k]):.2f}"

    with open(DATA / "photos_tagged.csv", "w", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)
    from collections import Counter
    print(Counter(r["part"] for r in rows))


if __name__ == "__main__":
    main()
