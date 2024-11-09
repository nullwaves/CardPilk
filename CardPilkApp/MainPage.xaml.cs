using CardLib;
using CardPilkApp.ViewModels;
using System.Text;

namespace CardPilkApp
{
    public partial class MainPage : ContentPage
    {
        CardManager manager = App.CardLib.GetManager();

        internal CardListViewModel _viewmodel;

        public MainPage()
        {
            InitializeComponent();
            _viewmodel = new(manager);
            BindingContext = _viewmodel;
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            FileResult? result = await FilePicker.PickAsync(PickOptions.Default);
            if (result == null) return;
            TCGplayerImportResult res = await manager.ImportFromTCGplayer(result);
            if (!res.ValidHeaders)
            {
                await DisplayAlert("TCGplayer Import", "Invalid Headers detected. Please correct and try again.", "Done");
            }
            if (res.Items == null) return;
            TCGplayerUpsertResult ures = await manager.UpsertTCGplayerRows(res.Items);
            var resstr = new StringBuilder();
            resstr.AppendLine($"Valid Rows: {res.Items?.Length ?? 0}");
            resstr.AppendLine($"Invalid Rows: {res.InvalidRows}");
            resstr.AppendLine($"Created Cards: {ures.CreatedCards}");
            resstr.AppendLine($"Created Conditions: {ures.CreatedConditions}");
            resstr.AppendLine($"Created ProductLines: {ures.CreatedProductLines}");
            resstr.AppendLine($"Created Rarities: {ures.CreatedRarities}");
            resstr.AppendLine($"Created Sets: {ures.CreatedSets}");
            resstr.AppendLine($"Created Prices: {ures.CreatedPrices}");
            resstr.AppendLine($"Updated Qtys: {ures.UpdatedQuantities}");
            await DisplayAlert("TCGplayer Import", resstr.ToString(), "Done");
            await _viewmodel.RefreshListings();
            _viewmodel.Search();
        }

        private void SearchInput_Completed(object sender, EventArgs e)
        {
            _viewmodel.Search();
        }

        private void ToggleInStockOnly(object sender, CheckedChangedEventArgs e)
        {
            _viewmodel.Search();
        }

        private void MaxListingsPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is Picker p && p.SelectedIndex != -1)
            {
                _viewmodel.MaxListings = _viewmodel.MaxListingsOptions[p.SelectedIndex];
                _viewmodel.ExecuteSearch();
            }
        }

        private void FilterProductLinePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            object? oldSetFilter = FilterSetPicker.SelectedItem;
            if (sender is Picker p)
            {
                _viewmodel.UpdateSetsFilter();
            }
            if (FilterSetPicker.SelectedIndex < 0) FilterSetPicker.SelectedIndex = 0;
        }

        private async void Page_Loaded(object sender, EventArgs e)
        {
            await _viewmodel.RefreshListings();
            MaxListingsPicker.SelectedIndex = 1;
            FilterProductLinePicker.SelectedIndex = 0;
            FilterSetPicker.SelectedIndex = 0;
            FilterConditionPicker.SelectedIndex = 0;
        }

        private void VariantPicker_BindingChanged(object sender, EventArgs e)
        {
            if (sender is Picker p)
            {
                if (p.SelectedIndex == -1 && p.Items.Count > 0)
                    p.SelectedIndex = 0;
            }
        }

        private void VariantPicker_Clicked(object sender, EventArgs e)
        {
            SearchInput.Focus();
            SearchInput.CursorPosition = 0;
            SearchInput.SelectionLength = SearchInput.Text.Length;
        }

        private async void OnRepricerClicked(object sender, EventArgs e)
        {
            string[] pricers = ["Low", "Market", "Shipped Low", "Direct Low"];
            string basepricer = await DisplayActionSheet("Choose Base Price", "Cancel", null, pricers);
            int priceIndex = Array.IndexOf(pricers, basepricer);
            if (priceIndex < 0 || priceIndex >= pricers.Length) return;
            string percentstring = await DisplayPromptAsync("Repricer Tool", "Percentage of Base Price", initialValue: "100");
            decimal percent;
            if (!decimal.TryParse(percentstring, out percent)) { await DisplayAlert("Repricer Tool", "Invalid Percentage Entered.", "OK"); return; }
            string minimumPriceString = await DisplayPromptAsync("Repricer Tool", "Minimum Allowed Price", initialValue: "0.10");
            decimal minPrice;
            if (!decimal.TryParse(minimumPriceString, out minPrice)) { await DisplayAlert("Repricer Tool", "Invalid Minimum Price Entered.", "OK"); return; }
            bool includeOOS = await DisplayActionSheet("Include Out of Stock?", "No", null, ["Yes"]) == "Yes";
            await _viewmodel.RepriceCards(basepricer, percent, minPrice, includeOOS);
        }

        private async void SaveCart_Clicked(object sender, EventArgs e)
        {
            if (_viewmodel.CartItems.Count > 0)
                await _viewmodel.SaveCart();
        }

        private async void CartHistory_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("/Carts");
        }
    }
}
