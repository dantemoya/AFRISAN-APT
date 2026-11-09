[CmdletBinding()]
param([switch]$OmitirDependencias)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$deps = Join-Path $root '.local\validacion_sql_contexto'
$flags = @('AFRISAN_TEST_USUARIOS','AFRISAN_TEST_POWERBI','AFRISAN_TEST_RECTIFICACIONES','AFRISAN_TEST_LOCAL','AFRISAN_TEST_STORAGE','AFRISAN_TEST_MANTENEDORES','AFRISAN_TEST_CONTROL')
$uiPrevia = [Environment]::GetEnvironmentVariable('AFRISAN_TEST_MANTENEDORES_UI','Process')
$controlUiPrevia = [Environment]::GetEnvironmentVariable('AFRISAN_TEST_CONTROL_UI','Process')
$previos = @{}
foreach ($flag in $flags) { $previos[$flag] = [Environment]::GetEnvironmentVariable($flag,'Process') }
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        $env:PATH = (Join-Path $env:ProgramFiles 'dotnet') + ';' + $env:PATH
    }
    if (-not $OmitirDependencias) {
        New-Item -ItemType Directory -Path $deps -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'package.json'),(Join-Path $PSScriptRoot 'package-lock.json') -Destination $deps
        if (Get-Command npm -ErrorAction SilentlyContinue) { & npm ci --prefix $deps }
        elseif (Test-Path (Join-Path $root '.local\npm\package\bin\npm-cli.js')) { & node (Join-Path $root '.local\npm\package\bin\npm-cli.js') ci --prefix $deps }
        else { throw 'Se requiere Node.js con npm para instalar las dependencias de prueba.' }
        if ($LASTEXITCODE -ne 0) { throw 'Falló npm ci.' }
        & node (Join-Path $deps 'node_modules\playwright\cli.js') install chromium
        if ($LASTEXITCODE -ne 0) { throw 'Falló la preparación de Chromium.' }
    }
    & node AFRISAN_NUEVO/03_CODIGO/PowerBI/generar_informe.mjs
    if ($LASTEXITCODE -ne 0) { throw 'Falló la generación del proyecto Power BI.' }
    & dotnet build AFRISAN.slnx --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación.' }
    & dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Web -c Release -o .local/web-offline --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación local de la Web.' }
    & dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Web -c Release -o .local/powerbi-demo-web --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación de la prueba Power BI gratuito.' }
    & dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Web -c Release -o .local/web-mantenedores --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación del ensayo de mantenedores y bajas.' }
    & dotnet publish AFRISAN_NUEVO/03_CODIGO/Afrisan.Nuevo.Web -c Release -o .local/web-control --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falló la publicación del ensayo de costos y límites.' }
    & dotnet build AFRISAN_NUEVO/04_SEGUIMIENTO/STORAGE_2026_11_02/Migracion.Pruebas/Migracion.Pruebas.csproj -o .local/storage-tests --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del ensayo de migración de Storage.' }
    foreach ($flag in $flags) { [Environment]::SetEnvironmentVariable($flag,$null,'Process') }
    foreach ($flag in $flags) {
        [Environment]::SetEnvironmentVariable($flag,'1','Process')
        [Environment]::SetEnvironmentVariable('AFRISAN_TEST_MANTENEDORES_UI',$(if($flag -eq 'AFRISAN_TEST_MANTENEDORES'){'1'}else{$null}),'Process')
        [Environment]::SetEnvironmentVariable('AFRISAN_TEST_CONTROL_UI',$(if($flag -eq 'AFRISAN_TEST_CONTROL'){'1'}else{$null}),'Process')
        & node AFRISAN_NUEVO/04_SEGUIMIENTO/PERSISTENCIA_2026_11_02/validar_api.mjs
        if ($LASTEXITCODE -ne 0) { throw "Falló la suite $flag." }
        [Environment]::SetEnvironmentVariable($flag,$null,'Process')
    }
    & node AFRISAN_NUEVO/04_SEGUIMIENTO/MODULOS_2026_11_08/validar_pbip.mjs
    if ($LASTEXITCODE -ne 0) { throw 'Falló la validación de esquemas PBIP/PBIR.' }
    & node AFRISAN_NUEVO/04_SEGUIMIENTO/MODULOS_2026_11_08/validar_powerbi_gratuito.mjs
    if ($LASTEXITCODE -ne 0) { throw 'Falló la integración del módulo Power BI gratuito.' }
    & dotnet run --project (Join-Path $PSScriptRoot 'Modelo.Pruebas\Validar.csproj') -- (Join-Path $root 'AFRISAN_NUEVO\03_CODIGO\PowerBI\AFRISAN.SemanticModel\model.bim') (Join-Path $PSScriptRoot 'resultados_modelo.json')
    if ($LASTEXITCODE -ne 0) { throw 'Falló la lectura TMSL por Microsoft Analysis Services.' }
    Write-Output 'Validación completa. No se usaron datos ni credenciales de Supabase.'
}
finally {
    [Environment]::SetEnvironmentVariable('AFRISAN_TEST_MANTENEDORES_UI',$uiPrevia,'Process')
    [Environment]::SetEnvironmentVariable('AFRISAN_TEST_CONTROL_UI',$controlUiPrevia,'Process')
    foreach ($flag in $flags) { [Environment]::SetEnvironmentVariable($flag,$previos[$flag],'Process') }
    Pop-Location
}
