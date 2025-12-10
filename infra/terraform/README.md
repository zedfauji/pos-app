# Terraform Infrastructure as Code
## MagiDesk POS - GCP Cloud Run + Cloud SQL

This directory contains Terraform configurations for provisioning GCP infrastructure.

## Structure

```
infra/terraform/
├── main.tf                    # Main infrastructure definition
├── variables.tf               # Input variables
├── outputs.tf                 # Output values
├── terraform.tfvars.example  # Example variable values (create terraform.tfvars from this)
└── modules/
    └── cloudrun-service/      # Reusable Cloud Run service module
        └── main.tf
```

## Quick Start

### 1. Initialize Terraform

```bash
cd infra/terraform
terraform init
```

### 2. Create terraform.tfvars

Copy the example and fill in values:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars`:

```hcl
project_id              = "bola8pos"
region                  = "northamerica-south1"
cloud_sql_instance_name = "pos-app-1"
database_name           = "postgres"
database_user           = "posapp"
database_password       = "YOUR_SECURE_PASSWORD"
cloud_sql_tier          = "db-f1-micro"
```

### 3. Plan Changes

```bash
terraform plan
```

### 4. Apply Infrastructure

```bash
terraform apply
```

## What Gets Created

- **1 Cloud SQL PostgreSQL 17 instance** (db-f1-micro or db-g1-small)
- **9 Cloud Run services** (one per microservice)
- **Database and user** (configured per variables)

## Variables

See `variables.tf` for all available variables. Key variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `project_id` | GCP Project ID | `bola8pos` |
| `region` | GCP Region | `northamerica-south1` |
| `cloud_sql_tier` | Database tier | `db-f1-micro` |
| `min_instances` | Min Cloud Run instances | `0` |
| `max_instances` | Max Cloud Run instances | `10` |
| `memory` | Memory per service | `512Mi` |
| `cpu` | CPU per service | `1` |

## Outputs

After `terraform apply`, view outputs:

```bash
terraform output
```

Key outputs:
- `cloud_sql_connection_name` - For Cloud Run socket connection
- `service_urls` - All Cloud Run service URLs

## Remote State (Optional)

Uncomment and configure in `main.tf`:

```hcl
terraform {
  backend "gcs" {
    bucket = "magidesk-terraform-state"
    prefix = "terraform/state"
  }
}
```

## Destroy Infrastructure

**⚠️ WARNING: This will delete all infrastructure!**

```bash
terraform destroy
```

## Migration from Manual Deployment

1. **Import existing Cloud SQL instance** (if it exists):
   ```bash
   terraform import google_sql_database_instance.postgres bola8pos/northamerica-south1/pos-app-1
   ```

2. **Import existing Cloud Run services** (if they exist):
   ```bash
   terraform import module.tables_api.google_cloud_run_v2_service.service projects/bola8pos/locations/northamerica-south1/services/magidesk-tables
   ```

## Cost Estimation

Run Terraform plan to see cost estimate:

```bash
terraform plan | grep -i cost
```

Or use `terraform show` after plan to see resource details.

## Troubleshooting

**Error: "Project not found"**
- Verify `project_id` in `terraform.tfvars`
- Ensure you have access to the GCP project

**Error: "Permission denied"**
- Ensure service account has required roles:
  - `roles/cloudsql.admin`
  - `roles/run.admin`
  - `roles/iam.serviceAccountUser`

**Error: "Image not found"**
- Build and push Docker images first:
  ```bash
  gcloud builds submit --tag gcr.io/PROJECT_ID/magidesk-tables:latest
  ```

## Next Steps

1. Set up remote state (GCS bucket)
2. Configure Terraform Cloud for team collaboration
3. Add environment-specific configs (dev/staging/prod)
4. Implement Terraform modules for shared resources

