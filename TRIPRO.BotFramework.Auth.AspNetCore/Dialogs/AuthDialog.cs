using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TRIPRO.BotFramework.Auth.AspNetCore.Constants;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;
using TRIPRO.BotFramework.Auth.AspNetCore.Providers;

namespace TRIPRO.BotFramework.Auth.AspNetCore.Dialogs
{
    [Serializable]
    public class AuthDialog : IDialog<IAuthResult>
    {
        protected IAuthProvider authProvider;
        protected AuthenticationOptions authOptions;
        protected string prompt { get; }

        public AuthDialog(IAuthProvider AuthProvider, AuthenticationOptions AuthOptions, string Prompt = "Please click to sign in: ")
        {
            this.prompt = Prompt;
            this.authProvider = AuthProvider;
            this.authOptions = AuthOptions;
        }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            IMessageActivity msg = await argument;

            AuthResult authResult;
            string validated = "";
            int magicNumber = 0;
            if (context.UserData.TryGetValue($"{this.authProvider.Name}{ContextConstants.AuthResultKey}", out authResult))
            {
                try
                {
                    //IMPORTANT: DO NOT REMOVE THE MAGIC NUMBER CHECK THAT WE DO HERE. THIS IS AN ABSOLUTE SECURITY REQUIREMENT
                    //REMOVING THIS WILL REMOVE YOUR BOT AND YOUR USERS TO SECURITY VULNERABILITIES. 
                    //MAKE SURE YOU UNDERSTAND THE ATTACK VECTORS AND WHY THIS IS IN PLACE.
                    context.UserData.TryGetValue<string>($"{this.authProvider.Name}{ContextConstants.MagicNumberValidated}", out validated);
                    if (validated == "true" || !this.authOptions.UseMagicNumber)
                    {
                        // Try to get token to ensure it is still good
                        IAuthResult token = await this.authProvider.GetAccessToken(this.authOptions, context);
                        if (token != null)
                            context.Done(token);
                        else
                        {
                            // Save authenticationOptions in UserData
                            context.UserData.SetValue<AuthenticationOptions>($"{this.authProvider.Name}{ContextConstants.AuthOptions}", this.authOptions);

                            // Get ConversationReference and combine with AuthProvider type for the callback
                            ConversationReference conversationRef = context.Activity.ToConversationReference();
                            string state = getStateParam(conversationRef);
                            string authenticationUrl = this.authProvider.GetAuthUrl(this.authOptions, state);
                            await PromptToLogin(context, msg, authenticationUrl);
                            context.Wait(this.MessageReceivedAsync);
                        }
                    }
                    else if (context.UserData.TryGetValue<int>($"{this.authProvider.Name}{ContextConstants.MagicNumberKey}", out magicNumber))
                    {
                        if (msg.Text == null)
                        {
                            await context.PostAsync($"Please paste back the number you received in your authentication screen.");

                            context.Wait(this.MessageReceivedAsync);
                        }
                        else
                        {
                            // handle at mentions in Teams
                            string text = msg.Text;
                            if (text.Contains("</at>"))
                                text = text.Substring(text.IndexOf("</at>") + 5).Trim();

                            if (text.Length >= 6 && magicNumber.ToString() == text.Substring(0, 6))
                            {
                                context.UserData.SetValue<string>($"{this.authProvider.Name}{ContextConstants.MagicNumberValidated}", "true");
                                await context.PostAsync($"Thanks {authResult.UserName}. You are now logged in. ");
                                context.Done(authResult);
                            }
                            else
                            {
                                context.UserData.RemoveValue($"{this.authProvider.Name}{ContextConstants.AuthResultKey}");
                                context.UserData.SetValue<string>($"{this.authProvider.Name}{ContextConstants.MagicNumberValidated}", "false");
                                context.UserData.RemoveValue($"{this.authProvider.Name}{ContextConstants.MagicNumberKey}");
                                await context.PostAsync($"I'm sorry but I couldn't validate your number. Please try authenticating once again. ");
                                context.Wait(this.MessageReceivedAsync);
                            }
                        }
                    }
                }
                catch
                {
                    context.UserData.RemoveValue($"{this.authProvider.Name}{ContextConstants.AuthResultKey}");
                    context.UserData.SetValue($"{this.authProvider.Name}{ContextConstants.MagicNumberValidated}", "false");
                    context.UserData.RemoveValue($"{this.authProvider.Name}{ContextConstants.MagicNumberKey}");
                    await context.PostAsync($"I'm sorry but something went wrong while authenticating.");
                    context.Done<IAuthResult>(null);
                }
            }
            else
            {
                // Try to get token
                IAuthResult token = await this.authProvider.GetAccessToken(this.authOptions, context);
                if (token != null)
                    context.Done(token);
                else
                {
                    if (msg.Text != null &&
                        CancellationWords.GetCancellationWords().Contains(msg.Text.ToUpper()))
                    {
                        context.Done<IAuthResult>(null);
                    }
                    else
                    {
                        // Save authenticationOptions in UserData
                        context.UserData.SetValue<AuthenticationOptions>($"{this.authProvider.Name}{ContextConstants.AuthOptions}", this.authOptions);

                        // Get ConversationReference and combine with AuthProvider type for the callback
                        ConversationReference conversationRef = context.Activity.ToConversationReference();
                        string state = getStateParam(conversationRef);
                        string authenticationUrl = this.authProvider.GetAuthUrl(this.authOptions, state);
                        await PromptToLogin(context, msg, authenticationUrl);
                        context.Wait(this.MessageReceivedAsync);
                    }
                }
            }
        }

        private string getStateParam(ConversationReference conversationRef)
        {
            System.Collections.Specialized.NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["conversationRef"] = UrlToken.Encode(conversationRef);
            queryString["providerassembly"] = this.authProvider.GetType().Assembly.FullName;
            queryString["providertype"] = this.authProvider.GetType().FullName;
            queryString["providername"] = this.authProvider.Name;
            return HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(queryString.ToString()));
        }

        /// <summary>
        /// Prompts the user to login. This can be overridden inorder to allow custom prompt messages or cards per channel.
        /// </summary>
        /// <param name="context">Chat context</param>
        /// <param name="msg">Chat message</param>
        /// <param name="authenticationUrl">OAuth URL for authenticating user</param>
        /// <returns>Task from Posting or prompt to the context.</returns>
        protected virtual Task PromptToLogin(IDialogContext context, IMessageActivity msg, string authenticationUrl)
        {
            Attachment plAttachment = null;
            SigninCard plCard;
            if (msg.ChannelId == "msteams")
                plCard = new SigninCard(this.prompt, GetCardActions(authenticationUrl, "openUrl"));
            else
                plCard = new SigninCard(this.prompt, GetCardActions(authenticationUrl, "signin"));
            plAttachment = plCard.ToAttachment();

            IMessageActivity response = context.MakeMessage();
            response.Recipient = msg.From;
            response.Type = "message";

            response.Attachments = new List<Attachment>();
            response.Attachments.Add(plAttachment);

            return context.PostAsync(response);
        }

        private List<CardAction> GetCardActions(string authenticationUrl, string actionType)
        {
            List<CardAction> cardButtons = new List<CardAction>();
            CardAction plButton = new CardAction()
            {
                Value = authenticationUrl,
                Type = actionType,
                Title = "Authentication Required"
            };
            cardButtons.Add(plButton);
            return cardButtons;
        }
    }
}
