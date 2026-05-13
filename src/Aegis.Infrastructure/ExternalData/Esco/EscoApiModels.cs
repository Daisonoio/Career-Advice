using System.Text.Json.Serialization;

namespace Aegis.Infrastructure.ExternalData.Esco;

// ── Search endpoint ────────────────────────────────────────────────────────────

public record EscoSearchResponse(
    [property: JsonPropertyName("_embedded")] EscoSearchEmbedded? Embedded,
    [property: JsonPropertyName("total")]     int Total);

public record EscoSearchEmbedded(
    [property: JsonPropertyName("results")] List<EscoSearchResult>? Results);

public record EscoSearchResult(
    [property: JsonPropertyName("uri")]       string Uri,
    [property: JsonPropertyName("title")]     string Title,
    [property: JsonPropertyName("className")] string ClassName,
    [property: JsonPropertyName("score")]     double Score);

// ── Occupation resource ────────────────────────────────────────────────────────

public record EscoOccupationResource(
    [property: JsonPropertyName("uri")]    string Uri,
    [property: JsonPropertyName("title")]  string Title,
    [property: JsonPropertyName("_links")] EscoOccupationLinks? Links);

public record EscoOccupationLinks(
    [property: JsonPropertyName("hasEssentialSkill")] List<EscoSkillLink>? EssentialSkills,
    [property: JsonPropertyName("hasOptionalSkill")]  List<EscoSkillLink>? OptionalSkills);

public record EscoSkillLink(
    [property: JsonPropertyName("uri")]   string Uri,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("href")]  string Href);

// ── Skill resource ─────────────────────────────────────────────────────────────

public record EscoSkillResource(
    [property: JsonPropertyName("uri")]         string Uri,
    [property: JsonPropertyName("title")]       string Title,
    [property: JsonPropertyName("description")] Dictionary<string, EscoDescription>? Description,
    [property: JsonPropertyName("skillType")]   string? SkillType,
    [property: JsonPropertyName("_links")]      EscoSkillLinks? Links);

public record EscoDescription(
    [property: JsonPropertyName("literal")] string? Literal);

public record EscoSkillLinks(
    [property: JsonPropertyName("broaderHierarchyConcept")] List<EscoSkillLink>? BroaderConcepts,
    [property: JsonPropertyName("isInScheme")]              List<EscoSkillLink>? InScheme);

// ── Internal transfer object used between client and mapper ────────────────────

public record EscoSkillData(
    string Uri,
    string TitleEn,
    string? TitleIt,
    string? BroaderConceptTitle,
    string SkillType,
    bool   IsEssential);
