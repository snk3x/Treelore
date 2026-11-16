# Full scale-up run: tag, baseline, teacher, distilled student, export.
$ErrorActionPreference = "Continue"
$env:HF_HUB_DISABLE_IMPLICIT_TOKEN = 1
$py = ".\.venv\Scripts\python.exe"
function Step($name, [string[]]$cmd) {
    "=== $name $(Get-Date -Format T)" | Tee-Object -FilePath run_all.log -Append
    & $py @cmd *>> run_all.log
    if ($LASTEXITCODE -ne 0) { "FAILED: $name" | Tee-Object -FilePath run_all.log -Append; exit 1 }
}
Step "tag" @("tag_parts.py")
Step "baseline" @("train.py", "--name", "baseline", "--arch", "tf_efficientnet_lite0", "--size", "256", "--epochs", "40")
Step "teacher" @("train.py", "--name", "teacher", "--arch", "convnext_base.fb_in22k_ft_in1k_384", "--size", "320",
                 "--epochs", "15", "--lr", "1e-4", "--batch", "32")
Step "student" @("train.py", "--name", "student", "--arch", "tf_efficientnet_lite0", "--size", "256", "--epochs", "40",
                 "--teacher", "teacher")
Step "export" @("export_onnx.py", "--name", "student")
"=== done $(Get-Date -Format T)" | Tee-Object -FilePath run_all.log -Append
