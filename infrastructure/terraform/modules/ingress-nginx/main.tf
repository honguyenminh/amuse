resource "kubernetes_namespace_v1" "this" {
  metadata {
    name = var.namespace
  }
}

resource "helm_release" "this" {
  name       = var.release_name
  repository = "https://kubernetes.github.io/ingress-nginx"
  chart      = "ingress-nginx"
  version    = var.chart_version
  namespace  = kubernetes_namespace_v1.this.metadata[0].name

  atomic  = true
  timeout = var.timeout
  wait    = true

  set {
    name  = "controller.service.type"
    value = "LoadBalancer"
  }

  set {
    name  = "controller.service.annotations.service\\.beta\\.kubernetes\\.io/port_80_health-probe_protocol"
    value = "Tcp"
  }

  set {
    name  = "controller.service.annotations.service\\.beta\\.kubernetes\\.io/port_443_health-probe_protocol"
    value = "Tcp"
  }

  set {
    name  = "controller.ingressClassResource.name"
    value = var.ingress_class_name
  }

  set {
    name  = "controller.ingressClassResource.controllerValue"
    value = "k8s.io/ingress-nginx"
  }

  set {
    name  = "controller.ingressClassResource.default"
    value = tostring(var.default_ingress_class)
  }

  depends_on = [kubernetes_namespace_v1.this]
}
