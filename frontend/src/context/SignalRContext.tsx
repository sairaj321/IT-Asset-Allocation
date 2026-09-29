import React, { createContext, useContext, useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAuth } from './AuthContext';
import { NotificationItem } from '../types';
import { notificationService } from '../api/services';

interface SignalRContextType {
  notifications: NotificationItem[];
  unreadCount: number;
  markAsRead: (id: number) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  refreshNotifications: () => Promise<void>;
  latestToast: NotificationItem | null;
  clearToast: () => void;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

export const SignalRProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, token } = useAuth();
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [latestToast, setLatestToast] = useState<NotificationItem | null>(null);

  const refreshNotifications = async () => {
    if (user && token) {
      try {
        const list = await notificationService.getNotifications();
        setNotifications(list);
      } catch {
        // Fallback silently
      }
    }
  };

  useEffect(() => {
    if (user && token) {
      refreshNotifications();
    } else {
      setNotifications([]);
    }
  }, [user, token]);

  useEffect(() => {
    if (!token || !user) return;

    const HUB_URL = import.meta.env.VITE_HUB_URL || 'http://localhost:5000/hubs/notifications';

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveNotification', (notif: NotificationItem) => {
      setNotifications((prev) => [notif, ...prev]);
      setLatestToast(notif);
    });

    connection
      .start()
      .catch((err) => console.log('SignalR connection failed (will retry automatically):', err.message));

    return () => {
      connection.stop();
    };
  }, [token, user]);

  const markAsRead = async (id: number) => {
    try {
      await notificationService.markAsRead(id);
      setNotifications((prev) =>
        prev.map((n) => (n.notificationId === id ? { ...n, isRead: true } : n))
      );
    } catch {
      // ignore
    }
  };

  const markAllAsRead = async () => {
    try {
      await notificationService.markAllAsRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    } catch {
      // ignore
    }
  };

  const clearToast = () => setLatestToast(null);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  return (
    <SignalRContext.Provider
      value={{
        notifications,
        unreadCount,
        markAsRead,
        markAllAsRead,
        refreshNotifications,
        latestToast,
        clearToast,
      }}
    >
      {children}
    </SignalRContext.Provider>
  );
};

export const useSignalR = () => {
  const context = useContext(SignalRContext);
  if (!context) {
    throw new Error('useSignalR must be used within a SignalRProvider');
  }
  return context;
};
