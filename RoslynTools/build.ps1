[CmdletBinding()]
Param(
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

Write-Output "PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

###########################################################################
# CONFIGURATION
###########################################################################

$BuildProjectFile = "$PSScriptRoot\build\_build.csproj"

$DotNetGlobalFile = "$PSScriptRoot\\global.json"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1

###########################################################################
# EXECUTION
###########################################################################

function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# If global.json exists, load expected version
if (Test-Path $DotNetGlobalFile) {
    $DotNetGlobal = $(Get-Content $DotNetGlobalFile | Out-String | ConvertFrom-Json)
    if ($DotNetGlobal.PSObject.Properties["sdk"] -and $DotNetGlobal.sdk.PSObject.Properties["version"]) {
        $DotNetVersion = $DotNetGlobal.sdk.version
    }
}

$isDotnetInstalled = $null -ne (Get-Command "dotnet" -ErrorAction SilentlyContinue) -and `
    (!(Test-Path variable:DotNetVersion) -or $(& dotnet --version) -eq $DotNetVersion)

# If dotnet is installed locally, and expected version is not set or installation matches the expected version
if ($isDotnetInstalled) {
    $env:DOTNET_EXE = (Get-Command "dotnet").Path
}
else {
    $AppData = $env:LocalAppData
    $env:DOTNET_EXE = "$AppData\Microsoft\dotnet\dotnet.exe"

    $installCommand = "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -NoPath"

    if (Test-Path variable:DotNetVersion) {
        $installCommand = "$installCommand -Version $DotNetVersion"
    }

    ExecSafe { &powershell -NoProfile -ExecutionPolicy unrestricted -Command $installCommand }
}

if (-Not $isDotnetInstalled -and -Not (Test-Path $env:DOTNET_EXE)) {
    Write-Error 'Failed to download dotnet sdk!!!'
    return
}

Write-Output "Microsoft (R) .NET Core SDK version $(& $env:DOTNET_EXE --version)"

ExecSafe { & $env:DOTNET_EXE build $BuildProjectFile /nodeReuse:false -nologo -clp:NoSummary --verbosity quiet -o "$PSScriptRoot\build\bin\Debug\" }
ExecSafe { & $env:DOTNET_EXE "$PSScriptRoot\build\bin\Debug\_build.dll" $BuildArguments }
