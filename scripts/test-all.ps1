cd .\src
dotnet build

cd ..
powershell -File .\scripts\CopySqlDllToExtensionBundle.ps1

cd test\Microsoft.Azure.WebJobs.Extensions.PostgreSQL.Tests
dotnet test --logger "trx;LogFileName=results.trx"