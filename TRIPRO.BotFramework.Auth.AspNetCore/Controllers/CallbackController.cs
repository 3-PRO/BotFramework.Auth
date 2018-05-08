using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Specialized;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TRIPRO.BotFramework.Auth.AspNetCore.Constants;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;
using TRIPRO.BotFramework.Auth.AspNetCore.Providers;

namespace TRIPRO.BotFramework.Auth.AspNetCore.Controllers
{
    [Route("[controller]")]
    public class CallbackController : Controller
    {
        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private static readonly uint MaxWriteAttempts = 5;
        private readonly MicrosoftAppCredentials credentials;

        public CallbackController(MicrosoftAppCredentials credentials)
        {
            this.credentials = credentials;
        }

        [HttpGet]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
        {
            try
            {
                // Use the state parameter to get correct IAuthProvider and ResumptionCookie
                string decoded = Encoding.UTF8.GetString(HttpServerUtility.UrlTokenDecode(state));
                NameValueCollection queryString = HttpUtility.ParseQueryString(decoded);
                Assembly assembly = Assembly.Load(queryString["providerassembly"]);
                Type type = assembly.GetType(queryString["providertype"]);
                string providername = queryString["providername"];
                IAuthProvider authProvider;
                if (type.GetConstructor(new Type[] { typeof(string) }) != null)
                    authProvider = (IAuthProvider)Activator.CreateInstance(type, providername);
                else
                    authProvider = (IAuthProvider)Activator.CreateInstance(type);

                // Get the conversation reference
                ConversationReference conversationRef = UrlToken.Decode<ConversationReference>(queryString["conversationRef"]);

                Activity message = conversationRef.GetPostToBotMessage();
                using (ILifetimeScope scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                {
                    // Get the UserData from the original conversation
                    IBotDataStore<BotData> stateStore = scope.Resolve<IBotDataStore<BotData>>();
                    Address key = Address.FromActivity(message);
                    BotData userData = await stateStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);

                    // Get Access Token using authorization code
                    AuthenticationOptions authOptions = userData.GetProperty<AuthenticationOptions>($"{authProvider.Name}{ContextConstants.AuthOptions}");
                    IAuthResult authResult = await authProvider.GetTokenByAuthCodeAsync(authOptions, code);

                    // Generate magic number and attempt to write to userdata
                    int magicNumber = GenerateRandomNumber();
                    bool writeSuccessful = false;
                    uint writeAttempts = 0;
                    while (!writeSuccessful && writeAttempts++ < MaxWriteAttempts)
                    {
                        try
                        {
                            userData.SetProperty($"{authProvider.Name}{ContextConstants.AuthResultKey}", authResult);
                            //parse token and save to claims
                            if (authOptions.UseMagicNumber)
                            {
                                userData.SetProperty($"{authProvider.Name}{ContextConstants.MagicNumberKey}", magicNumber);
                                userData.SetProperty($"{authProvider.Name}{ContextConstants.MagicNumberValidated}", "false");
                            }
                            await stateStore.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                            await stateStore.FlushAsync(key, CancellationToken.None);
                            writeSuccessful = true;
                        }
                        catch (Exception)
                        {
                            writeSuccessful = false;
                        }
                    }

                    ContentResult contentResponse = new ContentResult();
                    contentResponse.ContentType = @"text/html";
                    message.Text = String.Empty; // fail the login process if we can't write UserData

                    //await Conversation.ResumeAsync(conversationRef, message);
                    //ConnectorClient client = new ConnectorClient(new Uri(message.ServiceUrl), credentials);
                    //await client.Conversations.SendToConversationAsync(message);

                    if (!writeSuccessful)
                    {
                        contentResponse.Content = await (new StringContent($"<html><body>Could not log you in at this time, please try again later</body></html>", Encoding.UTF8)).ReadAsStringAsync();
                        return contentResponse;
                    }
                    else
                    {
                        // check if the user has configured an alternate magic number view
                        if (!String.IsNullOrEmpty(authOptions.MagicNumberView))
                        {
                            return new RedirectResult(new Uri(String.Format(authOptions.MagicNumberView, magicNumber), UriKind.Relative).ToString());
                        }
                        else
                        {
                            contentResponse.Content = await (new StringContent($"<html><body>Almost done! Please copy Magic number to chat so your authentication can complete - <strong>{magicNumber}</strong>.</body></html>", Encoding.UTF8)).ReadAsStringAsync();
                            return contentResponse;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Callback is called with no pending message as a result the login flow cannot be resumed.
                return BadRequest(ex.Message);
            }
        }

        private int GenerateRandomNumber()
        {
            int number = 0;
            byte[] randomNumber = new byte[1];
            do
            {
                rngCsp.GetBytes(randomNumber);
                int digit = randomNumber[0] % 10;
                number = number * 10 + digit;
            } while (number.ToString().Length < 6);
            return number;
        }
    }
}
