using System;
using System.Collections.Generic;
using System.Text;

namespace InAppBillingTest.Models.VerifyReceipt
{
    public class Receipt
    {
        public string expires_date { get; set; } //UTC
        public string bid { get; set; }
    }
}
