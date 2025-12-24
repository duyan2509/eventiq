import React, { createContext, useContext, useState, useEffect } from 'react';
import {authAPI} from '../services/api';

const AuthContext = createContext();

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  // Khởi tạo từ localStorage để giữ phiên và roles
  const [user, setUser] = useState(() => {
    const stored = localStorage.getItem('user');
    return stored ? JSON.parse(stored) : null;
  });
  const [loading, setLoading] = useState(false);
  const [token, setToken] = useState(() => localStorage.getItem('token'));

  const decodeRolesFromToken = (jwtToken) => {
    try {
      const payload = JSON.parse(atob(jwtToken.split('.')[1]));
      const roles =
        payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
        payload?.role ||
        payload?.roles ||
        [];
      if (Array.isArray(roles)) return roles;
      if (roles) return [roles];
    } catch (_) {}
    return [];
  };

  useEffect(() => {
    if (token) {
      setLoading(true);
      // getMe dùng Authorization header nên không cần truyền token
      authAPI.getMe()
        .then((res) => {
          // Bổ sung roles từ token nếu API không trả
          const rolesFromToken = token ? decodeRolesFromToken(token) : [];
          const userWithRoles = rolesFromToken.length && !res.roles
            ? { ...res, roles: rolesFromToken }
            : res;
          setUser(userWithRoles);
          localStorage.setItem('user', JSON.stringify(userWithRoles));
        })
        .catch(() => {
          setUser(null);
          setToken(null);
          localStorage.removeItem('token');
          localStorage.removeItem('user');
        })
        .finally(() => setLoading(false));
    } else {
      setUser(null);
      setLoading(false);
      localStorage.removeItem('user');
    }
  }, [token]);

  const login = async (credentials) => {
    setLoading(true);
    try {
      const res = await authAPI.login(credentials);
      setToken(res.token);
      localStorage.setItem('token', res.token);
      const rolesFromToken = res.token ? decodeRolesFromToken(res.token) : [];
      // Nếu backend trả user kèm roles, dùng ngay; nếu không, gán roles từ token
      const nextUser = res.user
        ? (res.user.roles ? res.user : { ...res.user, roles: rolesFromToken })
        : (rolesFromToken.length ? { roles: rolesFromToken } : null);
      setUser(nextUser);
      if (nextUser) localStorage.setItem('user', JSON.stringify(nextUser));
      setLoading(false);
      return { success: true };
    } catch (error) {
      setLoading(false);
      return {
        success: false,
        message: error?.response?.data?.message || 'Login failed',
      };
    }
  };

  const register = async (userData) => {
    setLoading(true);
    try {
      const res = await authAPI.register(userData);
      setLoading(false);
      return { success: true, data: res };
    } catch (error) {
      setLoading(false);
      return {
        success: false,
        message: error?.response?.data?.message || 'Registration failed',
      };
    }
  };

  const logout = () => {
    localStorage.removeItem('token');
    setToken(null);
    setUser(null);
  };

  const forgotPassword = async (email) => {
    setLoading(true);
    try {
      await authAPI.requestReset({ email });
      setLoading(false);
      return { success: true };
    } catch (error) {
      setLoading(false);
      return {
        success: false,
        message: error?.response?.data?.message || 'Failed to send email',
      };
    }
  };

  const resetPassword = async (resetData) => {
    setLoading(true);
    try {
      await authAPI.confirmReset(resetData);
      setLoading(false);
      return { success: true };
    } catch (error) {
      setLoading(false);
      return {
        success: false,
        message: error?.response?.data?.message || 'Password reset failed',
      };
    }
  };

  const value = {
    user,
    loading,
    isAuthenticated: !!user,
    login,
    register,
    logout,
    forgotPassword,
    resetPassword,
    setToken
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};
