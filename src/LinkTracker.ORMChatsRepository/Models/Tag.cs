using LinqToDB.Mapping;

namespace LinkTracker.ORMChatsRepository.Models;

[Table("tags")]
public class Tag
{
    [Column("id"), PrimaryKey, Identity]
    public int Id { get; set; }

    [Column("link_id")]
    public long LinkId { get; set; }

    [Column("tag_name")]
    public string Value { get; set; } = "";
}
