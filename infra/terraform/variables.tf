# ============================================
# MagiDesk POS - Terraform Variables
# ============================================

variable "project_id" {
  description = "GCP Project ID"
  type        = string
  default     = "bola8pos"
}

variable "region" {
  description = "GCP Region"
  type        = string
  default     = "northamerica-south1"
}

variable "cloud_sql_instance_name" {
  description = "Cloud SQL instance name"
  type        = string
  default     = "pos-app-1"
}

variable "cloud_sql_tier" {
  description = "Cloud SQL machine tier"
  type        = string
  default     = "db-f1-micro"  # db-f1-micro or db-g1-small
}

variable "database_name" {
  description = "PostgreSQL database name"
  type        = string
  default     = "postgres"
}

variable "database_user" {
  description = "PostgreSQL database user"
  type        = string
  default     = "posapp"
  sensitive   = true
}

variable "database_password" {
  description = "PostgreSQL database password"
  type        = string
  sensitive   = true
}

variable "vpc_network_id" {
  description = "VPC network ID for Cloud SQL private IP"
  type        = string
  default     = null
}

variable "enable_deletion_protection" {
  description = "Enable deletion protection on Cloud SQL"
  type        = bool
  default     = true
}

variable "min_instances" {
  description = "Minimum Cloud Run instances (0 for serverless scaling)"
  type        = number
  default     = 0
}

variable "max_instances" {
  description = "Maximum Cloud Run instances"
  type        = number
  default     = 10
}

variable "memory" {
  description = "Memory per Cloud Run service (e.g., '512Mi')"
  type        = string
  default     = "512Mi"
}

variable "cpu" {
  description = "CPU per Cloud Run service"
  type        = number
  default     = 1
}

variable "timeout_seconds" {
  description = "Cloud Run request timeout in seconds"
  type        = number
  default     = 300
}

