using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using System.Threading;
using BotAuth.Models;
using BotAuth.Dialogs;
using BotAuth;
using BotAuth.AADv2;
using BotAuth.GenericOAuth2;
using System.Net.Http;
using SD = System.Diagnostics;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class MultiProviderAuthBot : IDialog<string>
    {
        public async Task StartAsync(IDialogContext context)
        {
            SD.Trace.TraceInformation("AppAuthDialog::StartAsync");

            await context.PostAsync("Let's make sure you're logged in ...");

            context.Wait(MessageReceivedAsync);

            //var providers = AuthProviderConfig.GetAuthProviders();
            //PromptDialog.Choice(context, this.AfterProviderChoiceDialog, providers, "Please select a provider we can use to look-up your information");
        }

        public virtual async Task MessageReceivedAsync(
            IDialogContext context,
            IAwaitable<object> awaitableMessage)
        {
            SD.Trace.TraceInformation("AppAuthDialog::MessageReceivedAsync");

            var activity = await awaitableMessage as Activity;

            // Save the message for later
            context.ConversationData.SetValue<Activity>("OriginalMessage", activity);

            // Let the user chose a provider
            var providers = AuthProviderConfig.GetAuthProviders();
            PromptDialog.Choice(context, this.AfterProviderChoiceDialog, providers, "Please select a provider we can use to look-up your information");
        }

        private async Task AfterProviderChoiceDialog(IDialogContext choiceContext, IAwaitable<AuthProviderConfig> choiceResult)
        {
            var providerConfig = await choiceResult;
            choiceContext.ConversationData.SetValue<AuthProviderConfig>("ProviderConfig", providerConfig);
            IAuthProvider authProvider;
            if (providerConfig.ProviderName == "Microsoft")
                authProvider = new MSALAuthProvider();
            else
                authProvider = new GenericOAuth2Provider($"GenericOAuth2Provider{providerConfig.ClientType}");

            choiceContext.ConversationData.SetValue<IAuthProvider>("AuthProvider", authProvider);

            choiceContext.Call(
                new AuthDialog(authProvider, providerConfig),
                this.AfterInitialAuthDialog);
        }

        private async Task AfterInitialAuthDialog(IDialogContext authContext, IAwaitable<AuthResult> awaitableAuthResult)
        {
            SD.Trace.TraceInformation("AppAuthDialog::AfterInitialAuthDialog");

            var result = await awaitableAuthResult;

            // Use token to call into service
            var prov = authContext.ConversationData.Get<AuthProviderConfig>("AuthProvider");
            if (prov.ProviderName == "Microsoft")
            {
                var bytes = await new HttpClient().GetStreamWithAuthAsync(result.AccessToken, prov.PictureEndpoint);
                var pic = "data:image/png;base64," + Convert.ToBase64String(bytes);
                var m = authContext.MakeMessage();
                m.Attachments.Add(new Attachment("image/png", pic));
                await authContext.PostAsync(m);
            }
            else
            {
                var json = await new HttpClient().GetWithAuthAsync(result.AccessToken, prov.PictureEndpoint);
                var pic = "";
                if (prov.ProviderName == "Google")
                    pic = json.Value<string>("picture");
                else if (prov.ProviderName == "Facebook")
                    pic = json.SelectToken("picture.data").Value<string>("url");
                else if (prov.ProviderName == "LinkedIn")
                    pic = json.Value<string>("pictureUrl");
                var m = authContext.MakeMessage();
                m.Attachments.Add(new Attachment("image/png", pic));
                await authContext.PostAsync(m);
            }

            authContext.Call(new BookReservationDialog(), this.ResumeAfterOptionDialog);
        }

        private async Task ResumeAfterOptionDialog(IDialogContext context, IAwaitable<object> result)
        {
            try
            {
                SD.Trace.TraceInformation("AppAuthDialog::ResumeAfterOptionDialog");
                var message = await result;
            }
            catch (Exception ex)
            {
                await context.PostAsync($"Failed with message: {ex.Message}");
            }
            finally
            {
                context.Done<object>(null);
            }
        }
    }
}