using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;

namespace InAppBillingTest.ViewModels
{
    public class BaseViewModel : ObservableObject
    {
        public ICommand SortedCommand { get; set; }

        public BaseViewModel()
        {

        }

        public Task DisplayAlert(string title, string message) => Application.Current.MainPage.DisplayAlert(title, message, "OK");
        public Task<bool> DisplayAlert(string title, string message, string accept, string cancel) =>
            Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);

        bool isBusy = false;
        bool isEnabled = true;
        bool _isControlEnable = true;
        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { SetProperty(ref isEnabled, value); }
        }

        public bool IsControlEnable
        {
            get => _isControlEnable;
            set => SetProperty(ref this._isControlEnable, value);
        }

        string title = string.Empty;
        
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }
    }
}
