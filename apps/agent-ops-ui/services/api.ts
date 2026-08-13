
import { authService } from './auth';

/**
 * API Client - 统一网络请求层
 * 
 * 封装了 fetch API，实现了类似于 Axios 的拦截器机制。
 * 1. 请求拦截: 自动注入 Bearer Token。
 * 2. 响应拦截: 统一处理 401 未授权、500 服务器错误等。
 * 3. Mock 机制: 在无后端环境下模拟数据返回。
 */

interface RequestConfig extends RequestInit {
  mockDelay?: number; // 模拟延迟时间 (ms)
  mockData?: any;     // 模拟返回数据
}

class ApiClient {
  private baseURL: string;

  constructor(baseURL: string) {
    this.baseURL = baseURL;
  }

  /**
   * 核心请求方法
   */
  private async request<T>(endpoint: string, config: RequestConfig = {}): Promise<T> {
    const { mockDelay, mockData, ...nativeConfig } = config;

    // --- 请求拦截器逻辑 Start ---
    const token = authService.getToken();
    const headers = {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      ...nativeConfig.headers,
    };
    // --- 请求拦截器逻辑 End ---

    // 模拟 Mock 数据返回 (用于演示)
    if (mockData !== undefined) {
      return new Promise((resolve) => {
        setTimeout(() => {
          console.debug(`[Mock API] ${config.method || 'GET'} ${endpoint}`, mockData);
          resolve(mockData);
        }, mockDelay || 500);
      });
    }

    // 真实网络请求
    try {
      const response = await fetch(`${this.baseURL}${endpoint}`, {
        ...nativeConfig,
        headers,
      });

      // --- 响应拦截器逻辑 Start ---
      if (response.status === 401) {
        console.warn('[API] Token expired or unauthorized. Redirecting to login...');
        authService.logout();
        throw new Error('Unauthorized');
      }

      if (!response.ok) {
        const errorBody = await response.json().catch(() => ({}));
        throw new Error(errorBody.message || `HTTP Error ${response.status}`);
      }
      // --- 响应拦截器逻辑 End ---

      return await response.json();
    } catch (error) {
      console.error('[API Error]', error);
      throw error;
    }
  }

  // HTTP 方法封装
  public get<T>(url: string, config?: RequestConfig) {
    return this.request<T>(url, { ...config, method: 'GET' });
  }

  public post<T>(url: string, data?: any, config?: RequestConfig) {
    return this.request<T>(url, { ...config, method: 'POST', body: JSON.stringify(data) });
  }

  public put<T>(url: string, data?: any, config?: RequestConfig) {
    return this.request<T>(url, { ...config, method: 'PUT', body: JSON.stringify(data) });
  }

  public delete<T>(url: string, config?: RequestConfig) {
    return this.request<T>(url, { ...config, method: 'DELETE' });
  }
}

// 导出实例，baseURL 指向网关地址
export const api = new ApiClient(import.meta.env.VITE_API_BASE_URL || 'https://api.agentproject.io/v1');
