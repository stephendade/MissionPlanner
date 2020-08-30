
set PATH=%PATH%;C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin

rem MSBuild.exe MissionPlanner.sln -target:Clean

del bin\release\MissionPlannerBeta.zip

.nuget\nuget.exe restore MissionPlanner.sln

MSBuild.exe MissionPlanner.sln /m /p:Configuration=Release /verbosity:n

cd bin\release\net461
for /f %%f in ('dir /a-d /b plugins') do if exist .\%%f del .\plugins\%%f

pause