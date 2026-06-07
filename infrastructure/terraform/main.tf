module "resource-group" {
  source = "./modules/resource-group"

  resource_group_base_name = var.resource_prefix
  environment              = var.environment
  location                 = var.location
}

locals {
  rg_name                       = module.resource-group.name
  rg_location                   = module.resource-group.location
  postgres_password             = coalesce(var.postgres_admin_password, random_password.postgres_admin.result)
  amuse_redis_connection_string = "redis:6379,password=${urlencode(var.amuse_redis_password)},abortConnect=false"
}

resource "random_password" "postgres_admin" {
  length  = 32
  special = true
}

module "networking" {
  source = "./modules/networking"

  resource_group_name = local.rg_name
  location            = local.rg_location
  resource_prefix     = var.resource_prefix
}

module "key_vault" {
  source = "./modules/key-vault"

  resource_prefix     = var.resource_prefix
  resource_group_name = local.rg_name
  location            = local.rg_location
  subnet_id           = module.networking.endpoints-subnet-id
  virtual_network_id  = module.networking.main-vnet-id
}

module "postgres" {
  source = "./modules/postgres"

  resource_prefix     = var.resource_prefix
  resource_group_name = local.rg_name
  location            = local.rg_location

  virtual_network_id = module.networking.main-vnet-id
  subnet_id          = module.networking.postgres-subnet-id

  admin_username         = var.postgres_admin_username
  admin_password         = local.postgres_password
  admin_password_version = var.postgres_admin_password_version

  key_vault_id                     = module.key_vault.id
  db_names                         = var.postgres_db_names
  connection_db_name               = var.postgres_connection_db_name
  connection_string_secret_version = var.postgres_kv_secret_version
  sku_name                         = var.postgres_sku_name
  enable_pgbouncer                 = var.postgres_enable_pgbouncer
  allowed_extensions               = var.postgres_allowed_extensions

  depends_on = [module.key_vault]
}

module "app-gw" {
  source = "./modules/app-gw"

  resource_prefix     = var.resource_prefix
  resource_group_name = local.rg_name
  location            = local.rg_location

  subnet_id     = module.networking.agc-subnet-id
  frontend_name = var.agc_frontend_name
}

module "aks" {
  source = "./modules/aks"

  resource_group_name = local.rg_name
  location            = local.rg_location
  name                = var.aks_name
  dns_prefix          = var.aks_dns_prefix

  kubernetes_version = var.aks_kubernetes_version
  sku_tier           = var.aks_sku_tier

  node_subnet_id            = module.networking.aks-nodes-subnet-id
  node_vm_size              = var.aks_node_vm_size
  node_auto_scaling_enabled = var.aks_node_auto_scaling_enabled
  node_count                = var.aks_node_count
  node_min_count            = var.aks_node_min_count
  node_max_count            = var.aks_node_max_count
  node_os_disk_size_gb      = var.aks_node_os_disk_size_gb
  max_pods                  = var.aks_max_pods

  pod_cidr       = var.aks_pod_cidr
  service_cidr   = var.aks_service_cidr
  dns_service_ip = var.aks_dns_service_ip
  outbound_type  = var.aks_outbound_type

  local_account_disabled = true
}

module "argocd" {
  source = "./modules/argocd"

  namespace = var.argocd_namespace

  depends_on = [module.aks]
}

module "agc_controller" {
  source = "./modules/agc-controller"

  namespace          = var.agc_controller_namespace
  alb_id             = module.app-gw.alb_id
  frontend_name      = var.agc_frontend_name
  gateway_class_name = var.agc_gateway_class_name
  alb_resource_name  = "${var.resource_prefix}-alb"

  depends_on = [module.aks, module.app-gw]
}

module "external_secrets" {
  source = "./modules/external-secrets"

  namespace = var.external_secrets_namespace

  depends_on = [module.aks]
}
