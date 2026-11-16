# Tree ID

An Android app that identifies trees from photos, fully offline. Photograph a leaf, the bark, buds, fruit or the whole tree; the app combines the photos, uses your location to favor trees that grow nearby, and asks a quick question when two species look alike.

The first region pack covers 30 common trees of Texas and the South Central US.

## Accuracy

Measured on 7,276 held-out iNaturalist photos (4,368 sightings) the app never saw in training:

| | Top-1 | Top-3 |
|---|---|---|
| Single photo | 82% | 93% |
| One sighting, all photos combined, with location | 91% | 97% |
| Sighting with 3 photos | 93% | 98% |

Leaves, fruit and flowers work best. Bark alone is the hardest (about 63% top-1).

## Privacy

Everything runs on the phone. The app has no internet permission, so photos and locations never leave the device.

## Projects

| Folder | What it is |
|---|---|
| `Android` | .NET MAUI app for Android 7.0 and newer (arm and arm64) |
| `Core` | Shared logic: region pack loading, image preprocessing, ONNX model, photo fusion, location prior, follow-up questions |
| `Eval` | Windows console tool that runs `Core` on the test photos and checks the question logic |
| `training` | Python pipeline: download photos, tag plant parts, train, export the model and region pack |

## Building the app

Requires the .NET 10 SDK with the `maui-android` workload.

```
cd Android
dotnet build -c Release
adb install --no-incremental bin/Release/net10.0-android/com.snk3x.treeid-Signed.apk
```

After changing `AndroidManifest.xml`, delete `bin` and `obj` before rebuilding. An incremental build can produce an APK that crashes on launch.

## Training

Requires Python 3.12 and an NVIDIA GPU.

```
cd training
python -m venv .venv
.venv\Scripts\pip install torch torchvision --index-url https://download.pytorch.org/whl/cu128
.venv\Scripts\pip install timm open_clip_torch onnx onnxruntime requests pillow tqdm scikit-learn
.venv\Scripts\python download_inat.py
powershell -File run_all.ps1
.venv\Scripts\python export_pack.py --dest ..\Android\Resources\Raw\pack
```

Photos are stored outside the repo in `%USERPROFILE%\Tree ID data` (override with `TREE_DATA`). Species names, traits, look-alikes and safety notes are in `training/species_info.json`.

## Credits

The model is trained on research-grade photos shared by iNaturalist observers under CC0, CC BY and CC BY-NC licenses. Per-photo attribution is recorded in `photos.csv` by the download script.

## Safety

Never eat, or use as medicine, any plant based on this app's answer.
