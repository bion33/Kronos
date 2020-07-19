#!/bin/bash

dotnet publish -r linux-x64 /p:PublishSingleFile=true
dotnet publish -r osx-x64 /p:PublishSingleFile=true
dotnet publish -r win-x64 /p:PublishSingleFile=true

rm -Rf ./build

mkdir -p ./build/linux
mkdir -p ./build/osx
mkdir -p ./build/windows

mv ./bin/Debug/netcoreapp3.1/linux-x64/publish/Kronos ./build/linux/Kronos
mv ./bin/Debug/netcoreapp3.1/osx-x64/publish/Kronos ./build/osx/Kronos
mv ./bin/Debug/netcoreapp3.1/win-x64/publish/Kronos.exe ./build/windows/Kronos.exe

cp ../../README.md ./build/linux
cp ../../LICENSE.md ./build/linux
cp ../../README.md ./build/osx
cp ../../LICENSE.md ./build/osx
cp ../../README.md ./build/windows
cp ../../LICENSE.md ./build/windows
touch "./build/windows/Open README and LICENSE with a text editor"

cd ./build/linux
zip ../linux *
cd ../osx
zip ../osx *
cd ../windows
zip ../windows *
cd ..

rm -Rf ./linux
rm -Rf ./osx
rm -Rf ./windows