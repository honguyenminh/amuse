locals {
  amuse_workload_identities = {
    api = {
      service_account = var.amuse_api_service_account_name
    }
    transcoder = {
      service_account = var.amuse_worker_transcoder_service_account_name
    }
    scheduler = {
      service_account = var.amuse_worker_scheduler_service_account_name
    }
  }
}

resource "azurerm_federated_identity_credential" "amuse_keyvault" {
  for_each = local.amuse_workload_identities

  name                         = "${var.resource_prefix}-amuse-${each.key}-kv-fic"
  user_assigned_identity_id    = module.key_vault.access_identity_id
  audience                     = ["api://AzureADTokenExchange"]
  issuer                       = module.aks.oidc_issuer_url
  subject                      = "system:serviceaccount:${var.amuse_namespace}:${each.value.service_account}"

  depends_on = [
    module.aks,
    module.key_vault,
  ]
}
