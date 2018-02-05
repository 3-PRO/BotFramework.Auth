using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;

namespace TRIPRO.BotFramework.Auth.AspNetCore.Providers
{
    public interface IAuthProvider
    {
        //void Init();
        string GetAuthUrl(AuthenticationOptions authOptions, string state);
        Task<IAuthResult> GetTokenByAuthCodeAsync(AuthenticationOptions authOptions, string authorizationCode);
        Task<IAuthResult> GetAccessToken(AuthenticationOptions authOptions, IDialogContext context);
        Task<IAuthResult> GetAccessTokenSilent(AuthenticationOptions options, IDialogContext context);
        Task Logout(AuthenticationOptions authOptions, IDialogContext context);
        string Name
        {
            get;
        }
    }
}
