SET HOST=localhost
REM SET HOST=10.0.0.64
SET REQUESTS=10000
SET DB=Sqlite


:AspNet
SET PORT=55000
SET PORT=55500
SET ID=aspnet
ab -n 1 "http://%HOST%:%PORT%/reset/full"
CALL :Benchmark

GOTO End

:Benchmark

SET URL=http://%HOST%:%PORT%

FOR %%A IN (fortunes updates queries) DO ab -k -n %REQUESTS% -c 10 "%URL%/%%A" > results\db_%DB%_%REQUESTS%_10_%%A_%ID%.txt
ab -n 1 "http://%HOST%:%PORT%/reset/gc" > nul
FOR %%A IN (fortunes updates queries) DO ab -k -n %REQUESTS% -c 100 "%URL%/%%A" > results\db_%DB%_%REQUESTS%_100_%%A_%ID%.txt
ab -n 1 "http://%HOST%:%PORT%/reset/gc" > nul
FOR %%A IN (fortunes updates queries) DO ab -k -n %REQUESTS% -c 256 "%URL%/%%A" > results\db_%DB%_%REQUESTS%_256_%%A_%ID%.txt
ab -n 1 "http://%HOST%:%PORT%/reset/gc" > nul

ab -k -n %REQUESTS% -c 256 "%URL%/queries?queries=5" > results\db_%DB%_%REQUESTS%_256_queries5_%ID%.txt
ab -k -n %REQUESTS% -c 256 "%URL%/queries?queries=10" > results\db_%DB%_%REQUESTS%_256_queries10_%ID%.txt
ab -k -n %REQUESTS% -c 256 "%URL%/queries?queries=20" > results\db_%DB%_%REQUESTS%_256_queries20_%ID%.txt

GOTO :EOF

:End
