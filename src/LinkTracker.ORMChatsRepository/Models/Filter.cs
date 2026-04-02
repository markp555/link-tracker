using LinqToDB.Mapping;

namespace LinkTracker.ORMChatsRepository.Models;

[Table("filters")]
public class Filter
{
    [Column("id"), PrimaryKey, Identity]
    public int Id { get; set; }

    [Column("link_id")]
    public long LinkId { get; set; }

    [Column("filter")]
    public string Value { get; set; } = "";
}