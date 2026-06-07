output "postgres-subnet-id" {
  value = azurerm_subnet.postgres.id
}

output "aks-nodes-subnet-id" {
  value = azurerm_subnet.aks_nodes.id
}

output "agc-subnet-id" {
  value = azurerm_subnet.agc.id
}

output "main-vnet-id" {
  value = azurerm_virtual_network.main.id
}

output "endpoints-subnet-id" {
  value = azurerm_subnet.endpoints.id
}

# dns zones
output "redis-dns-zone-id" {
  value = azurerm_private_dns_zone.redis.id
}