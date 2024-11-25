using CardLib;
using CardPilkApp.DataObjects;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace CardPilkApp.ViewModels
{
    internal partial class ScryfallSettingsViewmodel : ObservableObject
    {
        public ObservableCollection<ScryfallCard> Cards { get; set; }
        [ObservableProperty]
        private bool _importActivity = false;

        private CardManager _manager;

        public ScryfallSettingsViewmodel()
        {
            _manager = App.CardLib.GetManager();
            Cards = [];
        }

        internal void SetImportActivity(bool state)
        {
            ImportActivity = state;
        }

        public async Task LinkImagesFromScryfall(Stream stream, Func<double, Task> progressCallback)
        {
            SetImportActivity(true);
            List<ScryfallCard> cards = JsonSerializer.Deserialize<ScryfallCard[]>(stream)?.ToList() ?? [];
            Toast.Make($"Downloaded data for {cards.Count.ToMetric()} cards.");
            Cards.Clear();
            int numUpdated = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var pcard = await _manager.QueryCardListings()
                    .FirstOrDefaultAsync(x => x.Name.StartsWith(card.name) && x.CardNumber.Contains(card.collector_number));
                if (pcard != null)
                {
                    pcard.ImageUri = card.image_uris.FirstOrDefault().Value;
                    numUpdated += await _manager.UpdateListing(pcard);
                    Debug.WriteLine($"Matched pilk card to scryfall card by name & number: {pcard.Id} - {pcard.Name}");
                }
                Cards.Add(card);
                if (i % 1000 == 0)
                {
                    double percent = (double)i / cards.Count;
                    await progressCallback.Invoke(percent);
                }
            }
            SetImportActivity(false);
            await Shell.Current.DisplayAlert("Scryfall Service", $"Linked images to {numUpdated.ToMetric()} cards.", "OK");
        }
    }
}
