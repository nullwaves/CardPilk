using CardLib;
using CardLib.Models;
using CardPilkApp.DataObjects;
using System.Collections.ObjectModel;

namespace CardPilkApp.ViewModels
{
    internal class CartListViewModel
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
                    lineDOs.Add(new CartLineItemDO()
                    {
                        Id = line.CardId,
                        Name = listing?.Name ?? "",
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
    }
}
