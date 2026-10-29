param([string]$ToolchainPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if($ToolchainPath){$env:PATH=(Join-Path $ToolchainPath 'bin')+';'+$env:PATH}
$source=Join-Path $root 'core/melonds'
if(!(Test-Path (Join-Path $source 'Makefile'))){throw 'Execute git submodule update --init antes de compilar.'}
Push-Location $source
try {
    & make.exe -j4 platform=win ARCH=x86_64 JIT_ARCH=x64 HAVE_OPENGL=0 HAVE_THREADS=1 'LDFLAGS=-static -lws2_32 -lwinmm'
    if($LASTEXITCODE -ne 0){throw 'Falha na compilação do melonDS.'}
}finally{Pop-Location}
