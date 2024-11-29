using CardPilkApp.ViewModels;

namespace CardPilkApp;

public partial class ScryfallSettingsPage : ContentPage
{
    Func<double, Task> UpdateProgressCallback => async (double progress) =>
    {
        await ImportProgressBar.ProgressTo(progress, 1, Easing.Linear);
    };
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
                    UpdateProgressCallback);
            });
    }

    private async void UpdateFromLorcast_Clicked(object sender, EventArgs e)
    {
        ImportProgressBar.Progress = 0;
        Dispatcher.Dispatch(
            async () =>
            {
                await _viewmodel.LinkImagesFromLorcast(UpdateProgressCallback);
            });
    }
}