# Tree ID feasibility results

30 species, 7327 training photos, 1838 test photos (1061 test observations). Model tf_efficientnet_lite0 at 224px, 15 epochs.

### Single photo

| Slice | Test items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |
|---|---|---|---|---|---|
| all | 1838 | 61.6% | 79.2% | 65.1% | 81.8% |
| part:leaf | 699 | 71.1% | 88.4% | 74.2% | 90.8% |
| part:bark | 141 | 44.0% | 66.0% | 46.8% | 67.4% |
| part:buds_twig | 225 | 52.4% | 71.1% | 57.8% | 76.4% |
| part:fruit_flower | 359 | 74.1% | 87.5% | 79.1% | 88.6% |
| part:whole | 414 | 45.7% | 65.2% | 47.6% | 68.6% |
| with leaves | 1542 | 61.7% | 79.8% | 64.7% | 82.0% |
| leafless | 296 | 61.1% | 75.7% | 66.9% | 80.7% |

### Whole observation (all photos fused)

| Slice | Test items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |
|---|---|---|---|---|---|
| all | 1061 | 73.9% | 87.9% | 75.4% | 89.2% |
| 1 photo | 560 | 71.6% | 86.2% | 74.3% | 88.4% |
| 2 photos | 225 | 70.7% | 84.9% | 71.1% | 85.8% |
| 3 photos | 276 | 81.2% | 93.8% | 81.2% | 93.5% |
| with leaves | 903 | 74.0% | 88.6% | 75.4% | 89.6% |
| leafless | 158 | 73.4% | 84.2% | 75.3% | 86.7% |

### Most common confusions (single photo)

| True species | Predicted as | Count |
|---|---|---|
| Juniperus ashei | Juniperus virginiana | 20 |
| Juniperus virginiana | Juniperus ashei | 14 |
| Quercus macrocarpa | Quercus stellata | 10 |
| Maclura pomifera | Celtis laevigata | 8 |
| Quercus stellata | Quercus macrocarpa | 7 |
| Ulmus americana | Ulmus crassifolia | 7 |
| Juglans nigra | Carya illinoinensis | 7 |
| Quercus nigra | Quercus stellata | 6 |
| Ulmus crassifolia | Quercus virginiana | 6 |
| Taxodium distichum | Juniperus virginiana | 6 |
| Celtis laevigata | Fraxinus pennsylvanica | 5 |
| Carya illinoinensis | Juglans nigra | 5 |

### Exported model

| File | Size | CPU time, 2 threads (desktop) |
|---|---|---|
| tree_feasibility_fp32.onnx | 13.7 MB | 5.2 ms |
| tree_feasibility_int8.onnx | 3.7 MB | 38.6 ms |