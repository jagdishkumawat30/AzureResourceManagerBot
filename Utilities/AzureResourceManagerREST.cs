using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CoreBot.Utilities
{
    public static class AzureResourceManagerREST
    {
  

        public static void GetAuthorizationToken()
        {
            ClientCredential cc = new ClientCredential(AzureDetails.ClientID, AzureDetails.ClientSecret);
            var context = new AuthenticationContext("https://login.microsoftonline.com/" + AzureDetails.TenantID);
            var result = context.AcquireTokenAsync("https://management.azure.com/", cc);
            if (result == null)
            {
                throw new InvalidOperationException("Failed to obtain the JWT token");
            }
            AzureDetails.AccessToken = result.Result.AccessToken;
        }



        public static async Task getAllResourceGroupDetails()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://management.azure.com/subscriptions/" + AzureDetails.SubscriptionID + "/resourcegroups?api-version=2019-10-01");
            client.DefaultRequestHeaders.Accept.Clear();
            /*client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));*/ //ACCEPT header
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AzureDetails.AccessToken);

            //var requestUri = $"knowledgebases/{knowledgebaseId}/generateAnswer";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, client.BaseAddress);
            //var body = $"{{\"question\": \"{"symptoms"}\", \"top\": \"{1}\"}}";
            //var content = new StringContent(body, Encoding.UTF8, "application/json");
            //request.Content = content;
            var response = await MakeRequestAsync(request, client);
            AzureDetails.Response = response;
            //Console.WriteLine(response);
        }

        public static async Task createResourceGroup()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://management.azure.com/subscriptions/" + AzureDetails.SubscriptionID + "/resourcegroups/" + AzureDetails.ResourceGroupName + "?api-version=2019-10-01");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AzureDetails.AccessToken);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, client.BaseAddress);
            var body = $"{{\"location\": \"{AzureDetails.Location}\"}}";
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            request.Content = content;
            var response = await MakeRequestAsync(request, client);
            AzureDetails.Response = response;
        }

        public static async Task<string> MakeRequestAsync(HttpRequestMessage getRequest, HttpClient client)
        {
            var response = await client.SendAsync(getRequest).ConfigureAwait(false);
            var responseString = string.Empty;
            try
            {
                response.EnsureSuccessStatusCode();
                responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                // empty responseString
            }

            return responseString;
        }
    }
}
