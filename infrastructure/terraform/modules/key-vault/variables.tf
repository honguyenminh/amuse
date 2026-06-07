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
