resource "azurerm_application_load_balancer" "alb" {
  name                = "${var.resource_prefix}-agc"
  resource_group_name = var.resource_group_name
  location            = var.location
  tags                = var.tags
}

resource "azurerm_application_load_balancer_subnet_association" "association" {
  name                         = "${var.resource_prefix}-agc-assoc"
  application_load_balancer_id = azurerm_application_load_balancer.alb.id
  subnet_id                    = var.subnet_id
  tags                         = var.tags
}

resource "azurerm_application_load_balancer_frontend" "frontend" {
  name                         = var.frontend_name
  application_load_balancer_id = azurerm_application_load_balancer.alb.id
  tags                         = var.tags
}