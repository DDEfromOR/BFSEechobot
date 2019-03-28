
extern alias BBC;
extern alias BB;
extern alias SE;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using BB::Microsoft.Bot.Builder;
using BBC::Microsoft.Bot.Connector.Authentication;
using BB::Microsoft.Bot.Builder.Integration;
using Microsoft.Extensions.Logging;
using SE::Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace Microsoft.EchoBot
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        public AdapterWithErrorHandler(ICredentialProvider credentialProvider, IChannelProvider channelProvider, ILogger<BotFrameworkHttpAdapter> logger, ConversationState conversationState = null)
            : base(credentialProvider, channelProvider, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError($"Exception caught : {exception.Message}");

                // Send a catch-all appology to the user.
                await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");

                if (conversationState != null)
                {
                    try
                    {
                        // Delete the conversationState for the current conversation to prevent the
                        // bot from getting stuck in a error-loop caused by being in a bad state.
                        // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                        await conversationState.DeleteAsync(turnContext);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
                    }
                }
            };
        }
    }
}
