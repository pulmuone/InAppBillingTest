using Google.Apis.AndroidPublisher.v3.Data;
using InAppBillingTest.Helpers;
using InAppBillingTest.Interface;
using InAppBillingTest.Models.VerifyReceipt;
using Newtonsoft.Json;
using Plugin.InAppBilling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace InAppBillingTest.Services
{
    public class SyncSubsStatusService
    {
        const string productIdSub = "premium1";
        private static readonly SyncSubsStatusService instance = new SyncSubsStatusService();

        private SyncSubsStatusService()
        {

        }

        public static SyncSubsStatusService GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// 이 함수에 진입은 인터넷이 되는 상태에서 들어온다.
        /// </summary>
        public async Task SyncStatus()
        {
            if (Connectivity.NetworkAccess == NetworkAccess.Internet)
            {
                var nowDateTimeUtc = TimerServerUtcSync.GetNetworkTime();

                try
                {
                    var connected = await CrossInAppBilling.Current.ConnectAsync();
                    if (connected)
                    {
                        var subs = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.Subscription);
                        //일시 중지면 목록이 조회 되지 않는다.
                        //안드로이는 환불되면 목록에 보이지 않는다.
                        //안드로이는 구독중에 취소해도 남은 기간은 사용 가능하다.
                        if (subs?.Any(p => p.ProductId == productIdSub) ?? false)
                        {
                            var sorted = subs.Where(p => p.ProductId == productIdSub).OrderByDescending(i => i.TransactionDateUtc).ToList();
                            if (!sorted?.Any() ?? false)
                            {
                                Settings.HasPurchasedSub = false;
                                return;
                            }

                            var recentSub = sorted[0];
                            if (recentSub != null)
                            {
                                if (DeviceInfo.Platform == DevicePlatform.Android)
                                {
                                    if (recentSub.State == PurchaseState.Purchased || recentSub.State == PurchaseState.Restored)
                                    {
                                        IVerifyReceiptService service = DependencyService.Get<IVerifyReceiptService>();
                                        SubscriptionPurchaseV2 receipt = await service.GetReceipt("com.gwise.**************", recentSub.PurchaseToken ?? string.Empty);
                                        if (receipt != null)
                                        {
                                            //안드로이드는 우선 상태가 구독이 되어야 한다. 취소/만료도 구독 상태로 되어 있다.
                                            if (receipt.AcknowledgementState == "ACKNOWLEDGEMENT_STATE_ACKNOWLEDGED")
                                            {
                                                //SUBSCRIPTION_STATE_ACTIVE : 구독 활성 상태
                                                //SUBSCRIPTION_STATE_CANCELED : 정기 결제가 취소되었지만 아직 만료되지 않았다. 안드로이는 이 상태값인 경우도 만료일자를 확인해야 한다. 환불되지는 않았다.
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
                                                                                                                                 //구독 만료일자 체크
                                                            if (expirationDate > nowDateTimeUtc)
                                                            {
                                                                //Debug.WriteLine(recentSub.PurchaseToken);
                                                                Settings.HasPurchasedSub = true;
                                                                Settings.CheckSubStatus = true;
                                                                Settings.SubReceipt = recentSub.PurchaseToken ?? string.Empty;
                                                                Settings.SubExpirationDate = expirationDate;
                                                                Settings.OrderId = recentSub.Id;

                                                                //var diffDays = Settings.SubExpirationDate.Subtract(nowDateTimeUtc);
                                                                //this.SubscriptionDays = diffDays.Days;
                                                            }
                                                            else
                                                            {
                                                                //구독만료 일자가 지났습니다.
                                                                Settings.HasPurchasedSub = false;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Settings.HasPurchasedSub = false;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Settings.HasPurchasedSub = false;
                                                    }
                                                }
                                                else
                                                {
                                                    //구독만료 일자가 지났습니다.
                                                    Settings.HasPurchasedSub = false;
                                                }
                                            }
                                            else
                                            {
                                                //구독만료 일자가 지났습니다.
                                                Settings.HasPurchasedSub = false;
                                            }
                                        }
                                        else
                                        {
                                            //구독만료 일자가 지났습니다.
                                            Settings.HasPurchasedSub = false;
                                        }
                                    }
                                    else
                                    {
                                        Settings.HasPurchasedSub = false;
                                    }
                                }
                                else
                                { //iOS
                                    if (recentSub.State == PurchaseState.Purchased || recentSub.State == PurchaseState.Restored)
                                    {
                                        RequestBody req = new RequestBody()
                                        {
                                            password = "ee7b992---------------------------", //iOS Password
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
                                            //정상
                                            long elapsed_ms = long.Parse(responseBody.receipt.expires_date); //구독 만료일자
                                            DateTime expirationDate = DateTimeOffset.FromUnixTimeMilliseconds(elapsed_ms).UtcDateTime;

                                            //구독 만료일자 체크
                                            if (expirationDate > nowDateTimeUtc)
                                            {
                                                Settings.HasPurchasedSub = true;
                                                Settings.CheckSubStatus = true;
                                                Settings.SubReceipt = recentSub.PurchaseToken ?? string.Empty;
                                                //Settings.SubExpirationDate = recentSub.TransactionDateUtc.AddMonths(1).AddDays(5);
                                                Settings.SubExpirationDate = expirationDate;
                                                Settings.OrderId = recentSub.Id; //영수증 번호

                                                //var diffDays = Settings.SubExpirationDate.Subtract(nowDateTimeUtc);
                                                //this.SubscriptionDays = diffDays.Days;
                                            }
                                            else
                                            {
                                                Settings.HasPurchasedSub = false;
                                                //구독만료 일자가 지났습니다.
                                                //receipt_status = "subscription has expired";
                                                //expiration_intent = "ExpirationDate:" + expirationDate.ToString();
                                            }
                                        }
                                        else
                                        {
                                            Settings.HasPurchasedSub = false;
                                            //receipt_status = ReceiptSatusMessage(responseBody.status); //영수증 상태
                                            //expiration_intent = ExpirationIntentMessage(responseBody.expiration_intent); //구독 만료 사유
                                        }
                                    }
                                    else
                                    {
                                        Settings.HasPurchasedSub = false;
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
                            Settings.HasPurchasedSub = false;
                        }
                    }

                    Settings.SyncStatusDate = nowDateTimeUtc;
                }
                catch (Exception ex)
                {
                    //it is alright that we couldn't get the price
                }
                finally
                {
                    await CrossInAppBilling.Current.DisconnectAsync();
                }
            }
        }
    }
}
