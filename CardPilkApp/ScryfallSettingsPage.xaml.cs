using CardPilkApp.ViewModels;

namespace CardPilkApp;

public partial class ScryfallSettingsPage : ContentPage
{
    private ScryfallSettingsViewmodel _viewmodel;

    public ScryfallSettingsPage()
    {
        InitializeComponent();
        _viewmodel = new();
        BindingContext = _viewmodel;
    }

    private async void UpdateFromScryfall_Clicked(object sender, EventArgs e)
    {
        var stream = await App.Scryfall.FetchBulkDataAsync();
        if (stream == null)
        {
            await DisplayAlert("Scryfall Service", "Failed to fetch bulk data URI from Scryfall!", "OK");
            return;
        }
        ImportProgressBar.Progress = 0;
        Dispatcher.Dispatch(
            async () =>
            {
                await _viewmodel.LinkImagesFromScryfall(
                    stream,
                    async (double progress) =>
                    {
                        await ImportProgressBar.ProgressTo(progress, 1, Easing.Linear);
                    });
            });
    }
}