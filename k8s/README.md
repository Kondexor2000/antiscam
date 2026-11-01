# Kubernetes

Manifesty uruchamiają oba API, SQL Server oraz Redis. MongoDB pozostaje
opcjonalny i domyślnie wyłączony. SQL Server, Redis i MongoDB używają PVC;
ich kopie zapasowe i aktualizacje należy obsługiwać niezależnie.

## Wdrożenie

Najpierw zbuduj obrazy i udostępnij je klastrowi pod nazwami używanymi w
`kustomization.yaml` (albo zmień te nazwy na własny rejestr):

```powershell
docker build -f ci/dotnet/Dockerfile -t antiscam-blog-api:local .
docker build -f ci/python/Dockerfile -t antiscam-python-api:local .
```

Załaduj obrazy do lokalnego klastra (np. `kind load docker-image ...` albo
`minikube image load ...`). Utwórz sekret z własnymi wartościami; nie zapisuj
wartości sekretów w manifestach:

```powershell
kubectl apply -f k8s/namespace.yaml
kubectl -n antiscam create secret generic antiscam-secrets `
  --from-literal=MSSQL_SA_PASSWORD='WstawSilneHaslo123!' `
  --from-literal=ANTISCAM_CONTROL_PLANE_KEY='wstaw-losowy-sekret-min-32-bajty'
kubectl apply -k k8s
kubectl -n antiscam get pods,services
```

Użyj `kubectl port-forward -n antiscam service/blog-api 5000:8080` oraz
`kubectl port-forward -n antiscam service/python-api 8000:8000`, aby dostać się
do API lokalnie. W produkcji obrazy powinny być przypięte do wersji lub digestu,
a wartości sekretów dostarczane przez zewnętrzny menedżer sekretów. Klaster musi
zapewniać dynamiczne PVC; pojemności można zmienić w plikach StatefulSet.

Manifesty SQL Server zakładają obraz Linux SQL Server 2022 z obsługą
`fsGroup: 10001`. Zastosowanie manifestów nie konfiguruje ingress ani TLS.

## Opcjonalne MongoDB

Włącz MongoDB, dodając zasób do `kustomization.yaml`:

```yaml
resources:
  - mongodb.yaml
```

Następnie w `blog-api.yaml` zmień `NoSql__Enabled` na `"true"` i zastosuj
ponownie `kubectl apply -k k8s`.
