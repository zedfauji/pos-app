# ============================================
# Cloud Run Service Module
# Reusable module for deploying ASP.NET Core APIs
# ============================================

variable "service_name" {
  description = "Cloud Run service name"
  type        = string
}

variable "project_id" {
  description = "GCP Project ID"
  type        = string
}

variable "region" {
  description = "GCP Region"
  type        = string
}

variable "image" {
  description = "Container image URL"
  type        = string
}

variable "cloud_sql_instance" {
  description = "Cloud SQL instance connection name"
  type        = string
  default     = null
}

variable "environment_variables" {
  description = "Environment variables for Cloud Run service"
  type        = map(string)
  default     = {}
}

variable "memory" {
  description = "Memory allocation (e.g., '512Mi')"
  type        = string
  default     = "512Mi"
}

variable "cpu" {
  description = "CPU allocation"
  type        = number
  default     = 1
}

variable "min_instances" {
  description = "Minimum instances (0 for serverless)"
  type        = number
  default     = 0
}

variable "max_instances" {
  description = "Maximum instances"
  type        = number
  default     = 10
}

variable "timeout_seconds" {
  description = "Request timeout in seconds"
  type        = number
  default     = 300
}

variable "allow_unauthenticated" {
  description = "Allow unauthenticated access"
  type        = bool
  default     = true
}

# Cloud Run Service
resource "google_cloud_run_v2_service" "service" {
  name     = var.service_name
  location = var.region
  project  = var.project_id

  template {
    containers {
      image = var.image
      
      ports {
        container_port = 8080
      }
      
      resources {
        limits = {
          cpu    = "${var.cpu}"
          memory = var.memory
        }
      }
      
      env {
        name  = "ASPNETCORE_URLS"
        value = "http://0.0.0.0:8080"
      }
      
      dynamic "env" {
        for_each = var.environment_variables
        content {
          name  = env.key
          value = env.value
        }
      }
    }
    
    scaling {
      min_instance_count = var.min_instances
      max_instance_count = var.max_instances
    }
    
    timeout = "${var.timeout_seconds}s"
    
    dynamic "volumes" {
      for_each = var.cloud_sql_instance != null ? [1] : []
      content {
        name = "cloudsql"
        cloud_sql_instance {
          instances = [var.cloud_sql_instance]
        }
      }
    }
  }
  
  traffic {
    percent = 100
    type    = "TRAFFIC_TARGET_ALLOCATION_TYPE_LATEST"
  }
}

# IAM Policy for unauthenticated access
resource "google_cloud_run_service_iam_member" "public_access" {
  count    = var.allow_unauthenticated ? 1 : 0
  service  = google_cloud_run_v2_service.service.name
  location = google_cloud_run_v2_service.service.location
  role     = "roles/run.invoker"
  member   = "allUsers"
}

# Outputs
output "service_url" {
  description = "Cloud Run service URL"
  value       = google_cloud_run_v2_service.service.uri
}

output "service_name" {
  description = "Cloud Run service name"
  value       = google_cloud_run_v2_service.service.name
}

