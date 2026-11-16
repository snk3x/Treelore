# Treelore

An Android app that identifies trees from photos, fully offline. Photograph a leaf, the bark, buds, fruit or the whole tree; the app combines the photos, uses your location to favor trees that grow nearby, and asks a quick question when two species look alike.

The first region pack covers 30 common trees of Texas and the South Central US.

## Tree book (for kids)

Every tree in the pack starts as a mystery shadow in the **Tree book**. Identifying one reveals its card with the player's own photo and earns **leaves**:

- New species: 10 (common), 25 (uncommon) or 50 (rare) leaves; rarity is local, based on how often the tree is actually observed nearby
- Finding a species again: 2 leaves; photographing 3 different parts: +5
- Ranks from Seedling to Forest Ranger, and badges such as Oak Squad, Winter Detective, Bark Reader, Invader Spotter and Safety Smart
- Three weekly challenges worth 15 leaves each, built from the pack's traits and look-alike pairs ("Find a tree whose leaves grow in pairs", "Find both pecan and black walnut"); winter weeks skip challenges that need leaves

Kid mode (on by default, in About) uses simpler words and bigger buttons and turns on the Tree book. Turning it off asks a quick multiplication question for grown-ups.

Only answers the app is sure about (85% or more, after any follow-up question) count, so kids don't learn wrong names. Poison warnings always show before the celebration. There are no accounts, maps, sharing, ads or purchases.

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

## Releasing

Release APKs are signed with the owner's Treelore key, kept outside the repo at `%USERPROFILE%\Treelore signing\treelore.keystore`. Run this in a terminal, type the keystore password when asked, and the signed APK lands in `Android\release\Treelore.apk`:

```
powershell -ExecutionPolicy Bypass -File .\Android\build-release.ps1
```

The script does a clean build, keeps the password only in memory, and stops if the APK came out signed with the debug key. Signing certificate SHA-256: `405516c603e86d37cb37c2b659734677804e747877f086bfedc3703b5ee884d3`.

## Training

Requires Python 3.12 and an NVIDIA GPU.

```
cd training
python -m venv .venv
.venv\Scripts\pip install torch torchvision --index-url https://download.pytorch.org/whl/cu128
.venv\Scripts\pip install timm open_clip_torch onnx onnxruntime requests pillow tqdm scikit-learn
.venv\Scripts\python download_inat.py
powershell -File run_all.ps1
.venv\Scripts\python fetch_abundance.py
.venv\Scripts\python export_pack.py --dest ..\Android\Resources\Raw\pack
```

Photos are stored outside the repo in `%USERPROFILE%\Tree ID data` (override with `TREE_DATA`). `fetch_abundance.py` saves real observation counts per area (wild and planted) to `abundance.json` for Tree book rarity; escarpment live oak is counted as live oak. Species names, traits, look-alikes and safety notes are in `training/species_info.json`.

## Credits

The model is trained on research-grade photos shared by iNaturalist observers under CC0, CC BY and CC BY-NC licenses. Per-photo attribution is recorded in `photos.csv` by the download script.

## Safety

Never eat, or use as medicine, any plant based on this app's answer.
