<div align="center">
<img width="1200" height="475" alt="GHBanner" src="https://github.com/user-attachments/assets/0aa67016-6eaf-458a-adb2-6e31a0763ed6" />
</div>

# Agent Operations UI

The React/Vite operations and MLOps dashboard for AgentProject. See the [repository README](../../README.md) for architecture and backend configuration.

## Run Locally

**Prerequisites:** Node.js 22.12+ and Corepack.


1. Install dependencies: `corepack enable && pnpm install --frozen-lockfile`
2. Set public endpoints when needed: `VITE_API_BASE_URL` and `VITE_RPC_URL`
3. Validate: `pnpm run lint && pnpm run build`
4. Run locally: `pnpm run dev`

The terminal assistant calls `Agent.Api`; no provider API key is embedded in the browser bundle.
