variable "namespace" {
  description = "Kubernetes namespace for Argo CD."
  type        = string
}

variable "release_name" {
  description = "Helm release name for Argo CD."
  type        = string
  default     = "argocd"
}

variable "chart_version" {
  description = "Optional argo-cd Helm chart version. Leave null to use the repository default."
  type        = string
  default     = null
}

variable "timeout" {
  description = "Helm install/upgrade timeout in seconds."
  type        = number
  default     = 600
}
