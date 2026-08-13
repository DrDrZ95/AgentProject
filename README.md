# AgentProject

AgentProject is a modular AI-agent platform built with .NET 8 and React 19. It combines LLM orchestration, retrieval-augmented generation, workflows, tool integration, real-time updates, and operational telemetry in one repository.

[简体中文](./README.zh_CN.md)

> Project status: active development. The source projects build successfully, while several infrastructure and integration modules still require environment-specific services and credentials. Review the relevant documentation before production deployment.

## Components

| Component | Path | Purpose |
| --- | --- | --- |
| Agent API | `apps/agent-api/Agent.Api` | ASP.NET Core API, authentication, workflows, RAG, SignalR, MCP, and Semantic Kernel integration |
| Application/Core | `apps/agent-api/Agent.Application`, `Agent.Core` | Application services, domain models, persistence, tools, memory, and orchestration |
| MCP gateway | `apps/agent-api/Agent.McpGateway` | MCP client abstractions and external tool adapters |
| Agent UI | `apps/agent-ui` | Main React/Vite agent experience |
| Operations API | `apps/agent-ops/Agent.Metering` | Metering, rate limiting, telemetry, and eBPF-oriented operations APIs |
| Operations UI | `apps/agent-ops-ui` | React/Vite operations and MLOps dashboard |
| Agent tools | `apps/agent-tools` | Python examples and RAG utilities |
| Infrastructure | `infra` | Docker, Helm, Kubernetes, monitoring, and CI/CD examples |
| LLM utilities | `llm` | Model serving and fine-tuning examples |

## Main capabilities

- Multi-provider LLM orchestration through Microsoft Semantic Kernel
- Retrieval-augmented generation with ChromaDB integration
- Workflow planning, execution, state management, and SignalR notifications
- MCP clients and a tool registry for extensible agent actions
- ASP.NET Core Identity, JWT authentication, and permission-based authorization
- OpenTelemetry, Prometheus, Hangfire, Redis, PostgreSQL, and operational metering
- Docker, Kubernetes, and Helm assets for environment-specific deployment work

## Technology

- .NET 8 / ASP.NET Core / Entity Framework Core
- React 19 / TypeScript 5 / Vite 6 / pnpm 10
- PostgreSQL, Redis, and ChromaDB
- Semantic Kernel and Model Context Protocol
- OpenTelemetry, Prometheus, Grafana, and Jaeger

## Repository layout

```text
AgentProject/
├── apps/
│   ├── agent-api/       # Main .NET API and supporting libraries
│   ├── agent-ui/        # Main React UI
│   ├── agent-ops/       # Metering/operations API
│   ├── agent-ops-ui/    # Operations UI
│   └── agent-tools/     # Python tools and examples
├── docs/                # Integration and deployment guides
├── infra/               # Docker, Kubernetes, Helm, and CI/CD assets
├── llm/                 # Fine-tuning and model-serving utilities
├── skills/              # Agent prompt and skill resources
└── test/                # .NET test projects
```

## Prerequisites

- .NET SDK 8.0 or newer with .NET 8 targeting support
- Node.js 22.12 or newer
- Corepack with pnpm 10.33.0 (the version is pinned in each frontend package)
- Docker and Docker Compose only when external infrastructure is needed

## Build from source

The following commands compile the repository without starting services or connecting to external systems.

### Backend

```bash
dotnet build apps/agent-api/Agent.Api/Agent.Api.csproj --configuration Release
dotnet build apps/agent-ops/Agent.Metering/Agent.Metering.csproj --configuration Release
```

### Main UI

```bash
cd apps/agent-ui
corepack enable
pnpm install --frozen-lockfile
pnpm run lint
pnpm run build
```

### Operations UI

```bash
cd apps/agent-ops-ui
corepack enable
pnpm install --frozen-lockfile
pnpm run lint
pnpm run build
```

Production frontend assets are written to each application's `dist/` directory.

## Configuration

Do not commit credentials. Use .NET user secrets, environment variables, or the secret manager provided by the deployment platform.

Required API settings:

| Environment variable | Description |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `JwtSettings__SecretKey` | JWT signing key containing at least 32 bytes |
| `SemanticKernel__OpenAIApiKey` | OpenAI key when using OpenAI |
| `SemanticKernel__AzureOpenAIEndpoint` | Azure OpenAI endpoint when using Azure OpenAI |
| `SemanticKernel__AzureOpenAIApiKey` | Azure OpenAI key when using Azure OpenAI |

Choose either OpenAI or Azure OpenAI configuration. Model names can be overridden with `SemanticKernel__ChatModel` and `SemanticKernel__EmbeddingModel`.

Optional administrator seed settings are disabled by default:

```text
Identity__SeedAdmin__Enabled=true
Identity__SeedAdmin__Email=admin@example.com
Identity__SeedAdmin__Password=<strong password>
```

Frontend variables are public build-time values and must never contain secrets:

```text
VITE_API_BASE_URL=http://localhost:5069/api/v1
VITE_RPC_URL=http://localhost:5069
```

Provider API keys remain in `Agent.Api`; they are not embedded into browser bundles.

## Local development

After configuring PostgreSQL, JWT, and an LLM provider, start the API:

```bash
dotnet run --project apps/agent-api/Agent.Api/Agent.Api.csproj
```

The default development endpoints are `http://localhost:5069` and `https://localhost:7185`. Start either frontend with `pnpm dev`; both default to `http://127.0.0.1:3000`, so select another port when running them together:

```bash
pnpm dev -- --port 3001
```

## Security defaults

- Sandbox terminal endpoints require the `system.admin` permission.
- Sandbox system information does not expose host environment variables.
- Hangfire Dashboard access is restricted to authenticated users in the `Administrator` role.
- JWT signing keys and database credentials have no source-controlled fallback.
- Default administrator creation is opt-in and requires externally supplied credentials.
- Vite configuration does not inject LLM provider keys into client JavaScript.

The terminal feature executes commands on its host process. Deploy it only inside an appropriately isolated environment and grant `system.admin` sparingly.

## Infrastructure and deployment

`infra/docker/docker-compose.yml` currently orchestrates supporting infrastructure such as PostgreSQL, Redis, ChromaDB, Prometheus, Grafana, Jaeger, and Nginx. It is not a complete API-and-UI application stack. Treat the Docker, Kubernetes, and Helm assets as environment-specific templates and validate them before production use.

Useful guides:

- [Environment setup](./docs/environment_setup.md)
- [API documentation](./docs/api_documentation.md)
- [Docker quick start](./docs/docker_quickstart.md)
- [Workflow integration](./docs/workflow_integration.md)
- [MCP integration guide](./docs/mcp_integration_guide.zh_CN.md)
- [RAG prompt engineering](./docs/rag_prompt_engineering.md)
- [Observability with Prometheus](./docs/prometheus_integration.md)
- [vLLM integration](./docs/vllm_integration.md)

## Development notes

- Keep changes scoped to the owning application or layer.
- Update and commit the relevant `pnpm-lock.yaml` whenever a frontend dependency changes.
- Keep secrets out of `VITE_*` variables because Vite exposes them to the browser.
- Build the affected backend and frontend projects before submitting changes.

## License

This repository does not currently include a license file. Unless the owner adds one, standard copyright restrictions apply.
