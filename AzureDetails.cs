using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot
{
    public static class AzureDetails
    {
        public static string SubscriptionID { get; set; }
        public static string ClientID { get; set; }
        public static string TenantID { get; set; }
        public static string ClientSecret { get; set; }
        public static string AccessToken { get; set; }
        public static string Intent { get; set; }
        public static string Entity { get; set; }
        public static string Response { get; set; }
        public static string ResourceGroupName { get; set; }
        public static string Location { get; set; }
    }
}
