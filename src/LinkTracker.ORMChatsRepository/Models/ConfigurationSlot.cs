using LinqToDB.Mapping;

namespace LinkTracker.ORMChatsRepository.Models;

[Table("config")]
public class ConfigurationSlot
{
    [Column("id"), PrimaryKey, Identity]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("value")]
    public long Value { get; set; }
}
