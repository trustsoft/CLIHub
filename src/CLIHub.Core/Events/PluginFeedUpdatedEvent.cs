namespace CLIHub.Core.Events;

/// <summary>
///   Published when the background plugin feed refresh observes that the feed's
///   <c> { id, version, compatibility } </c> tuples changed. The payload is the SHA-256 hash of the
///   new index tuples, not the index itself, so subscribers know the feed changed without carrying
///   the parsed index across the event. No event is published when a refresh is unchanged (including
///   a 304 Not Modified) or fails.
/// </summary>
/// <param name="IndexHash"> The hex-encoded hash of the compared index tuples. </param>
public record PluginFeedUpdatedEvent(string IndexHash) : IDomainEvent;
