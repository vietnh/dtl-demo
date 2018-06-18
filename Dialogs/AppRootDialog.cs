using Google.Maps;
using Google.Maps.StaticMaps;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    public delegate Task LuisActionHandler(IDialogContext context, object actionResult);
    public delegate Task LuisActionActivityHandler(IDialogContext context, IMessageActivity message, object actionResult);

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
        private const string UserSessionDataKey = "userdata";
        private const string LuisActionDataKey = "luisaction";
        private const string ActivityDataKey = "activity";
        private const string IntentDataKey = "intent";
        private readonly LuisActionResolver _actionResolver;

        public AppRootDialog() : base(new LuisService(new LuisModelAttribute("203c9bdc-1a3d-473b-8373-ef622e482601", "3a99606f687246c5b8d953ab4052afd8")))
        {
            _actionResolver = new LuisActionResolver(typeof(GetOrderStatusAction).Assembly);
        }

        //[LuisIntent("Reserve.Book")]
        //public async Task ReserveBook(IDialogContext context,
        //                         IAwaitable<IMessageActivity> activity,
        //                         LuisResult result)
        //{
        //    Trace.TraceInformation("AppRootDialog::ReserveBook");

        //    var message = await activity;
        //    IAwaitable<object> awaitableMessage = await activity as IAwaitable<object>;

        //    await context.PostAsync("I see you want to buy a book.");

        //    await context.Forward(new AppAuthDialog(),
        //        this.ResumeAfterSuccessfulDialog, message, CancellationToken.None);
        //}

        [LuisIntent("Order.GetStatus")]
        public async Task OrderStatus(IDialogContext context, object actionResult)
        {
            Trace.TraceInformation("AppRootDialog::GetOrderStatus");

            IMessageActivity message = null;

            var heroCard = actionResult as HeroCard;
            if (heroCard == null)
            {
                message = context.MakeMessage();
                message.Text = actionResult as string;
            }
            else
            {
                message = GetCardMessage(context, heroCard);
            }

            await context.PostAsync(message);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, object result)
        {
            await context.PostAsync("Hi! How can I help you?");

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, object result)
        {
            string message = $"Sorry, I did not understand. Type 'help' if you need assistance.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }

        protected override async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            var message = await item as Activity;
            var messageText = await GetLuisQueryTextAsync(context, message);

            var tasks = this.services.Select(s => s.QueryAsync(messageText, context.CancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks);

            var winners = from result in results.Select((value, index) => new { value, index })
                          let resultWinner = this.BestIntentFrom(result.value)
                          where resultWinner != null
                          select new LuisServiceResult(result.value, resultWinner, this.services[result.index]);

            var winner = this.BestResultFrom(winners);

            if (winner == null)
            {
                throw new InvalidOperationException("No winning intent selected from Luis results.");
            }

            var userProfile = context.ConversationData.GetValueOrDefault<UserProfile>(UserSessionDataKey, null);
            if (userProfile == null || string.IsNullOrEmpty(userProfile.EMail))
            {
                context.ConversationData.SetValue<LuisResult>(LuisActionDataKey, winner.Result);
                context.ConversationData.SetValue<Activity>(ActivityDataKey, message);
                await context.Forward(new AppAuthDialog(), this.ResumeAfterLoggedInDialog, message, CancellationToken.None);
            }
            else
            {
                var intentName = default(string);
                var luisAction = _actionResolver.ResolveActionFromLuisIntent(winner.Result, out intentName);
                if (luisAction != null)
                    await DispatchToLuisActionActivityHandler(context, message, intentName, luisAction);
                else
                    await base.MessageReceived(context, item);
            }
        }

        protected virtual async Task DispatchToLuisActionActivityHandler(IDialogContext context, IMessageActivity item, string intentName, ILuisAction luisAction)
        {
            var actionHandlerByIntent = new Dictionary<string, LuisActionActivityHandler>(this.GetActionHandlersByIntent());

            var handler = default(LuisActionActivityHandler);
            if (!actionHandlerByIntent.TryGetValue(intentName, out handler))
            {
                handler = actionHandlerByIntent[string.Empty];
            }

            if (handler != null)
            {
                await handler(context, item, await PerformActionFulfillment(context, item, luisAction));
            }
            else
            {
                throw new Exception($"No default intent handler found.");
            }
        }

        protected virtual IDictionary<string, LuisActionActivityHandler> GetActionHandlersByIntent()
        {
            return EnumerateHandlers(this).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        static IEnumerable<KeyValuePair<string, LuisActionActivityHandler>> EnumerateHandlers(object dialog)
        {
            var type = dialog.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                var intents = method.GetCustomAttributes<LuisIntentAttribute>(inherit: true).ToArray();
                LuisActionActivityHandler intentHandler = null;

                try
                {
                    intentHandler = (LuisActionActivityHandler)Delegate.CreateDelegate(typeof(LuisActionActivityHandler), dialog, method, throwOnBindFailure: false);
                }
                catch (ArgumentException)
                {
                    // "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type."
                    // https://github.com/Microsoft/BotBuilder/issues/634
                    // https://github.com/Microsoft/BotBuilder/issues/435
                }

                // fall back for compatibility
                if (intentHandler == null)
                {
                    try
                    {
                        var handler = (LuisActionHandler)Delegate.CreateDelegate(typeof(LuisActionHandler), dialog, method, throwOnBindFailure: false);

                        if (handler != null)
                        {
                            // thunk from new to old delegate type
                            intentHandler = (context, message, result) => handler(context, result);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // "Cannot bind to the target method because its signature or security transparency is not compatible with that of the delegate type."
                        // https://github.com/Microsoft/BotBuilder/issues/634
                        // https://github.com/Microsoft/BotBuilder/issues/435
                    }
                }

                if (intentHandler != null)
                {
                    var intentNames = intents.Select(i => i.IntentName).DefaultIfEmpty(method.Name);

                    foreach (var intentName in intentNames)
                    {
                        var key = string.IsNullOrWhiteSpace(intentName) ? string.Empty : intentName;
                        yield return new KeyValuePair<string, LuisActionActivityHandler>(intentName, intentHandler);
                    }
                }
                else
                {
                    if (intents.Length > 0)
                    {
                        var msg = $"Handler '{method.Name}' signature is not valid for the following intent/s: {string.Join(";", intents.Select(i => i.IntentName))}";
                        throw new InvalidIntentHandlerException(msg, method);
                    }
                }
            }
        }

        private async Task ResumeAfterLoggedInDialog(IDialogContext context, IAwaitable<object> result)
        {
            var intentName = default(string);
            var winnerResult = context.ConversationData.GetValueOrDefault<LuisResult>(LuisActionDataKey, null);
            var luisAction = _actionResolver.ResolveActionFromLuisIntent(winnerResult, out intentName);
            if (luisAction != null)
            {
                var item = context.ConversationData.GetValue<Activity>(ActivityDataKey);
                await DispatchToLuisActionActivityHandler(context, item, intentName, luisAction);
            }
            else
            {
                context.Wait(this.MessageReceived);
            }
        }

        protected virtual async Task<object> PerformActionFulfillment(IDialogContext context, IMessageActivity item, ILuisAction luisAction)
        {
            return await luisAction.FulfillAsync();
        }

        private static IMessageActivity GetCardMessage(IDialogContext context, HeroCard card)
        {
            var message = context.MakeMessage();
            if (message.Attachments == null)
                message.Attachments = new List<Attachment>();

            message.Attachments.Add(card.ToAttachment());
            return message;
        }
    }
}