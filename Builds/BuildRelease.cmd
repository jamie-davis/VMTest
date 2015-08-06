call "c:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\VSVARS32.bat"

msbuild ..\src\VMTest.sln /t:clean
msbuild ..\src\VMTest.sln /t:Rebuild /p:Configuration=Release

if NOT EXIST "VMTest %VMTestversion%" md "VMTest %VMTestversion%"

copy ..\src\VMTest\bin\release\*.dll "VMTest %VMTestversion%"
copy ..\src\VMTest\bin\release\*.xml "VMTest %VMTestversion%"

.nuget\nuget pack ..\src\VMTest\VMTest.csproj -outputdirectory "VMTest %VMTestversion%" -IncludeReferencedProjects -Prop Configuration=Release -Version %VMTestversion%

git add "VMTest %VMTestversion%\VMTest.dll" -f
git add "VMTest %VMTestversion%\VMTest.xml" -f
git add "VMTest %VMTestversion%\VMTest.%VMTestversion%.nupkg" -f
pause