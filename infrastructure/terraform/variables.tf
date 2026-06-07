variable "subscription_id" {
  type = string
}

variable "environment" {
  description = "Deployment environment suffix for the resource group (e.g. staging)"
  type        = string
  default     = null
}

variable "acr_name" {
  type = string
}

variable "acr_sku" {
  description = "Azure Container Registry SKU. Use Basic for cost-sensitive staging."
  type        = string
  default     = "Basic"
  validation {
    condition     = contains(["Basic", "Premium", "Standard"], var.acr_sku)
    error_message = "Invalid SKU, valid values are: Basic, Premium, Standard."
  }
}

variable "resource_prefix" {
  description = "Prefix for the name for all resources"
  type        = string
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
}

variable "postgres_admin_username" {
  type = string
}

variable "postgres_admin_password" {
  description = "Optional override. When null, a random password is generated and stored in Key Vault."
  type        = string
  sensitive   = true
  default     = null
}

variable "postgres_admin_password_version" {
  type = number
}

variable "postgres_db_names" {
  type    = set(string)
  default = ["amuse_staging"]
}

variable "postgres_connection_db_name" {
  type    = string
  default = "amuse_staging"
}

variable "postgres_kv_secret_version" {
  type    = number
  default = 1
}

variable "postgres_sku_name" {
  description = "PostgreSQL Flexible Server compute SKU."
  type        = string
  default     = "B_Standard_B1ms"
}

variable "postgres_enable_pgbouncer" {
  description = "Enable built-in PgBouncer for PostgreSQL. Must be false for Burstable SKUs."
  type        = bool
  default     = false
}

variable "postgres_allowed_extensions" {
  description = "PostgreSQL extensions allowed on Azure Flexible Server for Amuse EF migrations."
  type        = set(string)
  default     = ["pgcrypto"]
}

variable "aks_name" {
  type = string
}

variable "aks_dns_prefix" {
  type = string
}

variable "aks_kubernetes_version" {
  type    = string
  default = ""
}

variable "aks_sku_tier" {
  type    = string
  default = "Standard"
}

variable "aks_node_vm_size" {
  type    = string
  default = "Standard_D4ds_v5"
}

variable "aks_node_auto_scaling_enabled" {
  type    = bool
  default = true
}

variable "aks_node_count" {
  type    = number
  default = 2
}

variable "aks_node_min_count" {
  type    = number
  default = 2
}

variable "aks_node_max_count" {
  type    = number
  default = 6
}

variable "aks_node_os_disk_size_gb" {
  type    = number
  default = 128
}

variable "aks_max_pods" {
  type    = number
  default = 60
}

variable "aks_pod_cidr" {
  type    = string
  default = "10.244.0.0/16"
}

variable "aks_service_cidr" {
  type    = string
  default = "10.240.0.0/16"
}

variable "aks_dns_service_ip" {
  type    = string
  default = "10.240.0.10"
}

variable "aks_outbound_type" {
  type    = string
  default = "loadBalancer"
}

variable "agc_frontend_name" {
  type    = string
  default = "amuse-staging-fe"
}

variable "agc_gateway_class_name" {
  description = "Gateway API GatewayClass name created by the AGC controller."
  type        = string
  default     = "azure-alb-external"
}

variable "agc_controller_namespace" {
  type    = string
  default = "alb-infra"
}

variable "external_secrets_namespace" {
  type    = string
  default = "external-secrets"
}

variable "argocd_namespace" {
  description = "Kubernetes namespace for Argo CD."
  type        = string
  default     = "argocd"
}

variable "amuse_namespace" {
  description = "Kubernetes namespace for Amuse workloads."
  type        = string
  default     = "amuse"
}

variable "amuse_api_service_account_name" {
  type    = string
  default = "amuse-api"
}

variable "amuse_worker_transcoder_service_account_name" {
  type    = string
  default = "amuse-worker-transcoder"
}

variable "amuse_worker_scheduler_service_account_name" {
  type    = string
  default = "amuse-worker-scheduler"
}

variable "amuse_jwt_signing_key" {
  description = "JWT signing key for Amuse API (min 32 chars). Set from secure tfvars."
  type        = string
  sensitive   = true
}

variable "amuse_jwt_signing_key_version" {
  type    = number
  default = 1
}

variable "amuse_r2_endpoint" {
  description = "Cloudflare R2 S3 API endpoint URL."
  type        = string
  sensitive   = true
}

variable "amuse_r2_endpoint_version" {
  type    = number
  default = 1
}

variable "amuse_r2_access_key" {
  type      = string
  sensitive = true
}

variable "amuse_r2_access_key_version" {
  type    = number
  default = 1
}

variable "amuse_r2_secret_key" {
  type      = string
  sensitive = true
}

variable "amuse_r2_secret_key_version" {
  type    = number
  default = 1
}

variable "amuse_r2_public_base_url" {
  description = "Public base URL for cover art (CDN or R2 public endpoint)."
  type        = string
  sensitive   = true
}

variable "amuse_r2_public_base_url_version" {
  type    = number
  default = 1
}

variable "amuse_smtp_host" {
  type      = string
  sensitive = true
  default   = "localhost"
}

variable "amuse_smtp_host_version" {
  type    = number
  default = 1
}

variable "amuse_smtp_user" {
  type      = string
  sensitive = true
  default   = ""
}

variable "amuse_smtp_user_version" {
  type    = number
  default = 1
}

variable "amuse_smtp_password" {
  type      = string
  sensitive = true
  default   = ""
}

variable "amuse_smtp_password_version" {
  type    = number
  default = 1
}

variable "amuse_rabbitmq_password" {
  description = "RabbitMQ password for in-cluster broker on stage."
  type        = string
  sensitive   = true
}

variable "amuse_rabbitmq_password_version" {
  type    = number
  default = 1
}
