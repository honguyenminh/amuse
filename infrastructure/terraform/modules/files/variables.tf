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

variable "allowed_subnet_ids" {
  type = set(string)
}

variable "key_vault_id" {
  type = string
}

variable "upload_observability_configs" {
  description = "Whether to upload observability config files to the configs share"
  type        = bool
  default     = false
}

variable "storage_key_secret_version" {
  description = "Version bump to rotate observability storage key secret"
  type        = number
  default     = 1
}