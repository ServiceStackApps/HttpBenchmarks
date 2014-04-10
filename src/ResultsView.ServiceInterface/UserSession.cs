using ServiceStack;
using ServiceStack.Auth;

namespace ResultsView.ServiceInterface
{
    public class UserSession : AuthUserSession
    {
        public string ProfileUrl64 { get; set; }

        public override void OnRegistered(IServiceBase registerService)
        {
            base.OnRegistered(registerService);
        }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, System.Collections.Generic.Dictionary<string, string> authInfo)
        {
            base.OnAuthenticated(authService, session, tokens, authInfo);

            try
            {
                if (!this.Email.IsNullOrEmpty())
                    this.ProfileUrl64 = this.Email.ToGravatarUrl(size: 64);

                if (tokens.Provider == FacebookAuthProvider.Name)
                {
                    this.DisplayName = tokens.DisplayName ?? this.DisplayName;
                    this.ProfileUrl64 = "http://avatars.io/facebook/{0}?size=medium".Fmt(this.UserName)
                        .GetRedirectUrlIfAny() ?? this.ProfileUrl64;
                }
                else if (tokens.Provider == TwitterAuthProvider.Name)
                {
                    this.DisplayName = tokens.UserName ?? this.DisplayName;
                    this.ProfileUrl64 = "http://avatars.io/twitter/{0}?size=medium".Fmt(this.UserName)
                        .GetRedirectUrlIfAny() ?? this.ProfileUrl64;
                }
                else if (tokens.Provider == "GoogleOAuth")
                {
                    if (authInfo.ContainsKey("picture"))
                        this.ProfileUrl64 = authInfo["picture"];
                }
                else if (tokens.Provider == "LinkedIn")
                {
                    if (authInfo.ContainsKey("picture"))
                        this.ProfileUrl64 = authInfo["picture"];
                }
            }
            catch { }
        }
    }

    public static class SessionExtensions
    {
        /// <summary>
        /// Remove the userId from the url by using the anon CDN url
        /// </summary>
        public static string GetRedirectUrlIfAny(this string url)
        {
            var finalUrl = url;
            try
            {
                var ignore = url.GetBytesFromUrl(
                    requestFilter: req => req.AllowAutoRedirect = false,
                    responseFilter: res => finalUrl = res.Headers[HttpHeaders.Location] ?? finalUrl);
            }
            catch { }

            return finalUrl.EndsWith("avatar-64.png") ? null : finalUrl;
        }
    }

}