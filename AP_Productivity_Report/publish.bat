@echo off
echo Publishing AP Productivity Report...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
echo.
echo Done! Output is in the ./publish folder.
echo Run publish\AP_Productivity_Report.exe to start the application.
pause
