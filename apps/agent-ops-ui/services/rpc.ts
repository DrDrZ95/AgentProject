
/**
 * RPC Client - 远程过程调用服务 (Simulation)
 * 
 * 设计模式: Proxy Pattern (代理模式)
 * 作用:
 * 模拟 gRPC-Web 或 JSON-RPC 风格的调用。
 * 通常用于高性能、低延迟或强类型的内部服务通讯 (如模型推理、日志流控制)。
 * 
 * 区别于 REST (http.ts):
 * REST 面向资源 (Resource-Oriented)，RPC 面向动作 (Action-Oriented)。
 */

interface RPCRequest {
  service: string; // 服务名 (e.g., 'InferenceService')
  method: string;  // 方法名 (e.g., 'Predict')
  payload: any;    // 参数
}

interface RPCResponse<T> {
  result?: T;
  error?: {
    code: number;
    message: string;
  };
}

class RPCClient {
  private endpoint: string;

  constructor() {
    this.endpoint = import.meta.env.VITE_RPC_URL || 'https://rpc.agentproject.io';
  }

  /**
   * 统一 RPC 调用入口
   * @param service 服务名称
   * @param method 方法名称
   * @param payload请求参数
   */
  public async call<T>(service: string, method: string, payload: any): Promise<T> {
    console.debug(`[RPC Call] ${service}.${method}`, payload);

    // 模拟网络延迟
    await new Promise(resolve => setTimeout(resolve, 800));

    // 模拟不同服务的响应
    if (service === 'InferenceService' && method === 'Predict') {
        return this.mockInference(payload) as unknown as T;
    }
    
    if (service === 'LogService' && method === 'GetTail') {
        return { logs: ['Log line 1...', 'Log line 2...'] } as unknown as T;
    }

    throw new Error(`RPC Method Not Found: ${service}.${method}`);
  }

  /**
   * 模拟推理服务返回
   */
  private mockInference(payload: any) {
     return {
         prediction: 'Class_A',
         confidence: 0.98,
         latency_ms: 45
     };
  }
}

// 导出单例
export const rpcClient = new RPCClient();
