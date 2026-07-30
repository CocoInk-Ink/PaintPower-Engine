@echo off
setlocal

REM === Set the folder you want to print ===
set TARGET=PaintPower

REM === Output file ===
set OUTPUT=directory_tree.txt

REM === Generate tree without ASCII lines ===
tree "%TARGET%" /F /A > "%OUTPUT%"

echo Directory tree written to %OUTPUT%
endlocal
