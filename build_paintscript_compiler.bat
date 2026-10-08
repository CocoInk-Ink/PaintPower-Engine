set "ROOT=%~dp0"
call "%ROOT%PaintScript Standard\PaintScript 0.1\compiler\build.bat"
if errorlevel 1 exit /b %errorlevel%
copy /Y "%ROOT%PaintScript Standard\PaintScript 0.1\compiler\dist\output.js" "%ROOT%Assets\Resources\Binary\Compilers\PaintScript\p0.1.0.js"
if errorlevel 1 exit /b %errorlevel%