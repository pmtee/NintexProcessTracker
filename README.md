# ProcessTracker API

A production-grade C# .NET 8 REST API for automating business process lifecycle management.
Built as a portfolio project demonstrating enterprise software engineering, DevOps, and cloud skills.

## Tech Stack

| Layer | Technology |
|---|---|
| API | C# .NET 8 Minimal API |
| Database | SQLite + Entity Framework Core |
| Architecture | Repository Pattern + Dependency Injection |
| Testing | xUnit — 9 integration tests |
| Documentation | Swagger / OpenAPI |
| Containerisation | Docker (multi-stage build) |
| Orchestration | Kubernetes (Deployment, HPA, PVC) |
| CI/CD | GitHub Actions |
| Monitoring | Prometheus + Grafana |
| Cloud | AWS S3 export |
| Automation | Background service (auto-fails stalled processes) |

## Run Locally

```bash
cd ProcessTracker
dotnet run
```

Open: http://localhost:5033/swagger

## Run with Docker

```bash
docker build -t phethot/process-tracker:latest .
docker run -p 8080:8080 phethot/process-tracker:latest
```

Open: http://localhost:8080/swagger

## Run Full Stack (API + Prometheus + Grafana)

```bash
docker-compose up --build
```

| Service | URL |
|---|---|
| API + Swagger | http://localhost:8080/swagger |
| Dashboard UI | http://localhost:8080 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 |

## Run Tests

```bash
cd ProcessTracker.Tests
dotnet test --verbosity normal
```

Expected: 9/9 passing

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | /processes | Get all processes |
| GET | /processes/{id} | Get by ID |
| POST | /processes | Create new process |
| PUT | /processes/{id} | Update status |
| DELETE | /processes/{id} | Delete process |
| GET | /processes/{id}/audit | Full audit trail |
| GET | /audit | All audit logs |
| POST | /processes/export | Export to JSON / S3 |
| GET | /stats | Live process counts |
| GET | /health | Health check (K8s probe) |

## Deploy to Kubernetes

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/pvc.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/hpa.yaml

# Watch pods come up
kubectl get pods -n processtracker -w

# Prove self-healing
kubectl delete pod -l app=processtracker -n processtracker
kubectl get pods -n processtracker -w
```

## Author

**Phetho Mogotle Tlaka**
[LinkedIn](https://linkedin.com/in/phetho-tlaka) · [GitHub](https://github.com/pmtee) · [Portfolio](https://pmtee.github.io)
