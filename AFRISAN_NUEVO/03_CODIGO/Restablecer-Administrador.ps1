[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$api=Join-Path $PSScriptRoot 'Afrisan.Nuevo.Api\Afrisan.Nuevo.Api.csproj'
& dotnet build $api --nologo -m:1 -p:UseSharedCompilation=false
if($LASTEXITCODE -ne 0){throw 'No se pudo compilar la API. Si Visual Studio la está ejecutando, detén la depuración y vuelve a intentarlo.'}
# Pide el correo de un administrador activo y la nueva contraseña en entrada oculta.
& dotnet run --project $api --no-build --launch-profile http -- --restablecer-administrador
if($LASTEXITCODE -ne 0){throw 'No se restableció la contraseña. Revisa el resultado anterior.'}
