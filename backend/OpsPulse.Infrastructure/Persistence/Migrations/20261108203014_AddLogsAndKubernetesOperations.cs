using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpsPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLogsAndKubernetesOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "kubernetes_deployments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @namespace = table.Column<string>(name: "namespace", type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    replicas_desired = table.Column<int>(type: "integer", nullable: false),
                    replicas_ready = table.Column<int>(type: "integer", nullable: false),
                    image = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    synced_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kubernetes_deployments", x => x.id);
                    table.ForeignKey(
                        name: "fk_kubernetes_deployments_kubernetes_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalTable: "kubernetes_clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    exception = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    source = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    process = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_kubernetes_deployments_cluster_id_namespace_name",
                table: "kubernetes_deployments",
                columns: new[] { "cluster_id", "namespace", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_system_logs_level",
                table: "system_logs",
                column: "level");

            migrationBuilder.CreateIndex(
                name: "ix_system_logs_timestamp_utc_level",
                table: "system_logs",
                columns: new[] { "timestamp_utc", "level" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "kubernetes_deployments");

            migrationBuilder.DropTable(
                name: "system_logs");
        }
    }
}
