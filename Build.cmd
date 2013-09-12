@ECHO OFF
IF NOT "%NuGetHome%"=="" GOTO :nuget
SET NuGetHome=tools\NuGet
:nuget
"%NuGetHome%\NuGet.exe" install IntelliFactory.Build -version 0.2.24-alpha -pre -ExcludeVersion -o tools\packages
IF NOT "%FSharpHome%"=="" GOTO :fs
SET PF=%ProgramFiles(x86)%
IF NOT "%PF%"=="" GOTO w64
SET PF=%ProgramFiles%
:w64
SET FSharpHome=%PF%\Microsoft SDKs\F#\3.0\Framework\v4.0
:fs
"%FSharpHome%\fsi.exe" --exec build.fsx %*