
/**
 * Sends terminal-assistant prompts through Agent.Api so provider credentials
 * remain on the server and are never bundled into browser JavaScript.
 */
export const analyzeCommand = async (command: string, context: string): Promise<string> => {
  const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5069/api/v1')
    .replace(/\/+$/, '');

  try {
    const response = await fetch(`${apiBaseUrl}/SemanticKernel/chat/completion`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        prompt: `The user typed: "${command}". Current context: ${context}.`,
        systemMessage: 'You are a concise MLOps terminal assistant. Return plain-text guidance and never claim that a command was executed.'
      })
    });

    if (!response.ok) {
      throw new Error(`Agent.Api returned HTTP ${response.status}`);
    }

    const payload = await response.json() as {
      data?: { response?: string };
      message?: string;
    };

    return payload.data?.response || payload.message || 'No output generated.';
  } catch (error) {
    console.error('Agent.Api assistant request failed:', error);
    return 'AgentProject AI is unavailable. Check VITE_API_BASE_URL and the Agent.Api service.';
  }
};
