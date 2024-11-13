using CardLib;
using CardLib.Models;
using CardPilkApp.DataObjects;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CardPilkApp.ViewModels
{
    internal partial class RepricerHistoryViewModel : ObservableObject
    {
        public ObservableCollection<RepricerUpdateDO> Updates { get; set; }

        public string[] BasePricers => ["Low", "Market", "Shipped Low", "Direct Low"];
        public int SelectedPricerIndex { get; set; }
        public string PercentageEntry { get; set; } = "100";
        public string MinPriceEntry { get; set; } = "0.10";
        public bool IncludeOOS { get; set; } = true;

        private CardManager manager;

        public RepricerHistoryViewModel(CardManager manager)
        {
            Updates = [];
            this.manager = manager;
            RefreshUpdates();
        }

        private async void RefreshUpdates()
        {
            Updates.Clear();
            var ups = (await manager.GetRepricerUpdates(50) ?? []).ToList();
            for (int i = 0; i < ups.Count; i++)
            {
                Updates.Add(RepricerUpdateDO.FromModel(ups[i]));
            }
        }

        #region Commands

        [RelayCommand]
        private async Task RunRepricer()
        {
            if (!(await Shell.Current.DisplayAlert("Repricer Tool", "Are you sure you'd like to run the Repricer?", "Yes", "No"))) return;
            if (SelectedPricerIndex < 0) return;
            string basepricer = BasePricers[SelectedPricerIndex];
            if (!decimal.TryParse(PercentageEntry, out var percent)) return;
            if (!decimal.TryParse(MinPriceEntry, out var minPrice)) return;
            RepricerUpdate res = await manager.RepriceCards(IncludeOOS, basepricer, percent, minPrice);
            Toast.Make($"Updated {res.PricesChanged} prices.");
            RefreshUpdates();
        }

        #endregion
    }
}
