# AgentProject

AgentProject 是一个基于 .NET 8 和 React 19 的模块化 AI Agent 平台，在同一仓库中整合了大模型编排、检索增强生成（RAG）、工作流、工具调用、实时通信与运维遥测。

[English](./README.md)

> 项目状态：持续开发中。源码项目目前可正常构建，但部分基础设施和集成模块仍依赖特定环境、外部服务与凭据；生产部署前请逐项验证相关配置和文档。

## 主要组件

| 组件 | 路径 | 作用 |
| --- | --- | --- |
| Agent API | `apps/agent-api/Agent.Api` | ASP.NET Core API、认证、工作流、RAG、SignalR、MCP 与 Semantic Kernel 集成 |
| Application/Core | `apps/agent-api/Agent.Application`、`Agent.Core` | 应用服务、领域模型、数据访问、工具、记忆与编排 |
| MCP 网关 | `apps/agent-api/Agent.McpGateway` | MCP 客户端抽象与外部工具适配 |
| Agent UI | `apps/agent-ui` | 主要 React/Vite Agent 界面 |
| 运维 API | `apps/agent-ops/Agent.Metering` | 计量、限流、遥测与 eBPF 相关运维接口 |
| 运维 UI | `apps/agent-ops-ui` | React/Vite 运维与 MLOps 控制台 |
| Agent 工具 | `apps/agent-tools` | Python 示例与 RAG 工具 |
| 基础设施 | `infra` | Docker、Helm、Kubernetes、监控与 CI/CD 示例 |
| LLM 工具 | `llm` | 模型服务与微调示例 |

## 核心能力

- 基于 Microsoft Semantic Kernel 的多模型编排
- ChromaDB 检索增强生成集成
- 工作流规划、执行、状态管理与 SignalR 通知
- MCP 客户端和可扩展工具注册表
- ASP.NET Core Identity、JWT 认证与权限授权
- OpenTelemetry、Prometheus、Hangfire、Redis、PostgreSQL 与运维计量
- 面向不同环境的 Docker、Kubernetes 和 Helm 配置资产

## 技术栈

- .NET 8 / ASP.NET Core / Entity Framework Core
- React 19 / TypeScript 5 / Vite 6 / pnpm 10
- PostgreSQL、Redis、ChromaDB
- Semantic Kernel 与 Model Context Protocol
- OpenTelemetry、Prometheus、Grafana、Jaeger

## 仓库结构

```text
AgentProject/
├── apps/
│   ├── agent-api/       # 主 .NET API 及相关类库
│   ├── agent-ui/        # 主 React 界面
│   ├── agent-ops/       # 计量与运维 API
│   ├── agent-ops-ui/    # 运维界面
│   └── agent-tools/     # Python 工具与示例
├── docs/                # 集成与部署文档
├── infra/               # Docker、Kubernetes、Helm、CI/CD
├── llm/                 # 微调与模型服务工具
├── skills/              # Agent 提示词与技能资源
└── test/                # .NET 测试项目
```

## 环境要求

- .NET SDK 8.0 或更高版本，并具备 .NET 8 目标框架支持
- Node.js 22.12 或更高版本
- Corepack 与 pnpm 10.33.0（两个前端项目均已固定版本）
- 仅在需要外部基础设施时安装 Docker 和 Docker Compose

## 从源码构建

下列命令只编译代码，不启动服务，也不会连接数据库或其他外部系统。

### 后端

```bash
dotnet build apps/agent-api/Agent.Api/Agent.Api.csproj --configuration Release
dotnet build apps/agent-ops/Agent.Metering/Agent.Metering.csproj --configuration Release
```

### 主界面

```bash
cd apps/agent-ui
corepack enable
pnpm install --frozen-lockfile
pnpm run lint
pnpm run build
```

### 运维界面

```bash
cd apps/agent-ops-ui
corepack enable
pnpm install --frozen-lockfile
pnpm run lint
pnpm run build
```

两个前端的生产资源分别输出到各自的 `dist/` 目录。

## 配置

不要把凭据提交到 Git。请使用 .NET User Secrets、环境变量或部署平台提供的 Secret Manager。

API 必需配置：

| 环境变量 | 说明 |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL 连接字符串 |
| `JwtSettings__SecretKey` | 至少包含 32 字节的 JWT 签名密钥 |
| `SemanticKernel__OpenAIApiKey` | 使用 OpenAI 时的 API Key |
| `SemanticKernel__AzureOpenAIEndpoint` | 使用 Azure OpenAI 时的服务地址 |
| `SemanticKernel__AzureOpenAIApiKey` | 使用 Azure OpenAI 时的 API Key |

OpenAI 与 Azure OpenAI 配置二选一。模型名称可通过 `SemanticKernel__ChatModel` 与 `SemanticKernel__EmbeddingModel` 覆盖。

默认管理员初始化默认关闭。如需启用，必须从外部提供凭据：

```text
Identity__SeedAdmin__Enabled=true
Identity__SeedAdmin__Email=admin@example.com
Identity__SeedAdmin__Password=<高强度密码>
```

前端环境变量属于公开的构建期配置，禁止存放任何密钥：

```text
VITE_API_BASE_URL=http://localhost:5069/api/v1
VITE_RPC_URL=http://localhost:5069
```

模型提供商密钥只保留在 `Agent.Api`，不会打包进浏览器 JavaScript。

## 本地开发

配置 PostgreSQL、JWT 与任一模型提供商后，可启动 API：

```bash
dotnet run --project apps/agent-api/Agent.Api/Agent.Api.csproj
```

默认开发地址为 `http://localhost:5069` 与 `https://localhost:7185`。在任一前端目录执行 `pnpm dev` 即可启动界面；两个前端默认都使用 `http://127.0.0.1:3000`，同时启动时请为其中一个指定其他端口：

```bash
pnpm dev -- --port 3001
```

## 默认安全策略

- Sandbox Terminal 接口要求 `system.admin` 权限。
- 系统信息接口不返回宿主进程环境变量。
- Hangfire Dashboard 仅允许 `Administrator` 角色的已认证用户访问。
- JWT 签名密钥和数据库凭据不再提供源码内置回退值。
- 默认管理员创建为显式开启，并要求外部凭据。
- Vite 不会把模型提供商密钥注入客户端代码。

终端模块会在宿主进程执行命令。部署时必须使用可靠的隔离环境，并严格控制 `system.admin` 权限。

## 基础设施与部署

`infra/docker/docker-compose.yml` 当前只编排 PostgreSQL、Redis、ChromaDB、Prometheus、Grafana、Jaeger 与 Nginx 等支撑组件，并不是包含 API 和 UI 的完整应用栈。Docker、Kubernetes 与 Helm 文件应视为针对具体环境的模板，生产使用前必须验证。

常用文档：

- [环境配置](./docs/environment_setup.zh_CN.md)
- [API 文档](./docs/api_documentation.zh_CN.md)
- [Docker 快速开始](./docs/docker_quickstart.zh_CN.md)
- [工作流集成](./docs/workflow_integration.md)
- [MCP 集成指南](./docs/mcp_integration_guide.zh_CN.md)
- [RAG 提示词工程](./docs/rag_prompt_engineering.md)
- [Prometheus 集成](./docs/prometheus_integration.zh_CN.md)
- [vLLM 集成](./docs/vllm_integration.zh_CN.md)

## 开发约定

- 修改应限制在所属应用或架构层内。
- 前端依赖变化时必须同步提交对应的 `pnpm-lock.yaml`。
- `VITE_*` 变量会暴露给浏览器，严禁存放密钥。
- 提交前至少构建受影响的后端与前端项目。

## 许可证

仓库目前没有提供许可证文件。在仓库所有者补充许可证前，默认适用标准版权限制。
