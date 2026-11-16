### baseline (tf_efficientnet_lite0, 256px, 40 epochs)

#### Single photo

| Slice | Items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |
|---|---|---|---|---|---|
| all | 7276 | 82.6% | 92.7% | 84.2% | 93.9% |
| part:leaf | 3257 | 91.7% | 97.9% | 92.5% | 98.2% |
| part:bark | 512 | 62.3% | 81.4% | 66.8% | 82.8% |
| part:buds_twig | 740 | 68.6% | 83.9% | 72.7% | 87.7% |
| part:fruit_flower | 1360 | 90.1% | 96.3% | 90.5% | 97.0% |
| part:whole | 1407 | 69.0% | 86.0% | 71.4% | 88.1% |
| with leaves | 6464 | 84.1% | 93.6% | 85.3% | 94.3% |
| leafless | 812 | 70.9% | 86.0% | 75.6% | 90.1% |

#### Whole observation (photos fused)

| Slice | Items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |
|---|---|---|---|---|---|
| all | 4368 | 91.1% | 96.5% | 92.1% | 97.0% |
| 1 photo | 2449 | 89.6% | 95.5% | 90.8% | 96.3% |
| 2 photos | 930 | 92.6% | 97.6% | 93.3% | 97.8% |
| 3 photos | 989 | 93.2% | 98.0% | 94.1% | 98.1% |
| with leaves | 3973 | 91.9% | 97.0% | 92.7% | 97.4% |
| leafless | 395 | 82.3% | 91.6% | 86.1% | 93.2% |

#### Top confusions (single photo)

| True | Predicted | Count |
|---|---|---|
| Juniperus ashei | Juniperus virginiana | 32 |
| Juniperus virginiana | Juniperus ashei | 26 |
| Juglans nigra | Carya illinoinensis | 20 |
| Quercus macrocarpa | Quercus stellata | 17 |
| Juglans nigra | Fraxinus pennsylvanica | 15 |
| Carya illinoinensis | Juglans nigra | 13 |
| Quercus virginiana | Quercus nigra | 12 |
| Quercus stellata | Quercus macrocarpa | 12 |
| Fraxinus pennsylvanica | Juglans nigra | 12 |
| Ulmus crassifolia | Ulmus americana | 11 |