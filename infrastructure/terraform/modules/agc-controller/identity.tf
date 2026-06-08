data "azurerm_resource_group" "main" {
  name = var.resource_group_name
}

data "azurerm_resource_group" "node" {
  name = var.node_resource_group
}

data "azurerm_role_definition" "agc_configuration_manager" {
  name = "AppGw for Containers Configuration Manager"
}

data "azurerm_role_definition" "reader" {
  name = "Reader"
}

data "azurerm_role_definition" "network_contributor" {
  name = "Network Contributor"
}

resource "azurerm_user_assigned_identity" "alb_controller" {
  name                = "${var.resource_prefix}-alb-controller-id"
  location            = var.location
  resource_group_name = var.resource_group_name
}

resource "azurerm_role_assignment" "alb_controller_config_manager_main_rg" {
  scope                = data.azurerm_resource_group.main.id
  role_definition_id   = data.azurerm_role_definition.agc_configuration_manager.id
  principal_id       = azurerm_user_assigned_identity.alb_controller.principal_id
}

resource "azurerm_role_assignment" "alb_controller_config_manager_node_rg" {
  scope              = data.azurerm_resource_group.node.id
  role_definition_id = data.azurerm_role_definition.agc_configuration_manager.id
  principal_id       = azurerm_user_assigned_identity.alb_controller.principal_id
}

resource "azurerm_role_assignment" "alb_controller_reader_node_rg" {
  scope              = data.azurerm_resource_group.node.id
  role_definition_id = data.azurerm_role_definition.reader.id
  principal_id       = azurerm_user_assigned_identity.alb_controller.principal_id
}

resource "azurerm_role_assignment" "alb_controller_network_contributor_subnet" {
  scope              = var.agc_subnet_id
  role_definition_id = data.azurerm_role_definition.network_contributor.id
  principal_id       = azurerm_user_assigned_identity.alb_controller.principal_id
}

resource "azurerm_federated_identity_credential" "alb_controller" {
  name                = "${var.resource_prefix}-alb-controller-fic"
  user_assigned_identity_id = azurerm_user_assigned_identity.alb_controller.id
  audience            = ["api://AzureADTokenExchange"]
  issuer              = var.oidc_issuer_url
  subject             = "system:serviceaccount:${var.alb_controller_sa_namespace}:${var.alb_controller_sa_name}"
}
