using CardLib;
using System.Diagnostics;

namespace CardPilkApp
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        CardManager manager = new(FileSystem.Current.AppDataDirectory+"/cpilk.db");

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            FileResult? result = await FilePicker.PickAsync(PickOptions.Default);
            if (result == null) return;
            TCGplayerImportResult res = await manager.ImportFromTCGplayer(result);
            Debug.WriteLineIf(res.ValidHeaders, "INVALID HEADERS DETECTED?");
            Debug.WriteLine($"INVALID ROWS: {res.InvalidRows}");
            Debug.WriteLine($"VALID ROWS: {res.Items?.Length ?? 0}");
            if (res.Items == null) return;
            TCGplayerUpsertResult ures = await manager.UpsertTCGplayerRows(res.Items);
            Debug.WriteLine($"Created Cards: {ures.CreatedCards}");
            Debug.WriteLine($"Created Prices: {ures.CreatedPrices}");
            Debug.WriteLine($"Created Conditions: {ures.CreatedConditions}");
            Debug.WriteLine($"Created ProductLines: {ures.CreatedProductLines}");
            Debug.WriteLine($"Created Rarities: {ures.CreatedRarities}");
            Debug.WriteLine($"Created Sets: {ures.CreatedSets}");
            Debug.WriteLine($"Created Prices: {ures.CreatedPrices}");
            Debug.WriteLine($"Updated Qtys: {ures.UpdatedQuantities}");
    }
    }

}
