variable "resource_group_name" {
  description = "Name of the resource group to create resource in"
  type        = string
}

variable "location" {
  description = "Azure region of resource group"
  type        = string
}

variable "name" {
  description = "AKS cluster name"
  type        = string
}

variable "dns_prefix" {
  description = "DNS prefix for the AKS API server"
  type        = string
}

variable "kubernetes_version" {
  description = "Optional Kubernetes version (minor or full)"
  type        = string
  default     = ""
}

variable "sku_tier" {
  description = "AKS SKU tier"
  type        = string
  default     = "Standard"
}

variable "node_subnet_id" {
  description = "Subnet ID for AKS nodes"
  type        = string
}

variable "node_vm_size" {
  description = "VM size for the system node pool"
  type        = string
  default     = "Standard_D4ds_v4"
}

variable "node_auto_scaling_enabled" {
  type    = bool
  default = true
}

variable "node_count" {
  description = "Node count when autoscaling is disabled"
  type        = number
  default     = 2
}

variable "node_min_count" {
  type    = number
  default = 2
}

variable "node_max_count" {
  type    = number
  default = 6
}

variable "node_os_disk_size_gb" {
  type    = number
  default = 128
}

variable "max_pods" {
  type    = number
  default = 60
}

variable "network_plugin_mode" {
  description = "Network plugin mode for Azure CNI (overlay recommended)"
  type        = string
  default     = "overlay"
}

variable "pod_cidr" {
  description = "Pod CIDR for Azure CNI overlay"
  type        = string
  default     = "10.244.0.0/16"
}

variable "service_cidr" {
  description = "Service CIDR for Kubernetes services"
  type        = string
  default     = "10.240.0.0/16"
}

variable "dns_service_ip" {
  description = "DNS service IP within service CIDR"
  type        = string
  default     = "10.240.0.10"
}

variable "outbound_type" {
  description = "AKS outbound type"
  type        = string
  default     = "loadBalancer"
}

variable "local_account_disabled" {
  type    = bool
  default = false
}

variable "node_upgrade_max_surge" {
  description = "Extra nodes during pool upgrades (consumes vCPU quota). Must not be 0 — AKS rejects maxSurge=0 unless maxUnavailable is set (not exposed in this azurerm version)."
  type        = string
  default     = "1"
}

variable "enable_key_vault_csi" {
  description = "Enable the Key Vault CSI driver add-on"
  type        = bool
  default     = true
}

variable "key_vault_secret_rotation_interval" {
  description = "Secret rotation interval when CSI driver is enabled"
  type        = string
  default     = "2m"
}

variable "tags" {
  type    = map(string)
  default = {}
}
