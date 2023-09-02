#!/bin/bash

rm -rf src/*/obj src/*/bin

source ~/.bashrc

export PACKAGES=' SocketIOClient SocketIO.Serializer.SystemTextJson SocketIO.Serializer.MessagePack SocketIO.Core SocketIO.Serializer.Core '

for x in ${PACKAGES}
do
    dotnet msbuild /t:restore /p:Configuration=Release src/${x}/${x}.csproj
done

for x in ${PACKAGES}
do
    dotnet msbuild /t:build /p:Configuration=Release src/${x}/${x}.csproj
done

for x in ${PACKAGES}
do
    dotnet msbuild /t:pack /p:Configuration=Release src/${x}/${x}.csproj
done

for x in $(find src -name "*.nupkg")
do
    dotnet nuget push $x -s https://api.nuget.org/v3/index.json -k ${NugetOrgApiKeyTS}
done

