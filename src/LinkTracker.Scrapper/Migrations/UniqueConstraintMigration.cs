using FluentMigrator;

namespace LinkTracker.Scrapper.Migrations;

[Migration(20260402)]
public class UniqueConstraintMigration : Migration
{
    public override void Up()
    {
        Create.UniqueConstraint().OnTable("links").Columns("user_id", "resource_url");
        Create.UniqueConstraint().OnTable("tags").Columns("link_id", "tag_name");
        Create.UniqueConstraint().OnTable("filters").Columns("link_id", "filter");
    }

    public override void Down()
    {
        Delete.UniqueConstraint().FromTable("links").Columns("user_id", "resource_url");
        Delete.UniqueConstraint().FromTable("tags").Columns("link_id", "tag_name");
        Delete.UniqueConstraint().FromTable("filters").Columns("link_id", "filter");
    }
}