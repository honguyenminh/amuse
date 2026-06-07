variable "resource_group_name" {
  description = "Name of the resource group to create db in"
  type        = string
}

variable "resource_prefix" {
  description = "Prefix of the name for the resources"
  type        = string
}

variable "location" {
  description = "Azure region of resource group"
  type        = string
}

variable "virtual_network_id" {
  type = string
}

variable "subnet_id" {
  type = string
}

variable "db_names" {
  description = "List of db names to create"
  type        = set(string)
  default     = ["saleor"]
}

variable "connection_db_name" {
  description = "Database name used in the connection string secret"
  type        = string
  default     = "saleor"
}

variable "admin_username" {
  type = string
}

variable "admin_password" {
  type      = string
  sensitive = true
}

variable "admin_password_version" {
  type = number
}

variable "sku_name" {
  description = "Compute size SKU"
  type        = string
  default     = "GP_Standard_D4ds_v5"
}

variable "enable_pgbouncer" {
  description = "Enable built-in PgBouncer. Azure does not support this on Burstable PostgreSQL tiers."
  type        = bool
  default     = true
}

variable "allowed_extensions" {
  description = "PostgreSQL extensions to allow-list on Azure Flexible Server."
  type        = set(string)
  default     = ["btree_gin", "hstore", "pg_trgm", "pgcrypto"]
}

variable "key_vault_id" {
  description = "ID of the Key Vault to store connection string"
  type        = string
}

variable "connection_string_secret_version" {
  description = "Version bump to rotate the Postgres connection string secret"
  type        = number
  default     = 1
}
