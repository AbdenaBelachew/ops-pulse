using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoringModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "consecutive_failures",
                table: "monitored_services",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "current_status",
                table: "monitored_services",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_checked_at_utc",
                table: "monitored_services",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "last_response_time_ms",
                table: "monitored_services",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "alert_channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_channels", x => x.id);
                    table.ForeignKey(
                        name: "fk_alert_channels_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alert_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    metric = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    comparator = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    threshold = table.Column<double>(type: "double precision", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    cooldown_minutes = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alert_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_alert_rules_monitored_services_target_service_id",
                        column: x => x.target_service_id,
                        principalTable: "monitored_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_alert_rules_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    key_prefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_used_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_api_keys_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "database_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    database_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    port = table.Column<int>(type: "integer", nullable: false),
                    database_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    username = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    credentials_cipher = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    health = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_checked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_database_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_database_connections_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "docker_hosts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_reachable = table.Column<bool>(type: "boolean", nullable: false),
                    container_count = table.Column<int>(type: "integer", nullable: false),
                    running_count = table.Column<int>(type: "integer", nullable: false),
                    last_synced_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_docker_hosts", x => x.id);
                    table.ForeignKey(
                        name: "fk_docker_hosts_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    deduplication_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_event_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incidents", x => x.id);
                    table.ForeignKey(
                        name: "fk_incidents_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kubernetes_clusters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    api_endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    kube_config_cipher = table.Column<string>(type: "character varying(100000)", maxLength: 100000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_reachable = table.Column<bool>(type: "boolean", nullable: false),
                    kubernetes_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    node_count = table.Column<int>(type: "integer", nullable: false),
                    pod_count = table.Column<int>(type: "integer", nullable: false),
                    failed_pod_count = table.Column<int>(type: "integer", nullable: false),
                    last_synced_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kubernetes_clusters", x => x.id);
                    table.ForeignKey(
                        name: "fk_kubernetes_clusters_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    delivery_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "servers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    hostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    os_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    os_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    agent_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cpu_cores = table.Column<int>(type: "integer", nullable: false),
                    total_memory_mb = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_heartbeat_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_cpu_usage_percent = table.Column<double>(type: "double precision", nullable: true),
                    last_memory_usage_percent = table.Column<double>(type: "double precision", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servers", x => x.id);
                    table.ForeignKey(
                        name: "fk_servers_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_checks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: true),
                    response_time_ms = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status_after = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_checks", x => x.id);
                    table.ForeignKey(
                        name: "fk_service_checks_monitored_services_service_id",
                        column: x => x.service_id,
                        principalTable: "monitored_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "database_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    database_connection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_reachable = table.Column<bool>(type: "boolean", nullable: false),
                    active_connections = table.Column<int>(type: "integer", nullable: true),
                    max_connections = table.Column<int>(type: "integer", nullable: true),
                    database_size_mb = table.Column<long>(type: "bigint", nullable: true),
                    response_time_ms = table.Column<int>(type: "integer", nullable: true),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_database_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_database_metrics_database_connections_database_connection_id",
                        column: x => x.database_connection_id,
                        principalTable: "database_connections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "docker_containers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    docker_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    image = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    state = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cpu_usage_percent = table.Column<double>(type: "double precision", nullable: true),
                    memory_used_mb = table.Column<long>(type: "bigint", nullable: true),
                    memory_limit_mb = table.Column<long>(type: "bigint", nullable: true),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    synced_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_docker_containers", x => x.id);
                    table.ForeignKey(
                        name: "fk_docker_containers_docker_hosts_docker_host_id",
                        column: x => x.docker_host_id,
                        principalTable: "docker_hosts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "incident_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incident_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_incident_events_incidents_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kubernetes_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    roles = table.Column<string[]>(type: "text[]", nullable: false),
                    kubernetes_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cpu_capacity_cores = table.Column<long>(type: "bigint", nullable: true),
                    memory_capacity_mb = table.Column<long>(type: "bigint", nullable: true),
                    synced_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kubernetes_nodes", x => x.id);
                    table.ForeignKey(
                        name: "fk_kubernetes_nodes_kubernetes_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalTable: "kubernetes_clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kubernetes_pods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    restart_count = table.Column<int>(type: "integer", nullable: false),
                    node_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    is_failed = table.Column<bool>(type: "boolean", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    synced_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kubernetes_pods", x => x.id);
                    table.ForeignKey(
                        name: "fk_kubernetes_pods_kubernetes_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalTable: "kubernetes_clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "server_metrics",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    server_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cpu_usage_percent = table.Column<double>(type: "double precision", nullable: false),
                    memory_used_mb = table.Column<long>(type: "bigint", nullable: false),
                    memory_total_mb = table.Column<long>(type: "bigint", nullable: false),
                    disk_used_percent = table.Column<double>(type: "double precision", nullable: true),
                    network_in_mbps = table.Column<double>(type: "double precision", nullable: true),
                    network_out_mbps = table.Column<double>(type: "double precision", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_server_metrics", x => x.id);
                    table.ForeignKey(
                        name: "fk_server_metrics_servers_server_id",
                        column: x => x.server_id,
                        principalTable: "servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_monitored_services_last_checked_at_utc",
                table: "monitored_services",
                column: "last_checked_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_alert_channels_organization_id",
                table: "alert_channels",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_rules_organization_id",
                table: "alert_rules",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_alert_rules_target_service_id",
                table: "alert_rules",
                column: "target_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_key_hash",
                table: "api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_api_keys_organization_id",
                table: "api_keys",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_action",
                table: "audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_organization_id_occurred_at_utc",
                table: "audit_logs",
                columns: new[] { "organization_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_database_connections_organization_id_name",
                table: "database_connections",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_database_metrics_database_connection_id_collected_at_utc",
                table: "database_metrics",
                columns: new[] { "database_connection_id", "collected_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_docker_containers_docker_host_id_container_id",
                table: "docker_containers",
                columns: new[] { "docker_host_id", "container_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_docker_hosts_organization_id_name",
                table: "docker_hosts",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_incident_events_incident_id_occurred_at_utc",
                table: "incident_events",
                columns: new[] { "incident_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_deduplication_key",
                table: "incidents",
                column: "deduplication_key");

            migrationBuilder.CreateIndex(
                name: "ix_incidents_organization_id_status",
                table: "incidents",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_started_at_utc",
                table: "incidents",
                column: "started_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_kubernetes_clusters_organization_id_name",
                table: "kubernetes_clusters",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kubernetes_nodes_cluster_id_name",
                table: "kubernetes_nodes",
                columns: new[] { "cluster_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kubernetes_pods_cluster_id_is_failed",
                table: "kubernetes_pods",
                columns: new[] { "cluster_id", "is_failed" });

            migrationBuilder.CreateIndex(
                name: "ix_kubernetes_pods_cluster_id_namespace_name",
                table: "kubernetes_pods",
                columns: new[] { "cluster_id", "namespace", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notifications_organization_id_user_id_is_read_created_at_utc",
                table: "notifications",
                columns: new[] { "organization_id", "user_id", "is_read", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_server_metrics_server_id_collected_at_utc",
                table: "server_metrics",
                columns: new[] { "server_id", "collected_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_servers_last_heartbeat_at_utc",
                table: "servers",
                column: "last_heartbeat_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_servers_organization_id_hostname",
                table: "servers",
                columns: new[] { "organization_id", "hostname" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_servers_status",
                table: "servers",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_service_checks_service_id_checked_at_utc",
                table: "service_checks",
                columns: new[] { "service_id", "checked_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_channels");

            migrationBuilder.DropTable(
                name: "alert_rules");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "database_metrics");

            migrationBuilder.DropTable(
                name: "docker_containers");

            migrationBuilder.DropTable(
                name: "incident_events");

            migrationBuilder.DropTable(
                name: "kubernetes_nodes");

            migrationBuilder.DropTable(
                name: "kubernetes_pods");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "server_metrics");

            migrationBuilder.DropTable(
                name: "service_checks");

            migrationBuilder.DropTable(
                name: "database_connections");

            migrationBuilder.DropTable(
                name: "docker_hosts");

            migrationBuilder.DropTable(
                name: "incidents");

            migrationBuilder.DropTable(
                name: "kubernetes_clusters");

            migrationBuilder.DropTable(
                name: "servers");

            migrationBuilder.DropIndex(
                name: "ix_monitored_services_last_checked_at_utc",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "consecutive_failures",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "current_status",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "last_checked_at_utc",
                table: "monitored_services");

            migrationBuilder.DropColumn(
                name: "last_response_time_ms",
                table: "monitored_services");
        }
    }
}
