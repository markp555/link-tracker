using LinkTracker.ORMChatsRepository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace LinkTracker.ORMChatsRepository;

internal class LinqToDbConnection(string connectionString) : DataConnection(new DataOptions().UseConnectionString(ProviderName.PostgreSQL, connectionString))
{
    public ITable<ConfigurationSlot> Configuration => this.GetTable<ConfigurationSlot>();

    public ITable<Tag> Tags => this.GetTable<Tag>();

    public ITable<Filter> Filters => this.GetTable<Filter>();

    public ITable<Link> Links => this.GetTable<Link>();

    public ITable<Chat> Chats => this.GetTable<Chat>();
}
