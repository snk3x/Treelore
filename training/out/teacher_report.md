### teacher (convnext_base.fb_in22k_ft_in1k_384, 320px, 15 epochs)

#### Single photo

| Slice | Items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |
|---|---|---|---|---|---|
| all | 7276 | 91.7% | 97.2% | 92.5% | 97.4% |
| part:leaf | 3257 | 96.8% | 99.3% | 97.1% | 99.4% |
| part:bark | 512 | 77.1% | 91.2% | 79.3% | 92.0% |
| part:buds_twig | 740 | 85.5% | 94.6% | 88.5% | 95.1% |
| part:fruit_flower | 1360 | 95.7% | 99.0% | 96.0% | 98.7% |
| part:whole | 1407 | 84.6% | 94.2% | 85.4% | 94.7% |
| with leaves | 6464 | 92.7% | 97.5% | 93.1% | 97.6% |
| leafless | 812 | 84.0% | 95.2% | 88.1% | 96.4% |

#### Whole observation (photos fused)

| Slice | Items | Top-1 | Top-3 | Top-1 with location | Top-3 with location |
|---|---|---|---|---|---|
| all | 4368 | 96.5% | 99.1% | 96.7% | 99.3% |
| 1 photo | 2449 | 96.1% | 98.9% | 96.4% | 99.1% |
| 2 photos | 930 | 96.9% | 99.2% | 97.1% | 99.4% |
| 3 photos | 989 | 97.1% | 99.4% | 97.3% | 99.7% |
| with leaves | 3973 | 96.7% | 99.2% | 96.9% | 99.4% |
| leafless | 395 | 94.4% | 98.2% | 94.9% | 98.5% |

#### Top confusions (single photo)

| True | Predicted | Count |
|---|---|---|
| Quercus stellata | Quercus macrocarpa | 18 |
| Juniperus ashei | Juniperus virginiana | 17 |
| Juniperus virginiana | Juniperus ashei | 17 |
| Carya illinoinensis | Juglans nigra | 13 |
| Juglans nigra | Carya illinoinensis | 11 |
| Quercus macrocarpa | Quercus stellata | 10 |
| Juglans nigra | Fraxinus pennsylvanica | 7 |
| Quercus nigra | Quercus shumardii | 6 |
| Juglans nigra | Diospyros virginiana | 6 |
| Liquidambar styraciflua | Quercus macrocarpa | 6 |