using Google.Maps;
using Google.Maps.StaticMaps;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    [LuisActionBinding("Order.GetStatus", IntentDescription = "Get the status from order ID")]
    public class GetOrderStatusAction : ILuisAction
    {
        public string OrderId { get; set; }

        public Task<object> FulfillAsync()
        {
            var result = string.IsNullOrEmpty(this.OrderId) 
                ? (object)"Please give me your order number." 
                : GetCard(this.OrderId);
            return Task.FromResult((object)result);
        }

        private static HeroCard GetCard(string orderId)
        {
            var map = new StaticMapRequest
            {
                Path = new Path
                {
                    Color = MapColor.FromName("red"),
                    Points = new List<Location>
                    {
                        new Location("Port of Hai Phong, Hai Phong"),
                        new Location("Cam Ranh bay, Khanh Hoa, 650000"),
                        new Location("Saigon Port, Ho Chi Minh city")
                    }
                },
                Center = new Location("Cam Ranh bay, Khanh Hoa, 650000"),
                Size = new MapSize(500, 500),
                Zoom = 4
            };

            return new HeroCard
            {
                Title = $"Order number {orderId}",
                Subtitle = $"Expected arrive date: {DateTime.Now.AddDays(7).ToShortDateString()}",
                Text = "Shipment is currently in Cam Ranh bay, Khanh Hoa. The status is Delivered.",
                Images = new List<CardImage> { new CardImage(map.ToUri().ToString()) }
            };
        }
    }
}