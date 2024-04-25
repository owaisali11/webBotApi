using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace webBotApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookRequestController : ControllerBase
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly JsonParser _jsonParser =
      new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));

        [HttpPost]
        public async Task<ContentResult> DialogAction()
        {
            WebhookRequest request;
            using (var reader = new StreamReader(Request.Body))
            {
                string requestBody = await reader.ReadToEndAsync();
                if (string.IsNullOrEmpty(requestBody))
                {
                    return Content("Error: Empty request body", "text/plain");
                }
                request = await Task.Run(() => _jsonParser.Parse<WebhookRequest>(requestBody));
            }

            if (request.QueryResult == null)
            {
                return Content("Error: QueryResult is null", "text/plain");
            }

            if (request.QueryResult.Parameters == null)
            {
                // Handle null Parameters, e.g., return an error response
                return Content("Error: Parameters are null", "text/plain");
            }

            var orderIdField = request.QueryResult.Parameters.Fields["number"].ToString();

            string orderId = orderIdField;

            // Fetch the shipment date from the API
            string shipmentDate = await FetchShipmentDate(orderId);

            if (string.IsNullOrEmpty(shipmentDate))
            {
                return Content("Error: Failed to fetch shipment date");
            }
            // Construct the response message
            string responseMessage = $"Your Order {orderId} will be delieved {shipmentDate}";

            WebhookResponse response = new WebhookResponse
            {
                FulfillmentText = responseMessage
            };

            string responseJson = response.ToString();
            return Content(responseJson, "application/json");
        }
        private async Task<string> FetchShipmentDate(string orderId)
        {
            string apiUrl = "https://orderstatusapi-dot-organization-project-311520.uc.r.appspot.com/api/getOrderStatus";
            var payload = new { orderId = orderId };

            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(apiUrl, payload);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var responseData = JsonDocument.Parse(jsonResponse).RootElement;

                string iso8601ShipmentDate = responseData.GetProperty("shipmentDate").GetString();

                DateTimeOffset shipmentDateTimeOffset = DateTimeOffset.Parse(iso8601ShipmentDate);

                // Format the shipment date as "dddd, dd MMM yyyy"
                string formattedShipmentDate = shipmentDateTimeOffset.ToString("dddd, dd MMM yyyy");

                return formattedShipmentDate;
            }
            else
            {
                return null;
            }
        }

}
}
