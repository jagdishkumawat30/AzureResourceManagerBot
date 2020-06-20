using AdaptiveCards;
using CoreBot.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs.ResourceGroup
{
    public class CreateResourceGroupDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<User> _userAccessor;
        public CreateResourceGroupDialog(UserState userState) : base(nameof(CreateResourceGroupDialog))
        {
            _userAccessor = userState.CreateProperty<User>("User");

            var waterfallSteps = new WaterfallStep[]
            {
                ResourceGroupNameStepAsync,
                LocationStepAsync,
                ConfirmStepAsync,
                SummaryStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ResourceGroupNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please provide Resource group name.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> LocationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Resource Group Name"] = (string)stepContext.Result;
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please select a Location."));
            // Create card
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                // Use LINQ to turn the choices into submit actions
                Actions = AzureResourcesData.resourceGroupLocationList.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice,  // This will be a string
                }).ToList<AdaptiveAction>(),
            };
            // Prompt
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                    Prompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        // Convert the AdaptiveCard to a JObject
                        Content = JObject.FromObject(card),
                    }),
                    Choices = ChoiceFactory.ToChoices(AzureResourcesData.resourceGroupLocationList),
                    // Don't render the choices outside the card
                    Style = ListStyle.None,
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Location"] = ((FoundChoice)stepContext.Result).Value;
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to Confirm?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                AzureDetails.ResourceGroupName = (string)stepContext.Values["Resource Group Name"];
                AzureDetails.Location = (string)stepContext.Values["Location"];

                AzureResourceManagerREST.createResourceGroup().GetAwaiter().GetResult();

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Here is the response from Azure."));
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(AzureDetails.Response));

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Request Not Confirmed."));
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
    }
}
