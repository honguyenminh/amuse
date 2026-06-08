# Amuse Azure platform (staging / AKS)

Terraform provisions the **stage** AKS platform only. Dev runs on an existing K3s cluster bootstrapped separately (see `../kubernetes/bootstrap/k3s/README.md`).

Application images are built and published to **GHCR** by GitHub Actions — not Azure Container Registry.

## What this stack creates

- Resource group, VNet/subnets
- Key Vault (private endpoint) + workload identity
- PostgreSQL Flexible Server (private) + connection string secret
- Application Gateway for Containers (ALB) + ALB controller (Gateway API)
- AKS cluster, Argo CD, External Secrets Operator

## Active modules (`modules/`)

| Module | Purpose |
|--------|---------|
| `resource-group` | Azure RG |
| `networking` | VNet, subnets (AKS, Postgres, KV PE, AGC) |
| `key-vault` | Secrets store + workload identity for External Secrets |
| `postgres` | Flexible Server + connection string in KV |
| `app-gw` | Application Load Balancer (AGC frontend) |
| `aks` | Staging Kubernetes cluster |
| `argocd` | Argo CD Helm release |
| `agc-controller` | Gateway API ALB controller |
| `external-secrets` | External Secrets Operator Helm release |

## Common apply errors

**AKS `VM size ... is not allowed`** — Your subscription/region may not offer newer SKUs (e.g. `Standard_D4ds_v5`). Set `aks_node_vm_size = "Standard_D4ds_v4"` in `staging.tfvars` (or another size from the error’s allowed list).

**Key Vault `403 ForbiddenByConnection`** — Key Vault was created with public access disabled while Terraform runs from your laptop. Default is now `key_vault_public_network_access_enabled = true`. Re-apply to update the vault, then secret writes succeed. AKS still uses the private endpoint.

**AKS `disableLocalAccounts can only be set on Azure AD integration enabled cluster`** — Set `aks_local_account_disabled = false` (default). Enabling `true` requires AKS-managed Entra ID on the cluster.

**AKS `Insufficient regional vcpu quota`** — `Standard_D4ds_v4` uses 4 vCPUs per node; `aks_node_min_count = 2` needs 8. On quota-tight subs, set `aks_node_min_count = 1` and `aks_node_max_count = 1`.

**AKS `maxSurge and maxUnavailable cannot both be 0`** — Keep `aks_node_upgrade_max_surge = "1"` (default). Do not set it to `"0"` unless the azurerm provider gains `max_unavailable` support.

**AGC `no matches for kind "ApplicationLoadBalancer"` / `GatewayClass` (CRD may not be installed)** — The AGC module uses `kubectl_manifest` (not `kubernetes_manifest`) for ALB/GatewayClass. Gateway API CRDs come from the official `standard-install.yaml` release manifest (there is no stable Helm repo at `kubernetes-sigs.github.io/gateway-api`). Re-run `terraform apply` if a prior run partially succeeded.

**Gateway API Helm `404` on index.yaml** — Expected; use the pinned `gateway_api_version` (default `1.2.1`) HTTP install instead.

## Prerequisites

- Azure CLI + Terraform >= 1.11
- Contributor access on the target subscription
- Cloudflare R2 buckets + API token + cover CDN custom domain (see `../cloudflare/README.md`; stored via tfvars → Key Vault)
- Remote state storage configured before team use (uncomment `backend "azurerm"` in `provider.tf`)

## Apply order

Helm/Kubernetes providers depend on AKS. Use a two-pass apply on first bootstrap:

```bash
cd infrastructure/terraform
cp environments/staging.tfvars.example environments/staging.tfvars
# Edit staging.tfvars with real subscription, secrets, R2 credentials

terraform init
terraform apply -target=module.resource-group \
  -target=module.networking \
  -target=module.key_vault \
  -target=module.postgres \
  -target=module.app-gw \
  -target=module.aks \
  -var-file=environments/staging.tfvars

terraform apply -var-file=environments/staging.tfvars
```

Subsequent applies can be a single `terraform apply`.

## Post-apply

1. Point public DNS for staging API/app/business hosts to `agc_frontend_fqdn` output.
2. Clone [amuse-deploy](https://github.com/honguyenminh/amuse-deploy) and apply `argocd/bootstrap/stage-application.yaml` (see `../kubernetes/bootstrap/aks/README.md`).
3. Create GHCR `imagePullSecret` (`ghcr-pull`) in the `amuse` namespace on AKS.
4. Ensure `DEPLOY_REPO_TOKEN` is set on the **amuse** repo (see `../kubernetes/DEPLOY_REPO.md`).

## Migrating from a state that included ACR

If you previously applied a version of this stack that created ACR, remove orphaned resources from state (after confirming nothing uses the registry), then destroy the registry in Azure if it still exists:

```bash
terraform state rm module.acr
terraform state rm azurerm_role_assignment.aks_acr_pull
# Optional: az acr delete --name <old-acr-name> --resource-group <rg> --yes
```

Remove `acr_name` from your local `staging.tfvars` if present.

## Secrets rotation

Bump the corresponding `*_version` variable and re-apply. Write-only Key Vault secrets use `value_wo_version` for rotation without reading old values from state.
