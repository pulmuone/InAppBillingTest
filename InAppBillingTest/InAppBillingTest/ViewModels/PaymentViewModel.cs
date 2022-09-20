using Google.Apis.AndroidPublisher.v3.Data;
using InAppBillingTest.Helpers;
using InAppBillingTest.Interface;
using InAppBillingTest.Models.VerifyReceipt;
using InAppBillingTest.Services;
using Newtonsoft.Json;
using Plugin.InAppBilling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.Extensions;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace InAppBillingTest.ViewModels
{
    public class PaymentViewModel : BaseViewModel
    {
        const string productIdSub = "premium1";
        private string _subsPrice = string.Empty;
        private int _subscriptionDays;

        public ICommand BuySubscriptionCommand { get; private set; }
        public ICommand RestoreSubscriptionCommand { get; private set; }
        public ICommand GetProductInfoCommand { get; private set; }
        public ICommand CheckSubscriptionCommand { get; private set; }
        public PaymentViewModel()
        {
            BuySubscriptionCommand = new Command(async() => await BuySubscription(), () => IsEnabled);
            RestoreSubscriptionCommand = new Command(async() => await RestoreSubscription(), () => IsEnabled);
            CheckSubscriptionCommand = new Command(async () => await CheckSubscription(), () => IsEnabled);
            GetProductInfoCommand = new Command(async () => await GetProductInfo(), () => IsEnabled);
        }

        public void Init()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                IsBusy = true;
                IsControlEnable = false;

                if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    await GetPrice();
                    DateTime dateTime = TimerServerUtcSync.GetNetworkTime();

                    //구독상태, 구독만료일자
                    var SyncSub = SyncSubsStatusService.GetInstance();
                    await SyncSub.SyncStatus();

                    if (Settings.HasPurchasedSub && Settings.SubExpirationDate > dateTime)
                    {
                        //IsFree = false;
                        //IsPurchasedSub = true;

                        var diffDays = Settings.SubExpirationDate.Subtract(dateTime);
                        this.SubscriptionDays = diffDays.Days;
                    }
                    else
                    {
                        //IsFree = true;
                        //IsPurchasedSub = false;
                        this.SubscriptionDays = 0;
                    }
                }
                else
                {
                    var diffDays = Settings.SubExpirationDate.Subtract(TimerServerUtcSync.GetNetworkTime());
                    this.SubscriptionDays = diffDays.Days;

                    await DisplayAlert("No internet", "");
                }

                IsBusy = false;
                IsControlEnable = true;
            });

        }

        private async Task CheckSubscription()
        {


            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                DateTime dateTime = TimerServerUtcSync.GetNetworkTime();

                //구독상태, 구독만료일자
                var SyncSub = SyncSubsStatusService.GetInstance();
                await SyncSub.SyncStatus();

                if (Settings.HasPurchasedSub && Settings.SubExpirationDate > dateTime)
                {
                    //IsFree = false;
                    //IsPurchasedSub = true;

                    var diffDays = Settings.SubExpirationDate.Subtract(dateTime);
                    this.SubscriptionDays = diffDays.Days;
                }
                else
                {
                    //IsFree = true;
                    //IsPurchasedSub = false;
                    this.SubscriptionDays = 0;
                }
            }
            else
            {
                var diffDays = Settings.SubExpirationDate.Subtract(TimerServerUtcSync.GetNetworkTime());
                this.SubscriptionDays = diffDays.Days;

                await DisplayAlert("No internet", "");
            }
        }

        private async Task GetProductInfo()
        {
            IsControlEnable = false;
            (BuySubscriptionCommand as Command).ChangeCanExecute();
            (RestoreSubscriptionCommand as Command).ChangeCanExecute();
            (CheckSubscriptionCommand as Command).ChangeCanExecute();
            (GetProductInfoCommand as Command).ChangeCanExecute();

            await GetPrice();

            IsControlEnable = true;
            (BuySubscriptionCommand as Command).ChangeCanExecute();
            (RestoreSubscriptionCommand as Command).ChangeCanExecute();
            (CheckSubscriptionCommand as Command).ChangeCanExecute();
            (GetProductInfoCommand as Command).ChangeCanExecute();
        }

        private async Task RestoreSubscription()
        {
            IsControlEnable = false;
            (BuySubscriptionCommand as Command).ChangeCanExecute();
            (RestoreSubscriptionCommand as Command).ChangeCanExecute();
            (CheckSubscriptionCommand as Command).ChangeCanExecute();
            (GetProductInfoCommand as Command).ChangeCanExecute();

            await RestorePurchases();

            IsControlEnable = true;
            (BuySubscriptionCommand as Command).ChangeCanExecute();
            (RestoreSubscriptionCommand as Command).ChangeCanExecute();
            (CheckSubscriptionCommand as Command).ChangeCanExecute();
            (GetProductInfoCommand as Command).ChangeCanExecute();
        }

        private async Task BuySubscription()
        {
            IsControlEnable = false;
            (BuySubscriptionCommand as Command).ChangeCanExecute();
            (RestoreSubscriptionCommand as Command).ChangeCanExecute();
            (CheckSubscriptionCommand as Command).ChangeCanExecute();
            (GetProductInfoCommand as Command).ChangeCanExecute();

            await PurchaseSubscription();

            IsControlEnable = true;
            (BuySubscriptionCommand as Command).ChangeCanExecute();
            (RestoreSubscriptionCommand as Command).ChangeCanExecute();
            (CheckSubscriptionCommand as Command).ChangeCanExecute();
            (GetProductInfoCommand as Command).ChangeCanExecute();
        }

        public async Task PurchaseSubscription()
        {
            string receipt_status = string.Empty;
            string expiration_intent = string.Empty;

            var billing = CrossInAppBilling.Current;
            try
            {
                // check internet first with Essentials
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    //TranslateExtension te = new TranslateExtension();
                    //te.Text = AppResources.InternetCheck;
                    await DisplayAlert("No internet", "");
                    return;
                }

                // connect to the app store api
                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    //TranslateExtension te = new TranslateExtension();
                    //te.Text = AppResources.InAppBillingCheck;
                    await DisplayAlert("No InAppBilling", "");
                    return;
                }

                //try to make purchase, this will return a purchase, empty, or throw an exception
                //안드로이드는 중복 구매를 차단해 준다. 
                var purchase = await billing.PurchaseAsync(productIdSub, ItemType.Subscription);

                if (purchase == null)
                {
                    Settings.HasPurchasedSub = false;
                    //nothing was purchased
                    //Analytics.TrackEvent("PurchaseSubscription-PurchaseComplete", new Dictionary<string, string>
                    //{
                    //    ["purchase"] = "failed: null"
                    //});
                    return;
                }
                else if (purchase.State == PurchaseState.Purchased || purchase.State == PurchaseState.Restored)
                {
                    try
                    {
                        //if(!purchase.IsAcknowledged)
                        //{
                        var ack = await billing.FinalizePurchaseAsync(purchase.TransactionIdentifier);
                        Console.WriteLine(ack);
                        //}
                    }
                    catch (Exception ex)
                    {
                        //Analytics.TrackEvent("PurchaseSubscription-PurchaseComplete", new Dictionary<string, string>
                        //{
                        //    ["purchase"] = "failed: Acknowledged " + ex.Message
                        //});
                    }

                    //구독 성공
                    //Analytics.TrackEvent("PurchaseSubscription-PurchaseComplete", new Dictionary<string, string>
                    //{
                    //    ["purchase"] = "success"
                    //});


                    //구독 만료일자를 영수증에서 받아와서 설정한다.
                    if (DeviceInfo.Platform == DevicePlatform.Android)
                    {
                        IVerifyReceiptService service = DependencyService.Get<IVerifyReceiptService>();
                        SubscriptionPurchaseV2 receipt = await service.GetReceipt("com.gwise.************", purchase.PurchaseToken ?? string.Empty);

                        //https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.subscriptionsv2
                        if (receipt != null)
                        {
                            //상태값 확인
                            Debug.WriteLine(receipt.AcknowledgementState); //구독이 사용 가능한 확인 상태인지 확인
                            Debug.WriteLine(receipt.SubscriptionState); //구독 상태, (활성, 취소등)

                            if (receipt.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED")
                            {
                                if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_ACTIVE")
                                {
                                    //구매하고 나서는 상태값을 체크하지는 않는다.
                                    if (receipt.LineItems != null && receipt.LineItems.Count > 0)
                                    {
                                        IList<SubscriptionPurchaseLineItem> items = receipt.LineItems;
                                        var sortedSub = items.Where(p => p.ProductId == productIdSub).OrderByDescending(i => i.ExpiryTime).ToList();
                                        //if (sortedSub != null && sortedSub.Count > 0)
                                        if (sortedSub?.Any() ?? false)
                                        {
                                            var lastSubPur = sortedSub[0];
                                            DateTime expirationDate = Convert.ToDateTime(lastSubPur.ExpiryTime); //UTC
                                            var nowDateTimeUtc = TimerServerUtcSync.GetNetworkTime();
                                            //구독 만료일자 체크
                                            if (expirationDate > nowDateTimeUtc)
                                            {
                                                receipt_status = "Success";
                                                Settings.HasPurchasedSub = true;
                                                Settings.CheckSubStatus = true;
                                                Settings.SubReceipt = purchase.PurchaseToken ?? string.Empty;
                                                Settings.SubExpirationDate = expirationDate;
                                                Settings.OrderId = purchase.Id;

                                                this.SubscriptionDays = Settings.SubExpirationDate.Subtract(nowDateTimeUtc).Days;
                                            }
                                            else
                                            {
                                                Settings.HasPurchasedSub = false;
                                                this.SubscriptionDays = 0;
                                                //구독만료 일자가 지났습니다.
                                                receipt_status = "subscription has expired";
                                                expiration_intent = "ExpirationDate:" + expirationDate.ToString();
                                            }
                                        }
                                    }
                                }
                                else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_UNSPECIFIED")
                                {
                                    receipt_status = receipt.SubscriptionState;
                                    expiration_intent = "Unspecified subscription state.";
                                }
                                else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_PENDING")
                                {
                                    receipt_status = receipt.SubscriptionState;
                                    expiration_intent = "Subscription was created but awaiting payment during signup. In this state, all items are awaiting payment.";
                                }
                                else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_PAUSED")
                                {
                                    receipt_status = receipt.SubscriptionState;
                                    expiration_intent = "Subscription is paused. The state is only available when the subscription is an auto renewing plan. In this state, all items are in paused state.";
                                }
                                else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_IN_GRACE_PERIOD")
                                {
                                    receipt_status = receipt.SubscriptionState;
                                    expiration_intent = "Subscription is in grace period. The state is only available when the subscription is an auto renewing plan. In this state, all items are in grace period.";
                                }
                                else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_ON_HOLD")
                                {
                                    receipt_status = receipt.SubscriptionState;
                                    expiration_intent = "Subscription is on hold (suspended). The state is only available when the subscription is an auto renewing plan. In this state, all items are on hold.";
                                }
                                else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_EXPIRED")
                                {
                                    receipt_status = receipt.SubscriptionState;
                                    expiration_intent = "Subscription is expired. All items have expiryTime in the past.";
                                }
                            }
                            else if (receipt.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_UNSPECIFIED")
                            {
                                receipt_status = receipt.AcknowledgementState;
                                expiration_intent = "Unspecified acknowledgement state.";
                            }
                            else if (receipt.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_PENDING")
                            {
                                receipt_status = receipt.AcknowledgementState;
                                expiration_intent = "The subscription is not acknowledged yet.";
                            }

                            await DisplayAlert(string.Format("{0}, {1}", "Purchase Result", ""),
                            "Status:" + receipt_status
                          + System.Environment.NewLine
                          + "Reason:" + expiration_intent);
                        }
                    }
                    else
                    { //iOS
                        RequestBody req = new RequestBody()
                        {
                            password = "ee7b992cb68846d5ad3efa3b12a66677",
                            exclude_old_transactions = true,
                            receipt_data = purchase.PurchaseToken ?? String.Empty
                        };

                        var data = req == null ? null : JsonConvert.SerializeObject(req, Formatting.Indented);
                        string url = string.Empty;
                        ResponseBody responseBody = new ResponseBody();
#if DEBUG
                        url = "https://sandbox.itunes.apple.com/verifyReceipt";
#else
                        url = "https://buy.itunes.apple.com/verifyReceipt";
#endif
                        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                        {
                            if (data != null)
                            {
                                request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                            }

                            using var client = new HttpClient();
                            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                            if (response.Content != null)
                            {
                                var content = await response.Content.ReadAsStringAsync();
                                if (response.IsSuccessStatusCode)
                                {
                                    responseBody = JsonConvert.DeserializeObject<ResponseBody>(content);
                                }
                            }
                        }

                        if (responseBody.status == 0)
                        {
                            receipt_status = "OK";
                            //정상
                            long elapsed_ms = long.Parse(responseBody.receipt.expires_date); //구독 만료일자
                            DateTime expirationDate = DateTimeOffset.FromUnixTimeMilliseconds(elapsed_ms).UtcDateTime;

                            var nowDateTimeUtc = TimerServerUtcSync.GetNetworkTime();
                            //구독 만료일자 체크
                            if (expirationDate > nowDateTimeUtc)
                            {
                                Settings.HasPurchasedSub = true;
                                Settings.CheckSubStatus = true;
                                Settings.SubReceipt = purchase.PurchaseToken ?? string.Empty;
                                //Settings.SubExpirationDate = recentSub.TransactionDateUtc.AddMonths(1).AddDays(5);
                                Settings.SubExpirationDate = expirationDate;
                                Settings.OrderId = purchase.Id; //영수증 번호

                                var diffDays = Settings.SubExpirationDate.Subtract(nowDateTimeUtc);
                                this.SubscriptionDays = diffDays.Days;
                            }
                            else
                            {
                                Settings.HasPurchasedSub = false;
                                //구독만료 일자가 지났습니다.
                                receipt_status = "subscription has expired";
                                expiration_intent = "ExpirationDate:" + expirationDate.ToString();

                                await DisplayAlert(string.Format("{0}, {1}", "Purchase Fail", ""),
                                 "Status:" + receipt_status
                                + System.Environment.NewLine
                                + "Reason:" + expiration_intent);
                            }
                        }
                        else
                        {
                            Settings.HasPurchasedSub = false;

                            receipt_status = ReceiptSatusMessage(responseBody.status); //영수증 상태
                            expiration_intent = ExpirationIntentMessage(responseBody.expiration_intent); //구독 만료 사유

                            await DisplayAlert(string.Format("{0}, {1}", "Purchase Fail", ""),
                             "Status:" + receipt_status
                           + System.Environment.NewLine
                           + "Reason:" + expiration_intent);
                        }

                        //Analytics.TrackEvent("PurchaseSubscription-RestoreComplete", new Dictionary<string, string>
                        //{
                        //    ["purchase"] = "failed: misc, " + subState
                        //});

                        //iOS Error , UIKit.UIKitThreadAccessException: UIKit Consistency error: you are calling a UIKit method that can only be invoked from the UI thread.   at UIKit.UIAppl



                        ////정상
                        //long elapsed_ms = long.Parse(responseBody.receipt.expires_date); //구독 만료일자
                        //DateTime expirationDate = DateTimeOffset.FromUnixTimeMilliseconds(elapsed_ms).UtcDateTime;

                        //var nowDateTimeUtc = TimerServerUtcSync.GetNetworkTime();
                        ////구독 만료일자 체크
                        //if (expirationDate > nowDateTimeUtc)
                        //{
                        //    Settings.HasPurchasedSub = true;
                        //    Settings.CheckSubStatus = true;
                        //    Settings.SubReceipt = purchase.PurchaseToken ?? string.Empty;
                        //    //Settings.SubExpirationDate = recentSub.TransactionDateUtc.AddMonths(1).AddDays(5);
                        //    Settings.SubExpirationDate = expirationDate;
                        //    Settings.OrderId = purchase.Id; //영수증 번호

                        //    this.SubscriptionDays = Settings.SubExpirationDate.Subtract(nowDateTimeUtc).Days;
                        //}
                    }
                }
                else
                {
                    Settings.HasPurchasedSub = false;
                    //throw new InAppBillingPurchaseException(PurchaseError.GeneralError);
                    //Analytics.TrackEvent("PurchaseSubscription-PurchaseComplete", new Dictionary<string, string>
                    //{
                    //    ["purchase"] = $"failed: {purchase.State}"
                    //});
                    var message = string.Empty;

                    message = purchase.State.ToString();

                    if (string.IsNullOrWhiteSpace(message))
                        return;

                    Console.WriteLine("Issue connecting: " + purchase.State);
                    //await DisplayAlert(purchase.State.ToString(), message);
                    await DisplayAlert("Purchase Error", message);
                }
            }
            catch (InAppBillingPurchaseException purchaseEx)
            {
                //Analytics.TrackEvent("PurchaseSubscription-PurchaseComplete", new Dictionary<string, string>
                //{
                //    ["purchase"] = $"failed: {purchaseEx.PurchaseError}"
                //});
                var message = string.Empty;
                switch (purchaseEx.PurchaseError)
                {
                    case PurchaseError.AppStoreUnavailable:
                        message = "Currently the app store seems to be unavailble. Try again later.";
                        break;
                    case PurchaseError.BillingUnavailable:
                        message = "Billing seems to be unavailable, please try again later.";
                        break;
                    case PurchaseError.PaymentInvalid:
                        message = "Payment seems to be invalid, please try again.";
                        break;
                    case PurchaseError.PaymentNotAllowed:
                        message = "Payment does not seem to be enabled/allowed, please try again.";
                        break;
                    case PurchaseError.UserCancelled:
                        //don't do anything
                        break;
                    default:
                        message = "Something has gone wrong, please try again.";
                        break;
                }

                if (string.IsNullOrWhiteSpace(message))
                    return;

                Console.WriteLine("Issue connecting: " + purchaseEx);
                await DisplayAlert(purchaseEx.PurchaseError.ToString(), message);
            }
            catch (Exception ex)
            {
                // Handle a generic exception as something really went wrong
                //Analytics.TrackEvent("PurchaseSubscription-PurchaseComplete", new Dictionary<string, string>
                //{
                //    ["purchase"] = "failed: misc, " + ex.Message
                //});
                Console.WriteLine("Issue connecting: " + ex);
                await DisplayAlert("Uh Oh!", $"Looks like something has gone wrong, please try again. Code: {ex.Message}");
            }
            finally
            {
                await billing.DisconnectAsync();

                //Analytics.TrackEvent("PurchaseSubscription-Finish", new Dictionary<string, string>
                //{
                //    ["purchase"] = "failed: null"
                //});
            }
        }


        async Task RestorePurchases()
        {
            var billing = CrossInAppBilling.Current;
            try
            {
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {                    
                    await DisplayAlert("No internet", "");
                    return;
                }

                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    await DisplayAlert("No InAppBilling", "");
                    return;
                }

                bool foundStuff = false;
                string receipt_status = string.Empty;
                string expiration_intent = string.Empty;

                //var idsToNotFinish = new List<string>(new[] { productIdSub });
                var subs = await billing.GetPurchasesAsync(ItemType.Subscription);
                string subState = string.Empty; //영수증 상태
                string transactionDate = string.Empty;

                //일시 중지하면 목록 조회 되지 않음.
                if (subs?.Any(p => p.ProductId == productIdSub) ?? false)
                {
                    var sorted = subs.Where(p => p.ProductId == productIdSub).OrderByDescending(i => i.TransactionDateUtc).ToList();
                    if (!sorted?.Any() ?? false) return;

                    var recentSub = sorted[0];
                    if (recentSub != null)
                    {
                        //if (!recentSub.IsAcknowledged)
                        //{
                        try
                        {
                            //await CrossInAppBilling.Current.AcknowledgePurchaseAsync(recentSub.PurchaseToken);
                            var ack = await billing.FinalizePurchaseAsync(recentSub.TransactionIdentifier);
                            Console.WriteLine(ack);
                        }
                        catch (Exception ex)
                        {
                            //Logger.AppendLine("Unable to acknowledge purchase: " + ex);
                            //Analytics.TrackEvent("PurchaseSubscription-RestoreComplete", new Dictionary<string, string>
                            //{
                            //    ["purchase"] = "failed: Acknowledged " + ex.Message
                            //});
                        }
                        //}

                        transactionDate = recentSub.TransactionDateUtc.ToString();
                        subState = recentSub.State.ToString();
                        // On Android as long as you have one here then it is valid subscription
                        if (DeviceInfo.Platform == DevicePlatform.Android)
                        {
                            if (recentSub.State == PurchaseState.Purchased || recentSub.State == PurchaseState.Restored)
                            {
                                IVerifyReceiptService service = DependencyService.Get<IVerifyReceiptService>();
                                SubscriptionPurchaseV2 receipt = await service.GetReceipt("com.gwise.***********", recentSub.PurchaseToken ?? string.Empty);

                                //https://developers.google.com/android-publisher/api-ref/rest/v3/purchases.subscriptionsv2
                                if (receipt != null)
                                {
                                    //안드로이드는 우선 상태가 구독이 되어야 한다. 취소/만료도 구독 상태로 되어 있다.
                                    if (receipt.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED")
                                    {
                                        //SUBSCRIPTION_STATE_ACTIVE : 구독 활성 상태
                                        //SUBSCRIPTION_STATE_CANCELED : 정기 결제가 취소되었지만 아직 만료되지 않았다.
                                        //                              안드로이는 이 상태값인 경우도 만료일자를 확인해야 한다.
                                        //                              환불은 두개 상태 값이 아니기 때문에 즉시 사용 못한.            
                                        if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_ACTIVE" || receipt.SubscriptionState == "SUBSCRIPTION_STATE_CANCELED")
                                        {
                                            //LineItems : Item-level info for a subscription purchase
                                            if (receipt.LineItems != null && receipt.LineItems.Count > 0)
                                            {
                                                IList<SubscriptionPurchaseLineItem> items = receipt.LineItems;
                                                var sortedSub = items.Where(p => p.ProductId == productIdSub).OrderByDescending(i => i.ExpiryTime).ToList();
                                                //if (sortedSub != null && sortedSub.Count > 0)
                                                if (sortedSub?.Any() ?? false)
                                                {
                                                    var lastSubPur = sortedSub[0];
                                                    DateTime expirationDate = Convert.ToDateTime(lastSubPur.ExpiryTime); //UTC
                                                    var nowDateTimeUtc = TimerServerUtcSync.GetNetworkTime();
                                                    //구독 만료일자 체크
                                                    if (expirationDate > nowDateTimeUtc)
                                                    {
                                                        //Debug.WriteLine(recentSub.PurchaseToken);
                                                        foundStuff = true;
                                                        Settings.HasPurchasedSub = true;
                                                        Settings.CheckSubStatus = true;
                                                        Settings.SubReceipt = recentSub.PurchaseToken ?? string.Empty;
                                                        Settings.SubExpirationDate = expirationDate;
                                                        Settings.OrderId = recentSub.Id;

                                                        var diffDays = Settings.SubExpirationDate.Subtract(nowDateTimeUtc);
                                                        this.SubscriptionDays = diffDays.Days;
                                                    }
                                                    else
                                                    {
                                                        //구독만료 일자가 지났습니다.
                                                        receipt_status = "subscription has expired";
                                                        expiration_intent = "ExpirationDate:" + expirationDate.ToString();
                                                    }
                                                }
                                            }
                                        }
                                        else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_UNSPECIFIED")
                                        {
                                            receipt_status = receipt.SubscriptionState;
                                            expiration_intent = "Unspecified subscription state.";
                                        }
                                        else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_PENDING")
                                        {
                                            receipt_status = receipt.SubscriptionState;
                                            expiration_intent = "Subscription was created but awaiting payment during signup. In this state, all items are awaiting payment.";
                                        }
                                        else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_PAUSED")
                                        {
                                            receipt_status = receipt.SubscriptionState;
                                            expiration_intent = "Subscription is paused. The state is only available when the subscription is an auto renewing plan. In this state, all items are in paused state.";
                                        }
                                        else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_IN_GRACE_PERIOD")
                                        {
                                            receipt_status = receipt.SubscriptionState;
                                            expiration_intent = "Subscription is in grace period. The state is only available when the subscription is an auto renewing plan. In this state, all items are in grace period.";
                                        }
                                        else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_ON_HOLD")
                                        {
                                            receipt_status = receipt.SubscriptionState;
                                            expiration_intent = "Subscription is on hold (suspended). The state is only available when the subscription is an auto renewing plan. In this state, all items are on hold.";
                                        }
                                        else if (receipt.SubscriptionState == "SUBSCRIPTION_STATE_EXPIRED")
                                        {
                                            receipt_status = receipt.SubscriptionState;
                                            expiration_intent = "Subscription is expired. All items have expiryTime in the past.";
                                        }
                                    }
                                    else if (receipt.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_UNSPECIFIED")
                                    {
                                        receipt_status = receipt.AcknowledgementState;
                                        expiration_intent = "Unspecified acknowledgement state.";
                                    }
                                    else if (receipt.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_PENDING")
                                    {
                                        receipt_status = receipt.AcknowledgementState;
                                        expiration_intent = "The subscription is not acknowledged yet.";
                                    }
                                }
                            }
                            else
                            {
                                Settings.HasPurchasedSub = false;
                            }
                        }
                        else
                        {
                            //iOS에서 구독을 취소만 했을 경우 어떻게 되는가 ?
                            //환불은 즉시 사용 못함으로 되는데 구독을 취소 했을 경우 당월은 사용 가능 한?
                            if (recentSub.State == PurchaseState.Purchased || recentSub.State == PurchaseState.Restored)
                            {
                                //Debug.WriteLine(billing.ReceiptData); //iOS만 해당하고 전체 영수증 데이터가 조회 된다 
                                //Debug.WriteLine(recentSub.PurchaseToken);
                                //iOS는 GetPurchasesAsync하면 구독했던 영수증 모두를 가져온다 
                                //PurchaseToken을 verifyReceipt해서 json 데이터를 가져와서
                                //status : 0 : 구독 상
                                //expiration_intent : 0, 구독이 만료된 이유
                                RequestBody req = new RequestBody()
                                {
                                    password = "ee7b**************************",
                                    exclude_old_transactions = true,
                                    receipt_data = recentSub.PurchaseToken ?? string.Empty
                                };

                                var data = req == null ? null : JsonConvert.SerializeObject(req, Formatting.Indented);
                                string url = string.Empty;
                                ResponseBody responseBody = new ResponseBody();
#if DEBUG
                                url = "https://sandbox.itunes.apple.com/verifyReceipt";
#else
                                url = "https://buy.itunes.apple.com/verifyReceipt";
#endif
                                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                                {
                                    if (data != null)
                                    {
                                        request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                                    }

                                    using var client = new HttpClient();
                                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
                                    if (response.Content != null)
                                    {
                                        var content = await response.Content.ReadAsStringAsync();
                                        if (response.IsSuccessStatusCode)
                                        {
                                            responseBody = JsonConvert.DeserializeObject<ResponseBody>(content);
                                        }
                                    }
                                }

                                if (responseBody.status == 0)
                                {
                                    receipt_status = "OK";
                                    //정상
                                    long elapsed_ms = long.Parse(responseBody.receipt.expires_date); //구독 만료일자
                                    DateTime expirationDate = DateTimeOffset.FromUnixTimeMilliseconds(elapsed_ms).UtcDateTime;

                                    var nowDateTimeUtc = TimerServerUtcSync.GetNetworkTime();
                                    //구독 만료일자 체크
                                    if (expirationDate > nowDateTimeUtc)
                                    {
                                        foundStuff = true;
                                        Settings.HasPurchasedSub = true;
                                        Settings.CheckSubStatus = true;
                                        Settings.SubReceipt = recentSub.PurchaseToken ?? string.Empty;
                                        //Settings.SubExpirationDate = recentSub.TransactionDateUtc.AddMonths(1).AddDays(5);
                                        Settings.SubExpirationDate = expirationDate;
                                        Settings.OrderId = recentSub.Id; //영수증 번호

                                        var diffDays = Settings.SubExpirationDate.Subtract(nowDateTimeUtc);
                                        this.SubscriptionDays = diffDays.Days;
                                    }
                                    else
                                    {
                                        //구독만료 일자가 지났습니다.
                                        receipt_status = "subscription has expired";
                                        expiration_intent = "ExpirationDate:" + expirationDate.ToString();
                                    }
                                }
                                else
                                {
                                    receipt_status = ReceiptSatusMessage(responseBody.status); //영수증 상태
                                    expiration_intent = ExpirationIntentMessage(responseBody.expiration_intent); //구독 만료 사유
                                }
                            }
                            else
                            {
                                Settings.HasPurchasedSub = false;
                            }
                        }

                        //if (!recentSub.IsAcknowledged)
                        //{
                        //    try
                        //    {
                        //        //await CrossInAppBilling.Current.AcknowledgePurchaseAsync(recentSub.PurchaseToken);
                        //        var ack = await billing.FinalizePurchaseAsync(recentSub.TransactionIdentifier);
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        //Logger.AppendLine("Unable to acknowledge purchase: " + ex);
                        //        //Analytics.TrackEvent("PurchaseSubscription-RestoreComplete", new Dictionary<string, string>
                        //        //{
                        //        //    ["purchase"] = "failed: Acknowledged " + ex.Message
                        //        //});
                        //    }
                        //}
                    }
                }

                if (foundStuff)
                {
                    //Analytics.TrackEvent("PurchaseSubscription-RestoreComplete", new Dictionary<string, string>
                    //{
                    //    ["purchase"] = "Success: " + subState
                    //});

                    //TranslateExtension te = new TranslateExtension
                    //{
                    //    Text = AppResources.SubscriptionRestoreSuccessMessage
                    //};

                    await DisplayAlert(string.Format("{0}, {1}", "Status Refreshed!", subState), System.Environment.NewLine + transactionDate);
                }
                else
                {
                    //실패 사유
                    Settings.HasPurchasedSub = false;
                    //TranslateExtension te = new TranslateExtension();
                    //te.Text = AppResources.SubscriptionRestoreFailMessage;

                    //Analytics.TrackEvent("PurchaseSubscription-RestoreComplete", new Dictionary<string, string>
                    //{
                    //    ["purchase"] = "failed: misc, " + subState
                    //});

                    //iOS Error , UIKit.UIKitThreadAccessException: UIKit Consistency error: you are calling a UIKit method that can only be invoked from the UI thread.   at UIKit.UIAppl
                    await DisplayAlert(string.Format("{0}, {1}", "Restore Fail", subState),
                         System.Environment.NewLine
                       + "Status:" + receipt_status
                       + System.Environment.NewLine
                       + "Reason:" + expiration_intent);
                }
            }
            catch (InAppBillingPurchaseException purchaseEx)
            {
                //Analytics.TrackEvent("PurchaseSubscription-RestoreComplete", new Dictionary<string, string>
                //{
                //    ["purchase"] = $"failed: {purchaseEx.PurchaseError}"
                //});
                var message = string.Empty;
                switch (purchaseEx.PurchaseError)
                {
                    case PurchaseError.AppStoreUnavailable:
                        message = "Currently the app store seems to be unavailble. Try again later.";
                        break;
                    case PurchaseError.BillingUnavailable:
                        message = "Billing seems to be unavailable, please try again later.";
                        break;
                    case PurchaseError.PaymentInvalid:
                        message = "Payment seems to be invalid, please try again.";
                        break;
                    case PurchaseError.PaymentNotAllowed:
                        message = "Payment does not seem to be enabled/allowed, please try again.";
                        break;
                    case PurchaseError.UserCancelled:
                        //don't do anything
                        break;
                    default:
                        message = "Something has gone wrong, please try again.";
                        break;
                }

                if (string.IsNullOrWhiteSpace(message))
                    return;

                Console.WriteLine("Issue connecting: " + purchaseEx);
                await DisplayAlert(purchaseEx.PurchaseError.ToString(), message);
            }
            catch (Exception ex)
            {             
                //Debug.WriteLine("Issue connecting: " + ex);
                //TranslateExtension te = new TranslateExtension
                //{
                //    Text = AppResources.SubscriptionRestoreExceptionMessage
                //};
                await DisplayAlert("Restore Error", string.Format("{0} Code: {1}", "", ex.Message));
            }
            finally
            {
                await billing.DisconnectAsync();
            }
        }

        private async Task GetPrice()
        {
            var billing = CrossInAppBilling.Current;
            try
            {
                //Check Offline
                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    await DisplayAlert("No InAppBilling","");

                    //IsBusy = false;
                    return;
                }

                var items = await billing.GetProductInfoAsync(ItemType.Subscription, productIdSub);

                var item = items.FirstOrDefault(i => i.ProductId == productIdSub);
                if (item != null)
                {
                    Settings.SubPrice = item.LocalizedPrice;
                    Settings.SubPriceDate = TimerServerUtcSync.GetNetworkTime();
                    this.SubsPrice = Settings.SubPrice;
                }
            }
            catch (Exception ex)
            {
                //it is alright that we couldn't get the price
            }
            finally
            {
                await billing.DisconnectAsync();
                //IsBusy = false;
            }
        }

        /// <summary>
        /// iOS 구독 영수증 상태
        /// 구독을 취소하고 환불 받을 경우?
        /// 당월만 구독하는 경우?
        /// https://developer.apple.com/documentation/appstorereceipts/status
        /// </summary>
        /// <returns></returns>
        private string ReceiptSatusMessage(int status)
        {
            string msg = string.Empty;

            switch (status)
            {
                case 0:
                    msg = "OK";
                    break;
                case 21000:
                    msg = "21000,The request to the App Store was not made using the HTTP POST request method.";
                    break;
                case 21001:
                    msg = "21001,This status code is no longer sent by the App Store.";
                    break;
                case 21002:
                    msg = "21002,The data in the receipt-data property was malformed or the service experienced a temporary issue. Try again.";
                    break;
                case 21003:
                    msg = "21003,The receipt could not be authenticated.";
                    break;
                case 21004:
                    msg = "21004,The shared secret you provided does not match the shared secret on file for your account.";
                    break;
                case 21005:
                    msg = "21005,The receipt server was temporarily unable to provide the receipt. Try again.";
                    break;
                case 21006:
                    msg = "21006,This receipt is valid but the subscription has expired.";
                    break;
                case 21007:
                    msg = "21007,This receipt is from the test environment, but it was sent to the production environment for verification.";
                    break;
                case 21008:
                    msg = "21008,This receipt is from the production environment, but it was sent to the test environment for verification.";
                    break;
                case 21009:
                    msg = "21009,Internal data access error. Try again later.";
                    break;
                case 21010:
                    msg = "21010,The user account cannot be found or has been deleted.";
                    break;

                default:
                    msg = "";
                    break;
            }

            return msg;
        }

        /// <summary>
        /// The reason a subscription expired.
        /// iOS 구독기간 만료에 대한 이유 
        /// https://developer.apple.com/documentation/appstorereceipts/expiration_intent
        /// </summary>
        /// <param name="expiration_intent"></param>
        /// <returns></returns>
        private string ExpirationIntentMessage(int expiration_intent)
        {
            string msg = string.Empty;

            switch (expiration_intent)
            {
                case 1:
                    msg = "1,The customer canceled their subscription.";
                    break;
                case 2:
                    msg = "2,Billing error; for example, the customer’s payment information is no longer valid.";
                    break;
                case 3:
                    msg = "3,The customer didn’t consent to an auto-renewable subscription price increase that requires customer consent, allowing the subscription to expire.";
                    break;
                case 4:
                    msg = "4,The product wasn’t available for purchase at the time of renewal.";
                    break;
                case 5:
                    msg = "5,Unknown error.";
                    break;

                default:
                    msg = "";
                    break;
            }

            return msg;

        }

        public int SubscriptionDays
        {
            get => _subscriptionDays;
            set => SetProperty(ref this._subscriptionDays, value);
        }

        public string SubsPrice
        {
            get => _subsPrice;
            set => SetProperty(ref this._subsPrice, value);
        }
    }
}
