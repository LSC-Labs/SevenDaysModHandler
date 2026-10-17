@echo off
setlocal
pushd "%~dp0"
if errorlevel 1 exit /b 1

where dotnet >nul 2>&1
if errorlevel 1 (
    echo FEHLER: Das .NET 10 SDK muss installiert sein.
    popd
    exit /b 1
)

echo Erstelle Release-Build und aktualisiere artifacts\app ...
echo Bitte die Anwendung vor dem Build schliessen.
dotnet publish "SevenDaysModManager\SevenDaysModManager.csproj" --configuration Release --output "artifacts\app" --no-self-contained --nologo
set "BUILD_RESULT=%ERRORLEVEL%"
if not "%BUILD_RESULT%"=="0" (
    echo FEHLER: Build fehlgeschlagen.
) else (
    echo Build erfolgreich. Anwendung mit Start.cmd starten.
)
popd
exit /b %BUILD_RESULT%
