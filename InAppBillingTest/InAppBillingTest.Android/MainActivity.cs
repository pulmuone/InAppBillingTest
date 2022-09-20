using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using AndroidX.AppCompat.App;
using Google.Android.Vending.Licensing;
using static Android.Provider.Settings;
using Android.Content;

namespace InAppBillingTest.Droid
{
    [Activity(Label = "InAppBillingTest", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity, ILicenseCheckerCallback
    {
        static App formsApp;

        //App Licensing 
        private const string Base64PublicKey = "--------------------";
        private static readonly byte[] Salt = new byte[] { 46, 65, 30, 128, 103, 57, 74, 64, 51, 88, 95, 45, 77, 117, 36, 113, 11, 32, 64, 89 };

        private LicenseChecker checker;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
            this.Window.AddFlags(Android.Views.WindowManagerFlags.KeepScreenOn);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            string deviceId = Android.Provider.Settings.Secure.GetString(ContentResolver, Secure.AndroidId);
            var obfuscator = new AESObfuscator(Salt, PackageName, deviceId);
            var policy = new ServerManagedPolicy(this, obfuscator);

            //★★★★★
            checker = new LicenseChecker(this, policy, Base64PublicKey);
            DoCheck();

            //LoadApplication(new App());
            LoadApplication(formsApp ??= new App());
        }

        protected override void OnResume()
        {
            base.OnResume();

            Xamarin.Essentials.Platform.OnResume(this);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            Xamarin.Essentials.Platform.OnNewIntent(intent);
        }

        private void DoCheck()
        {
            checker.CheckAccess(this);
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            //★★★★★
            checker.OnDestroy();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public void Allow([GeneratedEnum] PolicyResponse reason)
        {
            if (IsFinishing)
            {
                // Don't update UI if Activity is finishing.
                return;
            }

            //라이선스 테스트 확인
            //AndroidX.AppCompat.App.AlertDialog.Builder alert = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            //alert.SetTitle("License Check");
            //alert.SetMessage("Allow the user access " + reason.ToString());
            //alert.SetPositiveButton("OK", delegate { }); // ok
            //alert.SetNegativeButton("Quit", delegate { Finish(); }); // cancel
            //Dialog dialog = alert.Create();
            //dialog.Show();
        }

        public void ApplicationError([GeneratedEnum] LicenseCheckerErrorCode errorCode)
        {
            if (IsFinishing)
            {
                // Don't update UI if Activity is finishing.
                return;
            }

            AndroidX.AppCompat.App.AlertDialog.Builder alert = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            alert.SetTitle("ApplicationError");
            alert.SetMessage(errorCode.ToString());
            //alert.SetPositiveButton("OK", delegate { }); // ok
            alert.SetNegativeButton("Quit", delegate { Finish(); }); // cancel
            Dialog dialog = alert.Create();
            dialog.Show();

            //DisplayResult(string.Format("Application error ", errorCode));
        }

        public void DontAllow([GeneratedEnum] PolicyResponse reason)
        {
            AndroidX.AppCompat.App.AlertDialog.Builder alert = new AndroidX.AppCompat.App.AlertDialog.Builder(this);
            alert.SetTitle("License Check");
            alert.SetMessage("DontAllow " + reason.ToString());
            //alert.SetPositiveButton("OK", delegate { }); // ok
            alert.SetNegativeButton("Quit", delegate { Finish(); }); // cancel
            Dialog dialog = alert.Create();
            dialog.Show();
        }
    }
}