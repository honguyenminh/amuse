resource "random_string" "storage_suffix" {
  length  = 6
  lower   = true
  numeric = true
  special = false
  upper   = false
}

locals {
  storage_account_base = substr(join("", regexall("[0-9a-z]", lower(var.resource_prefix))), 0, 10)
}

resource "azurerm_storage_account" "blob" {
  name                            = "${random_string.storage_suffix.result}${local.storage_account_base}blob"
  resource_group_name             = var.resource_group_name
  location                        = var.location
  account_tier                    = var.account_tier
  account_replication_type        = var.account_replication_type
  account_kind                    = "StorageV2"
  access_tier                     = var.access_tier
  https_traffic_only_enabled      = true
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  public_network_access_enabled   = var.public_network_access_enabled
  shared_access_key_enabled       = var.shared_access_key_enabled

  tags = var.tags
}

resource "azurerm_storage_container" "media" {
  name                  = var.media_container_name
  storage_account_id    = azurerm_storage_account.blob.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "media_private" {
  name                  = var.media_private_container_name
  storage_account_id    = azurerm_storage_account.blob.id
  container_access_type = "private"
}

resource "azurerm_private_dns_zone" "blob" {
  count               = var.enable_private_endpoint ? 1 : 0
  name                = "privatelink.blob.core.windows.net"
  resource_group_name = var.resource_group_name
}

resource "azurerm_private_dns_zone_virtual_network_link" "blob" {
  count                 = var.enable_private_endpoint ? 1 : 0
  name                  = "blob-link"
  resource_group_name   = var.resource_group_name
  private_dns_zone_name = azurerm_private_dns_zone.blob[0].name
  virtual_network_id    = var.virtual_network_id
}

resource "azurerm_private_endpoint" "blob" {
  count               = var.enable_private_endpoint ? 1 : 0
  name                = "${var.resource_prefix}-blob-endpoint"
  resource_group_name = var.resource_group_name
  location            = var.location
  subnet_id           = var.endpoint_subnet_id

  private_service_connection {
    name                           = "${var.resource_prefix}-blob-connection"
    private_connection_resource_id = azurerm_storage_account.blob.id
    is_manual_connection           = false
    subresource_names              = ["blob"]
  }

  private_dns_zone_group {
    name                 = "${var.resource_prefix}-blob-dns"
    private_dns_zone_ids = [azurerm_private_dns_zone.blob[0].id]
  }
}

resource "azurerm_key_vault_secret" "account_name" {
  name             = "${var.resource_prefix}-blob-account-name"
  key_vault_id     = var.key_vault_id
  value_wo         = azurerm_storage_account.blob.name
  value_wo_version = var.secret_version
}

resource "azurerm_key_vault_secret" "account_key" {
  name             = "${var.resource_prefix}-blob-account-key"
  key_vault_id     = var.key_vault_id
  value_wo         = azurerm_storage_account.blob.primary_access_key
  value_wo_version = var.secret_version
}

resource "azurerm_key_vault_secret" "media_container" {
  name             = "${var.resource_prefix}-blob-media-container"
  key_vault_id     = var.key_vault_id
  value_wo         = azurerm_storage_container.media.name
  value_wo_version = var.secret_version
}

resource "azurerm_key_vault_secret" "media_private_container" {
  name             = "${var.resource_prefix}-blob-media-private-container"
  key_vault_id     = var.key_vault_id
  value_wo         = azurerm_storage_container.media_private.name
  value_wo_version = var.secret_version
}
