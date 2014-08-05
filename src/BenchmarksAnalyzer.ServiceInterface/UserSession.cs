using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack;
using ServiceStack.Auth;

namespace BenchmarksAnalyzer.ServiceInterface
{
    public class UserSession : AuthUserSession
    {
        [DataMember]
        public string ProfileUrl64 { get; set; }

        public override void OnRegistered(IServiceBase registerService)
        {
            base.OnRegistered(registerService);
        }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, 
            IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            base.OnAuthenticated(authService, session, tokens, authInfo);

            this.ProfileUrl64 = session.GetProfileUrl();
        }
    }

}