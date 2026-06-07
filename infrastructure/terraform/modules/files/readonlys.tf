# Configuration files share (read-only configs)
resource "azurerm_storage_share" "configs" {
  name               = "observability-configs"
  storage_account_id = azurerm_storage_account.observability.id
  quota              = 1 # Small quota for config files
}

# Upload Prometheus configuration files
resource "azurerm_storage_share_file" "prometheus_config" {
  count             = var.upload_observability_configs ? 1 : 0
  name              = "prometheus.yml"
  storage_share_url = azurerm_storage_share.configs.url
  source            = "${path.root}/../../observability/prometheus.yml"
}

resource "azurerm_storage_share_file" "prometheus_alerts" {
  count             = var.upload_observability_configs ? 1 : 0
  name              = "prometheus-alerts.yml"
  storage_share_url = azurerm_storage_share.configs.url
  source            = "${path.root}/../../observability/prometheus-alerts.yml"
}

# Upload Loki configuration
resource "azurerm_storage_share_file" "loki_config" {
  count             = var.upload_observability_configs ? 1 : 0
  name              = "loki-config.yml"
  storage_share_url = azurerm_storage_share.configs.url
  source            = "${path.root}/../../observability/loki-config.yml"
}

# Upload Alloy configuration
resource "azurerm_storage_share_file" "alloy_config" {
  count             = var.upload_observability_configs ? 1 : 0
  name              = "alloy-config.alloy"
  storage_share_url = azurerm_storage_share.configs.url
  source            = "${path.root}/../../observability/alloy-config.alloy"
}
