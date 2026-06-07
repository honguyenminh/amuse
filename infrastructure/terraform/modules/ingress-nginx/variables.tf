variable "namespace" {
  description = "Kubernetes namespace for ingress-nginx."
  type        = string
}

variable "release_name" {
  description = "Helm release name for ingress-nginx."
  type        = string
  default     = "ingress-nginx"
}

variable "chart_version" {
  description = "Optional ingress-nginx Helm chart version. Leave null to use the repository default."
  type        = string
  default     = null
}

variable "ingress_class_name" {
  description = "IngressClass name managed by ingress-nginx."
  type        = string
  default     = "nginx"
}

variable "default_ingress_class" {
  description = "Whether nginx should be marked as the default IngressClass."
  type        = bool
  default     = false
}

variable "timeout" {
  description = "Helm install/upgrade timeout in seconds."
  type        = number
  default     = 600
}
