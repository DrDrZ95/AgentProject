/**
 * HTTP Network Core (Based on Axios)
 * 
 * 设计模式: 
 * 1. 单例/工厂模式 (Singleton/Factory): 创建统一的 axios 实例。
 * 2. 拦截器模式 (Interceptor): 统一处理请求头注入 (Token) 和响应错误处理。
 * 
 * 作用:
 * 它是整个应用与 RESTful API 交互的基础设施。
 */

import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse, AxiosError } from 'axios';
import { authService } from '../services/auth';

// 定义标准响应结构
interface ApiResponse<T = any> {
  code: number;
  data: T;
  message: string;
}

// 全局配置
const config: AxiosRequestConfig = {
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://api.opsnexus.io/v1',
  timeout: 10000, // 10秒超时
  headers: {
    'Content-Type': 'application/json',
  },
};

class Http {
  private instance: AxiosInstance;

  constructor(config: AxiosRequestConfig) {
    this.instance = axios.create(config);

    // --- 请求拦截器 ---
    // 在发送请求之前做些什么：注入 Token，添加 Trace ID 等
    this.instance.interceptors.request.use(
      (config) => {
        const token = authService.getToken();
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // --- 响应拦截器 ---
    // 对响应数据做点什么：解包 Data，统一处理错误码
    this.instance.interceptors.response.use(
      (response: AxiosResponse<ApiResponse>) => {
        // 假设后端标准返回 { code: 200, data: ..., message: ... }
        // 这里直接返回 data 字段，简化调用层逻辑
        const res = response.data;
        
        // 模拟环境兼容：如果 res.code 不存在，说明可能是直接返回了数据 (Mock)
        if (res.code === undefined) {
             return res as any;
        }

        if (res.code !== 200) {
          // 处理业务错误
          console.error(`[API Error] ${res.message}`);
          return Promise.reject(new Error(res.message || 'Error'));
        }
        return res.data;
      },
      (error: AxiosError) => {
        // 处理 HTTP 状态码错误
        if (error.response && error.response.status === 401) {
          console.warn('Token expired, redirecting to login...');
          authService.logout();
        }
        return Promise.reject(error);
      }
    );
  }

  // 通用 GET 方法
  public async get<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.get<T>(url, config);
    return response as unknown as T;
  }

  // 通用 POST 方法
  public async post<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.post<T>(url, data, config);
    return response as unknown as T;
  }

  // 通用 PUT 方法
  public async put<T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.put<T>(url, data, config);
    return response as unknown as T;
  }

  // 通用 DELETE 方法
  public async delete<T = any>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.instance.delete<T>(url, config);
    return response as unknown as T;
  }
}

// 导出单例
export const http = new Http(config);
