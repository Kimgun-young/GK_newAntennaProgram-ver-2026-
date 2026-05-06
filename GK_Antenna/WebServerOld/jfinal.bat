@echo off


setlocal & pushd


set MAIN_CLASS=net.tray.xphased.AppConfig



if "%1"=="start" goto normal
if "%1"=="stop" goto normal
if "%1"=="restart" goto normal

goto error


:error
echo Usage: jfinal.bat start | stop | restart
goto :eof


:normal
if "%1"=="start" goto start
if "%1"=="stop" goto stop
if "%1"=="restart" goto restart
goto :eof


:start
set APP_BASE_PATH=%~dp0
set CP=%APP_BASE_PATH%config;%APP_BASE_PATH%lib\*

echo starting jfinal undertow

SET "var="&for /f "delims=0123456789" %%i in ("%2") do set var=%%i

if defined var (
    echo %2 NOT numeric
) else (
    set "JAVA_OPTS=-Dundertow.port=%2 -Dundertow.host=0.0.0.0"
)

echo %JAVA_OPTS%

REM 
set JAVA_EXE="%APP_BASE_PATH%jre\bin\java.exe"

echo Using Java: %JAVA_EXE%

call %JAVA_EXE% -Xverify:none %JAVA_OPTS% -cp "%CP%" %MAIN_CLASS%

goto :eof


:stop
echo stopping jfinal undertow

for /f "tokens=1" %%i in ('"%APP_BASE_PATH%jre\bin\jps.exe" -l ^| find "%MAIN_CLASS%"') do (
    taskkill /F /PID %%i
)

goto :eof


:restart
call :stop
call :start
goto :eof

endlocal & popd
pause