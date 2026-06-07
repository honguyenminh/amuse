output "storage_account_id" {
  value = azurerm_storage_account.blob.id
}

output "storage_account_name" {
  value = azurerm_storage_account.blob.name
}

output "primary_blob_endpoint" {
  value = azurerm_storage_account.blob.primary_blob_endpoint
}

output "account_name_secret_id" {
  value = azurerm_key_vault_secret.account_name.versionless_id
}

output "account_key_secret_id" {
  value = azurerm_key_vault_secret.account_key.versionless_id
}

output "media_container_secret_id" {
  value = azurerm_key_vault_secret.media_container.versionless_id
}

output "media_private_container_secret_id" {
  value = azurerm_key_vault_secret.media_private_container.versionless_id
}
