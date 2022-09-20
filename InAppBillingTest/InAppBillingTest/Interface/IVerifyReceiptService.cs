using Google.Apis.AndroidPublisher.v3.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace InAppBillingTest.Interface
{
    public interface IVerifyReceiptService
    {
        Task<SubscriptionPurchaseV2> GetReceipt(string packageName, string purchaseToken);
    }
}
