# ============================================
# MagiDesk POS - Terraform Outputs
# ============================================

output "cloud_sql_connection_name" {
  description = "Cloud SQL instance connection name (for Cloud Run socket connection)"
  value       = google_sql_database_instance.postgres.connection_name
}

output "cloud_sql_private_ip" {
  description = "Cloud SQL instance private IP address"
  value       = google_sql_database_instance.postgres.private_ip_address
}

output "cloud_sql_public_ip" {
  description = "Cloud SQL instance public IP address (if enabled)"
  value       = google_sql_database_instance.postgres.public_ip_address
}

output "database_name" {
  description = "PostgreSQL database name"
  value       = google_sql_database.postgres_db.name
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

output "terraform_state_backend" {
  description = "Terraform state backend configuration (if using remote state)"
  value = {
    bucket = "magidesk-terraform-state"
    prefix = "terraform/state"
  }
}

