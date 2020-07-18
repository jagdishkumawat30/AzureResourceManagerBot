// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.9.2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

using Microsoft.Extensions.Configuration;
using CoreBot.Utilities;
using CoreBot.Dialogs.ResourceGroup;

namespace CoreBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        private readonly IStatePropertyAccessor<User> _userAccessor;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger, UserState userState)
            : base(nameof(MainDialog))
        {
            Configuration = configuration;
            Logger = logger;
            _userAccessor = userState.CreateProperty<User>("User");
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new InitialDialog(userState));
            AddDialog(new CreateResourceGroupDialog(userState));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                LUISStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Configuration["LuisAppId"]) || string.IsNullOrEmpty(Configuration["LuisAPIKey"]) || string.IsNullOrEmpty(Configuration["LuisAPIHostName"]))
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file."), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                if (AzureDetails.AccessToken == null)
                {
                    var messageText = stepContext.Options?.ToString() ?? "Before starting, fill the following form to connect you to Azure.";
                    var infoMessage = MessageFactory.Text(messageText, messageText);
                    await stepContext.Context.SendActivityAsync(infoMessage, cancellationToken);

                    return await stepContext.BeginDialogAsync(nameof(InitialDialog), new User(), cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync(stepContext, cancellationToken);
                }
            }
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);

        }

        private async Task<DialogTurnResult> LUISStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken);


            var intentValue = (string)stepContext.Result;

            
            switch (AzureDetails.Intent)
            {
                case "GetAllResourceGroupsIntent":
                    AzureResourceManagerREST.getAllResourceGroupDetails().GetAwaiter().GetResult();
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Here is the details of all your resource groups:"));
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(AzureDetails.Response));
                    break;
                case "Cancel":
                    AzureDetails.AccessToken = null;
                    AzureDetails.SubscriptionID = null;
                    AzureDetails.TenantID = null;
                    AzureDetails.ClientID = null;
                    AzureDetails.ClientSecret = null;
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("You are now successfully disconnected from Azure services. Your Azure details has been cleared."));
                    break;
                case "CreateResourceGroupIntent":
                    return await stepContext.BeginDialogAsync(nameof(CreateResourceGroupDialog), new User(), cancellationToken);
                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Kindly rephrase your query."));
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if (stepContext.Result != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you for using Azure Resource Manager Bot."), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you."), cancellationToken);
            }
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
