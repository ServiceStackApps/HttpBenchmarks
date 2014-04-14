@echo off
SET HOST=127.0.0.1
REM SET HOST=10.0.0.64
SET REQUESTS=10000
SET DB=SqlServer

:AspNet
SET PORT=55000
SET ID=aspnet
ab -n 1 "http://%HOST%:%PORT%/reset/full"
CALL :Benchmark

:HttpListener
SET PORT=55001
SET ID=httplistener
START servers\Techempower.HttpListener\bin\Release\Techempower.HttpListener.exe %DB%
ping -n 3 127.0.0.1 > nul
ab -n 1 "http://%HOST%:%PORT%/reset/full"
CALL :Benchmark
taskkill /f /im Techempower.HttpListener.exe

:HttpListenerPool
SET PORT=55002
SET ID=httplistenerpool
START servers\Techempower.HttpListener.Pool\bin\Release\Techempower.HttpListener.Pool.exe 64 %DB%
ping -n 3 127.0.0.1 > nul
ab -n 1 "http://%HOST%:%PORT%/reset/full"
CALL :Benchmark
taskkill /f /im Techempower.HttpListener.Pool.exe

:SmartPool
SET PORT=55003
SET ID=httplistenersmartpool
START servers\Techempower.HttpListener.SmartPool\bin\Release\Techempower.HttpListener.SmartPool.exe 16 %DB%
ping -n 3 127.0.0.1 > nul
ab -n 1 "http://%HOST%:%PORT%/reset/full"
CALL :Benchmark
taskkill /f /im Techempower.HttpListener.SmartPool.exe

GOTO End

:Benchmark

SET URL=http://%HOST%:%PORT%

FOR %%A IN (db queries fortunes updates) DO ab -k -n %REQUESTS% -c 10 "%URL%/%%A" > results\db_%REQUESTS%_10_%%A_%ID%.txt
ab -n 1 "http://%HOST%:%PORT%/reset/gc" > nul
FOR %%A IN (db queries fortunes updates) DO ab -k -n %REQUESTS% -c 100 "%URL%/%%A" > results\db_%REQUESTS%_100_%%A_%ID%.txt
ab -n 1 "http://%HOST%:%PORT%/reset/gc" > nul
FOR %%A IN (db queries fortunes updates) DO ab -k -n %REQUESTS% -c 256 "%URL%/%%A" > results\db_%REQUESTS%_256_%%A_%ID%.txt
ab -n 1 "http://%HOST%:%PORT%/reset/gc" > nul

GOTO :EOF

:End
