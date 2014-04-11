@echo off
SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
SET MSDEPLOY="C:\Program Files (x86)\IIS\Microsoft Web Deploy V3\msdeploy.exe"
SET AWSDEPLOY="C:\Program Files (x86)\AWS Tools\Deployment Tool\awsdeploy.exe"
SET PKGPATH=C:\src\HttpBenchmarks\src\deploy
SET DEPLOYPKG=%PKGPATH%\ResultsView-deploy.zip

%MSBUILD% ..\src\ResultsView\ResultsView.csproj /T:Package /property:PackageLocation=%DEPLOYPKG%

%MSDEPLOY% -verb:sync -source:manifest=manifest.xml -dest:package=%DEPLOYPKG% -declareParamFile=parameters.xml

%AWSDEPLOY% /r /DDeploymentPackage=%DEPLOYPKG% ResultsView-deploy.txt
