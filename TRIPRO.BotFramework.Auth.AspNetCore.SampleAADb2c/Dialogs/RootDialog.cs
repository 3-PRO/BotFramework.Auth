using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Threading;
using System.Threading.Tasks;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;
using TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Models;
using TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Providers;

namespace TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private AuthenticationOptions authOptions;

        public RootDialog(AuthenticationOptions authOptions)
        {
            this.authOptions = authOptions;
        }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            await context.Forward(new AuthDialog(new AADb2cAuthProvider(), authOptions), async (IDialogContext authContext, IAwaitable<IAuthResult> authResult) =>
            {
                TriProAuthResult result = (TriProAuthResult)await authResult;

                // Use TriProAuthResult
                await authContext.PostAsync($"I'm a simple bot that doesn't do much, but I know your name is {result.UserName} and you are logged in using {result.IdentityProvider}");

                authContext.Wait(MessageReceivedAsync);

            }, context.MakeMessage(), CancellationToken.None);
        }
    }
}
