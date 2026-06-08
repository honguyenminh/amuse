variable "namespace" {
  type    = string
  default = "alb-infra"
}

variable "release_name" {
  type    = string
  default = "alb-controller"
}

variable "chart_version" {
  type    = string
  default = "1.2.9"
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

variable "alb_id" {
  description = "Azure resource ID of the Application Load Balancer"
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
