#!/bin/bash

cd ./Kronos
dotnet pack
cd ..

cd ./KronosConsole
dotnet publish -r linux-x64 /p:PublishSingleFile=true
dotnet publish -r osx-x64 /p:PublishSingleFile=true
dotnet publish -r win-x64 /p:PublishSingleFile=true
cd ..

rm -Rf ./build

mkdir -p ./build/linux
mkdir -p ./build/osx
mkdir -p ./build/windows

mv ./Kronos/bin/Debug/Kronos*.nupkg ./Kronos/bin/Debug/Kronos.nupkg
mv ./Kronos/bin/Debug/Kronos.nupkg ./build
mv ./KronosConsole/bin/Debug/netcoreapp3.1/linux-x64/publish/Kronos-Console ./build/linux/Kronos-Console
mv ./KronosConsole/bin/Debug/netcoreapp3.1/osx-x64/publish/Kronos-Console ./build/osx/Kronos-Console
mv ./KronosConsole/bin/Debug/netcoreapp3.1/win-x64/publish/Kronos-Console.exe ./build/windows/Kronos-Console.exe

cp ./README.md ./build/linux
cp ./LICENSE.md ./build/linux
cp ./README.md ./build/osx
cp ./LICENSE.md ./build/osx
cp ./README.md ./build/windows
cp ./LICENSE.md ./build/windows
touch "./build/windows/Open README and LICENSE with a text editor"

cd ./build/linux
zip ../linux *
cd ../osx
zip ../osx *
cd ../windows
zip ../windows *
cd ../..

cd ./build
rm -Rf ./linux
rm -Rf ./osx
rm -Rf ./windows
cd ..