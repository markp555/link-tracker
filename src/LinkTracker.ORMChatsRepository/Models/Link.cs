using LinqToDB.Mapping;

namespace LinkTracker.ORMChatsRepository.Models;

[Table("links")]
public class Link
{
    [Column("id"), PrimaryKey, Identity]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("resource_url")]
    public string ResourceURL { get; set; } = null!;

    [Column("last_update")]
    public DateTime LastUpdate { get; set; } = DateTime.MinValue;

    [Association(ThisKey = nameof(Id), OtherKey = nameof(Filter.LinkId))]
    public List<Filter> Filters { get; set; } = [];

    [Association(ThisKey = nameof(Id), OtherKey = nameof(Tag.LinkId))]
    public List<Tag> Tags { get; set; } = [];
}
