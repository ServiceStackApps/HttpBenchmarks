using ResultsView.ServiceInterface;
using ServiceStack.Razor;

namespace ResultsView
{
    public static class HtmlExtensions
    {
         public static string ProfileUrl(this ViewPage view)
         {
             var session = view.SessionAs<UserSession>();
             return session == null || session.ProfileUrl64 == null
                 ? "/Content/img/no-profile-pic-64.png"
                 : session.ProfileUrl64.Replace("http:","");
         }
    }
}