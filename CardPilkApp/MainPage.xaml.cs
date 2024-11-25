using CardLib;
using CardLib.Models;
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
            MaxListingsPicker.SelectedIndex = 1;
            FilterProductLinePicker.SelectedIndex = 0;
            FilterSetPicker.SelectedIndex = 0;
            FilterConditionPicker.SelectedIndex = 0;
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            FileResult? result = await FilePicker.PickAsync(PickOptions.Default);
            if (result == null) return;
            _viewmodel.ResultListings.Clear();
            ImportActivityIndicator.IsVisible = true;
            ImportBatch? res = await manager.ImportFromTCGplayer(result);
            ImportActivityIndicator.IsVisible = false;
            if (res != null)
            {
                var resstr = new StringBuilder();
                resstr.AppendLine($"Valid Rows: {res.GetItems()?.Length ?? 0}");
                resstr.AppendLine($"Invalid Rows: {res.InvalidRows}");
                resstr.AppendLine($"Created Cards: {res.CreatedCards}");
                resstr.AppendLine($"Created Conditions: {res.CreatedConditions}");
                resstr.AppendLine($"Created ProductLines: {res.CreatedProductLines}");
                resstr.AppendLine($"Created Rarities: {res.CreatedRarities}");
                resstr.AppendLine($"Created Sets: {res.CreatedSets}");
                resstr.AppendLine($"Created Prices: {res.CreatedPrices}");
                resstr.AppendLine($"Updated Qtys: {res.UpdatedQuantities}");
                await DisplayAlert("TCGplayer Import", resstr.ToString(), "Done");
                _viewmodel.ResetFilters();
            }
            else await DisplayAlert("TCGplayer Import", "Invalid Headers detected. Please correct and try again.", "Done");
            _viewmodel.Search();
        }

        private void SearchInput_Completed(object sender, EventArgs e)
        {
            _viewmodel.ExecuteSearch();
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
                _viewmodel.Search();
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

        private void Page_Loaded(object sender, EventArgs e)
        {
        }

        private void VariantPicker_BindingChanged(object sender, EventArgs e)
        {
            if (sender is Picker p)
            {
                if (p.SelectedIndex == -1 && p.Items.Count > 0)
                    p.SelectedIndex = 0;
            }
        }

        private void AddToCart_Clicked(object sender, EventArgs e)
        {
            SearchInput.Focus();
            SearchInput.CursorPosition = 0;
            SearchInput.SelectionLength = SearchInput.Text.Length;
        }

        private async void OnRepricerClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("/Repricer");
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

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("/Scryfall");
        }
    }
}
