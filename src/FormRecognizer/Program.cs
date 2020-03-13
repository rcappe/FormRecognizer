using System;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace FormRecognizer
{
    class Program
    {
        static async Task Main()
        {
            var endpoint = "{endpoint}";
            var subscriptionKey = "{subscriptionKey}";
            var triningData = "{triningData}";
            var analyzeData = "{analyzeData}";
            
            var modelId = await TrainCustomModel(endpoint, subscriptionKey, triningData);
            Console.WriteLine($"modelId:{modelId}");

            Thread.Sleep(15000); //Attendiamo che il modello completi il traing

            var resultId = await AnalyzeForm(endpoint, subscriptionKey, modelId, analyzeData);
            Console.WriteLine($"resultId:{resultId}");

            Thread.Sleep(15000); //Attendiamo che venga analizzato

            var jsonResult = await GetAnalyzeFormResult(endpoint, subscriptionKey, modelId, resultId);

            Console.WriteLine(jsonResult);

            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
        }

        static async Task<string> TrainCustomModel(string endpoint, string subscriptionKey, string triningData)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = $"{endpoint}/formrecognizer/v2.0-preview/custom/models";

            HttpResponseMessage response;

            byte[] byteData = Encoding.UTF8.GetBytes(" { \"source\" : \"" + triningData + "\" } ");

            using var content = new ByteArrayContent(byteData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response = await client.PostAsync(uri, content);

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
                throw new InvalidOperationException($"StatusCode:{response.StatusCode} Message:{await response.Content.ReadAsStringAsync()}");

            var location = response.Headers.Location.AbsolutePath;

            var modelId = location.Substring(location.LastIndexOf('/') + 1);
            return modelId;
        }

        static async Task<string> AnalyzeForm(string endpoint, string subscriptionKey, string modelId, string analyData)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = $"{endpoint}/formrecognizer/v2.0-preview/custom/models/{modelId}/analyze";

            HttpResponseMessage response;

            byte[] byteData = Encoding.UTF8.GetBytes(" { \"source\" : \"" + analyData + "\" } ");

            using var content = new ByteArrayContent(byteData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response = await client.PostAsync(uri, content);

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
                throw new InvalidOperationException($"StatusCode:{response.StatusCode} Message:{await response.Content.ReadAsStringAsync()}");

            var location = response.Headers.GetValues("Operation-Location").First();

            var resultId = location.Substring(location.LastIndexOf('/') + 1);
            return resultId;
        }

        static async Task<string> GetAnalyzeFormResult(string endpoint, string subscriptionKey, string modelId, string resultId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

            var uri = $"{endpoint}/formrecognizer/v2.0-preview/custom/models/{modelId}/analyzeResults/{resultId}?" + queryString;

            var response = await client.GetAsync(uri);

            return await response.Content.ReadAsStringAsync();
        }
    }
}