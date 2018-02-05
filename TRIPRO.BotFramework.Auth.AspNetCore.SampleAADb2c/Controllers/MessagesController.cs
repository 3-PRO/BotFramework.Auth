using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using System.Threading.Tasks;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;
using TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Dialogs;

namespace TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Controllers
{
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        private readonly MicrosoftAppCredentials credentials;
        private readonly AuthenticationOptions authOptions;

        public MessagesController(MicrosoftAppCredentials credentials, AuthenticationOptions authOptions)
        {
            this.credentials = credentials;
            this.authOptions = authOptions;
        }

        [Authorize(Roles = "Bot")]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new RootDialog(authOptions));
            }
            else
            {
                await HandleSystemMessageAsync(activity);
            }
            return Ok();
        }

        private async Task HandleSystemMessageAsync(Activity activity)
        {
            if (activity.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (activity.MembersAdded != null && activity.MembersAdded.Any())
                {
                    foreach (var newMember in activity.MembersAdded)
                    {
                        if (newMember.Id != activity.Recipient.Id)
                        {
                            ConnectorClient client = new ConnectorClient(new Uri(activity.ServiceUrl), credentials);
                            Activity reply = activity.CreateReply("Hi from Bot!");
                            await client.Conversations.SendToConversationAsync(reply);
                        }
                    }
                }
            }
            else if (activity.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (activity.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (activity.Type == ActivityTypes.Ping)
            {
            }
        }
    }
}
