"""Train one model, optionally distilling from a saved teacher.

  python train.py --name baseline --arch tf_efficientnet_lite0 --size 256 --epochs 25
  python train.py --name teacher  --arch convnext_base.fb_in22k_ft_in1k_384 --size 320 --epochs 15 --lr 1e-4 --batch 32
  python train.py --name student  --arch tf_efficientnet_lite0 --size 256 --epochs 40 --teacher teacher

Saves out/<name>.pt, out/<name>_test_logp.npy and out/<name>_report.md.
"""
import argparse

import numpy as np
import timm
import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data import DataLoader

from common import OUT, Photos, evaluate, load_split, test_tf, train_tf


def predict(model, rows, cls, size, batch=128):
    dl = DataLoader(Photos(rows, cls, test_tf(size)), batch_size=batch, num_workers=8)
    out = []
    model.eval()
    with torch.no_grad(), torch.autocast("cuda"):
        for x, _ in dl:
            x = x.cuda()
            lp = model(x).float().log_softmax(-1) + model(x.flip(3)).float().log_softmax(-1)
            out.append((lp / 2).cpu().numpy())
    return np.concatenate(out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--name", required=True)
    ap.add_argument("--arch", required=True)
    ap.add_argument("--size", type=int, default=256)
    ap.add_argument("--epochs", type=int, default=25)
    ap.add_argument("--lr", type=float, default=1e-3)
    ap.add_argument("--batch", type=int, default=64)
    ap.add_argument("--teacher", default=None)
    ap.add_argument("--kd_t", type=float, default=2.0)
    ap.add_argument("--kd_w", type=float, default=0.7)
    a = ap.parse_args()
    torch.manual_seed(7)

    species, cls, train, test = load_split()
    print(f"{a.name}: {len(species)} species, {len(train)} train, {len(test)} test")

    teacher, t_size = None, a.size
    if a.teacher:
        ck = torch.load(OUT / f"{a.teacher}.pt", weights_only=False)
        teacher = timm.create_model(ck["arch"], num_classes=len(species))
        teacher.load_state_dict(ck["state"]); teacher.cuda().eval()
        t_size = ck["size"]

    # Load at the larger of the two sizes; the student gets a resized copy
    load_size = max(a.size, t_size)
    dl = DataLoader(Photos(train, cls, train_tf(load_size)), batch_size=a.batch, shuffle=True,
                    num_workers=8, pin_memory=True, persistent_workers=True, drop_last=True)
    extra = {"drop_path_rate": 0.1} if "convnext" in a.arch else {}
    model = timm.create_model(a.arch, pretrained=True, num_classes=len(species), **extra).cuda()
    opt = torch.optim.AdamW(model.parameters(), lr=a.lr, weight_decay=0.05)
    sched = torch.optim.lr_scheduler.OneCycleLR(opt, max_lr=a.lr, total_steps=a.epochs * len(dl), pct_start=0.1)
    ce = nn.CrossEntropyLoss(label_smoothing=0.1)
    scaler = torch.amp.GradScaler()

    for ep in range(a.epochs):
        model.train(); tot = 0.0
        for x, y in dl:
            x, y = x.cuda(non_blocking=True), y.cuda(non_blocking=True)
            xs = x if load_size == a.size else F.interpolate(x, size=a.size, mode="bilinear", antialias=True)
            with torch.autocast("cuda"):
                logits = model(xs)
                loss = ce(logits, y)
                if teacher is not None:
                    with torch.no_grad():
                        xt = x if load_size == t_size else F.interpolate(x, size=t_size, mode="bilinear", antialias=True)
                        tl = teacher(xt).float()
                    kd = F.kl_div(F.log_softmax(logits.float() / a.kd_t, -1), F.softmax(tl / a.kd_t, -1),
                                  reduction="batchmean") * a.kd_t ** 2
                    loss = (1 - a.kd_w) * loss + a.kd_w * kd
            opt.zero_grad(set_to_none=True)
            scaler.scale(loss).backward(); scaler.step(opt); scaler.update(); sched.step()
            tot += loss.item()
        print(f"epoch {ep + 1}/{a.epochs} loss {tot / len(dl):.3f}", flush=True)

    torch.save({"arch": a.arch, "size": a.size, "species": species, "state": model.state_dict()}, OUT / f"{a.name}.pt")
    logp = predict(model, test, cls, a.size)
    np.save(OUT / f"{a.name}_test_logp.npy", logp)
    summary, md = evaluate(f"{a.name} ({a.arch}, {a.size}px, {a.epochs} epochs"
                           f"{', distilled from ' + a.teacher if a.teacher else ''})", logp, train, test, species, cls)
    (OUT / f"{a.name}_report.md").write_text(md, encoding="utf-8")
    print(md)


if __name__ == "__main__":
    main()
