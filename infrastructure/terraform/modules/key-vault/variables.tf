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

variable "subnet_id" {
  description = "Subnet ID for the Key Vault private endpoint"
  type        = string
}

variable "virtual_network_id" {
  description = "Virtual network ID for private DNS zone linking"
  type        = string
}

variable "public_network_access_enabled" {
  description = "Allow Key Vault API access over the public internet (required for Terraform apply from a laptop). AKS still reaches KV via the private endpoint. Set false only when apply runs inside the VNet."
  type        = bool
  default     = true
}
