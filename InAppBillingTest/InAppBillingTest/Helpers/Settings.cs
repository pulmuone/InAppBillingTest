using InAppBillingTest.Services;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Essentials;

namespace InAppBillingTest.Helpers
{
    public static class Settings
    {

        public static bool HasPurchasedSub
        {
            get => Preferences.Get(nameof(HasPurchasedSub), false);
            set => Preferences.Set(nameof(HasPurchasedSub), value);
        }

        public static bool CheckSubStatus
        {
            get => Preferences.Get(nameof(CheckSubStatus), false);
            set => Preferences.Set(nameof(CheckSubStatus), value);
        }

        public static string SubReceipt
        {
            get => Preferences.Get(nameof(SubReceipt), string.Empty);
            set => Preferences.Set(nameof(SubReceipt), value);
        }

        public static DateTime SubPriceDate
        {
            get => Preferences.Get(nameof(SubPriceDate), TimerServerUtcSync.GetNetworkTime().AddDays(-3));
            set => Preferences.Set(nameof(SubPriceDate), value);
        }

        public static string SubPrice
        {
            get => Preferences.Get(nameof(SubPrice), string.Empty);
            set => Preferences.Set(nameof(SubPrice), value);
        }


        public static DateTime TransactionDateUtc
        {
            get => Preferences.Get(nameof(TransactionDateUtc), DateTime.UtcNow);
            set => Preferences.Set(nameof(TransactionDateUtc), value);
        }

        public static DateTime SubExpirationDate
        {
            get => Preferences.Get(nameof(SubExpirationDate), DateTime.UtcNow);
            set => Preferences.Set(nameof(SubExpirationDate), value);
        }

        //상태 체크는 하루에 한번만 한다.
        public static DateTime SyncStatusDate
        {
            get => Preferences.Get(nameof(SyncStatusDate), TimerServerUtcSync.GetNetworkTime().AddDays(-3));
            set => Preferences.Set(nameof(SyncStatusDate), value);
        }

        public static string OrderId
        {
            get => Preferences.Get(nameof(OrderId), string.Empty);
            set => Preferences.Set(nameof(OrderId), value);
        }

        public static bool IsSubValid
        {
            get => Preferences.Get(nameof(IsSubValid), Settings.HasPurchasedSub && Settings.SubExpirationDate > TimerServerUtcSync.GetNetworkTime());
            //get => Preferences.Get(nameof(IsSubValid), Settings.HasPurchasedSub && Settings.SubExpirationDate > DateTime.UtcNow);
            set => Preferences.Set(nameof(IsSubValid), value);
        }

    }
}
