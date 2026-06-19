@echo off
setlocal enabledelayedexpansion

:: Output file
set OUTPUT=AllSources.txt

:: Clear old output
if exist "%OUTPUT%" del "%OUTPUT%"

echo Combining source files into %OUTPUT% ...
echo.

:: Extensions to include
set EXTENSIONS=.cs .axaml .xaml .csproj .json .md .txt

:: Folders to exclude
set EXCLUDE_DIRS=\bin\ \obj\ \dist\ \Junk\ \publish\ \AppDir\

:: Root folder
set ROOT=%cd%

:: Loop through all files
for /r %%F in (*) do (

    :: Skip excluded directories
    set SKIP=0
    for %%D in (%EXCLUDE_DIRS%) do (
        echo "%%F" | findstr /i "%%D" >nul && set SKIP=1
    )
    if !SKIP!==1 (
        continue
    )

    :: Check extension
    for %%E in (%EXTENSIONS%) do (
        if /i "%%~xF"=="%%E" (
            echo ===============================================>>"%OUTPUT%"
            echo FILE: %%F >>"%OUTPUT%"
            echo ===============================================>>"%OUTPUT%"
            echo.>>"%OUTPUT%"
            type "%%F" >>"%OUTPUT%"
            echo.>>"%OUTPUT%"
        )
    )
)

echo Done!
echo Output saved to %OUTPUT%
pause
