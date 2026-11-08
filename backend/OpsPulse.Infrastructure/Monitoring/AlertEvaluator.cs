using System.Collections.Concurrent;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsPulse.Application.Common;
using OpsPulse.Application.Interfaces;
using OpsPulse.Domain.Alerts;
using OpsPulse.Domain.Incidents;
using OpsPulse.Domain.Notifications;
using OpsPulse.Infrastructure.Notifications;
using OpsPulse.Infrastructure.Persistence;

namespace OpsPulse.Infrastructure.Monitoring;

/// <summary>
/// Evaluates alert rules against incoming values. Breaches create deduplicated
/// incidents (respecting sustained duration and cooldown windows), resolve
/// automatically when the condition clears, and dispatch notifications to
/// in-app, email and webhook channels.
/// </summary>
public sealed class AlertEvaluator(
    OpsPulseDbContext db,
    IIncidentManager incidentManager,
    IRealTimeNotifier notifier,
    IEmailSender emailSender,
    IHttpClientFactory httpClientFactory,
    ILogger<AlertEvaluator> logger) : IAlertEvaluator
{
    // Breach/cooldown state; static because a single worker instance owns evaluation.
    private static readonly ConcurrentDictionary<string, BreachState> States = new();

    private sealed record BreachState(DateTime FirstBreachedAtUtc, DateTime? LastResolvedAtUtc);

    public async Task EvaluateMetricAsync(
        Guid organizationId,
        AlertMetric metric,
        double value,
        string sourceType,
        Guid? sourceId,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        var rules = await LoadRulesAsync(organizationId, metric, sourceType, sourceId, cancellationToken);

        foreach (var rule in rules)
        {
            var breached = rule.Comparator == AlertComparator.GreaterThan
                ? value > rule.Threshold
                : value < rule.Threshold;

            await EvaluateRuleAsync(rule, breached, sourceType, sourceId, sourceName,
                $"{metric} = {value:0.##} (threshold {rule.Comparator} {rule.Threshold:0.##})",
                cancellationToken);
        }
    }

    public async Task EvaluateHealthAsync(
        Guid organizationId,
        AlertMetric metric,
        bool isHealthy,
        string sourceType,
        Guid? sourceId,
        string sourceName,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        var rules = await LoadRulesAsync(organizationId, metric, sourceType, sourceId, cancellationToken);

        foreach (var rule in rules)
        {
            await EvaluateRuleAsync(rule, !isHealthy, sourceType, sourceId, sourceName,
                detail ?? (isHealthy ? "healthy" : "unhealthy"),
                cancellationToken);
        }
    }

    private Task<List<AlertRule>> LoadRulesAsync(
        Guid organizationId, AlertMetric metric, string sourceType, Guid? sourceId, CancellationToken cancellationToken)
        => db.AlertRules
            .AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.IsEnabled && r.Metric == metric)
            .Where(r => sourceType != "service" || sourceId == null || r.TargetServiceId == null || r.TargetServiceId == sourceId)
            .ToListAsync(cancellationToken);

    private async Task EvaluateRuleAsync(
        AlertRule rule,
        bool breached,
        string sourceType,
        Guid? sourceId,
        string sourceName,
        string detail,
        CancellationToken cancellationToken)
    {
        var key = $"rule:{rule.Id}:source:{sourceId?.ToString() ?? sourceType}";
        var now = DateTime.UtcNow;

        if (!breached)
        {
            States.TryRemove(key, out _);

            var openIncident = await incidentManager.FindOpenByDeduplicationKeyAsync(key, cancellationToken);
            if (openIncident is not null)
            {
                await incidentManager.ResolveAsync(openIncident, null, "OpsPulse",
                    "Auto-resolved: condition cleared.", cancellationToken);
            }

            return;
        }

        var state = States.TryGetValue(key, out var existing) ? existing : null;
        var firstBreachedAt = state?.FirstBreachedAtUtc ?? now;
        States[key] = new BreachState(firstBreachedAt, state?.LastResolvedAtUtc);

        // Sustained-duration window: trigger only after the condition holds long enough.
        if (rule.DurationMinutes > 0 && now - firstBreachedAt < TimeSpan.FromMinutes(rule.DurationMinutes))
        {
            return;
        }

        // Cooldown: don't re-open an incident that was resolved recently.
        if (state?.LastResolvedAtUtc is { } resolvedAt &&
            now - resolvedAt < TimeSpan.FromMinutes(rule.CooldownMinutes))
        {
            return;
        }

        var incident = await incidentManager.FindOpenByDeduplicationKeyAsync(key, cancellationToken);
        if (incident is not null)
        {
            await incidentManager.RecordRecurringAsync(incident, cancellationToken);
            return;
        }

        var created = new Incident
        {
            OrganizationId = rule.OrganizationId,
            Title = $"{rule.Name} — {sourceName}",
            Description = $"Alert rule \"{rule.Name}\" breached: {detail}.",
            // AlertSeverity and IncidentSeverity share the same ordinal values.
            Severity = (IncidentSeverity)(int)rule.Severity,
            Status = IncidentStatus.Open,
            SourceType = sourceType,
            SourceName = sourceName,
            DeduplicationKey = key,
            StartedAtUtc = now,
            LastEventAtUtc = now
        };

        await incidentManager.CreateAsync(created, cancellationToken);
        await notifier.AlertTriggeredAsync(rule.OrganizationId, IncidentManager.ToPayload(created), cancellationToken);
        await DispatchNotificationsAsync(rule, created, detail, cancellationToken);
    }

    private async Task DispatchNotificationsAsync(
        AlertRule rule,
        Incident incident,
        string detail,
        CancellationToken cancellationToken)
    {
        try
        {
            var userIds = await db.Users
                .AsNoTracking()
                .Where(u => u.OrganizationId == rule.OrganizationId && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            foreach (var userId in userIds)
            {
                db.Notifications.Add(new Notification
                {
                    OrganizationId = rule.OrganizationId,
                    UserId = userId,
                    Title = $"[{rule.Severity}] {incident.Title}",
                    Message = incident.Description ?? incident.Title,
                    Type = NotificationType.Alert,
                    Channel = NotificationChannel.InApp,
                    DeliveryStatus = DeliveryStatus.Sent,
                    DeliveredAtUtc = DateTime.UtcNow
                });
            }

            var channels = await db.AlertChannels
                .AsNoTracking()
                .Where(c => c.OrganizationId == rule.OrganizationId && c.IsEnabled)
                .ToListAsync(cancellationToken);

            foreach (var channel in channels.Where(c => c.Type != ChannelType.InApp))
            {
                var notification = new Notification
                {
                    OrganizationId = rule.OrganizationId,
                    Title = $"[{rule.Severity}] {incident.Title}",
                    Message = incident.Description ?? incident.Title,
                    Type = NotificationType.Alert,
                    Channel = channel.Type == ChannelType.Email ? NotificationChannel.Email : NotificationChannel.InApp,
                    Target = channel.Target,
                    DeliveryStatus = DeliveryStatus.Pending
                };
                db.Notifications.Add(notification);

                await db.SaveChangesAsync(cancellationToken);
                await DeliverAsync(channel, notification, rule, incident, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to dispatch notifications for incident {IncidentId}", incident.Id);
        }
    }

    private async Task DeliverAsync(
        AlertChannel channel,
        Notification notification,
        AlertRule rule,
        Incident incident,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (channel.Type)
            {
                case ChannelType.Email:
                    if (emailSender.IsConfigured && !string.IsNullOrEmpty(channel.Target))
                    {
                        await emailSender.SendAsync(
                            channel.Target,
                            $"[OpsPulse][{rule.Severity}] {incident.Title}",
                            $"{incident.Description}\n\nSource: {incident.SourceName}\nStarted: {incident.StartedAtUtc:u}",
                            cancellationToken);
                        notification.DeliveryStatus = DeliveryStatus.Sent;
                    }
                    else
                    {
                        notification.DeliveryStatus = DeliveryStatus.Skipped;
                    }
                    break;

                case ChannelType.Webhook:
                    if (!string.IsNullOrEmpty(channel.Target))
                    {
                        var client = httpClientFactory.CreateClient("webhooks");
                        await client.PostAsJsonAsync(channel.Target, new
                        {
                            incident.Id,
                            incident.Title,
                            incident.Description,
                            Severity = incident.Severity.ToString(),
                            Source = incident.SourceName,
                            StartedAtUtc = incident.StartedAtUtc
                        }, cancellationToken);
                        notification.DeliveryStatus = DeliveryStatus.Sent;
                    }
                    else
                    {
                        notification.DeliveryStatus = DeliveryStatus.Skipped;
                    }
                    break;
            }
        }
        catch (Exception exception)
        {
            notification.DeliveryStatus = DeliveryStatus.Failed;
            logger.LogWarning(exception, "Notification delivery via {Channel} {Target} failed", channel.Type, channel.Target);
        }
        finally
        {
            if (notification.DeliveryStatus != DeliveryStatus.Pending)
            {
                notification.DeliveredAtUtc = DateTime.UtcNow;
            }
        }
    }
}
