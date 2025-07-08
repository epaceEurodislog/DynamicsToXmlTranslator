@echo off
echo === Compilation du Traducteur Dynamics ===
echo.

echo Nettoyage...
dotnet clean

echo Compilation...
dotnet publish --configuration Release --self-contained true --runtime win-x64 --output ./publish --property:PublishSingleFile=true --property:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ✅ Compilation réussie !
    echo 📁 Fichier créé : ./publish/DynamicsToXmlTranslator.exe
    echo.
    echo N'oubliez pas de configurer appsettings.json avant utilisation.
) else (
    echo.
    echo ❌ Erreur lors de la compilation
)

pause