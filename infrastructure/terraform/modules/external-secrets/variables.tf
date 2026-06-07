variable "namespace" {
  type    = string
  default = "external-secrets"
}

variable "release_name" {
  type    = string
  default = "external-secrets"
}

variable "chart_version" {
  type    = string
  default = "0.14.3"
}

variable "timeout" {
  type    = number
  default = 600
}
