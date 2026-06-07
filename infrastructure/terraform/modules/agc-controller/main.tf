resource "kubernetes_namespace_v1" "this" {
  metadata {
    name = var.namespace
  }
}

resource "helm_release" "gateway_api_crds" {
  name       = "gateway-api"
  repository = "https://kubernetes-sigs.github.io/gateway-api"
  chart      = "gateway-api"
  version    = var.gateway_api_chart_version
  namespace  = "gateway-system"

  create_namespace = true
  atomic           = true
  timeout          = var.timeout
  wait             = true
}

resource "helm_release" "alb_controller" {
  name       = var.release_name
  repository = "oci://mcr.microsoft.com/application-lb/charts"
  chart      = "alb-controller"
  version    = var.chart_version
  namespace  = kubernetes_namespace_v1.this.metadata[0].name

  atomic  = true
  timeout = var.timeout
  wait    = true

  depends_on = [
    kubernetes_namespace_v1.this,
    helm_release.gateway_api_crds,
  ]
}

resource "kubernetes_manifest" "application_load_balancer" {
  manifest = {
    apiVersion = "alb.networking.azure.io/v1"
    kind       = "ApplicationLoadBalancer"
    metadata = {
      name      = var.alb_resource_name
      namespace = var.namespace
    }
    spec = {
      associations = [
        {
          albID = var.alb_id
        }
      ]
    }
  }

  depends_on = [helm_release.alb_controller]
}

resource "kubernetes_manifest" "gateway_class" {
  manifest = {
    apiVersion = "gateway.networking.k8s.io/v1"
    kind       = "GatewayClass"
    metadata = {
      name = var.gateway_class_name
    }
    spec = {
      controllerName = "alb.networking.azure.io/alb-controller"
    }
  }

  depends_on = [helm_release.alb_controller]
}
