using CardLib;
using CardPilkApp.ViewModels;
using System.Diagnostics;
using System.Text;

namespace CardPilkApp
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        CardManager manager = new(FileSystem.Current.AppDataDirectory+"/cpilk.db");

        internal CardListViewModel _viewmodel;

        public MainPage()
        {
            InitializeComponent();
            _viewmodel = new(manager);
            BindingContext = _viewmodel;
            MaxListingsPicker.SelectedIndex = 1;
            FilterProductLinePicker.SelectedIndex = 0;
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
            _viewmodel.RefreshListings();
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
                _viewmodel.Search();
            }
        }
    }

}
