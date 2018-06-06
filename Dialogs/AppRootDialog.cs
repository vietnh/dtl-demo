using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    // Model Id == App Id that you'll find in LUIS Portal
    // Find at: https://www.luis.ai/home/keys 
    // Subscription Key is one of the two keys from your Cognitive Services App.
    // Find at: https://portal.azure.com in the Resource Group where you've created
    // your Cognitive Services resource on the Keys blade.
    [LuisModel("203c9bdc-1a3d-473b-8373-ef622e482601", "3a99606f687246c5b8d953ab4052afd8")]
    [Serializable]
    public class AppRootDialog : LuisDialog<object>
    {
        private const string EntityBook = "Book";

        [LuisIntent("Reserve.Book")]
        public async Task ReserveBook(IDialogContext context,
                                 IAwaitable<IMessageActivity> activity,
                                 LuisResult result)
        {
            Trace.TraceInformation("AppRootDialog::ReserveBook");

            var message = await activity;
            IAwaitable<object> awaitableMessage = await activity as IAwaitable<object>;

            await context.PostAsync("I see you want to buy a book.");

            await context.Forward(new AppAuthDialog(),
                this.ResumeAfterSuccessfulDialog, message, CancellationToken.None);
        }

        private async Task ResumeAfterSuccessfulDialog(IDialogContext context, IAwaitable<object> result)
        {
            await context.PostAsync("Thank you. We're all done. What else can I do for you?");

            context.Done<object>(null);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hi! How can I help you?");

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }
    }
}