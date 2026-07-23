using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KadirliApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ad_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    icon = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_ad_categories_ad_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "ad_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "announcement_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcement_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    affected_id = table.Column<Guid>(type: "uuid", nullable: true),
                    affected_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "business_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_business_categories_business_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "business_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cemeteries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cemeteries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "event_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guide_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    icon = table.Column<string>(type: "text", nullable: true),
                    color = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guide_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_guide_categories_guide_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "guide_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "intercity_routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    destination = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    company = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intercity_routes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "intracity_routes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    route_number = table.Column<string>(type: "text", nullable: false),
                    route_name = table.Column<string>(type: "text", nullable: false),
                    first_departure = table.Column<TimeSpan>(type: "interval", nullable: true),
                    last_departure = table.Column<TimeSpan>(type: "interval", nullable: true),
                    frequency_minutes = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intracity_routes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mosques",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mosques", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "neighborhoods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    population = table.Column<int>(type: "integer", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_neighborhoods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pharmacies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    working_hours = table.Column<string>(type: "text", nullable: true),
                    pharmacist_name = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pharmacies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "place_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    icon = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_place_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_name = table.Column<string>(type: "text", nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_reason = table.Column<string>(type: "text", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    extension_count = table.Column<int>(type: "integer", nullable: false),
                    max_extensions = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    phone_click_count = table.Column<int>(type: "integer", nullable: false),
                    whatsapp_click_count = table.Column<int>(type: "integer", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ads", x => x.id);
                    table.ForeignKey(
                        name: "fk_ads_ad_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "ad_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "category_properties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    property_type = table.Column<string>(type: "text", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    default_value = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_category_properties_ad_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "ad_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "announcements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    target_type = table.Column<string>(type: "text", nullable: true),
                    target_neighborhoods = table.Column<string>(type: "jsonb", nullable: true),
                    target_user_ids = table.Column<string>(type: "jsonb", nullable: true),
                    scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    recurrence_pattern = table.Column<string>(type: "text", nullable: true),
                    send_push_notification = table.Column<bool>(type: "boolean", nullable: false),
                    source = table.Column<string>(type: "text", nullable: true),
                    source_url = table.Column<string>(type: "text", nullable: true),
                    visible_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    has_pdf = table.Column<bool>(type: "boolean", nullable: false),
                    pdf_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    has_link = table.Column<bool>(type: "boolean", nullable: false),
                    external_link = table.Column<string>(type: "text", nullable: true),
                    view_count = table.Column<int>(type: "integer", nullable: false),
                    click_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "draft"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcements", x => x.id);
                    table.ForeignKey(
                        name: "fk_announcements_announcement_types_type_id",
                        column: x => x.type_id,
                        principalTable: "announcement_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    event_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    venue_name = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    organizer = table.Column<string>(type: "text", nullable: true),
                    ticket_price = table.Column<decimal>(type: "numeric", nullable: true),
                    is_free = table.Column<bool>(type: "boolean", nullable: false),
                    age_restriction = table.Column<int>(type: "integer", nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    ticket_url = table.Column<string>(type: "text", nullable: true),
                    cover_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_recurring = table.Column<bool>(type: "boolean", nullable: false),
                    recurrence_pattern = table.Column<string>(type: "text", nullable: true),
                    is_local = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_events_event_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "event_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "intercity_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    departure_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intercity_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_intercity_schedules_intercity_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "intercity_routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "intracity_stops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stop_name = table.Column<string>(type: "text", nullable: false),
                    stop_order = table.Column<int>(type: "integer", nullable: false),
                    time_from_start = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intracity_stops", x => x.id);
                    table.ForeignKey(
                        name: "fk_intracity_stops_intracity_routes_route_id",
                        column: x => x.route_id,
                        principalTable: "intracity_routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "death_notices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    deceased_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    age = table.Column<int>(type: "integer", nullable: true),
                    photo_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    funeral_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    funeral_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    cemetery_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mosque_id = table.Column<Guid>(type: "uuid", nullable: true),
                    neighborhood_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condolence_address = table.Column<string>(type: "text", nullable: true),
                    added_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_reason = table.Column<string>(type: "text", nullable: true),
                    auto_archive_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_death_notices", x => x.id);
                    table.ForeignKey(
                        name: "fk_death_notices_cemeteries_cemetery_id",
                        column: x => x.cemetery_id,
                        principalTable: "cemeteries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_death_notices_mosques_mosque_id",
                        column: x => x.mosque_id,
                        principalTable: "mosques",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    age = table.Column<int>(type: "integer", nullable: true),
                    role = table.Column<string>(type: "varchar(20)", nullable: false),
                    primary_neighborhood_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location_type = table.Column<string>(type: "text", nullable: true),
                    fcm_token = table.Column<string>(type: "text", nullable: true),
                    profile_photo_url = table.Column<string>(type: "text", nullable: true),
                    username_last_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    neighborhood_last_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_banned = table.Column<bool>(type: "boolean", nullable: false),
                    ban_reason = table.Column<string>(type: "text", nullable: true),
                    banned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    banned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notification_preferences = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_neighborhoods_primary_neighborhood_id",
                        column: x => x.primary_neighborhood_id,
                        principalTable: "neighborhoods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    role = table.Column<string>(type: "varchar(20)", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pharmacy_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    pharmacy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duty_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    source = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pharmacy_schedules", x => x.id);
                    table.ForeignKey(
                        name: "fk_pharmacy_schedules_pharmacies_pharmacy_id",
                        column: x => x.pharmacy_id,
                        principalTable: "pharmacies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ad_extensions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ads_watched = table.Column<int>(type: "integer", nullable: false),
                    days_extended = table.Column<int>(type: "integer", nullable: false),
                    extended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_extensions", x => x.id);
                    table.ForeignKey(
                        name: "fk_ad_extensions_ads_ad_id",
                        column: x => x.ad_id,
                        principalTable: "ads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ad_favorites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_favorites", x => x.id);
                    table.ForeignKey(
                        name: "fk_ad_favorites_ads_ad_id",
                        column: x => x.ad_id,
                        principalTable: "ads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ad_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_cover = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_ad_images_ads_ad_id",
                        column: x => x.ad_id,
                        principalTable: "ads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ad_property_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ad_property_values", x => x.id);
                    table.ForeignKey(
                        name: "fk_ad_property_values_ads_ad_id",
                        column: x => x.ad_id,
                        principalTable: "ads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_ad_property_values_category_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "category_properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "property_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_property_options_category_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "category_properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "power_outages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    neighborhood = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_power_outages", x => x.id);
                    table.ForeignKey(
                        name: "fk_power_outages_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "event_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_images_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "admin_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    can_read = table.Column<bool>(type: "boolean", nullable: false),
                    can_create = table.Column<bool>(type: "boolean", nullable: false),
                    can_update = table.Column<bool>(type: "boolean", nullable: false),
                    can_delete = table.Column<bool>(type: "boolean", nullable: false),
                    can_approve = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_permissions", x => x.id);
                    table.ForeignKey(
                        name: "fk_admin_permissions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "announcement_views",
                columns: table => new
                {
                    announcement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_announcement_views", x => new { x.announcement_id, x.user_id });
                    table.ForeignKey(
                        name: "fk_announcement_views_announcements_announcement_id",
                        column: x => x.announcement_id,
                        principalTable: "announcements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_announcement_views_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "complaints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "text", nullable: true),
                    related_module = table.Column<string>(type: "text", nullable: true),
                    related_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    admin_notes = table.Column<string>(type: "text", nullable: true),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_complaints", x => x.id);
                    table.ForeignKey(
                        name: "fk_complaints_users_resolved_by",
                        column: x => x.resolved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_complaints_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    original_name = table.Column<string>(type: "text", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_path = table.Column<string>(type: "text", nullable: false),
                    cdn_url = table.Column<string>(type: "text", nullable: true),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    module_type = table.Column<string>(type: "text", nullable: true),
                    module_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_files_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: true),
                    related_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_type = table.Column<string>(type: "text", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fcm_sent = table.Column<bool>(type: "boolean", nullable: false),
                    fcm_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fcm_error = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_neighborhoods",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    neighborhood_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_neighborhoods", x => new { x.user_id, x.neighborhood_id });
                    table.ForeignKey(
                        name: "fk_user_neighborhoods_neighborhoods_neighborhood_id",
                        column: x => x.neighborhood_id,
                        principalTable: "neighborhoods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_neighborhoods_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "businesses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    business_name = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tax_number = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    instagram_handle = table.Column<string>(type: "text", nullable: true),
                    logo_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_businesses", x => x.id);
                    table.ForeignKey(
                        name: "fk_businesses_business_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "business_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_businesses_files_logo_file_id",
                        column: x => x.logo_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_businesses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "guide_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    working_hours = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric", nullable: true),
                    logo_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guide_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_guide_items_files_logo_file_id",
                        column: x => x.logo_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_guide_items_guide_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "guide_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "places",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    latitude = table.Column<decimal>(type: "numeric", nullable: false),
                    longitude = table.Column<decimal>(type: "numeric", nullable: false),
                    entrance_fee = table.Column<decimal>(type: "numeric", nullable: true),
                    is_free = table.Column<bool>(type: "boolean", nullable: false),
                    opening_hours = table.Column<string>(type: "text", nullable: true),
                    best_season = table.Column<string>(type: "text", nullable: true),
                    how_to_get_there = table.Column<string>(type: "text", nullable: true),
                    distance_from_center = table.Column<decimal>(type: "numeric", nullable: true),
                    cover_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_places", x => x.id);
                    table.ForeignKey(
                        name: "fk_places_files_cover_image_id",
                        column: x => x.cover_image_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_places_place_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "place_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_places_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "taxi_drivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    plaka = table.Column<string>(type: "text", nullable: true),
                    vehicle_info = table.Column<string>(type: "text", nullable: true),
                    license_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registration_file_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    total_calls = table.Column<int>(type: "integer", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxi_drivers", x => x.id);
                    table.ForeignKey(
                        name: "fk_taxi_drivers_files_license_file_id",
                        column: x => x.license_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_taxi_drivers_files_registration_file_id",
                        column: x => x.registration_file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_taxi_drivers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "campaigns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    discount_code = table.Column<string>(type: "text", nullable: true),
                    terms = table.Column<string>(type: "text", nullable: true),
                    minimum_amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    stock_limit = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cover_image_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code_view_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejected_reason = table.Column<string>(type: "text", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "fk_campaigns_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_campaigns_files_cover_image_id",
                        column: x => x.cover_image_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "place_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_place_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_place_images_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_place_images_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "taxi_calls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    passenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    called_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_taxi_calls", x => x.id);
                    table.ForeignKey(
                        name: "fk_taxi_calls_taxi_drivers_driver_id",
                        column: x => x.driver_id,
                        principalTable: "taxi_drivers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_taxi_calls_users_passenger_id",
                        column: x => x.passenger_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "campaign_code_views",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    viewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_code_views", x => x.id);
                    table.ForeignKey(
                        name: "fk_campaign_code_views_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_campaign_code_views_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_campaign_images", x => x.id);
                    table.ForeignKey(
                        name: "fk_campaign_images_campaigns_campaign_id",
                        column: x => x.campaign_id,
                        principalTable: "campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_campaign_images_files_file_id",
                        column: x => x.file_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ad_categories_parent_id",
                table: "ad_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_ad_categories_slug",
                table: "ad_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ad_extensions_ad_id",
                table: "ad_extensions",
                column: "ad_id");

            migrationBuilder.CreateIndex(
                name: "ix_ad_favorites_ad_id",
                table: "ad_favorites",
                column: "ad_id");

            migrationBuilder.CreateIndex(
                name: "ix_ad_favorites_user",
                table: "ad_favorites",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ad_favorites_user_id_ad_id",
                table: "ad_favorites",
                columns: new[] { "user_id", "ad_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ad_images_ad",
                table: "ad_images",
                column: "ad_id");

            migrationBuilder.CreateIndex(
                name: "ix_ad_images_ad_id_file_id",
                table: "ad_images",
                columns: new[] { "ad_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ad_prop_values_ad",
                table: "ad_property_values",
                column: "ad_id");

            migrationBuilder.CreateIndex(
                name: "ix_ad_property_values_ad_id_property_id",
                table: "ad_property_values",
                columns: new[] { "ad_id", "property_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ad_property_values_property_id",
                table: "ad_property_values",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_admin_permissions_user_id",
                table: "admin_permissions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_ads_category",
                table: "ads",
                column: "category_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ads_expires",
                table: "ads",
                column: "expires_at",
                filter: "status = 'approved'");

            migrationBuilder.CreateIndex(
                name: "ix_ads_price",
                table: "ads",
                column: "price",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ads_status_created",
                table: "ads",
                columns: new[] { "status", "created_at" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ads_user",
                table: "ads",
                column: "user_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_announcement_types_name",
                table: "announcement_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_announcement_types_slug",
                table: "announcement_types",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_announcement_views_user_id",
                table: "announcement_views",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_announcements_scheduled_for",
                table: "announcements",
                column: "scheduled_for",
                filter: "status = 'scheduled'");

            migrationBuilder.CreateIndex(
                name: "ix_announcements_status_created_at",
                table: "announcements",
                columns: new[] { "status", "created_at" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_announcements_type_id",
                table: "announcements",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_module_created_at",
                table: "audit_logs",
                columns: new[] { "module", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id_created_at",
                table: "audit_logs",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_business_categories_parent_id",
                table: "business_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_business_categories_slug",
                table: "business_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_businesses_category_id",
                table: "businesses",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_businesses_logo_file_id",
                table: "businesses",
                column: "logo_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_businesses_user_id",
                table: "businesses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_code_views_campaign_id",
                table: "campaign_code_views",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_code_views_user_id",
                table: "campaign_code_views",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_images_campaign_id",
                table: "campaign_images",
                column: "campaign_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_images_file_id",
                table: "campaign_images",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_business",
                table: "campaigns",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_cover_image_id",
                table: "campaigns",
                column: "cover_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_campaigns_status_dates",
                table: "campaigns",
                columns: new[] { "status", "start_date", "end_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_category_properties_category_id_property_name",
                table: "category_properties",
                columns: new[] { "category_id", "property_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_complaints_resolved_by",
                table: "complaints",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "ix_complaints_user_id",
                table: "complaints",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_death_notices_cemetery_id",
                table: "death_notices",
                column: "cemetery_id");

            migrationBuilder.CreateIndex(
                name: "ix_death_notices_mosque_id",
                table: "death_notices",
                column: "mosque_id");

            migrationBuilder.CreateIndex(
                name: "ix_deaths_archive",
                table: "death_notices",
                column: "auto_archive_at",
                filter: "status = 'approved'");

            migrationBuilder.CreateIndex(
                name: "ix_deaths_neighborhood",
                table: "death_notices",
                column: "neighborhood_id");

            migrationBuilder.CreateIndex(
                name: "ix_deaths_status_funeral",
                table: "death_notices",
                columns: new[] { "status", "funeral_date" },
                descending: new[] { false, true },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_categories_slug",
                table: "event_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_images_event_id_file_id",
                table: "event_images",
                columns: new[] { "event_id", "file_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_category",
                table: "events",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_status_date",
                table: "events",
                columns: new[] { "status", "event_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_files_file_name",
                table: "files",
                column: "file_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_files_uploaded_by",
                table: "files",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_guide_categories_parent_id",
                table: "guide_categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_guide_categories_slug",
                table: "guide_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guide_items_category",
                table: "guide_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_guide_items_logo_file_id",
                table: "guide_items",
                column: "logo_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_intercity_sched_route",
                table: "intercity_schedules",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "ix_intracity_stops_route",
                table: "intracity_stops",
                columns: new[] { "route_id", "stop_order" });

            migrationBuilder.CreateIndex(
                name: "ix_neighborhoods_slug",
                table: "neighborhoods",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notif_user_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_permissions_module_action",
                table: "permissions",
                columns: new[] { "module", "action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pharm_sched_date",
                table: "pharmacy_schedules",
                column: "duty_date");

            migrationBuilder.CreateIndex(
                name: "ix_pharmacy_schedules_pharmacy_id",
                table: "pharmacy_schedules",
                column: "pharmacy_id");

            migrationBuilder.CreateIndex(
                name: "ix_place_categories_slug",
                table: "place_categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_place_images_file_id",
                table: "place_images",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_place_images_place",
                table: "place_images",
                column: "place_id");

            migrationBuilder.CreateIndex(
                name: "ix_places_category",
                table: "places",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_places_cover_image_id",
                table: "places",
                column: "cover_image_id");

            migrationBuilder.CreateIndex(
                name: "ix_places_created_by",
                table: "places",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_power_outages_announcement_id",
                table: "power_outages",
                column: "announcement_id");

            migrationBuilder.CreateIndex(
                name: "ix_property_options_property_id",
                table: "property_options",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_permission_id",
                table: "role_permissions",
                columns: new[] { "role", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_taxi_calls_driver",
                table: "taxi_calls",
                column: "driver_id");

            migrationBuilder.CreateIndex(
                name: "ix_taxi_calls_passenger_id",
                table: "taxi_calls",
                column: "passenger_id");

            migrationBuilder.CreateIndex(
                name: "ix_taxi_drivers_license_file_id",
                table: "taxi_drivers",
                column: "license_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_taxi_drivers_registration_file_id",
                table: "taxi_drivers",
                column: "registration_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_taxi_drivers_user_id",
                table: "taxi_drivers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_neighborhoods_neighborhood_id",
                table: "user_neighborhoods",
                column: "neighborhood_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_phone",
                table: "users",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_primary_neighborhood_id",
                table: "users",
                column: "primary_neighborhood_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ad_extensions");

            migrationBuilder.DropTable(
                name: "ad_favorites");

            migrationBuilder.DropTable(
                name: "ad_images");

            migrationBuilder.DropTable(
                name: "ad_property_values");

            migrationBuilder.DropTable(
                name: "admin_permissions");

            migrationBuilder.DropTable(
                name: "announcement_views");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "campaign_code_views");

            migrationBuilder.DropTable(
                name: "campaign_images");

            migrationBuilder.DropTable(
                name: "complaints");

            migrationBuilder.DropTable(
                name: "death_notices");

            migrationBuilder.DropTable(
                name: "event_images");

            migrationBuilder.DropTable(
                name: "guide_items");

            migrationBuilder.DropTable(
                name: "intercity_schedules");

            migrationBuilder.DropTable(
                name: "intracity_stops");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "pharmacy_schedules");

            migrationBuilder.DropTable(
                name: "place_images");

            migrationBuilder.DropTable(
                name: "power_outages");

            migrationBuilder.DropTable(
                name: "property_options");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "taxi_calls");

            migrationBuilder.DropTable(
                name: "user_neighborhoods");

            migrationBuilder.DropTable(
                name: "ads");

            migrationBuilder.DropTable(
                name: "campaigns");

            migrationBuilder.DropTable(
                name: "cemeteries");

            migrationBuilder.DropTable(
                name: "mosques");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "guide_categories");

            migrationBuilder.DropTable(
                name: "intercity_routes");

            migrationBuilder.DropTable(
                name: "intracity_routes");

            migrationBuilder.DropTable(
                name: "pharmacies");

            migrationBuilder.DropTable(
                name: "places");

            migrationBuilder.DropTable(
                name: "announcements");

            migrationBuilder.DropTable(
                name: "category_properties");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "taxi_drivers");

            migrationBuilder.DropTable(
                name: "businesses");

            migrationBuilder.DropTable(
                name: "event_categories");

            migrationBuilder.DropTable(
                name: "place_categories");

            migrationBuilder.DropTable(
                name: "announcement_types");

            migrationBuilder.DropTable(
                name: "ad_categories");

            migrationBuilder.DropTable(
                name: "business_categories");

            migrationBuilder.DropTable(
                name: "files");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "neighborhoods");
        }
    }
}
