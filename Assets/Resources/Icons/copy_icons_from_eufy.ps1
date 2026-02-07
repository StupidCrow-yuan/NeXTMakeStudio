# Copy icons from eufyMake Studio to this project (run from project root or Resources/Icons)
# Source: D:\eufyMake Studio\resources\icons
$src = "D:\eufyMake Studio\resources\icons"
$dst = $PSScriptRoot
if (-not (Test-Path $src)) { Write-Host "Source not found: $src"; exit 1 }
$maps = @(
    @{ Name = "Upload";    File = "add_file.svg" },
    @{ Name = "ImageAI";  File = "add_model.png" },
    @{ Name = "Projects"; File = "light\edit_project_light.png" },
    @{ Name = "Templates"; File = "light\canvas_normal.svg" },
    @{ Name = "Textures"; File = "light\brush_item_normal.png" },
    @{ Name = "Text";     File = "toolbar_text.svg" },
    @{ Name = "Elements"; File = "light\add_normal.svg" }
)
foreach ($m in $maps) {
    $path = Join-Path $src $m.File
    if (Test-Path $path) {
        $ext = [System.IO.Path]::GetExtension($m.File)
        Copy-Item $path (Join-Path $dst ($m.Name + $ext)) -Force
        Write-Host "Copied: $($m.Name)$ext"
    }
}
