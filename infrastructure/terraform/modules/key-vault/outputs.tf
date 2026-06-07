output "kv_fdns" {
  value = azurerm_key_vault.keyvault.vault_uri
}

output "id" {
  value = azurerm_key_vault.keyvault.id
}

output "name" {
  value = azurerm_key_vault.keyvault.name
}

output "access_identity_id" {
  value = azurerm_user_assigned_identity.apps.id
}

output "access_identity_name" {
  value = azurerm_user_assigned_identity.apps.name
}

output "access_identity_client_id" {
  value = azurerm_user_assigned_identity.apps.client_id
}

output "access_identity_principal_id" {
  value = azurerm_user_assigned_identity.apps.principal_id
}
