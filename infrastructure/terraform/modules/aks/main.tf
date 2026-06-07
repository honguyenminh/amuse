resource "azurerm_kubernetes_cluster" "aks" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = var.dns_prefix

  kubernetes_version = var.kubernetes_version != "" ? var.kubernetes_version : null
  sku_tier           = var.sku_tier

  oidc_issuer_enabled       = true
  workload_identity_enabled = true

  role_based_access_control_enabled = true
  local_account_disabled            = var.local_account_disabled

  identity {
    type = "SystemAssigned"
  }

  default_node_pool {
    name                 = "system"
    vm_size              = var.node_vm_size
    vnet_subnet_id       = var.node_subnet_id
    type                 = "VirtualMachineScaleSets"
    auto_scaling_enabled = var.node_auto_scaling_enabled
    node_count           = var.node_auto_scaling_enabled ? null : var.node_count
    min_count            = var.node_auto_scaling_enabled ? var.node_min_count : null
    max_count            = var.node_auto_scaling_enabled ? var.node_max_count : null
    os_disk_size_gb      = var.node_os_disk_size_gb
    max_pods             = var.max_pods

    upgrade_settings {
      drain_timeout_in_minutes      = 0
      max_surge                     = "10%"
      node_soak_duration_in_minutes = 0
    }
  }

  network_profile {
    network_plugin      = "azure"
    network_plugin_mode = var.network_plugin_mode
    pod_cidr            = var.pod_cidr
    service_cidr        = var.service_cidr
    dns_service_ip      = var.dns_service_ip
    outbound_type       = var.outbound_type
  }

  dynamic "key_vault_secrets_provider" {
    for_each = var.enable_key_vault_csi ? [1] : []
    content {
      secret_rotation_enabled  = true
      secret_rotation_interval = var.key_vault_secret_rotation_interval
    }
  }

  tags = var.tags
}
