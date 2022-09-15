// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.BotBuilderSamples.Models;
using Microsoft.Recognizers.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace AdaptiveCardsBot
{
    // This bot will respond to the user's input with an Adaptive Card.
    // Adaptive Cards are a way for developers to exchange card content
    // in a common and consistent way. A simple open card format enables
    // an ecosystem of shared tooling, seamless integration between apps,
    // and native cross-platform performance on any device.
    // For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    // This is a Transient lifetime service. Transient lifetime services are created
    // each time they're requested. For each Activity received, a new instance of this
    // class is created. Objects that are expensive to construct, or have a lifetime
    // beyond the single turn, should be carefully managed.

    public class AdaptiveCardsBot : TeamsActivityHandler
    {
        private const string WelcomeText = @"This bot will introduce you to AdaptiveCards.
                                            Type anything to see an AdaptiveCard.";

        // This array contains the file location of our adaptive cards
        private readonly string[] _cards =
        {
            Path.Combine(".", "Resources", "FlightItineraryCard.json"),
            Path.Combine(".", "Resources", "ImageGalleryCard.json"),
            Path.Combine(".", "Resources", "LargeWeatherCard.json"),
            Path.Combine(".", "Resources", "RestaurantCard.json"),
            Path.Combine(".", "Resources", "SolitaireCard.json"),
        };

        protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        protected override Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            var asJobject = JObject.FromObject(taskModuleRequest.Data);
           // var value = asJobject.ToObject<CardTaskFetchValue<string>>()?.Data;

            var taskInfo = new TaskModuleTaskInfo();
            taskInfo.Url = taskInfo.FallbackUrl = "https://29c9-2604-3d09-2881-8f00-d00e-adb3-fd50-b773.ngrok.io";
            SetTaskInfo(taskInfo, new UISettings(1000, 700, "You Tube Video", "YouTube", "You Tube"));


            return Task.FromResult( new TaskModuleResponse
            {
                Task = new TaskModuleContinueResponse()
                {
                    Value = taskInfo,
                },
            });
        }

        private static void SetTaskInfo(TaskModuleTaskInfo taskInfo, UISettings uIConstants)
        {
            taskInfo.Height = uIConstants.Height;
            taskInfo.Width = uIConstants.Width;
            taskInfo.Title = uIConstants.Title.ToString();
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            var activityValue = ((JObject)turnContext.Activity.Value).ToObject<AdaptiveCardInvokeValue>();

            string cardJson = string.Empty;

            switch (activityValue.Action.Verb)
            {
                //When a user clicks the approve button, update the message with the base adaptive card where the user is in in the userIds array.
                case "approveClicked":
                    var cardAttachment = CreateAdaptiveCardAttachment(_cards[1]);

                    //cardJson = GetCard(@".\AdaptiveCards\ApprovalRequest_AdaptiveCard.json");
                    //var attachment = new Attachment
                    //{
                    //    ContentType = AdaptiveCard.ContentType,
                    //    Content = JsonConvert.DeserializeObject(cardJson),
                    //};

                    var messageActivity = MessageFactory.Attachment(cardAttachment);
                    messageActivity.Id = turnContext.Activity.ReplyToId;
                    await turnContext.UpdateActivityAsync(messageActivity);

                    break;

                //For each user in the userIds array, get the relevant card depending on their role and actions.
                case "refreshCard":
                    cardJson = _cards[1];
                    break;
            }

            var adaptiveCardResponse = new AdaptiveCardInvokeResponse()
            {
                StatusCode = 200,
                Type = AdaptiveCard.ContentType,
                Value = JsonConvert.DeserializeObject(cardJson)
            };

            return CreateInvokeResponse(adaptiveCardResponse);
        }

        /* protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
         {
             var name = turnContext.Activity.Name.ToString();
             await turnContext.SendActivityAsync(MessageFactory.Text($"Received card action"), cancellationToken);

             return CreateInvokeResponse(CardResponse());
         }*/

        private AdaptiveCardInvokeResponse CardResponse()
        {
            return new AdaptiveCardInvokeResponse()
            {
                StatusCode = 200,
                Type = AdaptiveCard.ContentType,
                Value = "Test"
            };
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await SendWelcomeMessageAsync(turnContext, cancellationToken);
        }

        protected override async Task OnInstallationUpdateActivityAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity; if
            (string.Equals(activity.Action, "Add",
            StringComparison.InvariantCultureIgnoreCase))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome message"), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text($"Exit message"), cancellationToken);
            }
            return;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            

            var value = turnContext.Activity.From.Id;

            var r = new Random();
            var cardAttachment = CreateAdaptiveCardAttachment(_cards[0]);
            var cardAttachment2 = CreateAdaptiveCardAttachment(_cards[1]);

            var welcomeAttachments = new List<Attachment>();
            welcomeAttachments.Add(cardAttachment);
            welcomeAttachments.Add(cardAttachment2);
           // await turnContext.SendActivityAsync(MessageFactory.Carousel(welcomeAttachments));

            await turnContext.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken);
            //Thread.Sleep(16000);


            await turnContext.SendActivityAsync(MessageFactory.Text("Please enter any text to see another card."), cancellationToken);
           
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                try
                {

                    /* var meetingId = turnContext.Activity.GetChannelData<TeamsChannelData>()?.Meeting?.Id;
                     var participant = await TeamsInfo.GetMeetingParticipantAsync(turnContext, meetingId, member.AadObjectId, "1cb47ee9-1bec-4069-8a9b-74ba004dea11").ConfigureAwait(false);
                     var member2 = participant.User;
                     var meetingInfo = participant.Meeting;
                     var conversation = participant.Conversation;

                     await turnContext.SendActivityAsync(MessageFactory.Text($"The participant role is: {meetingInfo.Role}"), cancellationToken);*/
                }
                catch (Exception ex)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Error: {ex.Message}"), cancellationToken);
                }

                if (member.Id != turnContext.Activity.Recipient.Id)
                {


                    await turnContext.SendActivityAsync(
                        $"Welcome to Adaptive Cards Bot {member.Name}. {WelcomeText}",
                        cancellationToken: cancellationToken);
                }
            }
        }

        private static Attachment CreateAdaptiveCardAttachment(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }
    }
}
