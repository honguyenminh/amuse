resource "azurerm_key_vault_secret" "amuse_jwt_signing_key" {
  name             = "amuse-jwt-signing-key"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_jwt_signing_key
  value_wo_version = var.amuse_jwt_signing_key_version

  depends_on = [module.key_vault]
}

resource "azurerm_key_vault_secret" "amuse_r2_endpoint" {
  name             = "amuse-r2-endpoint"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_r2_endpoint
  value_wo_version = var.amuse_r2_endpoint_version

  depends_on = [module.key_vault]
}

resource "azurerm_key_vault_secret" "amuse_r2_access_key" {
  name             = "amuse-r2-access-key"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_r2_access_key
  value_wo_version = var.amuse_r2_access_key_version

  depends_on = [module.key_vault]
}

resource "azurerm_key_vault_secret" "amuse_r2_secret_key" {
  name             = "amuse-r2-secret-key"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_r2_secret_key
  value_wo_version = var.amuse_r2_secret_key_version

  depends_on = [module.key_vault]
}

resource "azurerm_key_vault_secret" "amuse_r2_public_base_url" {
  name             = "amuse-r2-public-base-url"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_r2_public_base_url
  value_wo_version = var.amuse_r2_public_base_url_version

  depends_on = [module.key_vault]
}

resource "azurerm_key_vault_secret" "amuse_smtp_host" {
  name             = "amuse-smtp-host"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_smtp_host
  value_wo_version = var.amuse_smtp_host_version

  depends_on = [module.key_vault]
}

resource "azurerm_key_vault_secret" "amuse_smtp_user" {
  name             = "amuse-smtp-user"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_smtp_user
  value_wo_version = var.amuse_smtp_user_version

  depends_on = [module.key_vault]
}

resource "azurerm_key_vault_secret" "amuse_smtp_password" {
  name             = "amuse-smtp-password"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_smtp_password
  value_wo_version = var.amuse_smtp_password_version

  depends_on = [module.key_vault]
}

resource "azurerm_key_vault_secret" "amuse_rabbitmq_password" {
  name             = "amuse-rabbitmq-password"
  key_vault_id     = module.key_vault.id
  value_wo         = var.amuse_rabbitmq_password
  value_wo_version = var.amuse_rabbitmq_password_version

  depends_on = [module.key_vault]
}
