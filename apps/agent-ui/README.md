<div align="center">
<img width="1200" height="475" alt="GHBanner" src="https://github.com/user-attachments/assets/0aa67016-6eaf-458a-adb2-6e31a0763ed6" />
</div>

# Agent UI

The main React/Vite interface for AgentProject. See the [repository README](../../README.md) for architecture, configuration, and backend requirements.

## Run Locally

**Prerequisites:** Node.js 22.12+ and Corepack.


1. Install dependencies: `corepack enable && pnpm install --frozen-lockfile`
2. Optionally set the public API address: `VITE_API_BASE_URL=http://localhost:5069/api/v1`
3. Validate: `pnpm run lint && pnpm run build`
4. Run locally: `pnpm run dev`

Never put provider credentials in `VITE_*` variables; they are bundled into browser code. LLM credentials belong in `Agent.Api`.
