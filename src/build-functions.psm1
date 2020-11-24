<# 
    Note: Requires Powershell 2 or above

    # What does this script do? Provides functions that do the following

        * Cleans the directory ..\dist\ for your build output to be placed into
        * Restores nuget dependencies with the Restore command
        * Compiles with msbuild the sln file in the same directory as this script using the configured version of msbuild and sensible defaults
        * OctoPacks any projects with octopack installed included to the release directory
        * Nuget packs any projects containing a nuspec file
        * Executes any NUnit tests identified based on naming convention
        * Logs build and tests outputs to files for you
        * Throws errors if Build() or Test() fail
        * Reports progress to the console and teamcity nicely

    # How to use this library to create your own build script?

	1. Place this script in same folder as your solutions .sln file
	2. Install-Package NUnit.ConsoleRunner in your tests project
	3. If you are using octopus deploy then Install-Package OctoPack in each project to be deployed (db, website etc)
	3. Copy the following into a new file build.ps1 and place that file alongside this file
	*************************************** build.ps1 ************************************************
		Import-Module "$PSScriptRoot\build-functions.psm1" -Force

		# Execute the build!

		$config = "Release"
		Invoke-Clean -Config $config
		# OR for dotnet core change to the line below
		# Invoke-CleanCore -Config $config

		Restore
		# OR for dotnet core change to the line below
		# Restore -UseDotNet
		
		Invoke-Build -Config $config
		# OR for dotnet core change to the line below
		# Invoke-BuildCore -Config $config

		Invoke-TestNunit -Assembly "*UnitTests.dll" -NUnitConsole "nunit3-console.exe" -Config $config
		# OR for dotnet core change to the line below
		# Invoke-TestCore -ProjectNameFilter "*UnitTest*" -Config $config

		Invoke-DependencyCheck
		
		Pack -Config $config

		# for dotnet core you need to publish each project
   		# Invoke-PublishCore -Project MyProject -Config $config

		ReportResults

		Publish-TCArtifacts

	***************************************    Ends    ************************************************
	4. Execute the script with powershell PS> .\build.ps1
#> 

Set-StrictMode -Version latest

 # Configurable variables default values (override in your build script):
	 $OutputDirectory     = "..\dist"
	 $InfoMessageColour   = "Magenta"
	 $ToolsPath 			 = "\tools\dotnet"
 
 # Build and test functions
 
 
	 # Taken from psake https://github.com/psake/psake
	 <#
	 .SYNOPSIS
	 This is a helper function that runs a scriptblock and checks the PS variable $lastexitcode
	 to see if an error occcured. If an error is detected then an exception is thrown.
	 This function allows you to run command-line programs without having to
	 explicitly check the $lastexitcode variable.
	 .EXAMPLE
	 Invoke-Exec { svn info $repository_trunk } "Error executing SVN. Please verify SVN command-line client is installed"
	 #>
	 Function Invoke-Exec
	 {
		 [CmdletBinding()]
		 param(
			 [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd
		 )
		 $scriptExpanded =  $ExecutionContext.InvokeCommand.ExpandString($cmd).Trim().Trim("&")
		 InfoMessage "Executing command: $scriptExpanded"
 
		 & $cmd | Out-Default
 
		 if ($lastexitcode -ne 0) {
			 throw ("Non-zero exit code '$lastexitcode' detected from command: '$scriptExpanded'")
		 }
	 }
 
	 # pretty print messages
	 Function InfoMessage ([string] $message)
	 { 
		 Write-Host "$message`n" -ForegroundColor $infoMessageColour
	 }
 
	 Function WarnMessage ([string] $message)
	 { 
		 Write-Host "Warning: $message`n" -ForegroundColor "Yellow"
	 }
	 
	 Function Invoke-BuildTask (
		 [string] $Name,
		 [bool] $Skip = $false,
		 [int] $Retries = 0,
		 [int] $SecondsBetweenRetries = 0,
		 [scriptblock] $Command)
	 { 
		$taskName = $Name.Replace("'", "|'").Trim();
		$taskStopWatch = [Diagnostics.Stopwatch]::StartNew()
		$success = $false
		$result = $null
		if((Get-BuildTasksSummary).TasksFailed -gt 0) { $Skip = $true; }

		
		Write-Host "TASK: '$taskName' $(if($skip) { "SKIPPED" } else { "Started" })" -ForegroundColor "Blue"

		try {
			# Only run the task if all other preceeding have succeeded
			if(-not($Skip)) {

				# Run the task - will fail the task if throws or return $false otherwise is a success
				# optional retry feature to catch transient errors (e.g. npm audit is flaky)
				$result = Invoke-Retry -Retries:$Retries -SecondsDelay:$SecondsBetweenRetries -Cmd { 
					& $command
				}

				if($result -ne $false){
					$success = $true
				}
			}
		}
		catch {
			$success = $false
			Write-Error $PSItem
		}
		finally {			
			$_ = $taskStopWatch.Stop()
			$duration = $taskStopWatch.Elapsed.TotalSeconds

			# Recored the task result and timings in the task log
			$_ = $buildTasks.Add([PSCustomObject]@{
				BuildTask = $Name
				Skipped = $Skip
				Succeeded = $success
				Duration = [math]::Round($duration,1)
				Percentage = ""
			})
		 			
			if(-not($Skip)) {
				$duration = Get-PrettyDuration $taskStopWatch.Elapsed
				Write-Host "`TASK: '$taskName' $(if($success){ "succeeded" } else { "FAILED" }) in $duration`n" -ForegroundColor "Blue"				
			}
		}
		return $result
	}

	Function Get-BuildTasksSummary {
		[array] $failed = @($buildTasks | Where-Object {-not $_.Succeeded -and -not $_.Skipped })
		[array] $succeeded =  @($buildTasks | Where-Object { $_.Succeeded })
		[array] $skipped = @($buildTasks | Where-Object { $_.Skipped -and -not $_.Succeeded })
		[bool] $allSuccessful = $succeeded.Count -gt 0 -and $failed.Count -eq 0
		$firstFailure = if($failed.Count -gt 0) { $failed[0].BuildTask } else { $null }

		$totalDurationInSecs = if($buildTasks) { ($buildTasks | Select-Object Duration | Measure-Object -Property Duration -Sum).Sum } else { 1 }

		foreach($task in $buildTasks)
		{
			$task.Percentage = ($task.Duration / $totalDurationInSecs).ToString("P0")
		}

		return [PSCustomObject]@{
			TasksCount = $buildTasks.Count
			TasksFailed = $failed.Count
			TasksSkipped = $skipped.Count
			TasksSucceded = $succeeded.Count
			Succeeded = $allSuccessful
			FirstFailure = $firstFailure
		}
	}

	<#
	.SYNOPSIS
	Execute a script block more than once after a sleep if it fails. Useful for catching and retrying transient failures
	
	.EXAMPLE
	Invoke-Retry -Retries 3 -SecondsDelay 1 -Cmd {
		if(((Get-Random -Maximum 2) % 2) -eq 0) { throw "random transient error" }
		Write-Output "hello"
	}
	#>
	Function Invoke-Retry
	{
	param(
		[Parameter(Position = 0,Mandatory = $true)] [scriptblock]$Cmd,
		[Parameter(Mandatory = $false)] [int] $Retries = 3,
		[Parameter(Mandatory = $false)] [int] $SecondsDelay = 1
	)
		$retrycount = 0

		while ($True) {
			try {

				$completed = & $cmd
				if ($completed -eq $false)
				{
					throw
				}

				if ($retrycount -gt 0) {
					Write-Host "Retry attempt $retrycount/$retries succeeded" -ForegroundColor "Green"
				}
				return $completed
			} 
			catch 
			{
				if ($retrycount -eq $retries) {
					if($retrycount -gt 0) {
						Write-Host "Retry attempt $retrycount/$retries failed" -ForegroundColor "Red"
					}
					throw
				} 
				Write-Error $PSItem
				$retrycount++
				Write-Host "An error occured. Starting retry attempt $retrycount/$retries after $secondsDelay second(s) delay" -ForegroundColor "Red"
				Start-Sleep $secondsDelay
			}
		}
	}
	 
	 # finds the most recent file below this directory matching a pattern
	 Function GetMostRecentFileMatchingPath([string] $FilePathPattern, [string] $Project= "$PSScriptRoot", [switch] $IgnoreError) #e.g GetMostRecentFileMatchingPath("*.sln")
	 {
		 $file = Get-ChildItem -Path $Project -Recurse -Filter $filePathPattern | Sort-Object LastWriteTime | Select-Object -last 1
		 if($file -eq "" -or $file -eq $null){
			 if(!$IgnoreError){
			 throw "Unable to find a file in $SolutionFolder (or below) matching $filePathPattern"
			 }
			 return $null;
		 }
		 return $file
	 }
 
	 # find all the unique folders that contain a file matching a specification
	 Function GetFoldersContainingFilesThatMatch([string] $FilePattern, [string] $ExcludePattern)
	 {
		 $items = Get-ChildItem -Filter $FilePattern -Recurse `
			 | Select-Object -expandproperty FullName `
			 | Get-Unique `
			 | Where { $_ -NotMatch $ExcludePattern } `
			 | foreach { Split-Path $_ -Parent } 
 
		 return $items
	 }
 
	 Function Clean([string] $MSBuild = "${Env:ProgramFiles(x86)}\MSBuild\14.0\Bin\MSBuild.exe", [string] $Config = "Release")
	 {
	     WarnMessage "'Clean' function is deprecated and will be removed - use 'Invoke-Clean' or 'Invoke-CleanCore' instead"
	     Invoke-Clean -MSBuild $MSBuild -Config $Config
	 }
 
	 Function Invoke-Clean([string] $MSBuild = "(auto)", [string] $Config = "Release") 
	 {
		 if($MSBuild -eq "(auto)")
		 {
			 $MSBuild = Find-MsBuild
		 }		 
		 
		 InfoMessage "Clean step: Emptying $ReleaseDir"
		 $_ = Remove-Item -path $ReleaseDir\* -recurse -force -ErrorAction silentlycontinue
 
		 $MsBuildCleanArgs = $PathToSln, "/t:clean", "/m", "/p:Configuration=$Config", "/noconsolelogger"
		 InfoMessage "MsBuild Clean: `n  $MsBuild $MsBuildCleanArgs"
		 & $MsBuild $MsBuildCleanArgs
	 }

	Function Invoke-CleanCore(
		[string] $BuildCommand ="dotnet", 
		[string] $BuildArguments ="clean", 
		[string] $Config = "Release",
		[switch] $FullClean = $False) 
	{
		Invoke-BuildTask -Name "clean" -command {
			Write-Host "Empty release folder: Emptying $ReleaseDir"
			$_ = Remove-Item -path $ReleaseDir\* -recurse -force -ErrorAction silentlycontinue

			if($FullClean)
			{
				Write-Host "`nDoing a HARD git clean - you have 5s to cancel with CTRL-C otherwise all changes will be lost !!!`n" -ForegroundColor "RED"

				Start-Sleep -Seconds 5

				Invoke-Exec {
					git clean -xdf
				}
			} 
			else 
			{
				$FinalMsBuildArgs = $BuildArguments, $PathToSln, "--configuration", $Config, "--nologo", "--verbosity", "quiet"

				Invoke-Exec {
					& $BuildCommand $FinalMsBuildArgs			
				}
			}
		}
	}
 
	 # Try and find a recent version of msbuild
	 Function Find-MsBuild([int] $MaxVersion = 2017)
	 {
		 $agentPath = "$Env:programfiles (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\msbuild.exe"
		 $devPath = "$Env:programfiles (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\msbuild.exe"
		 $proPath = "$Env:programfiles (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\msbuild.exe"
		 $communityPath = "$Env:programfiles (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\msbuild.exe"
		 $fallback2015Path = "${Env:ProgramFiles(x86)}\MSBuild\14.0\Bin\MSBuild.exe"
		 $fallback2013Path = "${Env:ProgramFiles(x86)}\MSBuild\12.0\Bin\MSBuild.exe"
		 $fallbackPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319"
		 
		 If ((2017 -le $MaxVersion) -And (Test-Path $agentPath)) { return $agentPath } 
		 If ((2017 -le $MaxVersion) -And (Test-Path $devPath)) { return $devPath } 
		 If ((2017 -le $MaxVersion) -And (Test-Path $proPath)) { return $proPath } 
		 If ((2017 -le $MaxVersion) -And (Test-Path $communityPath)) { return $communityPath } 
		 If ((2015 -le $MaxVersion) -And (Test-Path $fallback2015Path)) { return $fallback2015Path } 
		 If ((2013 -le $MaxVersion) -And (Test-Path $fallback2013Path)) { return $fallback2013Path } 
		 If (Test-Path $fallbackPath) { return $fallbackPath } 
		 
		 throw "Yikes - Unable to find msbuild"
	 }
 
	 # Return a path to nuget.exe. Tries the following locations in order:
	 # - find it in child folders such as Nuget.CommandLine/octopack packages folders
	 # - see if it is available on the path
	 # - try and download it from nuget.org to the packages folder 
	 # - throw error if it cannot be found
	 Function Find-Nuget([string] $Executable = "nuget")
	 {
		 $localNugetPath = (GetMostRecentFileMatchingPath $Executable -IgnoreError)
		 $NugetExePath = If($localNugetPath -ne $null) { $localNugetPath.FullName } Else {"nuget"}
 
		 # cant find it locally or path? then download
		 if ((Get-Command $NugetExePath -ErrorAction SilentlyContinue) -eq $null) 
		 { 
			 $sourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
			 $NugetExePath = "$SolutionFolder\packages\nuget.exe"
 
			 WarnMessage "Unable to find nuget.exe in your PATH or in your project. Trying to get it from the web! Downloading $sourceNugetExe to $NugetExePath"
 
			 mkdir "packages" -Force | Out-Null
			 $result = Invoke-WebRequest $sourceNugetExe -OutFile $NugetExePath -PassThru -UseBasicParsing
 
			 if($result.StatusCode -ne 200) { 
				 throw "Unable to download nuget from the web so Run 'Install-Package Nuget.CommandLine' to fix"
			 } else {
				 InfoMessage "Downloaded nuget to $NugetExePath"
			 }
		 }
 
		 return $NugetExePath
	 }
 
	 Function Restore(
		 [switch] $UseDotnet,
		 [string] $ToolsManifestFile = ".config\dotnet-tools.json"
		 )
	 {
		
		if($UseDotnet) {
			$NugetExePath = "dotnet"
		} else {
			$NugetExePath = Find-Nuget
		}

		Invoke-BuildTask -Name "$NugetExePath restore" -command {
			$NugetArgs =  "restore", $PathToSln
						
			Invoke-Exec {
				& $NugetExePath $NugetArgs
			}
		}

		# Restore dotnet tools if manifest file is discovered
		if(Test-Path $ToolsManifestFile){
			Invoke-BuildTask -Name "dotnet tool restore" -command {
				InfoMessage "Restoring dotnet tools added in tool config file"
				invoke-exec { dotnet tool restore }
				}
		}
	 }
	 
	 # use msbuild to compile the sln file
	 # to find msbuild in 2017+ consider using vswhere, see https://github.com/Microsoft/vswhere/wiki/Find-MSBuild
	 # or use %programfiles(x86)%\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin on agents and C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin on dev
	 Function Build([string] $Config = "Release", [string] $MSBuild = "(auto)", [string] $MsBuildArgs ="") 
	 {
		 WarnMessage "'Build' function is deprecated and will be removed - use 'Invoke-Build' or 'Invoke-BuildCore' instead"
 
		 Invoke-Build -Config $config -MsBuild $MsBuild -MsBuildArgs $MsBuildArgs
	 }	
 
	 Function Invoke-Build([string] $MSBuild = "(auto)", [string] $MsBuildArgs ="", [string] $Config = "Release") 
	 {
		 if($MSBuild -eq "(auto)"){
			 $MSBuild = Find-MsBuild
		 }
 
		 InfoMessage "Build: Compiling $PathToSln in $Config configuration"
		 InfoMessage "Log file: $LogFile"
 
		 $UseOctopack = $false
		 If((GetMostRecentFileMatchingPath "OctoPack.Tasks.dll" -IgnoreError) -ne $null){
			 InfoMessage "Detected you have Octopack installed - Adding RunOctoPack=true to MsBuild parameters for automatic packing"
			 $UseOctopack = $true
		 }
 
		 $OctopackMsbuildParams = If($UseOctopack) { "/p:RunOctoPack=true;OctoPackPublishPackageToFileShare=$ReleaseDir;OctoPackPublishPackagesToTeamCity=false" } else { "" }
	 
		 $FinalMsBuildArgs = $MsBuildArgs, $PathToSln, "/p:Configuration=$config", "/p:OutputPath=$ReleaseDir\$ProjName", $OctopackMsbuildParams, "/t:build", "/noautorsp", "/ds", "/m", "/l:FileLogger,Microsoft.Build.Engine;logfile=$LogFile"
 
		 Invoke-Exec {
		 	& $MsBuild $FinalMsBuildArgs
		 }
	 }
 
	 Function Invoke-BuildCore([string] $BuildCommand ="dotnet", [string] $BuildArguments ="build", [string] $Config = "Release", [string] $VersionSuffix = "") 
	 {
		Invoke-BuildTask -Name "dotnet build" -command {
			$buildVersion = $BuildNumber + $VersionSuffix
			If($VersionSuffix -ne "")
			{
				WarnMessage "Building solution with pre-release version $buildVersion"
			}
			Write-Host "Using $BuildCommand $BuildArguments to compile $PathToSln in $Config configuration with version number $buildVersion"
			Write-Host  "Log file: $LogFile"
	
			$FinalMsBuildArgs = $BuildArguments, $PathToSln, "--no-restore", "--configuration", $Config, "/p:AssemblyVersion=$BuildNumber", "/p:Version=$buildVersion", "/flp:logfile=$LogFile"
	
			Invoke-Exec {
				& $BuildCommand $FinalMsBuildArgs
			}
		}
	 }

	 Function Invoke-PackCore(
		[string] $Project, 
		[string] $Config = "Release", 
		[string] $OutputPath = "$ReleaseDir",
		[string] $VersionSuffix = "") 
	 {
		Invoke-PublishCore -Project $Project -BuildCommand dotnet -BuildArguments pack -Config $Config -OutputPath $OutputPath -VersionSuffix $VersionSuffix
	 }
 
	 # Execute 'dotnet publish' on a project and place the output and log in the release distibution folder
	 # E.g Invoke-Publish -Project MyApp
	 Function Invoke-PublishCore(
		 [string] $Project, 
		 [string] $BuildCommand ="dotnet", 
		 [string] $BuildArguments ="publish", 
		 [string] $Config = "Release",
		 [string] $OutputPath = "$ReleaseDir\$Project",
		 [string] $VersionSuffix = "") 
	 {
		Invoke-BuildTask -Name "dotnet publish" -command {
			$buildVersion = $BuildNumber + $VersionSuffix
			If($VersionSuffix -ne "")
			{
				WarnMessage "Publishing ($BuildCommand $BuildArguments) project $Project with pre-release version $buildVersion in $Config configuration"
			} 
			else
			{
				Write-Host "Publishing ($BuildCommand $BuildArguments) project $Project with version number $buildVersion in $Config configuration"
			}
			$ProjectPublishLog = "$ReleaseDir\$Project-Publish.log"
			$FinalDotnetArgs = $BuildArguments, "$SolutionFolder\$Project", "--output", $OutputPath, "--no-restore", "--configuration", $Config, "/p:AssemblyVersion=$BuildNumber", "/p:Version=$buildVersion", "/flp:logfile=$ProjectPublishLog"

			Invoke-Exec {
			    & $BuildCommand $FinalDotnetArgs
			}

			if(Test-Path $ProjectPublishLog) {
				Get-Content $ProjectPublishLog -OutVariable PublishResult | out-null

				if ($PublishResult.IndexOf("Build succeeded.") -lt 0)
				{
					throw "Publish $Project FAILED! See msbuild log: $ProjectPublishLog"
				} 
				else{
					Write-Host "`nMsBuild publish log file reports success ($ProjectPublishLog)"
				}
			}
		}
	 }
 
	 # execute any nunit tests identified by tests naming convention 
	 Function Test-NUnit([string] $Assembly = "*Tests.dll", [string] $NUnitConsole = "nunit3-console.exe", [string] $Config = "Release") 
	 {
		 WarnMessage "'Test-NUnit' function is deprecated and will be removed - use 'Invoke-TestNunit' for nunit3-console or 'Invoke-TestCore' for dotnet test instead"
		 Invoke-TestNUnit -Assembly $Assembly -NUnitConsole $NUnitConsole -Config $Config
	 }		
 
	 # Run NUnit tests using the nunit console test runner. Finds all tests DLLs matching a pattern and executes the nunit tests
	 # Will report results to Teamcity (if available) and outputs the test result file to the Release directory
	 # TODO: each call should produce different test file outpus ($NunitTestOutput) but at the moment they overwrite each other
	 Function Invoke-TestNUnit([string] $Assembly = "*Tests.dll", [string] $NUnitConsole = "nunit3-console.exe", [string] $Config = "Release") 
	 {
		Invoke-BuildTask -Name "$NUnitConsole $Assembly" -command {
			 $NUnitConsolePath = (GetMostRecentFileMatchingPath $NUnitConsole -IgnoreError).FullName
 
			 if($NUnitConsolePath) {
				 InfoMessage "NUnit tests: Finding tests matching $Assembly in bin folders."
		 
				 # Find tests in child folders (except obj)
				 $TestDlls = Get-ChildItem -Path $PSScriptRoot -Recurse  -Include $Assembly | Select-Object -expandproperty FullName | Where-Object {$_ -NotLike "*\obj\*"} | % { "`"$_`"" }
 
				 if(@($TestDlls).Count -eq 0)  {
					 # Add the tests in the output folders (except obj)
					 $TestDlls += Get-ChildItem -Path $ReleaseDir -Recurse -Include $Assembly | Select-Object -expandproperty FullName | Where-Object {$_ -NotLike "*\obj\*"} | % { "`"$_`"" }
				 }
 
				 $teamcityOption = ""
 
				 $NUnitArgs  =  ("/config:$Config", "/process:Multiple", "--result=TestResult.xml", $teamcityOption)
				 $NUnitArgs += $TestDlls
 
				 InfoMessage "Found $(@($TestDlls).Count) test DLL(s): $TestDlls. Test Output will save to $nunitTestOutput)"
				 InfoMessage "Executing Nunit: `n  $NUnitConsolePath $NUnitArgs     " 
 
				 if(@($TestDlls).Count -eq 0) 
				 {
					 WarnMessage "No tests found!"
				 } else {
					 & $NUnitConsolePath $nunitArgs 2>&1 # redirect stderr to stdout, otherwise a build with muted tests is reported as failed because of the stdout text
					 
					 InfoMessage "Placing test result file at $NunitTestOutput"
					 Move-Item -Path "TestResult.xml" -Destination $NunitTestOutput
				 }
			 } else {
				 InfoMessage "Skipping Test-NUnit() - no nunit console available"
			 }
		 }
	 }
 
	 Function Get-AssemblyNameFromCsProj {
		 param (
			 [string] $ProjectFolder
		 )
		 
		 Try {
			 $csProjFile = (Get-ChildItem $ProjectFolder -Filter *.csproj)[0]
			 $AssemblyName = $csProjFile.BaseName
		 } Catch {
			 Throw "Unable to locate csproj file in '$ProjectFolder'"
		 }
 
		 $CsProjXml = ([xml](Get-Content $csProjFile.FullName)).Project
		 if(Get-Member -inputobject $CsProjXml.PropertyGroup -Name "AssemblyName" -MemberType Properties){
			 $AssemblyName = $CsProjXml.PropertyGroup.AssemblyName
		 }
		 $AssemblyName
	 }
 

	 <#
	 .SYNOPSIS
	 Discovers unit and integration tests and invokes a test CLI tool to execute them. Defaults to `dotnet test`
	 
	 .DESCRIPTION
	 Uses a folder naming convention to discover and execute your unit and integration test projects using a command line test executor such as `dotnet test`. Works well with nunit/xnuit and teamcity. You can use `Invoke-TestCore` in the solution root folder to discover the all the unit test projects automatically, and execute the all the tests using the dotnet CLI `dotnet test`. 
	 
	 By default `Invoke-TestCore` will:
	  * Localte all projects matching the pattern ProjectNameFilter and execute them sequentially
	  * Assume your project has already been restored/compiled (using `dotnet build`)
	  * Use sensible defaults, like using the dotnet test CLI in Release config but allow you to override them
	  * Output a code coverage report to the $ReleaseDir using coverlet
	  * Tell teamcity about your test coverage using Teamcity service messages
	  * Exclude projects named *Tests* from code coverage stats, and makes this configurable.
	
	 .PARAMETER TestCommand
	 The CLI test runner tool to execute. Defaults to `dotnet`
	 
	 .PARAMETER TestArguments
	 The CLI test runner tool arguments to be passed to TestCommand. Defaults to `test`
	 
	 .PARAMETER Config
	 The build configuration, usually Debug or Release. Defaults to Release.
	 
	 .PARAMETER ProjectNameFilter
	 A Get-ChildItem filter to be used to locate the folders that contain test projects. Defaults to *UnitTest* and would match folders named MyApp.MyProj.UnitTests or MyApp.UnitTest
	 
	 .PARAMETER SkipCodeCoverage
	 A bool switch to disable coverlet code coverage stats being generated. Defaults to False.
	 
	 .PARAMETER CoverletExcludes
	  Specify a string to define assembly or project exclusions that are passed to coverlet (https://github.com/tonerdo/coverlet). 
	  Defaults to "[*.*Tests?]*" which excludes assembly names containing 'Test' or 'Tests' from code coverage reports since we usually don't want the coverage of the classes in a unit test project inflating production code coverage stats. Also accepts an array such as:
	   "[*.UnitTests]*","[*.IntegrationTests]*"
	 
	 .EXAMPLE
	 Given a project MyApp.dll with a Unit Test project MyApp.UnitTests.dll you can execute the following Powershell in the solution root to locate the unit tests and execute them and output code coverage stats:

	 PS$> Invoke-TestCore

	 .EXAMPLE
	 Given a project MyApp.dll with a Test project MyApp.IntegrationTests.dll built in debug configuration you can execute the following Powershell in the solution root to locate the unit tests and execute them and output code coverage stats:

	 PS$> Invoke-TestCore -Config Debug -ProjectNameFilter "*.IntegrationTests"

	 .EXAMPLE
	 Given a project MyApp.dll with a Test project MyApp.IntegrationTests.dll built in debug configuration you can execute the following Powershell in the solution root to locate the unit tests and execute them and output code coverage stats:

	 PS$> Invoke-TestCore -Config Debug -ProjectNameFilter "*.IntegrationTests"

	 .NOTES
     Typically for each test project you need to have the following:
	
	 	$PS> dotnet add package Microsoft.NET.Test.Sdk
		$PS> dotnet add package NUnit   
		$PS> dotnet add package NUnit3TestAdapter
		$PS> dotnet add package TeamCity.VSTest.TestAdapter
	 #>
	 Function Invoke-TestCore(
		 [string] $TestCommand ="dotnet", 
		 [string] $TestArguments ="test", 
		 [string] $Config = "Release", 
		 [string] $ProjectNameFilter = "*UnitTest*",
		 [switch] $SkipCodeCoverage = $false,		
		 [switch] $Skip = $false,
		 [string[]] $CoverletExcludes = "[*.*Tests?]*"
		 ) 
	 {
		 # consider dotnet vstest (Get-ChildItem -recurse -File *.Tests.*dll | ? { $_.FullName -notmatch "\\obj\\?" }) for agregated test run
		
		if(-Not($SkipCodeCoverage) -and -not ($testToolsInstalled) -and -not $skip)
		{
			# Get code coverage dependencies
			Install-Tool -InstalledExePath reportgenerator.exe -ToolName dotnet-reportgenerator-globaltool
			Install-Tool -InstalledExePath coverlet.exe -ToolName coverlet.console
			$script:testToolsInstalled = $true
		}

		Invoke-BuildTask -Name "dotnet test $ProjectNameFilter" -Skip $Skip -command {

			if((Test-Path $TestCommand) -Or (Get-Command $TestCommand -ErrorAction SilentlyContinue) -ne $null) {
				
				InfoMessage "Tests: Finding tests matching $ProjectNameFilter"

				$TestFolders = Get-ChildItem -filter $ProjectNameFilter -Directory -Recurse -Path $PSScriptRoot
				$ReporterFolder = (Resolve-Path $ReleaseDir)
				
				InfoMessage "Found $(@($TestFolders).Count) test folders: $TestFolders."

				$TestFolders | ForEach-Object {
					$TestFolder = $_.FullName
					$FinalTestArgs = $TestArguments, $TestFolder, "-c:$Config", "-r:$ReporterFolder", "--no-build", "--no-restore"

					if($SkipCodeCoverage){
						InfoMessage "Executing Test: `n $TestCommand $FinalTestArgs"
						& $TestCommand $FinalTestArgs 
					} 
					else {
						$AssemblyName = Get-AssemblyNameFromCsProj -ProjectFolder $_.FullName
						$AssemblyPath = Get-ChildItem -Path $TestFolder\bin\$Config -Recurse -Include "$AssemblyName.dll"
						$CoverletTargetArgs = ($FinalTestArgs -Join " ")
						InfoMessage "Instrumenting $AssemblyPath for code coverage before testing"

						$command = "coverlet"
						$commandArgs = [System.Collections.ArrayList] ($AssemblyPath, `
							"--target", $TestCommand, `
							"--targetargs", $CoverletTargetArgs, `
							"--merge-with", "$CoverageDataFolder\coverage.json", `
							"--format","json", "--format", "opencover", "--output", "$CoverageDataFolder\")

						foreach($CoverletExclude in $CoverletExcludes){
							$commandArgs.Add("--exclude") | Out-Null
							$commandArgs.Add($CoverletExclude) | Out-Null
						}

						Invoke-Exec { & $command $commandArgs }
					}
				}

				if(-Not($SkipCodeCoverage))
				{
					Write-Host "Generating coverage html report to $CoverageDataFolder"
					reportgenerator -reports:$CoverageDataFolder\coverage.opencover.xml -targetdir:$CoverageDataFolder\ -reporttypes HTML #-assemblyfilters:"-nunit*;-*.test.dll;-*.tests.dll"					
				}
				
				if(@($TestFolders).Count -eq 0) 
				{
					WarnMessage "No tests found!"
				}
			} else {
				InfoMessage "Skipping Test-Core() - could not find $TestCommand"
			}
		}
	 }
 
	 # find folders with nuspec files and pack them using nuget
	 Function Pack([string] $Executable = "nuget.exe", [string] $Config = "Release")
	 {
		Invoke-BuildTask -Name "nuget pack" -command {

			# use nuget.exe from package Nuget.CommandLine/octopack if possible, else if it is on path, use that
			$NugetExePath = Find-Nuget $Executable

			$FoldersWithNuspecs = GetFoldersContainingFilesThatMatch "*.nuspec" "(packages)|(obj)"
			ForEach($projectFolder In $FoldersWithNuspecs)
			{
				# assuming here that we want to ensure the folder has a csproj
				If(Test-Path (Join-Path $projectFolder "*.csproj")){
					$projectToPack = Join-Path $projectFolder "*.csproj" -Resolve
					$NugetArgs =  "Pack", "$projectToPack", "-Properties", "Configuration=$Config", "-OutputDirectory", "$ReleaseDir"
					
					InfoMessage "Executing pack on $projectToPack `n   $NugetExePath $NugetArgs" 
					& $NugetExePath $NugetArgs
				}
			}

			if(@($FoldersWithNuspecs).Count -eq 0) 
			{
				InfoMessage "Skipping Pack() step - no *.nuspec files found to Pack"
				return $false
			}
		}
	 }

	 <#
	 .SYNOPSIS
	 List your dependencies and ensure they do not have published vulnerabilities
	 
	 .DESCRIPTION
	 See https://github.com/fabiano/dotnet-ossindex for more info. Will scan your csproj files for PackageReferences based on your auto-discovered sln file and extract nuget dependencies
	 
	 .EXAMPLE
	 Invoke-DependencyCheck
	 
	 .NOTES
	 General notes
	 #>
	 
	 Function Invoke-DependencyCheck([float] $CVSScoreFailThreshold = 0) 
	 { 
		 if($LASTEXITCODE -eq 0){
			InfoMessage "Using tool dotnet-ossindex to check for vulnerabilities"
			Install-Tool -InstalledExePath dotnet-ossindex.exe -ToolName dotnet-ossindex

			$command = "dotnet"
			$commandArgs = [System.Collections.ArrayList] ("ossindex", $PathToSln, "--cvsscore", $CVSScoreFailThreshold, "--verbose")
			
		   if($ENV:SONARTYPE_APIKEY){ 
				$commandArgs.Add("--username") | Out-Null
				$commandArgs.Add($ENV:SONARTYPE_USERNAME) | Out-Null
				$commandArgs.Add("--api-token") | Out-Null
				$commandArgs.Add($ENV:SONARTYPE_APIKEY) | Out-Null
			}

			Invoke-Exec { & $command $commandArgs }
		 }
	}
 
	 <#
	 .SYNOPSIS
	 Install a new CLI tool using dotnet tool install
 
	 .PARAMETER InstalledExePath
	 Once installed, what is the relative path from ToolsFolder to the executable, e.g. "dotnet-octo.exe" 
	 
	 .PARAMETER ToolName
	 nuegt package name for the dotnet tool, e.g. "dotnet-octo"
	 
	 .PARAMETER ToolsFolder
	 Full path of folder to install tool into. Defaults to "c:\tools\dotnet"
	 
	 .PARAMETER DaysBeforeUpdate
	 How many days since the last dotnet tool install/update before forcing a tool update from source	 
	 
	 .PARAMETER Version
	 Which version of the tool to install
	 
	 .EXAMPLE
	 Install-Tool -InstalledExePath coverlet.exe -ToolName coverlet.console
	 
	 #>
	 Function Install-Tool
	 {
		Param
		(
			[Parameter(Mandatory=$true)]
			[string] $InstalledExePath,
			[Parameter(Mandatory=$true)]
			[string] $ToolName,
			[string] $ToolsFolder = "\tools\dotnet",
			[int]    $DaysBeforeUpdate = 30,
			[string] $Version = $null
		)
		Invoke-BuildTask -Name "dotnet tool install $ToolName" -command {
		
			$toolPath   = "$ToolsFolder\$installedExePath"
			$needUpdate = $false
			$lastUpdate = (Get-Item $toolPath -ErrorAction SilentlyContinue)
			$command    = "install"

			if($lastUpdate) 
			{ 
				$command = "update" 
				$needUpdate = (((Get-Date) - $lastUpdate.LastWriteTime) -gt (New-timespan -days $DaysBeforeUpdate))
			} 

			$commandArgs = [System.Collections.ArrayList] ( `
				"tool", `
				$command, `
				$ToolName, `
				"--tool-path",  $ToolsFolder)

			if($null -ne $Version -and $Version.Length -gt 0){
				$commandArgs.Add("--version") | Out-Null
				$commandArgs.Add($Version) | Out-Null
			}

			if(($command -eq "install") -or $needUpdate) {
				Invoke-Exec { & dotnet $commandArgs }
			} else {
				$lastWrite = $lastUpdate.LastWriteTime.ToString("dd/MM/yyyy")
				InfoMessage "Skipping tool update for $toolPath as last update ($lastWrite) was less than $DaysBeforeUpdate days ago"
			}
	
			if(-not (Test-Path $toolPath)) {
				throw "Unable to find tool installed at $toolPath - Install probably failed. Check params InstalledExePath and ToolName"
			}
			
			$ToolsFolderPattern = "$ToolsFolder;".Replace("\", "\\")
			If (-Not ($env:PATH -match $ToolsFolderPattern )) {
				# Add the tools path to the PATH env variable
				InfoMessage "Adding $ToolsFolder to your PATH"
				$env:PATH = "$ToolsFolder;" + $env:PATH
			}
		}
	 }
 
	 <#
	 .SYNOPSIS
	 Installs dotnet octo CLI tool if not available and adds it to your path for the rest of your session. Will do an update if octo is out of date.
	 
	 .DESCRIPTION
	 Default install location is c:\tools\dotnet which is added to your PATH. once this is run you can run `dotnet octo`
	 
	 .EXAMPLE
	 Install-Octo
	 #>
	 Function Install-Octo() {
		 Install-Tool -InstalledExePath dotnet-octo.exe -ToolName Octopus.DotNet.Cli
	 }
	  
	 <#
	 .SYNOPSIS
	 Creates a zip file of a directory containing terraform files ready for deployment
	 
	 .DESCRIPTION
	 Creates a zip file of a directorys terraform files and outputs it to a parent directory called dist ready for deployment. This package can then be sent to octopus deploy
	 
	 .PARAMETER id
	 The ID of the package to create
	 
	 .PARAMETER version
	 The Semver version number of the package to create
	 
	 .PARAMETER FolderToZip
	 The folder to be zipped
	 
	 .PARAMETER outputFolder
	 Where to place the new zip file
	 
	 .PARAMETER Include
	 pattern for files to be included in zip by default. Defaults to *.*. For more an one comma seperate.
	 
	 .PARAMETER OctoPackArgs
	 Additional args to be passed to dotnet octo. For more an one comma seperate.

	 .PARAMETER RemoveFolder
	 Should the source FolderToZip be removed after sucessful zipping?
	 
	 .EXAMPLE
	 New-ZipPackage -id "hello-world" -version "1.0.0.0" -FolderToZip "c:\proj\hello\src" -outputFolder "c:\proj\hello\dist" -Include "*.tf","*.tfvars"
	 
	 .NOTES
	 
	 #>
	 Function New-ZipPackage([String] $Id, [String] $Version, [String] $FolderToZip, [String] $OutputFolder, [String[]] $Include = "**\*.*", [String[]] $OctoPackArgs = "", [switch] $RemoveFolder = $false) {
		return Invoke-BuildTask -Name "dotnet octo pack $Id" -command {

			if((Test-Path $FolderToZip -ErrorAction SilentlyContinue) -and (Test-Path $OutputFolder -ErrorAction SilentlyContinue)){
				InfoMessage "Zipping files in '$FolderToZip' to $OutputFolder"
			} else {
				WarnMessage "Unable to zip files in '$FolderToZip' to '$OutputFolder'"
				return $false
			}

			$format = "zip"
			$FolderToZip = Resolve-Path $FolderToZip
			$OutputFolder = Resolve-Path $OutputFolder

			# Build a cli command to pack the files into a zip with version number from Teamcity
			$command = "dotnet"
			$commandArgs = [System.Collections.ArrayList] ("octo", "pack", "--id=$Id", "--version=$Version", "--basePath=$FolderToZip", "--outFolder=$OutputFolder", "--format=$format")
	
			foreach($includePattern in $Include){
				$commandArgs.Add("--include=$IncludePattern") | Out-Null
			}
			foreach($octoPackArg in $OctoPackArgs){
				$commandArgs.Add($octoPackArg) | Out-Null
			}
	
			invoke-exec {
				 & $command $commandArgs
			}
			
			if($RemoveFolder)
			{
				Remove-Item $FolderToZip -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
			}
		
			# Return path to created package
			$zipPath = (Get-ChildItem "$outputFolder\$id.$version.$format" | Select-Object -First 1).FullName
	
			InfoMessage "Zipped files to $zipPath"
	
			return $ZipPath
		}
	 }
 
	 Function Get-PrettyDuration([timespan] $duration){

		$hrs = [Math]::Floor($duration.TotalHours);
		$ms = [Math]::Floor($duration.TotalMinutes);
		$s = [Math]::Ceiling($duration.Seconds);
		$ss = $duration.Seconds.ToString("#0.#");

		if($hrs -gt 0) { return "$($hrs)h $($ms)m $($s)s" }
		if($ms -gt 0) { return "$($ms)m $($s)s" }
		return "$($ss)s" 
	 }
 
	 # raise appropriate failure errors or report sucess
	 Function ReportResults()
	 {
		 $failMessage = ""
		 $stopwatch.Stop()
		 $duration = Get-PrettyDuration $stopwatch.Elapsed
 
		 if(Test-Path $LogFile) {
			 Get-Content $LogFile -OutVariable BuildSolutionResult | out-null
 
			 if ($BuildSolutionResult.IndexOf("Build succeeded.") -lt 0)
			 {
				 $failMessage = "MsBuild FAILED! See msbuild log: $LogFile`n "
			 } 
			 else{
				 InfoMessage "`nMsBuild log file reports success ($LogFile)"
			 }
		 }
 
		 # Iterate through any .trx test result files in the relase folder and check the outcome
		 # Get-ChildItem -Path $ReleaseDir -Filter *.trx | ForEach-Object {
		 # 	Get-Content $_.FullName -OutVariable testResult | out-null
		 # 	if($testResult -match "<ResultSummary outcome=`"Failed`">" -and $failMessage -eq ""){
		 # 		$failMessage = "Tests FAILED! See " + $_.FullName				
		 # 	}
		 # }
 
		 # check the NUnit tests file for failures
		 if(Test-Path $nunitTestOutput) 
		 {
			 Get-Content $nunitTestOutput -OutVariable testResult | out-null
 
			 if ($testResult -match 'result="Failed"' -and $failMessage -eq "")
			 {
				 $failMessage += "Tests FAILED! See $nunitTestOutput`n"
			 }
			 else{
				 InfoMessage "`nNUnit log file reports success ($nunitTestOutput)"
			 }
		 }

		if($buildTasks) {
			

			# Show build summary table. Teamcity is fussy about with so use Out-String
			$buildTasks | Format-Table `
				@{L='Build Task';E={ $_.BuildTask}},
				@{L='Succeeded';E={ if($_.Skipped){ "-" } else { "$($_.Succeeded)" }};Alignment='center'},
				@{L='Skipped';E={ if($_.Skipped){ "Yes" } else { "-"  }};Alignment='center'},
				@{L='Duration';E={ Get-PrettyDuration (New-TimeSpan -Seconds $_.Duration) };Alignment='right';},
				@{L='% of Build';E={$_.Percentage};Alignment='right';} | Out-String -Stream -Width  100

			$summary = Get-BuildTasksSummary
			If(-not ($summary.Succeeded)) 
			{
				$failMessage += "Task '$($summary.FirstFailure)' FAILED. $($summary.TasksFailed) build task(s) failed in $duration. ($($summary.TasksSkipped) skipped)."
				
				Write-Host  $failMessage -ForegroundColor "Red"
			} 
			else 
			{
				$message =  "$($summary.TasksSucceded) Tasks completed successfully in $duration. ($($summary.TasksSkipped) skipped)."
				Write-Host "Success! $message" -ForegroundColor "Green"
				
			}
		}
	 }
	  

	<#
	.SYNOPSIS
	Generate an OpenAPI/Swagger file using swashbuckle CLI tools. Requires your web project to use Swashbuckle.AspNetCore 5+
	This function assumes that dotnet swagger tool is installed as a dotnet core local tool
	
	.DESCRIPTION
	Install and invoke invoke dotnet swagger global tool. See https://github.com/domaindrivendev/Swashbuckle.AspNetCore/#swashbuckleaspnetcorecli
	
	.PARAMETER WebProjectFolderName
	The folder name that contains the web project, e.g "MyApp.Web"
	
	.PARAMETER StartupAssembly
	The file name of the assembly that references swashbuckle. E.g. MyApp.Web.Dll
	
	.PARAMETER OutputFilePath
	The full path to the file where the swagger/OpenAPI json should be generated
	
	.PARAMETER SwaggerDocName
	The name of the swagger doc defined in your project startup. Usually 'v1'

     .PARAMETER IsOpenApi
	 A bool switch to have swagger version V3(OpenApi) or V2. Defaults to False.
	
	.EXAMPLE
	Invoke-DotnetSwagger -WebProjectFolderName "Web" -StartupAssembly "MyApp.Web.dll" -OutputFilePath "$ReleaseDir\..\infrastructure\myapp-api-swagger.json"

	#>
	Function Invoke-DotnetSwagger(
		[string] $WebProjectFolderName,
		[string] $StartupAssembly = "$WebProjectFolderName.dll",
		[string] $OutputFilePath,
		[string] $SwaggerDocName = "v1",
		[switch] $IsOpenApi = $false
	) 
	{
		if((dotnet tool list | findstr "swashbuckle.aspnetcore.cli") -eq $null) { 
			WarnMessage "Unable to find swashbuckle.aspnetcore.cli installed as a dotnet core local tool. Please add it to the tools manifest." 
			}
			
		Invoke-BuildTask -Name "dotnet swagger" -command {
			InfoMessage "Using tool swashbuckle.aspnetcore.cli to generate swagger/openAPI file"

			$binFolder = "$SolutionFolder\$WebProjectFolderName\bin"
			if(-Not(Test-Path $binFolder)){
				throw "Cannot find bin folder for project to generate swagger file - $binFolder"
			}

			$WebDllPath = (GetMostRecentFileMatchingPath $StartupAssembly $binFolder)
			$jsonFile = Force-Resolve-Path $OutputFilePath

			if($IsOpenApi) {
				invoke-exec { dotnet swagger tofile --output $jsonFile $($WebDllPath.FullName) $SwaggerDocName }
			} else {
				invoke-exec { swagger tofile --serializeasv2  --output $jsonFile $($WebDllPath.FullName) $SwaggerDocName }
			}
			  	
			if(-Not(Test-Path $jsonFile)){
				throw "Cannot find generated swagger file - dotnet swagger probably failed - $jsonFile"
			}
		}																					
	}

	function Force-Resolve-Path {
		<#
		.SYNOPSIS
			Calls Resolve-Path but works for files that don't exist.
		.REMARKS
			From http://devhawk.net/blog/2010/1/22/fixing-powershells-busted-resolve-path-cmdlet
		#>
		param (
			[string] $FileName
		)
	     
		$FileName = Resolve-Path $FileName -ErrorAction SilentlyContinue `
										   -ErrorVariable _frperror
		if (-not($FileName)) {
            $FileName = $_frperror[0].TargetObject
		}
		return $FileName
	}
	
 <# Computed and other variables #> 
	 $PSScriptRoot          = If($PSScriptRoot -eq $null) { Split-Path $MyInvocation.MyCommand.Path -Parent } else { $PSScriptRoot }
	 $SolutionFolder        = (Get-Item -Path $PSScriptRoot -Verbose).FullName
	 $ProjName              = try {(GetMostRecentFileMatchingPath "*.sln").Name.Replace(".sln", "") } catch {}
	 $PathToSln             = "$SolutionFolder\$ProjName.sln"
	 $ReleaseDir            = "$SolutionFolder\$OutputDirectory"
	 $_ = New-Item -path $ReleaseDir -type directory -force -ErrorAction silentlycontinue
	 $ReleaseDir            = Resolve-Path $ReleaseDir
	 $LogFile               = "$ReleaseDir\$ProjName-Build.log"
	 $NunitTestOutput       = "$ReleaseDir\TestResult.xml"
	 $Stopwatch             = [Diagnostics.Stopwatch]::StartNew()	 
	 $BuildNumber 		    = if (Test-Path env:\build_number) { $env:build_number } Else { "1.0.0.0" }
	 $BranchName            = &git rev-parse --abbrev-ref HEAD 
	 $IsMasterBuild         = [bool] ($BranchName -eq "master")
	 $NugetVersionSuffix    = If($IsMasterBuild) { "" } else { "-PreRelease"}
	 $CoverageDataFolder 	= Join-Path (Resolve-Path $ReleaseDir) "\Coverage"
	 $buildTasks 			= [System.Collections.ArrayList] @()
	 $testToolsInstalled 	=  $false
 
 Write-host "Info: Running under Powershell version: " + $PSVersionTable.PSVersion
 
 Export-ModuleMember -Variable OutputDirectory,ToolsPath,SolutionFolder,ProjName,PathToSln,ReleaseDir,BuildNumber,BranchName,IsMasterBuild,NugetVersionSuffix -Function InfoMessage,WarnMessage,GetMostRecentFileMatchingPath,GetFoldersContainingFilesThatMatch,Clean,Find-Nuget,Find-MsBuild,Restore,Build,Invoke-Build,Invoke-BuildCore,Invoke-PublishCore,Test-NUnit,Invoke-TestNUnit,Invoke-TestCore,Pack,New-ZipPackage,Install-Octo,ReportResults,Invoke-Clean,Invoke-CleanCore,Invoke-Exec,Invoke-PackCore,Invoke-DependencyCheck,Invoke-DotnetSwagger,Invoke-BuildTask,Invoke-Retry