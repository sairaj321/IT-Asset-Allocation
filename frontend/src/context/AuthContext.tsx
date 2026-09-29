import React, { createContext, useContext, useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { User, LoginResponse } from '../types';
import { authService } from '../api/services';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
  hasPermission: (permission: string) => boolean;
  isManager: boolean;
  isExecutive: boolean;
  isEmployee: boolean;
  getDefaultRoute: () => string;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(() => {
    const saved = localStorage.getItem('its_user');
    return saved ? JSON.parse(saved) : null;
  });
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('its_access_token'));
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const navigate = useNavigate();

  useEffect(() => {
    const checkAuth = async () => {
      const storedToken = localStorage.getItem('its_access_token');
      if (storedToken) {
        try {
          const freshUser = await authService.getCurrentUser();
          setUser(freshUser);
          localStorage.setItem('its_user', JSON.stringify(freshUser));
        } catch {
          // Token might be invalid or refreshed
        }
      }
      setIsLoading(false);
    };

    checkAuth();
  }, []);

  const login = async (username: string, password: string) => {
    const res: LoginResponse = await authService.login({ username, password });
    localStorage.setItem('its_access_token', res.accessToken);
    localStorage.setItem('its_refresh_token', res.refreshToken);
    localStorage.setItem('its_user', JSON.stringify(res.user));
    setToken(res.accessToken);
    setUser(res.user);

    if (res.user.mustChangePassword) {
      navigate('/change-password');
      return;
    }

    if (res.user.roles.includes('ITS_MANAGER')) {
      navigate('/manager/dashboard');
    } else if (res.user.roles.includes('ITS_EXECUTIVE')) {
      navigate('/executive/dashboard');
    } else {
      navigate('/employee/dashboard');
    }
  };

  const logout = async () => {
    try {
      await authService.logout();
    } catch {
      // Ignore network errors on logout
    } finally {
      localStorage.removeItem('its_access_token');
      localStorage.removeItem('its_refresh_token');
      localStorage.removeItem('its_user');
      setToken(null);
      setUser(null);
      navigate('/login');
    }
  };

  const hasRole = (role: string) => user?.roles.includes(role) ?? false;
  const hasPermission = (permission: string) => {
    if (user?.roles.includes('ITS_MANAGER')) return true;
    return user?.permissions.includes(permission) ?? false;
  };

  const isManager = hasRole('ITS_MANAGER');
  const isExecutive = hasRole('ITS_EXECUTIVE');
  const isEmployee = hasRole('EMPLOYEE');

  const getDefaultRoute = () => {
    if (isManager) return '/manager/dashboard';
    if (isExecutive) return '/executive/dashboard';
    return '/employee/dashboard';
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isLoading,
        login,
        logout,
        hasRole,
        hasPermission,
        isManager,
        isExecutive,
        isEmployee,
        getDefaultRoute,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
