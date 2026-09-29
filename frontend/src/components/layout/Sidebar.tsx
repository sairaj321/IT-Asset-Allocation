import React from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import {
  LayoutDashboard,
  Users,
  FileText,
  Laptop,
  ArrowRightLeft,
  Wrench,
  RefreshCw,
  UserCheck,
  BarChart3,
  CheckSquare,
  Shield,
  Layers
} from 'lucide-react';

export const Sidebar: React.FC = () => {
  const { user, isManager, isExecutive, isEmployee } = useAuth();

  const getNavItems = () => {
    if (isManager) {
      return [
        { label: 'Dashboard', to: '/manager/dashboard', icon: <LayoutDashboard className="w-4 h-4" /> },
        { label: 'Employees', to: '/employees', icon: <Users className="w-4 h-4" /> },
        { label: 'Asset Requests', to: '/requests', icon: <FileText className="w-4 h-4" /> },
        { label: 'IT Assets', to: '/assets', icon: <Laptop className="w-4 h-4" /> },
        { label: 'Allocations', to: '/allocations', icon: <ArrowRightLeft className="w-4 h-4" /> },
        { label: 'Repairs', to: '/repairs', icon: <Wrench className="w-4 h-4" /> },
        { label: 'Replacements', to: '/replacements', icon: <RefreshCw className="w-4 h-4" /> },
        { label: 'ITS Executives', to: '/manager/executives', icon: <UserCheck className="w-4 h-4" /> },
        { label: 'Reports', to: '/reports', icon: <BarChart3 className="w-4 h-4" /> },
      ];
    }

    if (isExecutive) {
      return [
        { label: 'Dashboard', to: '/executive/dashboard', icon: <LayoutDashboard className="w-4 h-4" /> },
        { label: 'My Tasks', to: '/executive/dashboard', icon: <CheckSquare className="w-4 h-4" /> },
        { label: 'IT Assets', to: '/assets', icon: <Laptop className="w-4 h-4" /> },
        { label: 'Employees', to: '/employees', icon: <Users className="w-4 h-4" /> },
        { label: 'Repairs', to: '/repairs', icon: <Wrench className="w-4 h-4" /> },
      ];
    }

    // Default Employee
    return [
      { label: 'Dashboard', to: '/employee/dashboard', icon: <LayoutDashboard className="w-4 h-4" /> },
      { label: 'My Requests', to: '/employee/requests', icon: <FileText className="w-4 h-4" /> },
      { label: 'My Assets', to: '/employee/assets', icon: <Laptop className="w-4 h-4" /> },
    ];
  };

  const navItems = getNavItems();

  return (
    <aside className="w-60 min-w-[240px] bg-white border-r border-[#E2E8F0] min-h-screen flex flex-col justify-between select-none">
      <div>
        {/* Brand Header */}
        <div className="h-16 flex items-center px-6 border-b border-[#E2E8F0] gap-3">
          <div className="w-8 h-8 rounded-lg bg-[#2563EB] flex items-center justify-center text-white font-bold text-sm shadow-sm">
            <Layers className="w-5 h-5" />
          </div>
          <div>
            <h1 className="font-bold text-sm text-[#0F172A] tracking-tight leading-none">EMS IT Portal</h1>
            <span className="text-[10px] font-semibold text-[#2563EB] tracking-wider uppercase">Asset Allocation</span>
          </div>
        </div>

        {/* Navigation Menu */}
        <div className="py-4">
          <p className="px-6 text-[10px] font-bold text-[#64748B] uppercase tracking-wider mb-2">
            Main Menu
          </p>
          <nav className="space-y-1 px-3">
            {navItems.map((item) => (
              <NavLink
                key={item.to + item.label}
                to={item.to}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3.5 py-2.5 rounded-[8px] text-sm font-medium transition-all relative ${
                    isActive
                      ? 'bg-[#EFF6FF] text-[#2563EB] font-semibold before:content-[""] before:absolute before:left-0 before:top-2 before:bottom-2 before:w-[3px] before:bg-[#2563EB] before:rounded-r'
                      : 'text-[#64748B] hover:text-[#0F172A] hover:bg-slate-50'
                  }`
                }
              >
                {item.icon}
                <span>{item.label}</span>
              </NavLink>
            ))}
          </nav>
        </div>
      </div>

      {/* Role / User Badge at Bottom */}
      <div className="p-4 border-t border-[#E2E8F0] bg-slate-50/50 m-3 rounded-[8px]">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-blue-100 text-blue-700 font-semibold text-xs flex items-center justify-center">
            {user?.fullName?.slice(0, 2).toUpperCase() || 'EM'}
          </div>
          <div className="overflow-hidden">
            <p className="text-xs font-semibold text-[#0F172A] truncate">{user?.fullName}</p>
            <span className="text-[10px] text-[#64748B] font-medium block truncate">
              {user?.roles[0]?.replace('ITS_', '') || 'Employee'}
            </span>
          </div>
        </div>
      </div>
    </aside>
  );
};
