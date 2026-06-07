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

variable "subnet_id" {
  description = "Subnet ID for Application Gateway for Containers (delegated to Microsoft.ServiceNetworking/trafficControllers)"
  type        = string
}

variable "frontend_name" {
  description = "Name of the ALB frontend resource"
  type        = string
  default     = "saleor-frontend"
}

variable "tags" {
  type    = map(string)
  default = {}
}