"""Export a trained model to ONNX, quantize to int8 (static, QDQ, per-channel) and check accuracy.

  python export_onnx.py --name student
Writes out/<name>_fp32.onnx, out/<name>_int8.onnx, out/labels.txt and out/<name>_export.md.
"""
import argparse
import random
import time

import numpy as np
import onnxruntime as ort
import timm
import torch
from onnxruntime.quantization import (CalibrationDataReader, CalibrationMethod, QuantFormat, QuantType,
                                      quantize_static)
from onnxruntime.quantization.shape_inference import quant_pre_process

from common import OUT, Photos, evaluate, load_split, test_tf


class Calib(CalibrationDataReader):
    def __init__(self, ds, n=300):
        idx = random.Random(0).sample(range(len(ds)), n)
        self.items = iter([ds[i][0].unsqueeze(0).numpy() for i in idx])

    def get_next(self):
        x = next(self.items, None)
        return None if x is None else {"image": x}


def run(path, ds, threads=2):
    so = ort.SessionOptions(); so.intra_op_num_threads = threads
    sess = ort.InferenceSession(str(path), so, providers=["CPUExecutionProvider"])
    out = []
    for i in range(len(ds)):
        x = ds[i][0].unsqueeze(0).numpy()
        lg = sess.run(None, {"image": x})[0][0]
        lg = lg - lg.max()
        out.append(lg - np.log(np.exp(lg).sum()))
    x = ds[0][0].unsqueeze(0).numpy()
    t = time.perf_counter()
    for _ in range(30):
        sess.run(None, {"image": x})
    return np.stack(out), (time.perf_counter() - t) / 30 * 1000


def main():
    ap = argparse.ArgumentParser(); ap.add_argument("--name", required=True); a = ap.parse_args()
    species, cls, train, test = load_split()
    ck = torch.load(OUT / f"{a.name}.pt", weights_only=False)
    model = timm.create_model(ck["arch"], num_classes=len(species), exportable=True)
    model.load_state_dict(ck["state"]); model.eval()
    size = ck["size"]

    fp32, pre, int8 = OUT / f"{a.name}_fp32.onnx", OUT / f"{a.name}_pre.onnx", OUT / f"{a.name}_int8.onnx"
    torch.onnx.export(model, torch.randn(1, 3, size, size), fp32, input_names=["image"],
                      output_names=["logits"], opset_version=17, dynamo=False)
    quant_pre_process(str(fp32), str(pre))
    calib_ds = Photos(train, cls, test_tf(size))
    quantize_static(str(pre), str(int8), Calib(calib_ds), quant_format=QuantFormat.QDQ, per_channel=True,
                    activation_type=QuantType.QUInt8, weight_type=QuantType.QInt8,
                    calibrate_method=CalibrationMethod.Percentile, extra_options={"CalibPercentile": 99.99})
    pre.unlink()
    (OUT / "labels.txt").write_text("\n".join(species))

    ds = Photos(test, cls, test_tf(size))
    md = [f"### ONNX export: {a.name}", "", "| File | Size | Single-photo top-1 | Fused top-1 | CPU ms, 2 threads |",
          "|---|---|---|---|---|"]
    for f in (fp32, int8):
        logp, ms = run(f, ds)
        s, _ = evaluate(f.name, logp, train, test, species, cls)
        md.append(f"| {f.name} | {f.stat().st_size / 1e6:.1f} MB | {s['all']:.1f}% | {s['fused all']:.1f}% | {ms:.1f} |")
    (OUT / f"{a.name}_export.md").write_text("\n".join(md), encoding="utf-8")
    print("\n".join(md))


if __name__ == "__main__":
    main()
