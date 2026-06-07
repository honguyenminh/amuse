variable "resource_group_name" {
  description = "Name of the resource group to create resource in"
  type        = string
}

variable "resource_prefix" {
  description = "Prefix for the name for all resources"
  type        = string
}

variable "location" {
  description = "Azure region for all resources"
  type        = string
}

variable "sku_name" {
  description = "SKU name to use for underlying nodes."
  type        = string
  default     = "ComputeOptimized_X3"
}

variable "high_availability_enabled" {
  type    = bool
  default = true
}

variable "endpoint_subnet_id" {
  description = "Subnet id to install private endpoint into"
  type        = string
}

variable "endpoint_dns_zone_id" {
  type = string
}

variable "eviction_policy" {
  description = "Key eviction policy. https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/managed_redis#eviction_policy-1"
  type        = string
  default     = "AllKeysLRU"
}

variable "key_vault_id" {
  type = string
}

variable "access_key_secret_version" {
  description = "Version bump to rotate Redis access key secrets"
  type        = number
  default     = 1
}

variable "cache_url_secret_name" {
  description = "Optional Key Vault secret name for a Redis DB 0 CACHE_URL."
  type        = string
  default     = null
}

variable "cache_url_secret_version" {
  description = "Version bump to rotate the Redis CACHE_URL secret."
  type        = number
  default     = 1
}

variable "celery_broker_url_secret_name" {
  description = "Optional Key Vault secret name for a Redis DB 0 CELERY_BROKER_URL."
  type        = string
  default     = null
}

variable "celery_broker_url_secret_version" {
  description = "Version bump to rotate the Redis CELERY_BROKER_URL secret."
  type        = number
  default     = 1
}

variable "tags" {
  type    = map(string)
  default = {}
}
