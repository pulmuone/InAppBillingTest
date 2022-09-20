using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Google.Apis.AndroidPublisher.v3.Data;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using InAppBillingTest.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: Xamarin.Forms.Dependency(typeof(InAppBillingTest.Droid.Services.VerifyReceiptService))]
namespace InAppBillingTest.Droid.Services
{
    /// <summary>
    /// 조회 할당량(Quotas)
    /// 하루 200,000 개
    /// https://developers.google.com/android-publisher/quotas?hl=ko
    /// 
    /// 할당량 증가 신청
    /// https://support.google.com/googleplay/android-developer/contact/apiqr?hl=ko
    /// </summary>
    public class VerifyReceiptService : IVerifyReceiptService
    {
        private GoogleCredential _credential;
        private AndroidPublisherService _googleService;

        private ServiceAccountCredential _credential2;
        private AndroidPublisherService _googleService2;
        public VerifyReceiptService()
        {
            //How To #1, Service Account ~.json 파일 이용
            _credential = GoogleCredential.FromStream(Xamarin.Essentials.Platform.CurrentActivity.Assets.Open("pc-api-6084531073750454465-3-0d1259d09bd7.json"))
                .CreateScoped(new[] { AndroidPublisherService.Scope.Androidpublisher });
            
            _googleService = new AndroidPublisherService
            (
                new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = "VerifyReceipt Function",
                }
            );

            //HowTo #2, 인증서 입력 방법
            _credential2 = new ServiceAccountCredential
            (
                new ServiceAccountCredential.Initializer("--------------------------------------------.iam.gserviceaccount.com") //Service Account 
                {
                    Scopes = new[] { AndroidPublisherService.Scope.Androidpublisher }
                }.FromPrivateKey("-----BEGIN PRIVATE KEY-----\n----------------------------------------------------------------\n-----END PRIVATE KEY-----\n")
            );

            _googleService2 = new AndroidPublisherService
            (
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = _credential2,
                    ApplicationName = "VerifyReceipt Function",
                }
            );
        }

        public async Task<SubscriptionPurchaseV2> GetReceipt(string packageName, string purchaseToken)
        {
            SubscriptionPurchaseV2 result = null;
            try
            {
                var request = _googleService.Purchases.Subscriptionsv2.Get(packageName, purchaseToken);
                SubscriptionPurchaseV2 purchaseState = await request.ExecuteAsync();

                result = purchaseState;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
            }

            return result;
        }
    }
}