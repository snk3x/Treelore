### student (tf_efficientnet_lite0, 256px, 40 epochs, distilled from teacher)

#### Single photo

| Slice | Items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |
|---|---|---|---|---|---|
| all | 7276 | 82.6% | 92.7% | 84.0% | 93.9% |
| part:leaf | 3257 | 91.4% | 97.7% | 92.0% | 98.2% |
| part:bark | 512 | 63.3% | 82.6% | 66.6% | 84.4% |
| part:buds_twig | 740 | 69.3% | 84.6% | 72.8% | 88.0% |
| part:fruit_flower | 1360 | 89.9% | 96.0% | 90.6% | 96.5% |
| part:whole | 1407 | 69.1% | 85.7% | 71.2% | 88.1% |
| with leaves | 6464 | 84.1% | 93.4% | 85.1% | 94.3% |
| leafless | 812 | 70.6% | 86.9% | 75.1% | 90.4% |

#### Whole observation (photos fused)

| Slice | Items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |
|---|---|---|---|---|---|
| all | 4368 | 90.7% | 96.8% | 91.5% | 97.3% |
| 1 photo | 2449 | 89.1% | 95.9% | 89.9% | 96.7% |
| 2 photos | 930 | 92.6% | 98.1% | 93.2% | 98.3% |
| 3 photos | 989 | 93.0% | 97.9% | 93.7% | 98.1% |
| with leaves | 3973 | 91.5% | 97.1% | 92.0% | 97.6% |
| leafless | 395 | 83.0% | 93.7% | 86.3% | 95.2% |

#### Top confusions (single photo)

| True | Predicted | Count |
|---|---|---|
| Juniperus ashei | Juniperus virginiana | 41 |
| Quercus macrocarpa | Quercus stellata | 26 |
| Juniperus virginiana | Juniperus ashei | 22 |
| Carya illinoinensis | Juglans nigra | 18 |
| Juglans nigra | Carya illinoinensis | 16 |
| Fraxinus pennsylvanica | Juglans nigra | 13 |
| Quercus stellata | Quercus macrocarpa | 12 |
| Ulmus crassifolia | Ulmus americana | 12 |
| Juglans nigra | Fraxinus pennsylvanica | 12 |
| Quercus stellata | Quercus virginiana | 11 |