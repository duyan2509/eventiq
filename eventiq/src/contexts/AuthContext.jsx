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
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(false);
  const [token, setToken] = useState(() => localStorage.getItem('token'));

  useEffect(() => {
    if (token) {
      setLoading(true);
      authAPI.getMe(token)
        .then((res) => {
          setUser(res);
          localStorage.setItem('user', JSON.stringify(res));
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
      setUser(res.user || null);
      if (res.user) localStorage.setItem('user', JSON.stringify(res.user));
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
