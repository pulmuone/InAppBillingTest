using InAppBillingTest.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace InAppBillingTest
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void payment_Clicked(object sender, EventArgs e)
        {
            PaymentPage page = new PaymentPage();
            await this.Navigation.PushAsync(page);

        }
    }
}
