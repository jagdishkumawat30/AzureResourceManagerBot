using CoreBot.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs
{
    public class InitialDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<User> _userAccessor;

        public InitialDialog(UserState userState) : base(nameof(InitialDialog))
        {
            _userAccessor = userState.CreateProperty<User>("User");

            var waterfallSteps = new WaterfallStep[]
            {
                SubscriptionIdStepAsync,
                TenantIdStepAsync,
                ClientIdStepAsync,
                ClientSecretStepAsync,
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

        private async Task<DialogTurnResult> SubscriptionIdStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please provide your Azure Subscription ID.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> TenantIdStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Subscription ID"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please provide Tenant ID.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ClientIdStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Tenant ID"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please provide Client ID.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ClientSecretStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Client ID"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please provide Client Secret.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Client Secret"] = (string)stepContext.Result;

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to Confirm?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                
                AzureDetails.SubscriptionID = (string)stepContext.Values["Subscription ID"];
                AzureDetails.TenantID = (string)stepContext.Values["Tenant ID"];
                AzureDetails.ClientID = (string)stepContext.Values["Client ID"];
                AzureDetails.ClientSecret = (string)stepContext.Values["Client Secret"];

                var msg = "Here are the details you provided:\nSubscription ID: " + AzureDetails.SubscriptionID + ", Tenant ID: " + AzureDetails.TenantID + ", ClientID: " + AzureDetails.ClientID + ", Client Secret: ****************";

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg));

                AzureResourceManagerREST.GetAuthorizationToken();

                if (AzureDetails.AccessToken == null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Request failed. Access token not generated. Retry again."));
                } else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you, Request is Confirmed. You are now successfully connected to Azure Services."));
                }
                
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
