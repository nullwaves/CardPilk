using CardLib;
using CardLib.Models;
using CardPilkApp.DataObjects;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CardPilkApp.ViewModels
{
    internal partial class CartListViewModel : ObservableObject
    {
        private CardManager manager;
        public ObservableCollection<CartDO> Carts { get; set; }

        public CartListViewModel(CardManager manager)
        {
            Carts = [];
            this.manager = manager;
            RefreshCarts();
        }

        private async void RefreshCarts()
        {
            Carts.Clear();
            var res = await manager.GetCarts();
            var conds = await manager.GetConditions();
            foreach (var item in res)
            {
                var lines = item.GetLines();
                var lineDOs = new List<CartLineItemDO>();
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var listing = await manager.GetListingById(line.CardId);
                    if (listing == null) { throw new Exception("Database Integrity Error"); }
                    lineDOs.Add(new CartLineItemDO()
                    {
                        Id = line.CardId,
                        Name = listing.Name ?? "",
                        Condition = conds.First(x => x.Id == listing.ConditionId).Name,
                        Price = line.Price,
                        Quantity = line.Quantity,
                    });
                }
                Carts.Add(new()
                {
                    Id = item.Id,
                    CreatedAt = item.CreatedAt,
                    TotalQuantity = item.TotalQuantity,
                    Subtotal = item.Subtotal,
                    NumLines = item.NumLines,
                    Lines = lineDOs,
                });
            }
        }

        #region Commands

        [RelayCommand]
        public async Task ExportCart(CartDO cart)
        {
            Stream? output = await manager.CreateCSVFromCart(cart.Id);
            if (output != null)
            {
                string fileName = $"CardPilk_Export_Cart-{cart.Id}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.csv";
                FileSaverResult result = await FileSaver.Default.SaveAsync(fileName, output);
                if (result.IsSuccessful)
                {
                    await Toast.Make("Export Successful!").Show();
                }
                else
                {
                    await Toast.Make("Export Failed!").Show();
                }
            }
        }

        [RelayCommand]
        public async Task DeleteCart(CartDO cartObject)
        {
            bool promptResult = await Shell.Current.DisplayAlert($"Delete Cart #{cartObject.Id}", "Are you sure you want to delete this cart? It's permanent.", "Yes", "No");
            if (promptResult)
            {
                Cart cart = await manager.QueryCarts().FirstAsync(x => x.Id == cartObject.Id);
                int res = await manager.DeleteCart(cart);
                if (res < 1)
                {
                    Toast.Make("Failed to delete cart!");
                }
                else
                {
                    RefreshCarts();
                }
            }
        }

        #endregion
    }
}
