@echo off
cls
".nuget\nuget.exe" install -OutputDirectory packages .\packages.config
"packages\FAKE.1.52.6.0\tools\Fake.exe" "build.fsx"
pause
