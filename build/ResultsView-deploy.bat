SET MSBUILD=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe
SET AWSDEPLOY="C:\Program Files (x86)\AWS Tools\Deployment Tool\awsdeploy.exe"

%MSBUILD% ..\src\ResultsView\ResultsView.csproj /T:Package /property:PackageLocation=..\src\deploy\ResultsView-deploy.zip

%AWSDEPLOY% /r ResultsView-deploy.txt
