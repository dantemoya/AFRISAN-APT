# Publica la web AFRISAN en el App Service «afrisan» (https://afrisan.azurewebsites.net), que comparte el
# plan B1 de la API. Requiere Azure CLI con sesión iniciada y 7-Zip. Ver DESPLIEGUE.md §6.
param(
    [string]$Api = "https://afrisan-api-dm.azurewebsites.net/api/v1/",
    [string]$Grupo = "rg-afrisan",
    [string]$App = "afrisan",
    [string]$Salida = (Join-Path $env:TEMP "afrisan-web-appservice")
)
$ErrorActionPreference = "Stop"
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$az = "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
$sieteZip = "C:\Program Files\7-Zip\7z.exe"
$proyecto = Join-Path $PSScriptRoot "..\Afrisan.Nuevo.WebHost"

# Ejecuta un programa y falla solo por su código de salida. Con "Stop", PowerShell 5.1 trataría como error
# cualquier aviso escrito en stderr (az escribe WARNING aunque la publicación resulte bien).
function Invoke-Programa([string]$Programa, [string[]]$Argumentos, [string]$Paso) {
    $anterior = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try { & $Programa @Argumentos 2>&1 | ForEach-Object { "$_" } } finally { $ErrorActionPreference = $anterior }
    if ($LASTEXITCODE -ne 0) { throw "Falló $Paso (código $LASTEXITCODE)." }
}

if (Test-Path $Salida) { Remove-Item $Salida -Recurse -Force }
# Autocontenida, como la API: la imagen de App Service ya no trae .NET 8.
Invoke-Programa $dotnet @("publish", $proyecto, "-c", "Release", "-r", "linux-x64", "--self-contained", "true", "-o", "$Salida\app", "-nologo") "dotnet publish"

# Solo la copia publicada apunta a la API de Azure; el repositorio sigue en localhost para las pruebas locales.
$www = Join-Path $Salida "app\wwwroot"
$ajustes = Join-Path $www "appsettings.json"
$utf8 = New-Object Text.UTF8Encoding $false
[IO.File]::WriteAllText($ajustes, "{`n  `"ApiBaseUrl`": `"$Api`"`n}`n", $utf8)
# Sin copias comprimidas viejas de los archivos cambiados: el servidor las preferiría.
foreach ($archivo in @($ajustes, (Join-Path $www "service-worker-assets.js"))) {
    Remove-Item "$archivo.br", "$archivo.gz" -ErrorAction SilentlyContinue
}
# El service worker guarda cada archivo con su hash: se actualiza el de appsettings.json.
$sha = [Security.Cryptography.SHA256]::Create()
$hash = "sha256-" + [Convert]::ToBase64String($sha.ComputeHash([IO.File]::ReadAllBytes($ajustes)))
$manifiesto = Join-Path $www "service-worker-assets.js"
$texto = [IO.File]::ReadAllText($manifiesto)
$patron = '("hash":\s*")sha256-[^"]+(",\s*"url":\s*"appsettings\.json")'
if (-not [regex]::IsMatch($texto, $patron)) { throw "No está appsettings.json en service-worker-assets.js." }
$texto = [regex]::Replace($texto, $patron, { param($m) $m.Groups[1].Value + $hash + $m.Groups[2].Value })
[IO.File]::WriteAllText($manifiesto, $texto, $utf8)

$zip = Join-Path $Salida "web.zip"
Push-Location (Join-Path $Salida "app")
try { Invoke-Programa $sieteZip @("a", "-tzip", $zip, "*") "7-Zip" | Out-Null } finally { Pop-Location }
# az espera a que la aplicación arranque con el paquete nuevo; puede tardar varios minutos.
Invoke-Programa $az @("webapp", "deploy", "-g", $Grupo, "-n", $App, "--src-path", $zip, "--type", "zip") "az webapp deploy"
Write-Host "Publicada en https://$App.azurewebsites.net"
