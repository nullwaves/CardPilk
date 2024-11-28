using CardLib;
using CardLib.Models;
using CardPilkApp.DataObjects;
using CardPilkApp.Services;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using System.Collections.ObjectModel;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text.Json;

namespace CardPilkApp.ViewModels
{
    internal partial class ScryfallSettingsViewmodel : ObservableObject
    {
        public ObservableCollection<ScryfallCard> Cards { get; set; }
        [ObservableProperty]
        private int missingImages;
        [ObservableProperty]
        private bool _importActivity = false;

        private CardManager _manager;
        private List<LorcastSet> _lorcastSets;

        public ScryfallSettingsViewmodel()
        {
            _manager = App.CardLib.GetManager();
            Cards = [];
            _lorcastSets = [];
            Init();
        }

        private async void Init()
        {
            _lorcastSets = await App.Lorcast.GetSets();
            UpdateMissingImageCount();
        }

        private async void UpdateMissingImageCount()
        {
            MissingImages = await _manager.GetMissingImagesCount();
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
            UpdateMissingImageCount();
            SetImportActivity(false);
            await Shell.Current.DisplayAlert("Scryfall Service", $"Linked images to {numUpdated.ToMetric()} cards.", "OK");
        }

        public async Task LinkImagesFromLorcast(Func<double, Task> progressCallback)
        {
            SetImportActivity(true);
            int lorcanaPid = (await _manager.QueryProductLines().Where(x => x.Name.Contains("Lorcana")).FirstOrDefaultAsync())?.Id ?? -1;
            if (lorcanaPid < 0)
            {
                await Shell.Current.DisplayAlert("Lorcast Service", "Could not determine Lorcana product line.", "OK");
                return;
            }
            List<Set> pSets = (await _manager.GetSetsFromProductLine(lorcanaPid)).ToList();
            // get all lorcana cards that are missing images
            List<CardListing> listings = (
                await _manager.QueryCardListings()
                .Where(
                    x => x.ProductLineId == lorcanaPid &&
                    (x.ImageUri == null || x.ImageUri == "default_card.png"))
                .ToListAsync()
                ).DistinctBy(x => new { x.CardNumber, x.SetId }).ToList();
            // for each card, get matching lorcast set and card number, try fetch card and update image
            int numUpdated = 0;
            for (int i = 0; i < listings.Count; i++)
            {
                CardListing listing = listings[i];
                Set set = pSets.First(x => x.Id == listing.SetId);
                string? lorcastSetId = _lorcastSets.Where(x => x.name.Contains(set.Name)).FirstOrDefault().code ?? null;
                if (lorcastSetId != null)
                {
                    string cardNumber = listing.CardNumber.Contains('/') ? listing.CardNumber[..listing.CardNumber.IndexOf('/')] : listing.CardNumber;
                    var uri = await App.Lorcast.GetCardImageByNumber(lorcastSetId, cardNumber);
                    if (uri != null)
                    {
                        foreach (string old in new[] { "small", "normal", "large" })
                        {
                            uri = uri.Replace(old, "full");
                        }
                        uri = uri.Replace(".avif", ".jpg");
                        // Get and update all variants
                        List<CardListing> variants = await _manager.QueryCardListings().Where(x => x.SetId == listing.SetId && x.CardNumber == listing.CardNumber).ToListAsync();
                        foreach (var variant in variants)
                        {
                            variant.ImageUri = uri;
                            numUpdated += await _manager.UpdateListing(variant);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to match Lorcast set ID {lorcastSetId} and card number {cardNumber}.");
                    }
                }
                else
                {
                    Debug.WriteLine($"Failed to match Pilk Set \"{set.Name}\" to Lorcast Sets!");
                }
                if (i % 10 == 0) { await progressCallback.Invoke(i / listings.Count); }
            }
            await Shell.Current.DisplayAlert("Lorcast Service", $"Updated images for {numUpdated} cards.", "OK");
            SetImportActivity(false);
        }
    }
}
