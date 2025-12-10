# ============================================
# MagiDesk POS - GCP Infrastructure
# Terraform Configuration for Cloud Run + Cloud SQL
# ============================================

terraform {
  required_version = ">= 1.5.0"
  
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
  
  # Backend configuration (uncomment and configure for remote state)
  # backend "gcs" {
  #   bucket = "magidesk-terraform-state"
  #   prefix = "terraform/state"
  # }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

# ============================================
# Cloud SQL - PostgreSQL Instance
# ============================================

resource "google_sql_database_instance" "postgres" {
  name             = var.cloud_sql_instance_name
  database_version = "POSTGRES_17"
  region           = var.region
  
  settings {
    tier                        = var.cloud_sql_tier
    deletion_protection_enabled = var.enable_deletion_protection
    
    backup_configuration {
      enabled                        = true
      start_time                     = "03:00"
      point_in_time_recovery_enabled = true
      transaction_log_retention_days = 7
      backup_retention_settings {
        retained_backups = 7
        retention_unit   = "COUNT"
      }
    }
    
    ip_configuration {
      ipv4_enabled    = false
      private_network = var.vpc_network_id
    }
    
    database_flags {
      name  = "max_connections"
      value = "100"
    }
    
    insights_config {
      query_insights_enabled  = true
      query_string_length     = 1024
      record_application_tags = true
      record_client_address   = true
    }
  }
  
  deletion_protection = var.enable_deletion_protection
}

resource "google_sql_database" "postgres_db" {
  name     = var.database_name
  instance = google_sql_database_instance.postgres.name
}

resource "google_sql_user" "postgres_user" {
  name     = var.database_user
  instance = google_sql_database_instance.postgres.name
  password = var.database_password
}

# ============================================
# Cloud Run Services (9 Microservices)
# ============================================

# TablesApi
module "tables_api" {
  source = "./modules/cloudrun-service"
  
  service_name      = "magidesk-tables"
  project_id        = var.project_id
  region            = var.region
  image             = "gcr.io/${var.project_id}/magidesk-tables:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# OrdersApi
module "orders_api" {
  source = "./modules/cloudrun-service"
  
  service_name       = "magidesk-order"
  project_id         = var.project_id
  region             = var.region
  image              = "gcr.io/${var.project_id}/magidesk-order:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# PaymentApi
module "payment_api" {
  source = "./modules/cloudrun-service"
  
  service_name       = "magidesk-payment"
  project_id         = var.project_id
  region             = var.region
  image              = "gcr.io/${var.project_id}/magidesk-payment:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# MenuApi
module "menu_api" {
  source = "./modules/cloudrun-service"
  
  service_name       = "magidesk-menu"
  project_id         = var.project_id
  region             = var.region
  image              = "gcr.io/${var.project_id}/magidesk-menu:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# CustomerApi
module "customer_api" {
  source = "./modules/cloudrun-service"
  
  service_name       = "magidesk-customer"
  project_id         = var.project_id
  region             = var.region
  image              = "gcr.io/${var.project_id}/magidesk-customer:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# DiscountApi
module "discount_api" {
  source = "./modules/cloudrun-service"
  
  service_name       = "magidesk-discount"
  project_id         = var.project_id
  region             = var.region
  image              = "gcr.io/${var.project_id}/magidesk-discount:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# InventoryApi
module "inventory_api" {
  source = "./modules/cloudrun-service"
  
  service_name       = "magidesk-inventory"
  project_id         = var.project_id
  region             = var.region
  image              = "gcr.io/${var.project_id}/magidesk-inventory:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# SettingsApi
module "settings_api" {
  source = "./modules/cloudrun-service"
  
  service_name       = "magidesk-settings"
  project_id         = var.project_id
  region             = var.region
  image              = "gcr.io/${var.project_id}/magidesk-settings:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# UsersApi
module "users_api" {
  source = "./modules/cloudrun-service"
  
  service_name       = "magidesk-users"
  project_id         = var.project_id
  region             = var.region
  image              = "gcr.io/${var.project_id}/magidesk-users:latest"
  cloud_sql_instance = google_sql_database_instance.postgres.connection_name
  
  environment_variables = {
    ASPNETCORE_ENVIRONMENT = "Production"
    ASPNETCORE_URLS        = "http://0.0.0.0:8080"
  }
}

# ============================================
# Outputs
# ============================================

output "cloud_sql_connection_name" {
  description = "Cloud SQL instance connection name"
  value       = google_sql_database_instance.postgres.connection_name
}

output "cloud_sql_private_ip" {
  description = "Cloud SQL instance private IP"
  value       = google_sql_database_instance.postgres.private_ip_address
}

output "service_urls" {
  description = "Cloud Run service URLs"
  value = {
    tables_api    = module.tables_api.service_url
    orders_api    = module.orders_api.service_url
    payment_api   = module.payment_api.service_url
    menu_api      = module.menu_api.service_url
    customer_api  = module.customer_api.service_url
    discount_api  = module.discount_api.service_url
    inventory_api = module.inventory_api.service_url
    settings_api  = module.settings_api.service_url
    users_api     = module.users_api.service_url
  }
}

