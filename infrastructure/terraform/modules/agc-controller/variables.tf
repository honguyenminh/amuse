variable "namespace" {
  type    = string
  default = "alb-infra"
}

variable "release_name" {
  type    = string
  default = "alb-controller"
}

variable "chart_version" {
  description = "ALB Controller chart version — see https://learn.microsoft.com/azure/application-gateway/for-containers/alb-controller-release-notes"
  type        = string
  default     = "1.10.28"
}

variable "gateway_api_version" {
  description = "Gateway API release tag (standard-install.yaml), e.g. 1.2.1 — see kubernetes-sigs/gateway-api releases."
  type        = string
  default     = "1.2.1"
}

variable "timeout" {
  type    = number
  default = 600
}

variable "agc_subnet_id" {
  description = "Azure resource ID of the delegated AGC subnet (Microsoft.ServiceNetworking/trafficControllers)."
  type        = string
}

variable "alb_resource_name" {
  type    = string
  default = "amuse-staging-alb"
}

variable "gateway_class_name" {
  type    = string
  default = "azure-alb-external"
}

variable "frontend_name" {
  description = "AGC frontend name (used by GitOps Gateway resource)"
  type        = string
}

variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "resource_prefix" {
  type = string
}

variable "oidc_issuer_url" {
  type = string
}

variable "node_resource_group" {
  description = "AKS node resource group (MC_*), required for ALB controller RBAC."
  type        = string
}

variable "alb_controller_sa_namespace" {
  description = "Namespace of the ALB controller service account installed by the Helm chart."
  type        = string
  default     = "azure-alb-system"
}

variable "alb_controller_sa_name" {
  type    = string
  default = "alb-controller-sa"
}
