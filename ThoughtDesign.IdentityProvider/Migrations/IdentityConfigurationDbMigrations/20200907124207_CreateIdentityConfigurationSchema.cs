using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ThoughtDesign.IdentityProvider.Migrations.IdentityConfigurationDbMigrations
{
    public partial class CreateIdentityConfigurationSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_resource",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    enabled = table.Column<bool>(nullable: false),
                    name = table.Column<string>(maxLength: 200, nullable: false),
                    display_name = table.Column<string>(maxLength: 200, nullable: true),
                    description = table.Column<string>(maxLength: 1000, nullable: true),
                    allowed_access_token_signing_algorithms = table.Column<string>(maxLength: 100, nullable: true),
                    show_in_discovery_document = table.Column<bool>(nullable: false),
                    created = table.Column<DateTime>(nullable: false),
                    updated = table.Column<DateTime>(nullable: true),
                    last_accessed = table.Column<DateTime>(nullable: true),
                    non_editable = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_resource_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_scope",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    enabled = table.Column<bool>(nullable: false),
                    name = table.Column<string>(maxLength: 200, nullable: false),
                    display_name = table.Column<string>(maxLength: 200, nullable: true),
                    description = table.Column<string>(maxLength: 1000, nullable: true),
                    required = table.Column<bool>(nullable: false),
                    emphasize = table.Column<bool>(nullable: false),
                    show_in_discovery_document = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_scope_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    enabled = table.Column<bool>(nullable: false),
                    client_id = table.Column<string>(maxLength: 200, nullable: false),
                    protocol_type = table.Column<string>(maxLength: 200, nullable: false),
                    require_client_secret = table.Column<bool>(nullable: false),
                    client_name = table.Column<string>(maxLength: 200, nullable: true),
                    description = table.Column<string>(maxLength: 1000, nullable: true),
                    client_uri = table.Column<string>(maxLength: 2000, nullable: true),
                    logo_uri = table.Column<string>(maxLength: 2000, nullable: true),
                    require_consent = table.Column<bool>(nullable: false),
                    allow_remember_consent = table.Column<bool>(nullable: false),
                    always_include_user_claims_in_id_token = table.Column<bool>(nullable: false),
                    require_pkce = table.Column<bool>(nullable: false),
                    allow_plain_text_pkce = table.Column<bool>(nullable: false),
                    require_request_object = table.Column<bool>(nullable: false),
                    allow_access_tokens_via_browser = table.Column<bool>(nullable: false),
                    front_channel_logout_uri = table.Column<string>(maxLength: 2000, nullable: true),
                    front_channel_logout_session_required = table.Column<bool>(nullable: false),
                    back_channel_logout_uri = table.Column<string>(maxLength: 2000, nullable: true),
                    back_channel_logout_session_required = table.Column<bool>(nullable: false),
                    allow_offline_access = table.Column<bool>(nullable: false),
                    identity_token_lifetime = table.Column<int>(nullable: false),
                    allowed_identity_token_signing_algorithms = table.Column<string>(maxLength: 100, nullable: true),
                    access_token_lifetime = table.Column<int>(nullable: false),
                    authorization_code_lifetime = table.Column<int>(nullable: false),
                    consent_lifetime = table.Column<int>(nullable: true),
                    absolute_refresh_token_lifetime = table.Column<int>(nullable: false),
                    sliding_refresh_token_lifetime = table.Column<int>(nullable: false),
                    refresh_token_usage = table.Column<int>(nullable: false),
                    update_access_token_claims_on_refresh = table.Column<bool>(nullable: false),
                    refresh_token_expiration = table.Column<int>(nullable: false),
                    access_token_type = table.Column<int>(nullable: false),
                    enable_local_login = table.Column<bool>(nullable: false),
                    include_jwt_id = table.Column<bool>(nullable: false),
                    always_send_client_claims = table.Column<bool>(nullable: false),
                    client_claims_prefix = table.Column<string>(maxLength: 200, nullable: true),
                    pair_wise_subject_salt = table.Column<string>(maxLength: 200, nullable: true),
                    created = table.Column<DateTime>(nullable: false),
                    updated = table.Column<DateTime>(nullable: true),
                    last_accessed = table.Column<DateTime>(nullable: true),
                    user_sso_lifetime = table.Column<int>(nullable: true),
                    user_code_type = table.Column<string>(maxLength: 100, nullable: true),
                    device_code_lifetime = table.Column<int>(nullable: false),
                    non_editable = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_resource",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    enabled = table.Column<bool>(nullable: false),
                    name = table.Column<string>(maxLength: 200, nullable: false),
                    display_name = table.Column<string>(maxLength: 200, nullable: true),
                    description = table.Column<string>(maxLength: 1000, nullable: true),
                    required = table.Column<bool>(nullable: false),
                    emphasize = table.Column<bool>(nullable: false),
                    show_in_discovery_document = table.Column<bool>(nullable: false),
                    created = table.Column<DateTime>(nullable: false),
                    updated = table.Column<DateTime>(nullable: true),
                    non_editable = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("identity_resource_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "api_resource_claim",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(maxLength: 200, nullable: false),
                    api_resource_id = table.Column<int>(nullable: false),
                    api_resource_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_resource_claim_pkey", x => x.id);
                    table.ForeignKey(
                        name: "api_resource_claim FK api_resource. api_resource_id",
                        column: x => x.api_resource_id,
                        principalTable: "api_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_resource_claim_api_resource_api_resource_id1",
                        column: x => x.api_resource_id1,
                        principalTable: "api_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_resource_property",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(maxLength: 250, nullable: false),
                    value = table.Column<string>(maxLength: 2000, nullable: false),
                    api_resource_id = table.Column<int>(nullable: false),
                    api_resource_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_resource_property_pkey", x => x.id);
                    table.ForeignKey(
                        name: "api_resource_property FK api_resource. api_resource_id",
                        column: x => x.api_resource_id,
                        principalTable: "api_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_resource_property_api_resource_api_resource_id1",
                        column: x => x.api_resource_id1,
                        principalTable: "api_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_resource_scope",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    scope = table.Column<string>(maxLength: 200, nullable: false),
                    api_resource_id = table.Column<int>(nullable: false),
                    api_resource_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_resource_scope_pkey", x => x.id);
                    table.ForeignKey(
                        name: "api_resource_scope FK api_resource. api_resource_id",
                        column: x => x.api_resource_id,
                        principalTable: "api_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_resource_scope_api_resource_api_resource_id1",
                        column: x => x.api_resource_id1,
                        principalTable: "api_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_resource_secret",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(maxLength: 1000, nullable: true),
                    value = table.Column<string>(maxLength: 4000, nullable: false),
                    expiration = table.Column<DateTime>(nullable: true),
                    type = table.Column<string>(maxLength: 250, nullable: false),
                    created = table.Column<DateTime>(nullable: false),
                    api_resource_id = table.Column<int>(nullable: false),
                    api_resource_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_resource_secret_pkey", x => x.id);
                    table.ForeignKey(
                        name: "api_resource_secret FK api_resource. api_resource_id",
                        column: x => x.api_resource_id,
                        principalTable: "api_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_resource_secret_api_resource_api_resource_id1",
                        column: x => x.api_resource_id1,
                        principalTable: "api_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_scope_claim",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(maxLength: 200, nullable: false),
                    scope_id = table.Column<int>(nullable: false),
                    scope_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_scope_claim_pkey", x => x.id);
                    table.ForeignKey(
                        name: "api_scope_claim FK api_scope. scope_id",
                        column: x => x.scope_id,
                        principalTable: "api_scope",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_scope_claim_api_scope_scope_id1",
                        column: x => x.scope_id1,
                        principalTable: "api_scope",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "api_scope_property",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(maxLength: 250, nullable: false),
                    value = table.Column<string>(maxLength: 2000, nullable: false),
                    scope_id = table.Column<int>(nullable: false),
                    scope_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("api_scope_property_pkey", x => x.id);
                    table.ForeignKey(
                        name: "api_scope_property FK api_scope. scope_id",
                        column: x => x.scope_id,
                        principalTable: "api_scope",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_api_scope_property_api_scope_scope_id1",
                        column: x => x.scope_id1,
                        principalTable: "api_scope",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_claim",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(maxLength: 250, nullable: false),
                    value = table.Column<string>(maxLength: 250, nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_claim_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_claim FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_claim_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_cors_origin",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    origin = table.Column<string>(maxLength: 150, nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_cors_origin_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_cors_origin FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_cors_origin_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_grant_type",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grant_type = table.Column<string>(maxLength: 250, nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_grant_type_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_grant_type FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_grant_type_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_idp_restriction",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    provider = table.Column<string>(maxLength: 200, nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_idp_restriction_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_idp_restriction FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_idp_restriction_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_post_logout_redirect_uri",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_logout_redirect_uri = table.Column<string>(maxLength: 2000, nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_post_logout_redirect_uri_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_post_logout_redirect_uri FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_post_logout_redirect_uri_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_property",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(maxLength: 250, nullable: false),
                    value = table.Column<string>(maxLength: 2000, nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_property_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_property FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_property_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_redirect_uri",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    redirect_uri = table.Column<string>(maxLength: 2000, nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_redirect_uri_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_redirect_uri FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_redirect_uri_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_scope",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    scope = table.Column<string>(maxLength: 200, nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_scope_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_scope FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_scope_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_secret",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(maxLength: 2000, nullable: true),
                    value = table.Column<string>(maxLength: 4000, nullable: false),
                    expiration = table.Column<DateTime>(nullable: true),
                    type = table.Column<string>(maxLength: 250, nullable: false),
                    created = table.Column<DateTime>(nullable: false),
                    client_id = table.Column<int>(nullable: false),
                    client_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("client_secret_pkey", x => x.id);
                    table.ForeignKey(
                        name: "client_secret FK client. client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_secret_client_client_id1",
                        column: x => x.client_id1,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "identity_resource_claim",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(maxLength: 200, nullable: false),
                    identity_resource_id = table.Column<int>(nullable: false),
                    identity_resource_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("identity_resource_claim_pkey", x => x.id);
                    table.ForeignKey(
                        name: "identityResourceClaim FK identityResource. identityResourceId",
                        column: x => x.identity_resource_id,
                        principalTable: "identity_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_identity_resource_claim_identity_resource_identity_resource",
                        column: x => x.identity_resource_id1,
                        principalTable: "identity_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "identity_resource_property",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(maxLength: 250, nullable: false),
                    value = table.Column<string>(maxLength: 2000, nullable: false),
                    identity_resource_id = table.Column<int>(nullable: false),
                    identity_resource_id1 = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("identity_resource_property_pkey", x => x.id);
                    table.ForeignKey(
                        name: "identityResourceProprty FK identityResource. identityResourceId",
                        column: x => x.identity_resource_id,
                        principalTable: "identity_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_identity_resource_property_identity_resource_identity_resou",
                        column: x => x.identity_resource_id1,
                        principalTable: "identity_resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "api_resource. name. uq idx",
                table: "api_resource",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "api_resource_claim. api_resource_id. idx",
                table: "api_resource_claim",
                column: "api_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_resource_claim_api_resource_id1",
                table: "api_resource_claim",
                column: "api_resource_id1");

            migrationBuilder.CreateIndex(
                name: "api_resource_property. api_resource_id. idx",
                table: "api_resource_property",
                column: "api_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_resource_property_api_resource_id1",
                table: "api_resource_property",
                column: "api_resource_id1");

            migrationBuilder.CreateIndex(
                name: "api_resource_scope. api_resource_id. idx",
                table: "api_resource_scope",
                column: "api_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_resource_scope_api_resource_id1",
                table: "api_resource_scope",
                column: "api_resource_id1");

            migrationBuilder.CreateIndex(
                name: "api_resource_secret. api_resource_id. idx",
                table: "api_resource_secret",
                column: "api_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_resource_secret_api_resource_id1",
                table: "api_resource_secret",
                column: "api_resource_id1");

            migrationBuilder.CreateIndex(
                name: "api_scope. name. uq idx",
                table: "api_scope",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "api_scope_claim. scope_id. idx",
                table: "api_scope_claim",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_scope_claim_scope_id1",
                table: "api_scope_claim",
                column: "scope_id1");

            migrationBuilder.CreateIndex(
                name: "api_scope_property. scope_id. idx",
                table: "api_scope_property",
                column: "scope_id");

            migrationBuilder.CreateIndex(
                name: "ix_api_scope_property_scope_id1",
                table: "api_scope_property",
                column: "scope_id1");

            migrationBuilder.CreateIndex(
                name: "client. client_id. uq idx",
                table: "client",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "client_claim. client_id. idx",
                table: "client_claim",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_claim_client_id1",
                table: "client_claim",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "client_cors_origin. client_id. idx",
                table: "client_cors_origin",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_cors_origin_client_id1",
                table: "client_cors_origin",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "client_grant_type. client_id. idx",
                table: "client_grant_type",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_grant_type_client_id1",
                table: "client_grant_type",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "client_idp_restriction. client_id. idx",
                table: "client_idp_restriction",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_idp_restriction_client_id1",
                table: "client_idp_restriction",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "client_post_logout_redirect_uri. client_id. idx",
                table: "client_post_logout_redirect_uri",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_post_logout_redirect_uri_client_id1",
                table: "client_post_logout_redirect_uri",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "client_property. client_id. idx",
                table: "client_property",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_property_client_id1",
                table: "client_property",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "client_redirect_uri. client_id. idx",
                table: "client_redirect_uri",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_redirect_uri_client_id1",
                table: "client_redirect_uri",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "client_scope. client_id. idx",
                table: "client_scope",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_scope_client_id1",
                table: "client_scope",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "client_secret. client_id. idx",
                table: "client_secret",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_secret_client_id1",
                table: "client_secret",
                column: "client_id1");

            migrationBuilder.CreateIndex(
                name: "identity_resource. name. uq idx",
                table: "identity_resource",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "identity_resource_claim. identity_resource_id. idx",
                table: "identity_resource_claim",
                column: "identity_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_resource_claim_identity_resource_id1",
                table: "identity_resource_claim",
                column: "identity_resource_id1");

            migrationBuilder.CreateIndex(
                name: "identity_resource_property. identity_resource_id. idx",
                table: "identity_resource_property",
                column: "identity_resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_resource_property_identity_resource_id1",
                table: "identity_resource_property",
                column: "identity_resource_id1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_resource_claim");

            migrationBuilder.DropTable(
                name: "api_resource_property");

            migrationBuilder.DropTable(
                name: "api_resource_scope");

            migrationBuilder.DropTable(
                name: "api_resource_secret");

            migrationBuilder.DropTable(
                name: "api_scope_claim");

            migrationBuilder.DropTable(
                name: "api_scope_property");

            migrationBuilder.DropTable(
                name: "client_claim");

            migrationBuilder.DropTable(
                name: "client_cors_origin");

            migrationBuilder.DropTable(
                name: "client_grant_type");

            migrationBuilder.DropTable(
                name: "client_idp_restriction");

            migrationBuilder.DropTable(
                name: "client_post_logout_redirect_uri");

            migrationBuilder.DropTable(
                name: "client_property");

            migrationBuilder.DropTable(
                name: "client_redirect_uri");

            migrationBuilder.DropTable(
                name: "client_scope");

            migrationBuilder.DropTable(
                name: "client_secret");

            migrationBuilder.DropTable(
                name: "identity_resource_claim");

            migrationBuilder.DropTable(
                name: "identity_resource_property");

            migrationBuilder.DropTable(
                name: "api_resource");

            migrationBuilder.DropTable(
                name: "api_scope");

            migrationBuilder.DropTable(
                name: "client");

            migrationBuilder.DropTable(
                name: "identity_resource");
        }
    }
}
