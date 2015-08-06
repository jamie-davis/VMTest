call "c:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\VSVARS32.bat"

msbuild ..\src\TestConsole.sln /t:clean
msbuild ..\src\TestConsole.sln /t:Rebuild /p:Configuration=Debug

.nuget\nuget pack ..\src\TestConsole\TestConsole.csproj -outputdirectory "..\..\localnuget" -IncludeReferencedProjects -Prop Configuration=Debug -symbols
