using CardLib;
using CardLib.Models;
using System.Collections.ObjectModel;

namespace CardPilkApp.ViewModels
{
    internal class CartListViewModel
    {
        private CardManager manager;
        public ObservableCollection<Cart> Carts { get; set; }

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
            foreach (var item in res)
            {
                Carts.Add(item);
            }
        }
    }
}
