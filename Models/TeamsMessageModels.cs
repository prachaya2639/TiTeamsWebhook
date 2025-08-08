using System.Text.Json.Serialization;

namespace TiTeamsWebhook.Models
{
    public class TeamsMessageCard
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; } = "MessageCard";
        
        [JsonPropertyName("@context")]
        public string Context { get; set; } = "http://schema.org/extensions";
        
        public string ThemeColor { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<TeamsSection> Sections { get; set; } = new();
        public List<TeamsPotentialAction>? PotentialAction { get; set; }
    }

    public class TeamsSection
    {
        public string ActivityTitle { get; set; } = string.Empty;
        public string? ActivitySubtitle { get; set; }
        public string? ActivityImage { get; set; }
        public List<TeamsFact> Facts { get; set; } = new();
        public bool Markdown { get; set; } = true;
    }

    public class TeamsFact
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class TeamsPotentialAction
    {
        [JsonPropertyName("@type")]
        public string Type { get; set; } = "OpenUri";
        public string Name { get; set; } = string.Empty;
        public List<TeamsTarget> Targets { get; set; } = new();
    }

    public class TeamsTarget
    {
        public string Os { get; set; } = "default";
        public string Uri { get; set; } = string.Empty;
    }
}
