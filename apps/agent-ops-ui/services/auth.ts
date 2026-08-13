
/**
 * Auth Service - 身份认证与用户会话管理
 * 
 * 设计模式: Singleton (单例模式)
 * 作用: 
 * 1. 管理 JWT Token 的生命周期 (存储、读取、清除)。
 * 2. 提供登录 (Login) 和登出 (Logout) 的业务逻辑封装。
 * 3. 作为全局用户状态的 Source of Truth。
 */

import { http } from '../utils/http';
import { encrypt, decrypt } from '../utils/crypto';

export interface UserProfile {
  id: string;
  username: string;
  role: 'admin' | 'editor' | 'viewer';
  avatar?: string;
}

interface AuthResponse {
  token: string;
  refreshToken: string;
  user: UserProfile;
  expiresIn: number;
}

class AuthService {
  private static instance: AuthService;
  private tokenKey = 'agentproject_access_token';
  private userKey = 'agentproject_user_profile';

  private constructor() {}

  public static getInstance(): AuthService {
    if (!AuthService.instance) {
      AuthService.instance = new AuthService();
    }
    return AuthService.instance;
  }

  /**
   * 登录接口
   * @param username 用户名
   * @param password 密码
   * @returns Promise<UserProfile>
   * 
   * 设计说明:
   * 1. 使用 RSA 公钥加密密码，防止明文传输。
   * 2. 模拟后端解密验证过程。
   */
  public async login(username: string, password: string): Promise<UserProfile> {
    // 1. 前端加密 (RSA Encryption)
    const encryptedPassword = encrypt(password);

    // 2. 发送请求 (Mock Network Request)
    return new Promise((resolve, reject) => {
      setTimeout(() => {
        // 3. 后端解密验证 (Mock Server Logic)
        const decryptedPassword = decrypt(encryptedPassword);
        
        if (username === 'admin' && decryptedPassword === 'password') {
          const mockResponse: AuthResponse = {
            token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock.signature',
            refreshToken: 'mock_refresh_token_12345',
            user: {
              id: 'u_001',
              username: 'Admin User',
              role: 'admin',
              avatar: 'https://github.com/shadcn.png'
            },
            expiresIn: 3600
          };
          this.setSession(mockResponse);
          resolve(mockResponse.user);
        } else {
          reject(new Error('Invalid credentials'));
        }
      }, 800);
    });
  }

  public logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    window.location.reload();
  }

  private setSession(authResult: AuthResponse): void {
    localStorage.setItem(this.tokenKey, authResult.token);
    localStorage.setItem(this.userKey, JSON.stringify(authResult.user));
  }

  public getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  public getUser(): UserProfile | null {
    const userStr = localStorage.getItem(this.userKey);
    return userStr ? JSON.parse(userStr) : null;
  }

  public isAuthenticated(): boolean {
    return !!this.getToken();
  }
}

export const authService = AuthService.getInstance();
