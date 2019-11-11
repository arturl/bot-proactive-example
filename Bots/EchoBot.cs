// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples.Models
{

    class AgentResponse
    {
        public ConversationReference ConversationReference;
        public Activity ActivityFromAgent;
    }
}

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var text = turnContext.Activity.Text;

            // This is a meta-command from the user:
            if (text.StartsWith("!"))
            {
                // Pretend this code runs outside of the bot in the agent hub
                try
                {
                    var agentResponse = new Models.AgentResponse();

                    // Agent hub does not see the Activity object but it needs to maintain the ConversationReference
                    // It can get it from the first message in the conversation. Here, just get it from the current conversation:
                    agentResponse.ConversationReference = turnContext.Activity.GetConversationReference();

                    agentResponse.ActivityFromAgent = MessageFactory.Text($"You are now talking to a human");

                    string json = JsonConvert.SerializeObject(agentResponse);
                    var httpClient = new HttpClient();
                    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                    string endpointUrl;
                    bool testLocal = false;
                    if (testLocal)
                    {
                        endpointUrl = "http://localhost:3978/api/messages/external";
                    }
                    else
                    {
                        endpointUrl = "https://arturl-bot-msg.azurewebsites.net/api/messages/external";
                    }
                    var response = await httpClient.PostAsync(endpointUrl, httpContent);

                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        await turnContext.SendActivityAsync($"response: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    await turnContext.SendActivityAsync($"Exception message: {ex.Message}");
                    await turnContext.SendActivityAsync($"Exception stack: {ex.StackTrace}");
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"Bot says: {text}");
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Hello and welcome!"), cancellationToken);
                }
            }
        }
    }
}
