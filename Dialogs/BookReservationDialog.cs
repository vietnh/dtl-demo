using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class BookReservationDialog : IDialog<object>
    {
        private const string UserSessionDataKey = "userdata";
        private string _reservationChoice;

        public async Task StartAsync(IDialogContext context)
        {
            Trace.TraceInformation("BookReservationDialog::StartAsync");

            await context.PostAsync("Welcome to Book Store!");

            UserProfile userInfo = context.ConversationData.GetValue<UserProfile>(UserSessionDataKey);
            IReservationOperations ihro = ServiceLocator.GetReservationOperations();
            IList<string> menuOptions = await ihro.GetExistingReservations(userInfo.Id);

            PromptDialog.Choice(context, OnOptionSelected, menuOptions,
                    "Please choose book genres:",
                    "Not a valid option", 2);
        }

        private async Task OnOptionSelected(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                Trace.TraceInformation("AppAuthDialog::OnOptionSelected");
                string optionSelected = await result;

                UserProfile userInfo = context.ConversationData.GetValue<UserProfile>(UserSessionDataKey);
                userInfo.ActiveReservation = optionSelected;
                _reservationChoice = optionSelected;
                context.ConversationData.SetValue<UserProfile>(UserSessionDataKey, userInfo);

                await context.PostAsync($"Ok. Searching for Books with genre {optionSelected}");

                IBookReservationOperations icro = ServiceLocator.GetBookReservationOperations();
                IList<Book> books = await icro.GetBookAvailability(_reservationChoice);

                await context.PostAsync($"I found in total {books.Count()} Books for your genre");

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                List<string> bookChoices = new List<string>();
                foreach (var book in books)
                {
                    bookChoices.Add(book.Name);
                    HeroCard heroCard = new HeroCard()
                    {
                        Title = book.Name,
                        Subtitle = $"{book.Rating} stars. {book.NumberOfReviews} reviews. From ${book.PriceStarting} per day.",
                        Images = new List<CardImage>()
                        {
                            new CardImage() { Url = book.Image }
                        },
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "More details",
                                Type = ActionTypes.OpenUrl,
                                Value = $"https://www.bing.com/search?q=books+genre+" + HttpUtility.UrlEncode(optionSelected)
                            }
                        }
                    };

                    resultMessage.Attachments.Add(heroCard.ToAttachment());
                }

                await context.PostAsync(resultMessage);
                bookChoices.Sort((h1, h2) => String.Compare(h1, h2, StringComparison.Ordinal));

                PromptDialog.Choice(context, this.OnPickBook, bookChoices,
                   "Please pick your Book:",
                   "Not a valid option", 3);
            }
            catch (TooManyAttemptsException ex)
            {
                string fullError = ex.ToString();
                Trace.TraceError(fullError);

                await context.PostAsync($"Sorry, I don't understand.");

                context.Done(true);
            }
        }

        private IForm<BookQuery> BuildBookForm()
        {
            return new FormBuilder<BookQuery>()
                .Field(nameof(BookQuery.Start))
                .AddRemainingFields()
                .Build();
        }

        private async Task OnPickBook(IDialogContext context, IAwaitable<string> result)
        {
            string optionSelected = await result;
            string BookIdText = optionSelected.Substring(optionSelected.LastIndexOf(' ') + 1);
            int BookId = int.Parse(BookIdText);

            await context.PostAsync($"Booking your Book '{optionSelected}', please wait ...");

            //BookQuery searchQuery = context.ConversationData.GetValue<BookQuery>("BookQuery");
            int reservationId = 1;

            DateTime startDate = DateTime.Now;
            int days = 3;

            UserProfile userInfo = context.ConversationData.GetValue<UserProfile>(UserSessionDataKey);
            IBookReservationOperations icro = ServiceLocator.GetBookReservationOperations();
            BookReservation cres = await icro.ReserveBook(reservationId, BookId, startDate, days);

            await context.PostAsync($"Success. Your Book Booking Id is {cres.BookingId}. An email will be send to {userInfo.EMail}");

            userInfo.ActiveReservation = optionSelected;
            context.ConversationData.SetValue<UserProfile>(UserSessionDataKey, userInfo);

            context.Done<object>(null);
        }

        //private async Task ResumeAfterBookFormDialog(IDialogContext context, IAwaitable<BookQuery> result)
        //{
        //    try
        //    {
        //        BookQuery searchQuery = await result;
        //        context.ConversationData.SetValue("BookQuery", searchQuery);

        //        await context.PostAsync($"Ok. Searching for Books with genre {searchQuery.Genre}");

        //        IBookReservationOperations icro = ServiceLocator.GetBookReservationOperations();
        //        IList<Book> books = await icro.GetBookAvailability(_reservationChoice, searchQuery);

        //        await context.PostAsync($"I found in total {books.Count()} Books for your genre");

        //        var resultMessage = context.MakeMessage();
        //        resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
        //        resultMessage.Attachments = new List<Attachment>();

        //        List<string> bookChoices = new List<string>();
        //        foreach (var book in books)
        //        {
        //            bookChoices.Add(book.Name);
        //            HeroCard heroCard = new HeroCard()
        //            {
        //                Title = book.Name,
        //                Subtitle = $"{book.Rating} stars. {book.NumberOfReviews} reviews. From ${book.PriceStarting} per day.",
        //                Images = new List<CardImage>()
        //                {
        //                    new CardImage() { Url = book.Image }
        //                },
        //                Buttons = new List<CardAction>()
        //                {
        //                    new CardAction()
        //                    {
        //                        Title = "More details",
        //                        Type = ActionTypes.OpenUrl,
        //                        Value = $"https://www.bing.com/search?q=hotels+in+" + HttpUtility.UrlEncode(book.Location)
        //                    }
        //                }
        //            };

        //            resultMessage.Attachments.Add(heroCard.ToAttachment());
        //        }

        //        await context.PostAsync(resultMessage);
        //        bookChoices.Sort((h1, h2) => String.Compare(h1, h2, StringComparison.Ordinal));

        //        PromptDialog.Choice(context, this.OnPickBook, bookChoices,
        //           "Please pick your Book:",
        //           "Not a valid option", 3);
        //    }
        //    catch (FormCanceledException ex)
        //    {
        //        string reply;

        //        if (ex.InnerException == null)
        //        {
        //            reply = "You have canceled the operation. Quitting from the BookDialog";
        //        }
        //        else
        //        {
        //            reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
        //        }

        //        await context.PostAsync(reply);
        //    }
        //}

    }
}