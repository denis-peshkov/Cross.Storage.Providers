dotnet build --configuration release ..\Cross.Storage.Providers.sln
REM nuget.exe pack config.nuspec -Symbols -SymbolPackageFormat snupkg
nuget.exe pack config.nuspec -Symbols
