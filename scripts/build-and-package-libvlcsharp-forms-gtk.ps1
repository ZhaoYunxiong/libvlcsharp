param([string]$msbuild)

Remove-Item ..\LibVLCSharp.Forms.Platforms.GTK\bin -Force -Recurse -ErrorAction Ignore
Remove-Item ..\LibVLCSharp.Forms.Platforms.GTK\obj -Force -Recurse -ErrorAction Ignore

if(!$msbuild)
{
    $msbuild = "'C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe'";
}

iex ("& {0} {1}" -f $msbuild, "../LibVLCSharp.Forms.Platforms.GTK/LibVLCSharp.Forms.Platforms.GTK.csproj /t:Restore,Clean,Build /p:Configuration=Release")

robocopy  ..\LibVLCSharp.Forms.Platforms.GTK\bin\Release\ . *.nupkg