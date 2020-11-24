[CmdletBinding()]
param (
	[Parameter()][switch] $SkipTests = $false,
	[Parameter()][switch] $SkipBackend = $false,
	[Parameter()][switch] $SkipFrontend = $false,
	[Parameter()][switch] $FullClean = $false,
	[Parameter()][string] $Config = "Release"
)

<#
Configurable build script options:

Full Build: 		PS> .\build.ps1
Debug Build: 		PS> .\build.ps1 -Config "Debug"
Fast Build: 		PS> .\build.ps1 -SkipTests
API/Infra Build: 	PS> .\build.ps1 -SkipFrontend
UI Client Build:  	PS> .\build.ps1 -SkipBackend
#####Add full git clean  PS> .\build.ps1 -FullClean      (Watch out - this will HARD reset your working copy so you will lose uncommitted changes)
#>

Import-Module -Name $PSScriptRoot\build-functions.psm1 -Force

$project = "PaymentGateway"
$packageId = "Checkout"
$publishedFolder = "$ReleaseDir\$project"

####$angularConfigName = "config.deploy.json"

try {
	Install-Octo
	Invoke-CleanCore -Config $config -FullClean:$FullClean

	# ==== Back End ==== #
	if (-not $SkipBackend) {
		# Restore Dependencies
		Restore -UseDotNet

		# Compile the app
		Invoke-BuildCore -Config $config -VersionSuffix $NugetVersionSuffix

		# Run tests
		Invoke-TestCore -ProjectNameFilter "*FunctionalTests" -Config $config -CoverletExcludes "[*.*Tests?]*", "[LazyCache*]*" -Skip:$SkipTests 
		Invoke-TestCore -ProjectNameFilter "*IntegrationTests" -Config $config -CoverletExcludes "[*.*Tests?]*", "[LazyCache*]*" -Skip:$SkipTests 
		Invoke-TestCore -ProjectNameFilter "*UnitTest*" -Config $config -CoverletExcludes "[*.*Tests?]*", "[LazyCache*]*" -Skip:$SkipTests

		# publish web api project, zip it up and remove the unzipped output. Use a child files folder ready for deployment.
		Invoke-PublishCore -Project $project -Config $config
		New-Item -ItemType Directory -Force -Path $publishedFolder\files | Out-Null
		
        #Get-ChildItem -Path "$publishedFolder\*" -Exclude "deploy.ps1" | Move-Item -Destination "$publishedFolder\files" | Out-Null
		New-ZipPackage -id "$packageId.$project.Api" -version $BuildNumber -FolderToZip $publishedFolder -outputFolder $ReleaseDir -RemoveFolder
	}
}
finally {
	ReportResults
}

