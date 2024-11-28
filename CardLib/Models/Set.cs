#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using SQLite;

namespace CardLib.Models
{
    public class Set
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int ProductLineId { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name;
    }
}
