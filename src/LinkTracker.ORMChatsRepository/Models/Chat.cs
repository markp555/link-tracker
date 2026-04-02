using LinqToDB.Mapping;

namespace LinkTracker.ORMChatsRepository.Models;

[Table("chats")]
public class Chat
{
    [Column("id"), PrimaryKey, Identity]
    public long Id { get; set; }

    [Column("tgid"), NotNull]
    public long TgID { get; set; }

    [Association(ThisKey = nameof(TgID), OtherKey = nameof(Link.UserId))]
    public List<Link> Links { get; set; } = null!;
}
