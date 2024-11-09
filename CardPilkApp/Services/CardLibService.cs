using CardLib;

namespace CardPilkApp.Services
{
    public interface ICardLibService
    {
        public CardManager GetManager();
    }

    public class CardLibService : ICardLibService
    {
        CardManager instance;
        public CardLibService()
        {
            instance = new(FileSystem.Current.AppDataDirectory + "\\cpilk.db");
        }

        public CardManager GetManager()
        {
            return instance;
        }
    }
}
