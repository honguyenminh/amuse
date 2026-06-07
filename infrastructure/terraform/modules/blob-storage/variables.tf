variable "resource_group_name" {
  description = "Name of the resource group to create resource in"
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
  description = "VNet ID for private DNS zone links"
  type        = string
}

variable "endpoint_subnet_id" {
  description = "Subnet ID for private endpoints"
  type        = string
}

variable "key_vault_id" {
  description = "Key Vault ID for storing storage secrets"
  type        = string
}

variable "secret_version" {
  description = "Version bump to rotate Blob storage secrets"
  type        = number
  default     = 1
}

variable "account_tier" {
  type    = string
  default = "Standard"
}

variable "account_replication_type" {
  type    = string
  default = "LRS"
}

variable "access_tier" {
  type    = string
  default = "Hot"
}

variable "media_container_name" {
  type    = string
  default = "saleor-media"
}

variable "media_private_container_name" {
  type    = string
  default = "saleor-media-private"
}

variable "enable_private_endpoint" {
  type    = bool
  default = true
}

variable "public_network_access_enabled" {
  type    = bool
  default = false
}

variable "shared_access_key_enabled" {
  type    = bool
  default = true
}

variable "tags" {
  type    = map(string)
  default = {}
}
