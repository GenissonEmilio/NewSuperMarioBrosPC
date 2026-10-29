param([string]$DotnetPath='dotnet',[string]$Version='v0.1.0-alpha.1',[string]$ToolchainPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$release=Join-Path $root "artifacts/$Version"
$stage=Join-Path $root ('artifacts/staging-'+[Guid]::NewGuid().ToString('N'))
$app=Join-Path $stage 'MarioPC'
$zip=Join-Path $release "NewSuperMarioBrosPC-$Version-windows-x64.zip"
if(Test-Path $zip){throw 'Esse ZIP já existe. Escolha outra versão ou preserve o pacote anterior.'}
New-Item -ItemType Directory -Path $release,$app -Force | Out-Null
& $DotnetPath publish (Join-Path $root 'src/MarioPC/MarioPC.csproj') -c Release -r win-x64 --self-contained true -o $app '-p:DebugType=None' '-p:DebugSymbols=false' "-p:PathMap=$root=/src"
if($LASTEXITCODE -ne 0){throw 'Falha no publish.'}
Copy-Item (Join-Path $root 'core/melonds/melonds_libretro.dll') $app
Copy-Item (Join-Path $root 'README.md'),(Join-Path $root 'LICENSE'),(Join-Path $root 'THIRD-PARTY.md'),(Join-Path $root 'RELEASE_NOTES.md') $app
$licensePath=Join-Path $app 'licenses';New-Item -ItemType Directory -Path $licensePath -Force | Out-Null
Copy-Item (Join-Path $root 'core/melonds/LICENSE') (Join-Path $licensePath 'melonDS-GPL-3.0.txt')
$nugetRoot=(& $DotnetPath nuget locals global-packages --list) -replace '^global-packages:\s*',''
foreach($package in @('microsoft.netcore.app.runtime.win-x64','microsoft.windowsdesktop.app.runtime.win-x64')){
    $pkgRoot=Join-Path $nugetRoot $package
    $pkg=Get-ChildItem -LiteralPath $pkgRoot -Directory | Sort-Object LastWriteTime | Select-Object -Last 1
    if(!$pkg){throw "Pacote de runtime ausente: $package"}
    Get-ChildItem -LiteralPath $pkg.FullName -File | Where-Object Name -Match 'LICENSE|NOTICE' | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $licensePath ($package+'-'+$_.Name))
    }
    if($package -eq 'microsoft.netcore.app.runtime.win-x64'){Copy-Item (Join-Path $pkg.FullName 'LICENSE.TXT') (Join-Path $licensePath 'dotnet-LICENSE.txt')}
}
if(!$ToolchainPath){throw 'Informe -ToolchainPath para incluir os avisos de runtime da toolchain.'}
Copy-Item (Join-Path $ToolchainPath 'COPYING.MinGW-w64-runtime.txt') $licensePath
Copy-Item (Join-Path $root 'licenses/GCC-RUNTIME-LIBRARY-EXCEPTION.txt') $licensePath
@'
NEW SUPER MARIO BROS. PC - ALPHA PARA WINDOWS X64

Extraia a pasta inteira. Abra MarioPC.exe e importe sua ROM .nds ou ZIP.
Esta versão suporta A2DE (USA/Australia), revisão 0.
WASD: mover; Espaço: pular; Shift: correr/disparar; E: item reserva.
M ou botão Mapa: mapa/progresso; Esc: menu PC; Enter: confirmar; Backspace: voltar;
P: Start/pause original; F11: tela cheia. Todas as teclas podem ser remapeadas.
No menu PC, abra Controles ou Configurações. Use o mouse ou W/S + Enter.
ROM e saves ficam em %LOCALAPPDATA%\GenissonEmilio\NewSuperMarioBrosPC.

Uma adaptação com núcleo de emulação melonDS. Não é um port nativo.
Não inclui ROM, BIOS da Nintendo, saves ou arte extraída do jogo.
O runtime .NET acompanha o pacote; nenhuma instalação adicional é necessária.
Leia README.md e RELEASE_NOTES.md para limitações e testes.
'@ | Set-Content -LiteralPath (Join-Path $app 'LEIA-ME.txt') -Encoding utf8
$manifest=[ordered]@{
    Version=$Version;Platform='windows-x64';SourceCommit=(& git -C $root rev-parse HEAD).Trim()
    CoreCommit=(& git -C (Join-Path $root 'core/melonds') rev-parse HEAD).Trim();GameDataIncluded=$false
    Files=@(Get-ChildItem -LiteralPath $app -Recurse -File | Sort-Object FullName | ForEach-Object {
        @{Path=[IO.Path]::GetRelativePath($app,$_.FullName).Replace('\','/');Bytes=$_.Length;Sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
    })
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $app 'package-manifest.json') -Encoding utf8
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage,$zip,[IO.Compression.CompressionLevel]::Optimal,$false)
& python (Join-Path $root 'scripts/audit-package.py') $zip --output (Join-Path $release 'package-audit.json')
if($LASTEXITCODE -ne 0){throw 'Falha na auditoria. Não publique o pacote.'}

# GPL corresponding source: frontend and the exact complete core tree, including vendored dependencies.
$sourceParent=Join-Path $root ('artifacts/source-'+[Guid]::NewGuid().ToString('N'))
$source=Join-Path $sourceParent 'NewSuperMarioBrosPC-Source';New-Item -ItemType Directory -Path $source -Force | Out-Null
& git -C $root archive -o (Join-Path $sourceParent 'frontend.tar') HEAD
if($LASTEXITCODE -ne 0){throw 'Falha ao arquivar o frontend.'}
& tar -xf (Join-Path $sourceParent 'frontend.tar') -C $source
& git -C (Join-Path $root 'core/melonds') archive -o (Join-Path $sourceParent 'core.tar') HEAD
New-Item -ItemType Directory -Path (Join-Path $source 'core/melonds') -Force | Out-Null
& tar -xf (Join-Path $sourceParent 'core.tar') -C (Join-Path $source 'core/melonds')
$sourceZip=Join-Path $release "NewSuperMarioBrosPC-$Version-source.zip"
# Only include the source directory, not the temporary TAR files.
[IO.Compression.ZipFile]::CreateFromDirectory($source,$sourceZip,[IO.Compression.CompressionLevel]::Optimal,$false)
$sums=@($zip,$sourceZip) | ForEach-Object {(Get-FileHash $_ -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+[IO.Path]::GetFileName($_)}
$sums | Set-Content (Join-Path $release 'SHA256SUMS.txt') -Encoding ascii
Write-Output "Pacote auditado: $zip"
