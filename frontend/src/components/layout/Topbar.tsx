import React, { useState, useRef, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { useSignalR } from '../../context/SignalRContext';
import { Bell, Search, LogOut, KeyRound, User, ChevronDown, CheckCircle2, Shield } from 'lucide-react';

export const Topbar: React.FC = () => {
  const { user, logout, login } = useAuth();
  const { notifications, unreadCount, markAsRead, markAllAsRead } = useSignalR();
  const [showNotifMenu, setShowNotifMenu] = useState(false);
  const [showUserMenu, setShowUserMenu] = useState(false);
  const notifRef = useRef<HTMLDivElement>(null);
  const userRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const location = useLocation();

  // Close menus on outside click
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (notifRef.current && !notifRef.current.contains(event.target as Node)) {
        setShowNotifMenu(false);
      }
      if (userRef.current && !userRef.current.contains(event.target as Node)) {
        setShowUserMenu(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const getPageTitle = () => {
    const path = location.pathname;
    if (path.includes('/manager/dashboard')) return 'ITS Manager Dashboard';
    if (path.includes('/executive/dashboard')) return 'ITS Executive Dashboard';
    if (path.includes('/employee/dashboard')) return 'Employee IT Dashboard';
    if (path.includes('/manager/executives')) return 'ITS Executive Management';
    if (path.includes('/assets')) return 'IT Asset Inventory';
    if (path.includes('/requests')) return 'Asset Allocation Requests';
    if (path.includes('/allocations')) return 'Asset Allocation History';
    if (path.includes('/repairs')) return 'Asset Repair & Maintenance';
    if (path.includes('/replacements')) return 'Asset Replacements';
    if (path.includes('/employees')) return 'Employee Directory';
    if (path.includes('/reports')) return 'Reports & Analytics';
    if (path.includes('/notifications')) return 'Notifications Center';
    return 'IT Asset Allocation (ITS)';
  };

  const handleQuickSwitchRole = async (username: string, defaultPassword: string) => {
    try {
      await login(username, defaultPassword);
      setShowUserMenu(false);
    } catch {
      alert('Could not switch user automatically.');
    }
  };

  return (
    <header className="h-16 bg-white border-b border-[#E2E8F0] px-8 flex items-center justify-between sticky top-0 z-30">
      {/* Title */}
      <div className="flex items-center gap-3">
        <h2 className="text-lg font-bold text-[#0F172A]">{getPageTitle()}</h2>
      </div>

      {/* Right controls */}
      <div className="flex items-center gap-4">
        {/* Global Search */}
        <div className="relative hidden md:block w-64">
          <Search className="w-4 h-4 text-[#64748B] absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            placeholder="Search assets, requests..."
            className="w-full bg-[#F8FAFC] border border-[#E2E8F0] rounded-full pl-9 pr-4 py-1.5 text-xs text-[#0F172A] focus:bg-white focus:outline-none focus:border-[#2563EB] transition-all"
          />
        </div>

        {/* Notifications Bell */}
        <div className="relative" ref={notifRef}>
          <button
            onClick={() => setShowNotifMenu(!showNotifMenu)}
            className="p-2 rounded-full hover:bg-slate-100 text-[#64748B] hover:text-[#0F172A] transition-colors relative"
            title="Notifications"
          >
            <Bell className="w-5 h-5" />
            {unreadCount > 0 && (
              <span className="absolute top-1 right-1 w-4 h-4 bg-[#2563EB] text-white text-[10px] font-bold rounded-full flex items-center justify-center animate-pulse">
                {unreadCount > 9 ? '9+' : unreadCount}
              </span>
            )}
          </button>

          {/* Notifications Dropdown Popover */}
          {showNotifMenu && (
            <div className="absolute right-0 mt-2 w-80 bg-white border border-[#E2E8F0] rounded-[10px] shadow-dropdown py-2 z-50">
              <div className="flex items-center justify-between px-4 py-2 border-b border-[#E2E8F0]">
                <span className="text-xs font-bold text-[#0F172A]">Notifications</span>
                {unreadCount > 0 && (
                  <button
                    onClick={() => markAllAsRead()}
                    className="text-[11px] font-semibold text-[#2563EB] hover:underline"
                  >
                    Mark all read
                  </button>
                )}
              </div>
              <div className="max-h-72 overflow-y-auto divide-y divide-slate-100">
                {notifications.length === 0 ? (
                  <div className="py-6 text-center text-xs text-[#64748B]">No notifications</div>
                ) : (
                  notifications.slice(0, 8).map((n) => (
                    <div
                      key={n.notificationId}
                      onClick={() => markAsRead(n.notificationId)}
                      className={`p-3 text-xs cursor-pointer hover:bg-slate-50 transition-colors ${
                        !n.isRead ? 'bg-blue-50/40 font-medium' : ''
                      }`}
                    >
                      <div className="flex items-center justify-between">
                        <span className="font-semibold text-[#0F172A]">{n.title}</span>
                        {!n.isRead && <span className="w-2 h-2 rounded-full bg-[#2563EB]" />}
                      </div>
                      <p className="text-[#64748B] text-[11px] mt-0.5 line-clamp-2">{n.message}</p>
                      <span className="text-[10px] text-slate-400 mt-1 block">
                        {new Date(n.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                  ))
                )}
              </div>
              <div className="pt-2 px-3 border-t border-[#E2E8F0] text-center">
                <button
                  onClick={() => {
                    setShowNotifMenu(false);
                    navigate('/notifications');
                  }}
                  className="text-xs font-semibold text-[#2563EB] hover:underline block w-full py-1"
                >
                  View all notifications
                </button>
              </div>
            </div>
          )}
        </div>

        {/* User Avatar & Dropdown */}
        <div className="relative" ref={userRef}>
          <button
            onClick={() => setShowUserMenu(!showUserMenu)}
            className="flex items-center gap-2 p-1.5 rounded-full hover:bg-slate-100 transition-colors"
          >
            <div className="w-8 h-8 rounded-full bg-blue-600 text-white font-bold text-xs flex items-center justify-center shadow-sm">
              {user?.fullName?.slice(0, 2).toUpperCase() || 'U'}
            </div>
            <div className="hidden lg:block text-left">
              <p className="text-xs font-semibold text-[#0F172A] leading-tight">{user?.fullName}</p>
              <span className="text-[10px] text-[#64748B] block">{user?.roles[0]?.replace('ITS_', '')}</span>
            </div>
            <ChevronDown className="w-4 h-4 text-[#64748B]" />
          </button>

          {showUserMenu && (
            <div className="absolute right-0 mt-2 w-64 bg-white border border-[#E2E8F0] rounded-[10px] shadow-dropdown py-2 z-50">
              <div className="px-4 py-2 border-b border-[#E2E8F0]">
                <p className="text-xs font-bold text-[#0F172A]">{user?.fullName}</p>
                <p className="text-[11px] text-[#64748B] truncate">{user?.email}</p>
                <span className="inline-block mt-1 px-2 py-0.5 rounded text-[10px] font-semibold bg-blue-50 text-blue-700">
                  {user?.roles[0]}
                </span>
              </div>

              {/* Demo Quick Role Switcher */}
              <div className="py-2 border-b border-[#E2E8F0] px-3">
                <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider block mb-1">
                  Switch Persona (Demo)
                </span>
                <div className="space-y-1">
                  <button
                    onClick={() => handleQuickSwitchRole('manager', 'Manager@123')}
                    className={`w-full text-left px-2 py-1 rounded text-xs flex items-center justify-between hover:bg-blue-50 ${
                      user?.roles.includes('ITS_MANAGER') ? 'text-blue-600 font-bold' : 'text-slate-700'
                    }`}
                  >
                    <span>ITS Manager (Sairaj)</span>
                    {user?.roles.includes('ITS_MANAGER') && <CheckCircle2 className="w-3.5 h-3.5 text-blue-600" />}
                  </button>
                  <button
                    onClick={() => handleQuickSwitchRole('exec1', 'Exec@123')}
                    className={`w-full text-left px-2 py-1 rounded text-xs flex items-center justify-between hover:bg-blue-50 ${
                      user?.roles.includes('ITS_EXECUTIVE') ? 'text-blue-600 font-bold' : 'text-slate-700'
                    }`}
                  >
                    <span>ITS Executive (Priya)</span>
                    {user?.roles.includes('ITS_EXECUTIVE') && <CheckCircle2 className="w-3.5 h-3.5 text-blue-600" />}
                  </button>
                  <button
                    onClick={() => handleQuickSwitchRole('emp005', 'Emp@123')}
                    className={`w-full text-left px-2 py-1 rounded text-xs flex items-center justify-between hover:bg-blue-50 ${
                      user?.roles.includes('EMPLOYEE') && !user?.roles.includes('ITS_MANAGER') ? 'text-blue-600 font-bold' : 'text-slate-700'
                    }`}
                  >
                    <span>Employee (Rahul)</span>
                    {user?.roles.includes('EMPLOYEE') && !user?.roles.includes('ITS_MANAGER') && <CheckCircle2 className="w-3.5 h-3.5 text-blue-600" />}
                  </button>
                </div>
              </div>

              <button
                onClick={() => {
                  setShowUserMenu(false);
                  navigate('/change-password');
                }}
                className="w-full px-4 py-2 text-left text-xs text-[#0F172A] hover:bg-slate-50 flex items-center gap-2"
              >
                <KeyRound className="w-3.5 h-3.5 text-slate-500" />
                <span>Change Password</span>
              </button>

              <button
                onClick={() => {
                  setShowUserMenu(false);
                  logout();
                }}
                className="w-full px-4 py-2 text-left text-xs text-[#DC2626] hover:bg-red-50 flex items-center gap-2"
              >
                <LogOut className="w-3.5 h-3.5 text-[#DC2626]" />
                <span>Sign Out</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
