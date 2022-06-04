@echo off

if defined APPVEYOR_BUILD_FOLDER (
   cd %APPVEYOR_BUILD_FOLDER%
   goto genMoFiles
) else (
   goto checkForGitForWindows
)

:checkForGitForWindows
if exist "C:\Program Files\Git\usr\bin\msgfmt.exe" (
   set path="C:\Program Files\Git\usr\bin"
   rem set batDir=%~dp0
   cd /D %~dp0
   cd ../..
   rem cd
   goto genMoFiles
) else (
   echo Git for Windows not found.
   exit /b 1
)

:genMoFiles
msgfmt.exe -o PhotoSift\locale\zh-CN\LC_MESSAGES\PhotoSift.mo PhotoSift\locale\zh-CN\LC_MESSAGES\PhotoSift.po

if defined %1 if not exist "%1\locale\zh-CN\LC_MESSAGES\PhotoSift.mo" (
   if not exist "%1\locale\zh-CN\LC_MESSAGES\" (
      mkdir "%1\locale\zh-CN\LC_MESSAGES\"
   )
   mklink /H "%1\locale\zh-CN\LC_MESSAGES\PhotoSift.mo" "PhotoSift\locale\zh-CN\LC_MESSAGES\PhotoSift.mo"
) else (
   exit /b 0
)