using System;
using System.Collections.Generic;
using System.Text;

namespace InAppBillingTest.Models.VerifyReceipt
{
    public class ResponseBody
    {
        //앱 영수증 상태, 정상이면 0, developer.apple.com/documentation/appstorereceipts/status
        //The value for status is 0 if the receipt is valid, or a status code if there is an error.
        public int status { get; set; }
        //구독 만료 사유, developer.apple.com/documentation/appstorereceipts/expiration_intent
        //The reason a subscription expired.
        public int expiration_intent { get; set; }

        public Receipt receipt { get; set; }
    }
}
