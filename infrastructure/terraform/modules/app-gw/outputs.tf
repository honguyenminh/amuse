output "alb_id" {
  value = azurerm_application_load_balancer.alb.id
}

output "alb_primary_configuration_endpoint" {
  value = azurerm_application_load_balancer.alb.primary_configuration_endpoint
}

output "frontend_id" {
  value = azurerm_application_load_balancer_frontend.frontend.id
}

output "frontend_fqdn" {
  value = azurerm_application_load_balancer_frontend.frontend.fully_qualified_domain_name
}