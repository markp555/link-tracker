using FluentMigrator;

namespace LinkTracker.Scrapper.Migrations;

[Migration(20263003)]
public class InitialMigration : Migration
{
    public override void Up()
    {
        Create.Table("chats").WithDescription("stores registered chats information")
            .WithColumn("id").AsInt64().NotNullable().PrimaryKey().Identity()
            .WithColumn("tgid").AsInt64().NotNullable().Unique();
        Create.Table("links").WithDescription("stores information about user's links")
            .WithColumn("id").AsInt64().NotNullable().PrimaryKey().Identity()
            .WithColumn("resource_url").AsString().NotNullable()
            .WithColumn("last_update").AsDateTime().NotNullable().WithDefaultValue(DateTime.MinValue)
            .WithColumn("user_id").AsInt64().NotNullable().ForeignKey("chats", "tgid").OnDelete(System.Data.Rule.Cascade);
        Create.Table("tags").WithDescription("stores information about link's tags")
            .WithColumn("id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("tag_name").AsString()
            .WithColumn("link_id").AsInt64().NotNullable().ForeignKey("links", "id").OnDelete(System.Data.Rule.Cascade);
        Create.Table("filters").WithDescription("stores information about link's filters")
            .WithColumn("id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("filter").AsString()
            .WithColumn("link_id").AsInt64().NotNullable().ForeignKey("links", "id").OnDelete(System.Data.Rule.Cascade);
        Create.Table("config").WithDescription("stores ID counter")
            .WithColumn("id").AsInt32().NotNullable().PrimaryKey().Identity()
            .WithColumn("name").AsAnsiString().NotNullable().Unique()
            .WithColumn("value").AsInt64().NotNullable();
        Insert.IntoTable("config").Row(new { name = "freeid", value = 1 });
        Create.Index().OnTable("tags").OnColumn("link_id");
        Create.Index().OnTable("links").OnColumn("user_id");
        Create.Index().OnTable("filters").OnColumn("link_id");
    }

    public override void Down()
    {
        Delete.Index().OnTable("tags").OnColumn("link_id");
        Delete.Index().OnTable("links").OnColumn("user_id");
        Delete.Index().OnTable("filters").OnColumn("link_id");
        Delete.Table("config");
        Delete.Table("filters");
        Delete.Table("tags");
        Delete.Table("links");
        Delete.Table("chats");
    }
}