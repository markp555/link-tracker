using LinqToDB.Mapping;

namespace LinkTracker.ORMChatsRepository.Models;

[Table("link_updates")]
public class LinkUpdate
{
    [Column("id"), PrimaryKey, Identity]
    public long Id { get; set; }

    [Column("url")]
    public string Url { get; set; } = "";

    [Column("content")]
    public string Value { get; set; } = "";

    [Column("datetime")]
    public DateTime DateTime { get; set; }
}
