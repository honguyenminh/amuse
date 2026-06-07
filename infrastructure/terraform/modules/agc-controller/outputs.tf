output "namespace" {
  value = kubernetes_namespace_v1.this.metadata[0].name
}

output "gateway_class_name" {
  value = var.gateway_class_name
}

output "release_name" {
  value = helm_release.alb_controller.name
}
