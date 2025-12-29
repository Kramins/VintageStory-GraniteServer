#!/bin/bash

##############################################################################
# EF Core Migration Script for GraniteServer
# 
# A comprehensive script for managing Entity Framework Core migrations
# for both PostgreSQL and SQLite contexts.
#
# Usage: ./ef-migration.sh [COMMAND] [OPTIONS]
#
# Commands:
#   create <name>          Create a new migration
#   remove                 Remove the last migration
#   rollback <migration>   Rollback to a specific migration
#   list                   List all migrations
#   status                 Show migration status for both contexts
#   reset-postgres         Reset PostgreSQL context to initial state
#   reset-sqlite           Reset SQLite context to initial state
#   help                   Show this help message
#
# Options:
#   --context <context>    Specify context: postgres, sqlite, or all (default: all)
#
# Examples:
#   ./ef-migration.sh create AddUserTable
#   ./ef-migration.sh create AddUserTable --context postgres
#   ./ef-migration.sh rollback Initial --context sqlite
#   ./ef-migration.sh list
#   ./ef-migration.sh status
##############################################################################

set -e  # Exit on error

# Configuration
WORKSPACE_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DATA_PROJECT="${WORKSPACE_ROOT}/GraniteServer.Data"
MIGRATIONS_POSTGRES="${DATA_PROJECT}/Migrations/Postgres"
MIGRATIONS_SQLITE="${DATA_PROJECT}/Migrations/Sqlite"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

log_success() {
    echo -e "${GREEN}✓${NC} $1"
}

log_error() {
    echo -e "${RED}✗${NC} $1" >&2
}

log_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

# Validate project exists
validate_project() {
    if [ ! -d "$DATA_PROJECT" ]; then
        log_error "GraniteServer.Data project not found at $DATA_PROJECT"
        exit 1
    fi
    log_info "Using project: $DATA_PROJECT"
}

# Create migration for specified context(s)
create_migration() {
    local migration_name="$1"
    local context="$2"
    
    if [ -z "$migration_name" ]; then
        log_error "Migration name is required"
        echo "Usage: ./ef-migration.sh create <migration-name> [--context postgres|sqlite|all]"
        exit 1
    fi
    
    log_info "Creating migration: $migration_name"
    
    case "$context" in
        postgres)
            log_info "Creating migration for PostgreSQL context..."
            cd "$DATA_PROJECT"
            dotnet ef migrations add "$migration_name" \
                --context GraniteDataContextPostgres \
                --output-dir Migrations/Postgres \
                --project . \
                || { log_error "Failed to create PostgreSQL migration"; exit 1; }
            log_success "PostgreSQL migration created"
            ;;
        sqlite)
            log_info "Creating migration for SQLite context..."
            cd "$DATA_PROJECT"
            dotnet ef migrations add "$migration_name" \
                --context GraniteDataContextSqlite \
                --output-dir Migrations/Sqlite \
                --project . \
                || { log_error "Failed to create SQLite migration"; exit 1; }
            log_success "SQLite migration created"
            ;;
        all)
            log_info "Creating migration for PostgreSQL context..."
            cd "$DATA_PROJECT"
            dotnet ef migrations add "$migration_name" \
                --context GraniteDataContextPostgres \
                --output-dir Migrations/Postgres \
                --project . \
                || { log_error "Failed to create PostgreSQL migration"; exit 1; }
            log_success "PostgreSQL migration created"
            
            log_info "Creating migration for SQLite context..."
            dotnet ef migrations add "$migration_name" \
                --context GraniteDataContextSqlite \
                --output-dir Migrations/Sqlite \
                --project . \
                || { log_error "Failed to create SQLite migration"; exit 1; }
            log_success "SQLite migration created"
            ;;
        *)
            log_error "Invalid context: $context"
            exit 1
            ;;
    esac
    
    log_success "Migration creation completed for $context context(s)"
}

# Remove the last migration for specified context(s)
remove_migration() {
    local context="$1"
    
    log_info "Removing last migration"
    
    case "$context" in
        postgres)
            log_info "Removing last migration from PostgreSQL context..."
            cd "$DATA_PROJECT"
            dotnet ef migrations remove \
                --context GraniteDataContextPostgres \
                --force \
                --project . \
                || { log_error "Failed to remove PostgreSQL migration"; exit 1; }
            log_success "PostgreSQL migration removed"
            ;;
        sqlite)
            log_info "Removing last migration from SQLite context..."
            cd "$DATA_PROJECT"
            dotnet ef migrations remove \
                --context GraniteDataContextSqlite \
                --force \
                --project . \
                || { log_error "Failed to remove SQLite migration"; exit 1; }
            log_success "SQLite migration removed"
            ;;
        all)
            log_info "Removing last migration from PostgreSQL context..."
            cd "$DATA_PROJECT"
            dotnet ef migrations remove \
                --context GraniteDataContextPostgres \
                --force \
                --project . \
                || { log_error "Failed to remove PostgreSQL migration"; exit 1; }
            log_success "PostgreSQL migration removed"
            
            log_info "Removing last migration from SQLite context..."
            dotnet ef migrations remove \
                --context GraniteDataContextSqlite \
                --force \
                --project . \
                || { log_error "Failed to remove SQLite migration"; exit 1; }
            log_success "SQLite migration removed"
            ;;
        *)
            log_error "Invalid context: $context"
            exit 1
            ;;
    esac
    
    log_success "Migration removal completed for $context context(s)"
}

# Rollback to a specific migration
rollback_migration() {
    local target_migration="$1"
    local context="$2"
    
    if [ -z "$target_migration" ]; then
        log_error "Target migration name is required"
        echo "Usage: ./ef-migration.sh rollback <migration-name> [--context postgres|sqlite|all]"
        exit 1
    fi
    
    log_warning "Rolling back to migration: $target_migration"
    
    case "$context" in
        postgres)
            log_info "Rolling back PostgreSQL context..."
            cd "$DATA_PROJECT"
            dotnet ef database update "$target_migration" \
                --context GraniteDataContextPostgres \
                --project . \
                || { log_error "Failed to rollback PostgreSQL"; exit 1; }
            log_success "PostgreSQL rolled back"
            ;;
        sqlite)
            log_info "Rolling back SQLite context..."
            cd "$DATA_PROJECT"
            dotnet ef database update "$target_migration" \
                --context GraniteDataContextSqlite \
                --project . \
                || { log_error "Failed to rollback SQLite"; exit 1; }
            log_success "SQLite rolled back"
            ;;
        all)
            log_info "Rolling back PostgreSQL context..."
            cd "$DATA_PROJECT"
            dotnet ef database update "$target_migration" \
                --context GraniteDataContextPostgres \
                --project . \
                || { log_error "Failed to rollback PostgreSQL"; exit 1; }
            log_success "PostgreSQL rolled back"
            
            log_info "Rolling back SQLite context..."
            dotnet ef database update "$target_migration" \
                --context GraniteDataContextSqlite \
                --project . \
                || { log_error "Failed to rollback SQLite"; exit 1; }
            log_success "SQLite rolled back"
            ;;
        *)
            log_error "Invalid context: $context"
            exit 1
            ;;
    esac
    
    log_success "Rollback completed for $context context(s)"
}

# List all migrations
list_migrations() {
    local context="$1"
    
    log_info "Listing migrations"
    
    case "$context" in
        postgres)
            echo -e "\n${BLUE}PostgreSQL Migrations:${NC}"
            cd "$DATA_PROJECT"
            dotnet ef migrations list \
                --context GraniteDataContextPostgres \
                --project . \
                || log_warning "Failed to list PostgreSQL migrations"
            ;;
        sqlite)
            echo -e "\n${BLUE}SQLite Migrations:${NC}"
            cd "$DATA_PROJECT"
            dotnet ef migrations list \
                --context GraniteDataContextSqlite \
                --project . \
                || log_warning "Failed to list SQLite migrations"
            ;;
        all)
            echo -e "\n${BLUE}PostgreSQL Migrations:${NC}"
            cd "$DATA_PROJECT"
            dotnet ef migrations list \
                --context GraniteDataContextPostgres \
                --project . \
                || log_warning "Failed to list PostgreSQL migrations"
            
            echo -e "\n${BLUE}SQLite Migrations:${NC}"
            dotnet ef migrations list \
                --context GraniteDataContextSqlite \
                --project . \
                || log_warning "Failed to list SQLite migrations"
            ;;
        *)
            log_error "Invalid context: $context"
            exit 1
            ;;
    esac
}

# Show migration status
show_status() {
    local context="$1"
    
    log_info "Showing migration status"
    
    case "$context" in
        postgres)
            echo -e "\n${BLUE}PostgreSQL Migration Status:${NC}"
            list_migrations postgres
            ;;
        sqlite)
            echo -e "\n${BLUE}SQLite Migration Status:${NC}"
            list_migrations sqlite
            ;;
        all)
            echo -e "\n${BLUE}PostgreSQL Migration Status:${NC}"
            list_migrations postgres
            echo -e "\n${BLUE}SQLite Migration Status:${NC}"
            list_migrations sqlite
            ;;
        *)
            log_error "Invalid context: $context"
            exit 1
            ;;
    esac
}

# Reset PostgreSQL context (remove all migrations)
reset_postgres() {
    log_warning "Resetting PostgreSQL context - this will remove all migrations"
    
    if [ -d "$MIGRATIONS_POSTGRES" ]; then
        log_info "Removing PostgreSQL migrations directory..."
        rm -rf "$MIGRATIONS_POSTGRES"
        log_success "PostgreSQL migrations directory removed"
    else
        log_warning "PostgreSQL migrations directory not found"
    fi
}

# Reset SQLite context (remove all migrations)
reset_sqlite() {
    log_warning "Resetting SQLite context - this will remove all migrations"
    
    if [ -d "$MIGRATIONS_SQLITE" ]; then
        log_info "Removing SQLite migrations directory..."
        rm -rf "$MIGRATIONS_SQLITE"
        log_success "SQLite migrations directory removed"
    else
        log_warning "SQLite migrations directory not found"
    fi
}

# Display help
show_help() {
    head -n 30 "$0" | tail -n +2
}

# Main command handler
main() {
    local command="${1:---help}"
    local target="${2:-all}"
    local context="${3:-all}"
    
    # Parse --context flag if present
    if [ "$2" = "--context" ]; then
        context="$3"
        target=""
    fi
    
    validate_project
    
    case "$command" in
        create)
            create_migration "$target" "$context"
            ;;
        remove)
            remove_migration "$context"
            ;;
        rollback)
            rollback_migration "$target" "$context"
            ;;
        list)
            list_migrations "$context"
            ;;
        status)
            show_status "$context"
            ;;
        reset-postgres)
            reset_postgres
            ;;
        reset-sqlite)
            reset_sqlite
            ;;
        help|-h|--help)
            show_help
            ;;
        *)
            log_error "Unknown command: $command"
            echo ""
            show_help
            exit 1
            ;;
    esac
}

# Run main function
main "$@"