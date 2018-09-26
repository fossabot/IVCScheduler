move packages\repositories.config .
rmdir packages /s /q
mkdir packages
move repositories.config packages/
.nuget\NuGet.exe locals all -clear
.nuget\NuGet.exe restore
