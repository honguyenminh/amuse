output "resource_group_name" {
  value = local.rg_name
}

output "agc_alb_id" {
  value = module.app-gw.alb_id
}

output "agc_frontend_fqdn" {
  value = module.app-gw.frontend_fqdn
}

output "agc_gateway_class_name" {
  value = var.agc_gateway_class_name
}

output "aks_cluster_name" {
  value = module.aks.name
}

output "aks_oidc_issuer_url" {
  value = module.aks.oidc_issuer_url
}

output "key_vault_name" {
  value = module.key_vault.name
}

output "key_vault_uri" {
  value = module.key_vault.kv_fdns
}

output "postgres_connection_string_secret_name" {
  value = "${var.resource_prefix}-postgres-connection-string"
}

output "argocd_namespace" {
  value = module.argocd.namespace
}

output "argocd_release_name" {
  value = module.argocd.release_name
}

output "amuse_workload_identity_client_id" {
  value = module.key_vault.access_identity_client_id
}

output "amuse_workload_identity_name" {
  value = module.key_vault.access_identity_name
}

output "amuse_namespace" {
  value = var.amuse_namespace
}

output "amuse_key_vault_secret_names" {
  value = {
    jwt_signing_key     = azurerm_key_vault_secret.amuse_jwt_signing_key.name
    r2_endpoint         = azurerm_key_vault_secret.amuse_r2_endpoint.name
    r2_access_key       = azurerm_key_vault_secret.amuse_r2_access_key.name
    r2_secret_key       = azurerm_key_vault_secret.amuse_r2_secret_key.name
    r2_public_base_url  = azurerm_key_vault_secret.amuse_r2_public_base_url.name
    r2_presign_base_url = azurerm_key_vault_secret.amuse_r2_presign_base_url.name
    smtp_host           = azurerm_key_vault_secret.amuse_smtp_host.name
    smtp_user           = azurerm_key_vault_secret.amuse_smtp_user.name
    smtp_password       = azurerm_key_vault_secret.amuse_smtp_password.name
    rabbitmq_password   = azurerm_key_vault_secret.amuse_rabbitmq_password.name
    redis_password      = azurerm_key_vault_secret.amuse_redis_password.name
    redis_connection    = azurerm_key_vault_secret.amuse_redis_connection_string.name
    postgres            = "${var.resource_prefix}-postgres-connection-string"
  }
}

output "amuse_federated_identity_subjects" {
  value = {
    for key, cred in azurerm_federated_identity_credential.amuse_keyvault :
    key => cred.subject
  }
}
