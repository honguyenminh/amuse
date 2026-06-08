resource "kubernetes_namespace_v1" "this" {
  metadata {
    name = var.namespace
  }
}

# Official Gateway API standard channel (no Helm chart index — upstream recommends kubectl).
data "http" "gateway_api_standard_install" {
  url = "https://github.com/kubernetes-sigs/gateway-api/releases/download/v${var.gateway_api_version}/standard-install.yaml"

  lifecycle {
    postcondition {
      condition     = self.status_code == 200
      error_message = "Gateway API standard-install.yaml not found for version ${var.gateway_api_version} (HTTP ${self.status_code})."
    }
  }
}

data "kubectl_file_documents" "gateway_api_crds" {
  content = data.http.gateway_api_standard_install.response_body
}

resource "kubectl_manifest" "gateway_api_crds" {
  for_each = toset(data.kubectl_file_documents.gateway_api_crds.documents)

  yaml_body         = each.value
  server_side_apply = true

  depends_on = [kubernetes_namespace_v1.this]
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

  # Gateway API CRDs are installed from the official release manifest above.
  set {
    name  = "albController.installGatewayApiCRDs"
    value = "false"
  }

  set {
    name  = "albController.podIdentity.clientID"
    value = azurerm_user_assigned_identity.alb_controller.client_id
  }

  depends_on = [
    kubernetes_namespace_v1.this,
    kubectl_manifest.gateway_api_crds,
    azurerm_federated_identity_credential.alb_controller,
    azurerm_role_assignment.alb_controller_config_manager_main_rg,
    azurerm_role_assignment.alb_controller_config_manager_node_rg,
    azurerm_role_assignment.alb_controller_reader_node_rg,
    azurerm_role_assignment.alb_controller_network_contributor_subnet,
  ]
}

# kubectl_manifest applies at runtime (no plan-time CRD lookup). kubernetes_manifest
# fails when CRDs are installed in the same apply — see hashicorp/terraform-provider-kubernetes#1917.
resource "kubectl_manifest" "application_load_balancer" {
  yaml_body = yamlencode({
    apiVersion = "alb.networking.azure.io/v1"
    kind       = "ApplicationLoadBalancer"
    metadata = {
      name      = var.alb_resource_name
      namespace = var.namespace
    }
    spec = {
      # Subnet resource IDs (strings), not ALB ARM IDs — see Microsoft AGC quickstart.
      associations = [var.agc_subnet_id]
    }
  })

  depends_on = [
    kubectl_manifest.gateway_api_crds,
    helm_release.alb_controller,
  ]
}

resource "kubectl_manifest" "gateway_class" {
  yaml_body = yamlencode({
    apiVersion = "gateway.networking.k8s.io/v1"
    kind       = "GatewayClass"
    metadata = {
      name = var.gateway_class_name
    }
    spec = {
      controllerName = "alb.networking.azure.io/alb-controller"
    }
  })

  depends_on = [
    kubectl_manifest.gateway_api_crds,
    helm_release.alb_controller,
  ]
}
