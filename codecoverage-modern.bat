REM This script calculates code coverage for current solution
REM Prerequests: reportgenerator
REM you can install them with dotnet tool install -g ...
REM Note: no reports is generated if some test fail. In that case please use codecoverage.bat instead
@echo Removing old test results
@rmdir /s src\LinkTracker.Tests\TestResults
@rmdir /s src\LinkTracker.UnitTests\TestResults
@echo Testing code...
@dotnet test --collect:"XPlat Code Coverage"
@echo Creating report...
@reportgenerator -reports:src\*\TestResults\**\*.xml -targetdir:CodeCoverage
@echo Completed!
@start CodeCoverage\index.html
@pause