SET HOST=127.0.0.1
REM SET HOST=10.0.0.64
SET REQUESTS=10000

:HttpListener
SET PORT=55001
SET ID=httplistener
CALL :Benchmark

GOTO End

:Benchmark

SET URL=http://%HOST%:%PORT%
REM START servers\Techempower.HttpListener\bin\Release\Techempower.HttpListener.exe 128 mysql

FOR %%A IN (json plaintext) DO ab -k -n %REQUESTS% -c 10 "%URL%/%%A" > results\raw_%REQUESTS%_10_%%A_%ID%.txt
FOR %%A IN (json plaintext) DO ab -k -n %REQUESTS% -c 100 "%URL%/%%A" > results\raw_%REQUESTS%_100_%%A_%ID%.txt
FOR %%A IN (json plaintext) DO ab -k -n %REQUESTS% -c 256 "%URL%/%%A" > results\raw_%REQUESTS%_256_%%A_%ID%.txt
ab -k -n %REQUESTS% -c 1000 "%URL%/plaintext" > results\raw_10000_1000_plaintext_%ID%.txt

FOR %%A IN (db queries fortunes updates) DO ab -k -n %REQUESTS% -c 10 "%URL%/%%A" > results\db_%REQUESTS%_10_%%A_%ID%.txt
FOR %%A IN (db queries fortunes updates) DO ab -k -n %REQUESTS% -c 100 "%URL%/%%A" > results\db_%REQUESTS%_100_%%A_%ID%.txt
FOR %%A IN (db queries fortunes updates) DO ab -k -n %REQUESTS% -c 256 "%URL%/%%A" > results\db_%REQUESTS%_256_%%A_%ID%.txt

REM FOR %%A IN (redis) DO ab -k -n %REQUESTS% -c 10 "%URL%/%%A" > results\redis_%REQUESTS%_10_%%A_%ID%.txt
REM FOR %%A IN (redis) DO ab -k -n %REQUESTS% -c 100 "%URL%/%%A" > results\redis_%REQUESTS%_100_%%A_%ID%.txt
REM FOR %%A IN (redis) DO ab -k -n %REQUESTS% -c 256 "%URL%/%%A" > results\redis_%REQUESTS%_100_%%A_%ID%.txt

GOTO :EOF

:End
