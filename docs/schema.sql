CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108175449_InitialCreate') THEN
    CREATE TABLE organizations (
        id uuid NOT NULL,
        name character varying(200) NOT NULL,
        slug character varying(100) NOT NULL,
        description character varying(1000),
        is_active boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_organizations PRIMARY KEY (id),
        CONSTRAINT ck_organizations_slug_format CHECK (slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$')
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108175449_InitialCreate') THEN
    CREATE TABLE monitored_services (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        url character varying(2048) NOT NULL,
        description character varying(1000),
        environment character varying(20) NOT NULL,
        check_interval_seconds integer NOT NULL,
        timeout_seconds integer NOT NULL,
        expected_status_codes character varying(100) NOT NULL,
        is_active boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_monitored_services PRIMARY KEY (id),
        CONSTRAINT ck_monitored_services_check_interval_positive CHECK (check_interval_seconds > 0),
        CONSTRAINT ck_monitored_services_timeout_positive CHECK (timeout_seconds > 0),
        CONSTRAINT fk_monitored_services_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108175449_InitialCreate') THEN
    CREATE INDEX ix_monitored_services_is_active ON monitored_services (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108175449_InitialCreate') THEN
    CREATE INDEX ix_monitored_services_organization_id_environment ON monitored_services (organization_id, environment);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108175449_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_monitored_services_organization_id_name ON monitored_services (organization_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108175449_InitialCreate') THEN
    CREATE INDEX ix_organizations_is_active ON organizations (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108175449_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_organizations_slug ON organizations (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108175449_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261108175449_InitialCreate', '10.0.14');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE TABLE asp_net_roles (
        id uuid NOT NULL,
        name character varying(256),
        normalized_name character varying(256),
        concurrency_stamp text,
        CONSTRAINT pk_asp_net_roles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE TABLE asp_net_users (
        id uuid NOT NULL,
        full_name character varying(200) NOT NULL,
        organization_id uuid,
        is_active boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        user_name character varying(256),
        normalized_user_name character varying(256),
        email character varying(256),
        normalized_email character varying(256),
        email_confirmed boolean NOT NULL,
        password_hash text,
        security_stamp text,
        concurrency_stamp text,
        phone_number text,
        phone_number_confirmed boolean NOT NULL,
        two_factor_enabled boolean NOT NULL,
        lockout_end timestamp with time zone,
        lockout_enabled boolean NOT NULL,
        access_failed_count integer NOT NULL,
        CONSTRAINT pk_asp_net_users PRIMARY KEY (id),
        CONSTRAINT fk_asp_net_users_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE TABLE asp_net_role_claims (
        id integer GENERATED BY DEFAULT AS IDENTITY,
        role_id uuid NOT NULL,
        claim_type text,
        claim_value text,
        CONSTRAINT pk_asp_net_role_claims PRIMARY KEY (id),
        CONSTRAINT fk_asp_net_role_claims_asp_net_roles_role_id FOREIGN KEY (role_id) REFERENCES asp_net_roles (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE TABLE asp_net_user_claims (
        id integer GENERATED BY DEFAULT AS IDENTITY,
        user_id uuid NOT NULL,
        claim_type text,
        claim_value text,
        CONSTRAINT pk_asp_net_user_claims PRIMARY KEY (id),
        CONSTRAINT fk_asp_net_user_claims_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE TABLE asp_net_user_logins (
        login_provider text NOT NULL,
        provider_key text NOT NULL,
        provider_display_name text,
        user_id uuid NOT NULL,
        CONSTRAINT pk_asp_net_user_logins PRIMARY KEY (login_provider, provider_key),
        CONSTRAINT fk_asp_net_user_logins_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE TABLE asp_net_user_roles (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        CONSTRAINT pk_asp_net_user_roles PRIMARY KEY (user_id, role_id),
        CONSTRAINT fk_asp_net_user_roles_asp_net_roles_role_id FOREIGN KEY (role_id) REFERENCES asp_net_roles (id) ON DELETE CASCADE,
        CONSTRAINT fk_asp_net_user_roles_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE TABLE asp_net_user_tokens (
        user_id uuid NOT NULL,
        login_provider text NOT NULL,
        name text NOT NULL,
        value text,
        CONSTRAINT pk_asp_net_user_tokens PRIMARY KEY (user_id, login_provider, name),
        CONSTRAINT fk_asp_net_user_tokens_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token_hash character varying(64) NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        revoked_at_utc timestamp with time zone,
        replaced_by_token_hash text,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_refresh_tokens PRIMARY KEY (id),
        CONSTRAINT fk_refresh_tokens_asp_net_users_user_id FOREIGN KEY (user_id) REFERENCES asp_net_users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE INDEX ix_asp_net_role_claims_role_id ON asp_net_role_claims (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE UNIQUE INDEX "RoleNameIndex" ON asp_net_roles (normalized_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE INDEX ix_asp_net_user_claims_user_id ON asp_net_user_claims (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE INDEX ix_asp_net_user_logins_user_id ON asp_net_user_logins (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE INDEX ix_asp_net_user_roles_role_id ON asp_net_user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE INDEX "EmailIndex" ON asp_net_users (normalized_email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE INDEX ix_asp_net_users_is_active ON asp_net_users (is_active);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE INDEX ix_asp_net_users_organization_id ON asp_net_users (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE UNIQUE INDEX "UserNameIndex" ON asp_net_users (normalized_user_name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE UNIQUE INDEX ix_refresh_tokens_token_hash ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108182854_AddIdentityAndRefreshTokens') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261108182854_AddIdentityAndRefreshTokens', '10.0.14');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    ALTER TABLE monitored_services ADD consecutive_failures integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    ALTER TABLE monitored_services ADD current_status character varying(20) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    ALTER TABLE monitored_services ADD last_checked_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    ALTER TABLE monitored_services ADD last_response_time_ms integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE alert_channels (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        type character varying(20) NOT NULL,
        target character varying(500),
        is_enabled boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_alert_channels PRIMARY KEY (id),
        CONSTRAINT fk_alert_channels_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE alert_rules (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        metric character varying(50) NOT NULL,
        comparator character varying(20) NOT NULL,
        threshold double precision NOT NULL,
        duration_minutes integer NOT NULL,
        cooldown_minutes integer NOT NULL,
        severity character varying(20) NOT NULL,
        target_service_id uuid,
        is_enabled boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_alert_rules PRIMARY KEY (id),
        CONSTRAINT fk_alert_rules_monitored_services_target_service_id FOREIGN KEY (target_service_id) REFERENCES monitored_services (id) ON DELETE SET NULL,
        CONSTRAINT fk_alert_rules_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE api_keys (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        key_hash character varying(64) NOT NULL,
        key_prefix character varying(16) NOT NULL,
        is_active boolean NOT NULL,
        last_used_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_api_keys PRIMARY KEY (id),
        CONSTRAINT fk_api_keys_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE audit_logs (
        id uuid NOT NULL,
        organization_id uuid,
        user_id uuid,
        user_email character varying(320),
        action character varying(100) NOT NULL,
        entity_type character varying(100),
        entity_id character varying(100),
        details character varying(2000),
        ip_address character varying(45),
        occurred_at_utc timestamp with time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_audit_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE database_connections (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        database_type character varying(20) NOT NULL,
        host character varying(255) NOT NULL,
        port integer NOT NULL,
        database_name character varying(200) NOT NULL,
        username character varying(200) NOT NULL,
        credentials_cipher character varying(2000) NOT NULL,
        health character varying(20) NOT NULL,
        last_checked_at_utc timestamp with time zone,
        is_active boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_database_connections PRIMARY KEY (id),
        CONSTRAINT fk_database_connections_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE docker_hosts (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        endpoint character varying(500) NOT NULL,
        is_active boolean NOT NULL,
        is_reachable boolean NOT NULL,
        container_count integer NOT NULL,
        running_count integer NOT NULL,
        last_synced_at_utc timestamp with time zone,
        last_error character varying(1000),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_docker_hosts PRIMARY KEY (id),
        CONSTRAINT fk_docker_hosts_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE incidents (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        title character varying(300) NOT NULL,
        description character varying(2000),
        severity character varying(20) NOT NULL,
        status character varying(20) NOT NULL,
        source_type character varying(50) NOT NULL,
        source_name character varying(255),
        deduplication_key character varying(200),
        started_at_utc timestamp with time zone NOT NULL,
        acknowledged_at_utc timestamp with time zone,
        acknowledged_by_user_id uuid,
        resolved_at_utc timestamp with time zone,
        resolved_by_user_id uuid,
        last_event_at_utc timestamp with time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_incidents PRIMARY KEY (id),
        CONSTRAINT fk_incidents_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE kubernetes_clusters (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        api_endpoint character varying(500),
        kube_config_cipher character varying(100000),
        is_active boolean NOT NULL,
        is_reachable boolean NOT NULL,
        kubernetes_version character varying(50),
        node_count integer NOT NULL,
        pod_count integer NOT NULL,
        failed_pod_count integer NOT NULL,
        last_synced_at_utc timestamp with time zone,
        last_error character varying(1000),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_kubernetes_clusters PRIMARY KEY (id),
        CONSTRAINT fk_kubernetes_clusters_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE notifications (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        user_id uuid,
        title character varying(300) NOT NULL,
        message character varying(2000) NOT NULL,
        type character varying(20) NOT NULL,
        channel character varying(20) NOT NULL,
        target character varying(500),
        is_read boolean NOT NULL,
        delivery_status character varying(20) NOT NULL,
        delivered_at_utc timestamp with time zone,
        read_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_notifications PRIMARY KEY (id),
        CONSTRAINT fk_notifications_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE servers (
        id uuid NOT NULL,
        organization_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        hostname character varying(255) NOT NULL,
        ip_address character varying(45),
        os_name character varying(100),
        os_version character varying(100),
        agent_version character varying(50),
        cpu_cores integer NOT NULL,
        total_memory_mb bigint NOT NULL,
        status character varying(20) NOT NULL,
        last_heartbeat_at_utc timestamp with time zone,
        last_cpu_usage_percent double precision,
        last_memory_usage_percent double precision,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_servers PRIMARY KEY (id),
        CONSTRAINT fk_servers_organizations_organization_id FOREIGN KEY (organization_id) REFERENCES organizations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE service_checks (
        id uuid NOT NULL,
        service_id uuid NOT NULL,
        checked_at_utc timestamp with time zone NOT NULL,
        is_success boolean NOT NULL,
        status_code integer,
        response_time_ms integer NOT NULL,
        error_message character varying(500),
        status_after character varying(20) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_service_checks PRIMARY KEY (id),
        CONSTRAINT fk_service_checks_monitored_services_service_id FOREIGN KEY (service_id) REFERENCES monitored_services (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE database_metrics (
        id uuid NOT NULL,
        database_connection_id uuid NOT NULL,
        collected_at_utc timestamp with time zone NOT NULL,
        is_reachable boolean NOT NULL,
        active_connections integer,
        max_connections integer,
        database_size_mb bigint,
        response_time_ms integer,
        error_message character varying(500),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_database_metrics PRIMARY KEY (id),
        CONSTRAINT fk_database_metrics_database_connections_database_connection_id FOREIGN KEY (database_connection_id) REFERENCES database_connections (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE docker_containers (
        id uuid NOT NULL,
        docker_host_id uuid NOT NULL,
        container_id character varying(64) NOT NULL,
        name character varying(255) NOT NULL,
        image character varying(500) NOT NULL,
        state character varying(50) NOT NULL,
        status character varying(200) NOT NULL,
        cpu_usage_percent double precision,
        memory_used_mb bigint,
        memory_limit_mb bigint,
        started_at_utc timestamp with time zone,
        synced_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_docker_containers PRIMARY KEY (id),
        CONSTRAINT fk_docker_containers_docker_hosts_docker_host_id FOREIGN KEY (docker_host_id) REFERENCES docker_hosts (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE incident_events (
        id uuid NOT NULL,
        incident_id uuid NOT NULL,
        type character varying(20) NOT NULL,
        message character varying(2000) NOT NULL,
        actor_user_id uuid,
        actor_name character varying(200),
        occurred_at_utc timestamp with time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_incident_events PRIMARY KEY (id),
        CONSTRAINT fk_incident_events_incidents_incident_id FOREIGN KEY (incident_id) REFERENCES incidents (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE kubernetes_nodes (
        id uuid NOT NULL,
        cluster_id uuid NOT NULL,
        name character varying(255) NOT NULL,
        status character varying(50) NOT NULL,
        roles text[] NOT NULL,
        kubernetes_version character varying(50),
        cpu_capacity_cores bigint,
        memory_capacity_mb bigint,
        synced_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_kubernetes_nodes PRIMARY KEY (id),
        CONSTRAINT fk_kubernetes_nodes_kubernetes_clusters_cluster_id FOREIGN KEY (cluster_id) REFERENCES kubernetes_clusters (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE kubernetes_pods (
        id uuid NOT NULL,
        cluster_id uuid NOT NULL,
        namespace character varying(255) NOT NULL,
        name character varying(255) NOT NULL,
        status character varying(50) NOT NULL,
        restart_count integer NOT NULL,
        node_name character varying(255),
        is_failed boolean NOT NULL,
        started_at_utc timestamp with time zone,
        synced_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_kubernetes_pods PRIMARY KEY (id),
        CONSTRAINT fk_kubernetes_pods_kubernetes_clusters_cluster_id FOREIGN KEY (cluster_id) REFERENCES kubernetes_clusters (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE TABLE server_metrics (
        id uuid NOT NULL,
        server_id uuid NOT NULL,
        collected_at_utc timestamp with time zone NOT NULL,
        cpu_usage_percent double precision NOT NULL,
        memory_used_mb bigint NOT NULL,
        memory_total_mb bigint NOT NULL,
        disk_used_percent double precision,
        network_in_mbps double precision,
        network_out_mbps double precision,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_server_metrics PRIMARY KEY (id),
        CONSTRAINT fk_server_metrics_servers_server_id FOREIGN KEY (server_id) REFERENCES servers (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_monitored_services_last_checked_at_utc ON monitored_services (last_checked_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_alert_channels_organization_id ON alert_channels (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_alert_rules_organization_id ON alert_rules (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_alert_rules_target_service_id ON alert_rules (target_service_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE UNIQUE INDEX ix_api_keys_key_hash ON api_keys (key_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_api_keys_organization_id ON api_keys (organization_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_audit_logs_action ON audit_logs (action);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_audit_logs_organization_id_occurred_at_utc ON audit_logs (organization_id, occurred_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE UNIQUE INDEX ix_database_connections_organization_id_name ON database_connections (organization_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_database_metrics_database_connection_id_collected_at_utc ON database_metrics (database_connection_id, collected_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE UNIQUE INDEX ix_docker_containers_docker_host_id_container_id ON docker_containers (docker_host_id, container_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE UNIQUE INDEX ix_docker_hosts_organization_id_name ON docker_hosts (organization_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_incident_events_incident_id_occurred_at_utc ON incident_events (incident_id, occurred_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_incidents_deduplication_key ON incidents (deduplication_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_incidents_organization_id_status ON incidents (organization_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_incidents_started_at_utc ON incidents (started_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE UNIQUE INDEX ix_kubernetes_clusters_organization_id_name ON kubernetes_clusters (organization_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE UNIQUE INDEX ix_kubernetes_nodes_cluster_id_name ON kubernetes_nodes (cluster_id, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_kubernetes_pods_cluster_id_is_failed ON kubernetes_pods (cluster_id, is_failed);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE UNIQUE INDEX ix_kubernetes_pods_cluster_id_namespace_name ON kubernetes_pods (cluster_id, namespace, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_notifications_organization_id_user_id_is_read_created_at_utc ON notifications (organization_id, user_id, is_read, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_server_metrics_server_id_collected_at_utc ON server_metrics (server_id, collected_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_servers_last_heartbeat_at_utc ON servers (last_heartbeat_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE UNIQUE INDEX ix_servers_organization_id_hostname ON servers (organization_id, hostname);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_servers_status ON servers (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    CREATE INDEX ix_service_checks_service_id_checked_at_utc ON service_checks (service_id, checked_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108193225_AddMonitoringModules') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261108193225_AddMonitoringModules', '10.0.14');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108203014_AddLogsAndKubernetesOperations') THEN
    CREATE TABLE kubernetes_deployments (
        id uuid NOT NULL,
        cluster_id uuid NOT NULL,
        namespace character varying(255) NOT NULL,
        name character varying(255) NOT NULL,
        replicas_desired integer NOT NULL,
        replicas_ready integer NOT NULL,
        image character varying(500),
        synced_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_kubernetes_deployments PRIMARY KEY (id),
        CONSTRAINT fk_kubernetes_deployments_kubernetes_clusters_cluster_id FOREIGN KEY (cluster_id) REFERENCES kubernetes_clusters (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108203014_AddLogsAndKubernetesOperations') THEN
    CREATE TABLE system_logs (
        id uuid NOT NULL,
        timestamp_utc timestamp with time zone NOT NULL,
        level character varying(20) NOT NULL,
        message character varying(4000) NOT NULL,
        exception character varying(8000),
        source character varying(300),
        process character varying(200) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT pk_system_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108203014_AddLogsAndKubernetesOperations') THEN
    CREATE UNIQUE INDEX ix_kubernetes_deployments_cluster_id_namespace_name ON kubernetes_deployments (cluster_id, namespace, name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108203014_AddLogsAndKubernetesOperations') THEN
    CREATE INDEX ix_system_logs_level ON system_logs (level);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108203014_AddLogsAndKubernetesOperations') THEN
    CREATE INDEX ix_system_logs_timestamp_utc_level ON system_logs (timestamp_utc, level);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261108203014_AddLogsAndKubernetesOperations') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261108203014_AddLogsAndKubernetesOperations', '10.0.14');
    END IF;
END $EF$;
COMMIT;

