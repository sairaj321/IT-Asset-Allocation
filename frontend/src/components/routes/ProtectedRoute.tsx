import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { Loader2, ShieldAlert } from 'lucide-react';

interface ProtectedRouteProps {
  roles?: string[];
  permissions?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ roles, permissions }) => {
  const { user, token, isLoading, hasRole, hasPermission } = useAuth();

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#F8FAFC]">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="w-8 h-8 text-[#2563EB] animate-spin" />
          <p className="text-sm font-medium text-[#64748B]">Loading your session...</p>
        </div>
      </div>
    );
  }

  if (!token || !user) {
    return <Navigate to="/login" replace />;
  }

  if (roles && roles.length > 0) {
    const hasAnyRole = roles.some((r) => hasRole(r));
    if (!hasAnyRole) {
      return (
        <div className="min-h-[60vh] flex flex-col items-center justify-center text-center p-6">
          <div className="w-16 h-16 rounded-full bg-red-100 flex items-center justify-center text-red-600 mb-4">
            <ShieldAlert className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-[#0F172A]">Access Forbidden (403)</h2>
          <p className="text-sm text-[#64748B] mt-2 max-w-md">
            You do not have the required role ({roles.join(', ')}) to access this page.
          </p>
        </div>
      );
    }
  }

  if (permissions && permissions.length > 0) {
    const hasAnyPerm = permissions.some((p) => hasPermission(p));
    if (!hasAnyPerm) {
      return (
        <div className="min-h-[60vh] flex flex-col items-center justify-center text-center p-6">
          <div className="w-16 h-16 rounded-full bg-red-100 flex items-center justify-center text-red-600 mb-4">
            <ShieldAlert className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-[#0F172A]">Permission Denied</h2>
          <p className="text-sm text-[#64748B] mt-2 max-w-md">
            Your account lacks the permission ({permissions.join(', ')}) required for this action.
          </p>
        </div>
      );
    }
  }

  return <Outlet />;
};
