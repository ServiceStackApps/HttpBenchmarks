COPY C:\src\ServiceStack.OrmLite\src\ServiceStack.OrmLite.PostgreSQL\bin\Release\ServiceStack.OrmLite.*
COPY C:\src\ServiceStack.OrmLite\src\ServiceStack.OrmLite.PostgreSQL\bin\Release\Mono.Security.dll
COPY C:\src\ServiceStack.OrmLite\src\ServiceStack.OrmLite.PostgreSQL\bin\Release\Npgsql.dll

COPY C:\src\ServiceStack.OrmLite\src\ServiceStack.OrmLite.MySql\bin\Release\ServiceStack.OrmLite.MySql.*
COPY C:\src\ServiceStack.OrmLite\src\ServiceStack.OrmLite.MySql\bin\Release\MySql.Data.*

COPY C:\src\ServiceStack.OrmLite\src\ServiceStack.OrmLite.SqlServer\bin\Release\ServiceStack.OrmLite.SqlServer.*

COPY C:\src\ServiceStack.OrmLite\src\ServiceStack.OrmLite.Sqlite\bin\Release\ServiceStack.OrmLite.Sqlite.*
COPY C:\src\ServiceStack\lib\Mono.Data.Sqlite.dll .
COPY C:\src\ServiceStack\lib\sqlite3.dll .

COPY C:\src\ServiceStack.Redis\src\ServiceStack.Redis\bin\Release\ServiceStack.Redis.* .

COPY C:\src\ServiceStack\src\ServiceStack.Authentication.OAuth2\bin\Release\ServiceStack.Authentication.OAuth2.* .
COPY C:\src\ServiceStack\src\ServiceStack.Authentication.OAuth2\bin\Release\DotNetOpenAuth.* .

COPY C:\src\ServiceStack\src\ServiceStack.Api.Swagger\bin\Release\ServiceStack.Api.Swagger.* .
COPY C:\src\ServiceStack\src\ServiceStack.Server\bin\Release\ServiceStack.Server.* .
COPY C:\src\ServiceStack\src\ServiceStack.Razor\bin\Release\* .

COPY C:\src\Stripe\src\Stripe\StripeGateway.cs C:\src\Licensing\www.logic\