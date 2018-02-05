using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using TRIPRO.BotFramework.Auth.AspNetCore.Constants;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;
using TRIPRO.BotFramework.Auth.AspNetCore.Providers;

namespace TRIPRO.BotFramework.Auth.AspNetCore.AADb2cProvider
{

    [Serializable]
    public class AADb2cAuthProvider : IAuthProvider
    {
        public string Name
        {
            get { return "AADb2cAuthProvider"; }
        }

        public async Task<IAuthResult> GetAccessToken(AuthenticationOptions authOptions, IDialogContext context)
        {
            AuthResult authResult;
            string validated = null;
            if (context.UserData.TryGetValue($"{this.Name}{ContextConstants.AuthResultKey}", out authResult) &&
                (!authOptions.UseMagicNumber ||
                (context.UserData.TryGetValue($"{this.Name}{ContextConstants.MagicNumberValidated}", out validated) &&
                validated == "true")))
            {
                try
                {
                    // Check for expired token
                    if (authResult.ExpiresOnUtcTicks > DateTime.UtcNow.Ticks)
                        return authResult;
                    else
                    {
                        // Use refresh token to get new token
                        HttpClient client = new HttpClient();
                        string scopes = (authOptions.Scopes.Length > 0) ? String.Join("%20", authOptions.Scopes) : "openid";
                        HttpContent content = new StringContent($"grant_type=refresh_token" +
                            $"&client_id={authOptions.ClientId}" +
                            $"&client_secret={HttpUtility.UrlEncode(authOptions.ClientSecret)}" +
                            $"&scope={scopes}" +
                            $"&refresh_token={authResult.RefreshToken}" +
                            $"&redirect_uri={authOptions.RedirectUrl}");
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                        using (HttpResponseMessage response = await client.PostAsync($"{authOptions.Authority}/oauth2/v2.0/token?p={authOptions.Policy}", content))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
                                return json.ToAuthResult();
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Failed to renew token: " + ex.Message);
                    await context.PostAsync("Your credentials expired and could not be renewed automatically!");
                    await Logout(authOptions, context);
                    return null;
                }
            }
            else
                return null;
        }

        public string GetAuthUrl(AuthenticationOptions authOptions, string state)
        {
            // Build manually as MSAL does not provide a method for getting this
            string scopes = (authOptions.Scopes.Length > 0) ? String.Join("%20", authOptions.Scopes) : "openid";
            return $"{authOptions.Authority}/oauth2/v2.0/authorize?" +
                $"client_id={authOptions.ClientId}&" +
                $"response_type=code&" +
                $"redirect_uri={authOptions.RedirectUrl}&" +
                $"response_mode=query&" +
                $"scope={scopes}&" +
                $"state={state}&" +
                $"p={authOptions.Policy}";
        }

        public async Task<IAuthResult> GetTokenByAuthCodeAsync(AuthenticationOptions authOptions, string authorizationCode)
        {
            //TODO: manual
            HttpClient client = new HttpClient();
            string scopes = (authOptions.Scopes.Length > 0) ? String.Join("%20", authOptions.Scopes) : "openid";
            HttpContent content = new StringContent($"grant_type=authorization_code" +
                $"&client_id={authOptions.ClientId}" +
                $"&client_secret={HttpUtility.UrlEncode(authOptions.ClientSecret)}" +
                $"&scope={scopes}" +
                $"&code={authorizationCode}" +
                $"&redirect_uri={authOptions.RedirectUrl}");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            using (HttpResponseMessage response = await client.PostAsync($"{authOptions.Authority}/oauth2/v2.0/token?p={authOptions.Policy}", content))
            {
                if (response.IsSuccessStatusCode)
                {
                    JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    return json.ToAuthResult();
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task Logout(AuthenticationOptions authOptions, IDialogContext context)
        {
            context.UserData.RemoveValue($"{this.Name}{ContextConstants.AuthResultKey}");
            context.UserData.RemoveValue($"{this.Name}{ContextConstants.MagicNumberKey}");
            context.UserData.RemoveValue($"{this.Name}{ContextConstants.MagicNumberValidated}");
            string signoutURl = $"{authOptions.Authority}/oauth2/logout?post_logout_redirect_uri={System.Net.WebUtility.UrlEncode(authOptions.RedirectUrl)}";
            await context.PostAsync($"In order to finish the sign out, please click at this [link]({signoutURl}).");
        }

        public async Task<IAuthResult> GetAccessTokenSilent(AuthenticationOptions authOptions, IDialogContext context)
        {
            string validated = null;
            AuthResult result;
            if (context.UserData.TryGetValue($"{this.Name}{ContextConstants.AuthResultKey}", out result) &&
                context.UserData.TryGetValue($"{this.Name}{ContextConstants.MagicNumberValidated}", out validated) &&
                validated == "true")
            {
                try
                {
                    // Use refresh token to get new token
                    HttpClient client = new HttpClient();
                    string scopes = (authOptions.Scopes.Length > 0) ? String.Join("%20", authOptions.Scopes) : "openid";
                    HttpContent content = new StringContent($"grant_type=refresh_token" +
                        $"&client_id={authOptions.ClientId}" +
                        $"&client_secret={HttpUtility.UrlEncode(authOptions.ClientSecret)}" +
                        $"&scope={scopes}" +
                        $"&refresh_token={result.RefreshToken}" +
                        $"&redirect_uri={authOptions.RedirectUrl}");
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    using (HttpResponseMessage response = await client.PostAsync($"{authOptions.Authority}/oauth2/v2.0/token?p={authOptions.Policy}", content))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
                            result = json.ToAuthResult();
                            return result;
                        }
                        else
                            return null;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
                return null;
        }
    }
}
