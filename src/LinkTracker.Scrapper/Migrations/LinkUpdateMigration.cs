using FluentMigrator;

namespace LinkTracker.Scrapper.Migrations;

[Migration(20260403)]
public class LinkUpdateMigration : Migration
{
    public override void Up()
    {
        Create.Table("link_updates").WithDescription("Stores link updates in case there was some problem delivering them to bot")
            .WithColumn("id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("url").AsAnsiString().NotNullable()
            .WithColumn("content").AsString().Nullable()
            .WithColumn("datetime").AsDateTime().NotNullable();
        Create.Index().OnTable("link_updates").OnColumn("url");
        Create.Index().OnTable("link_updates").OnColumn("url").Ascending().OnColumn("datetime").Descending();
    }

    public override void Down()
    {
        Delete.Index().OnTable("link_updates").OnColumn("url");
        Delete.Index().OnTable("link_updates").OnColumns("url", "datetime");
        Delete.Table("link_updates");
    }
}