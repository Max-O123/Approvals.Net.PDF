$buildConfigurationName = "Release"

$projectFileInfo = Get-ChildItem -Path $PSScriptRoot | where { $_.Extension -eq ".csproj"} | select -First 1  
  
& dotnet @("publish", $projectFileInfo.FullName,"-c", $buildConfigurationName)

$package = Get-ChildItem "$PSScriptRoot\bin\$buildConfigurationName\" -Filter *.nupkg | sort -Descending -Property LastWriteTime | select -First 1
# Install nuget.exe with chocolately which will put it on the system path
& nuget.exe @("push", "-Source", "Ecark.IT.BFS.Netstandard", "-ApiKey", "AzureDevOps", $package.FullName)