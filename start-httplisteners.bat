start servers\Techempower.HttpListener\bin\Release\Techempower.HttpListener.exe
start servers\Techempower.HttpListener.Pool\bin\Release\Techempower.HttpListener.Pool.exe
start servers\Techempower.HttpListener.SmartPool\bin\Release\Techempower.HttpListener.SmartPool.exe

ab "http://localhost:55000/json"
ab "http://localhost:55001/json"
ab "http://localhost:55002/json"
ab "http://localhost:55003/json"
