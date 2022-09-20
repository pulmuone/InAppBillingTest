using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace InAppBillingTest.Models.VerifyReceipt
{
    public class RequestBody
    {
        [JsonProperty("receipt-data")]
        public string receipt_data { get; set; } // PurchaseToken
        public string password { get; set; } //사용자 및 액세스 > 공유 암호
        [JsonProperty("exclude-old-transactions")]
        public bool exclude_old_transactions { get; set; } = true;
    }
}
