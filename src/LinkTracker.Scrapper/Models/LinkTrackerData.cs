using LinkTracker.Dto;

namespace LinkTracker.Scrapper.Models;

public class LinkTrackerData : IEquatable<LinkTrackerData>
{
    public required string ResourceURL { get; set; }

    public List<string> Tags { get; set; } = [];

    public List<string> Filters { get; set; } = [];

    public DateTime LastUpdate { get; set; } = DateTime.MinValue;

    public required long UserId { get; set; }

    public required long Id { get; init; }

    public bool Equals(LinkTrackerData? other)
    {
        if (other is null)
        {
            return false;
        }
        return Id == other.Id;
    }

    public static bool operator ==(LinkTrackerData? a, LinkTrackerData? b)
    {
        if (a is null && b is null)
        {
            return true;
        }
        if (a is null || b is null)
        {
            return false;
        }
        return a.Equals(b);
    }

    public static bool operator !=(LinkTrackerData? a, LinkTrackerData? b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as LinkTrackerData);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public LinkResponseDto ResponseDto => new() { Id = Id, Filters = Filters, Tags = Tags, Url = ResourceURL };
}