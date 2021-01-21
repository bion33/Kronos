cd ./Kronos
dotnet pack -c Release
cd ..

cd ./KronosConsole
dotnet publish -c Release -r linux-x64 /p:PublishSingleFile=true
dotnet publish -c Release -r osx-x64 /p:PublishSingleFile=true
dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true
cd ..

RMDIR /S /Q build

mkdir build
mkdir build\linux
mkdir build\osx
mkdir build\windows

move /y Kronos\bin\Release\Kronos*.nupkg Kronos\bin\Release\Kronos.nupkg
move /y Kronos\bin\Release\Kronos.nupkg build
move KronosConsole\bin\Release\netcoreapp3.1\linux-x64\publish\Kronos-Console build\linux\Kronos-Console
move KronosConsole\bin\Release\netcoreapp3.1\osx-x64\publish\Kronos-Console build\osx\Kronos-Console
move KronosConsole\bin\Release\netcoreapp3.1\win-x64\publish\Kronos-Console.exe build\windows\Kronos-Console.exe
copy .\README.md build\linux
copy .\LICENSE.md build\linux
copy .\README.md build\osx
copy .\LICENSE.md build\osx
copy .\README.md build\windows
copy .\LICENSE.md build\windows

type nul >>"build\windows\Open README and LICENSE with a text editor" & copy "build\windows\Open README and LICENSE with a text editor" +,,

cd build\linux
powershell "Compress-Archive ..\linux linux.zip"
move linux.zip ..\linux.zip
cd ..\osx
powershell "Compress-Archive ..\osx osx.zip"
move osx.zip ..\osx.zip
cd ..\windows
powershell "Compress-Archive ..\windows windows.zip"
move windows.zip ..\windows.zip

cd ..\..

cd build

RMDIR /S /Q linux
RMDIR /S /Q osx
RMDIR /S /Q windows
cd ..
