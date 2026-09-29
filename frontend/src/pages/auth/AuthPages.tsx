import React, { useState } from 'react';
import { useAuth } from '../../context/AuthContext';
import { Button } from '../../components/common/Button';
import { FormField, Input } from '../../components/common/FormField';
import { Layers, Lock, User, AlertCircle, ShieldCheck } from 'lucide-react';

export const LoginPage: React.FC = () => {
  const { login } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim() || !password) {
      setError('Please provide both username and password.');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      await login(username, password);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Login failed. Please check your credentials.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleQuickFill = (u: string, p: string) => {
    setUsername(u);
    setPassword(p);
  };

  return (
    <div className="min-h-screen bg-[#F8FAFC] flex flex-col justify-center items-center p-4">
      {/* Centered White Card */}
      <div className="w-full max-w-md bg-white border border-[#E2E8F0] rounded-[12px] shadow-card p-8">
        <div className="flex flex-col items-center text-center mb-6">
          <div className="w-12 h-12 rounded-xl bg-[#2563EB] flex items-center justify-center text-white shadow-md mb-3">
            <Layers className="w-7 h-7" />
          </div>
          <h1 className="text-xl font-bold text-[#0F172A]">Employee Management System</h1>
          <p className="text-xs text-[#64748B] mt-1 font-medium">IT Asset Allocation (ITS) Module</p>
        </div>

        {error && (
          <div className="mb-5 p-3 rounded-[8px] bg-red-50 border border-red-200 text-red-700 text-xs flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0 text-red-600" />
            <span>{error}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <FormField label="Username / Employee Code" required>
            <div className="relative">
              <User className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <Input
                type="text"
                placeholder="e.g. manager, exec1, or EMP005"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                className="pl-9"
                required
              />
            </div>
          </FormField>

          <FormField label="Password" required>
            <div className="relative">
              <Lock className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <Input
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="pl-9"
                required
              />
            </div>
          </FormField>

          <Button type="submit" className="w-full mt-2" isLoading={isLoading} size="lg">
            Sign in
          </Button>
        </form>

        {/* Demo Credentials Quick Switcher Box */}
        <div className="mt-8 pt-6 border-t border-[#E2E8F0]">
          <div className="flex items-center gap-1.5 text-xs font-semibold text-[#64748B] mb-3">
            <ShieldCheck className="w-4 h-4 text-[#2563EB]" />
            <span>Click to autofill demo credentials:</span>
          </div>
          <div className="grid grid-cols-3 gap-2">
            <button
              type="button"
              onClick={() => handleQuickFill('manager', 'Manager@123')}
              className="p-2 border border-[#E2E8F0] rounded-[8px] text-[11px] text-left hover:border-blue-500 hover:bg-blue-50/50 transition-all"
            >
              <span className="font-bold text-[#0F172A] block">Manager</span>
              <span className="text-[10px] text-[#64748B]">manager</span>
            </button>
            <button
              type="button"
              onClick={() => handleQuickFill('exec1', 'Exec@123')}
              className="p-2 border border-[#E2E8F0] rounded-[8px] text-[11px] text-left hover:border-blue-500 hover:bg-blue-50/50 transition-all"
            >
              <span className="font-bold text-[#0F172A] block">Executive</span>
              <span className="text-[10px] text-[#64748B]">exec1</span>
            </button>
            <button
              type="button"
              onClick={() => handleQuickFill('emp005', 'Emp@123')}
              className="p-2 border border-[#E2E8F0] rounded-[8px] text-[11px] text-left hover:border-blue-500 hover:bg-blue-50/50 transition-all"
            >
              <span className="font-bold text-[#0F172A] block">Employee</span>
              <span className="text-[10px] text-[#64748B]">emp005</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export const ChangePasswordPage: React.FC = () => {
  const { user, logout } = useAuth();
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (newPassword !== confirmPassword) {
      setError('New passwords do not match.');
      return;
    }
    if (newPassword.length < 6) {
      setError('Password must be at least 6 characters.');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const { authService } = await import('../../api/services');
      await authService.changePassword({ currentPassword, newPassword });
      setSuccess(true);
      setTimeout(() => {
        window.location.href = '/';
      }, 1500);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to update password.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#F8FAFC] flex flex-col justify-center items-center p-4">
      <div className="w-full max-w-md bg-white border border-[#E2E8F0] rounded-[12px] shadow-card p-8">
        <h2 className="text-xl font-bold text-[#0F172A]">Change Password</h2>
        <p className="text-xs text-[#64748B] mt-1 mb-6">
          {user?.mustChangePassword
            ? 'First login detected. Please choose a new secure password.'
            : 'Update your account login password.'}
        </p>

        {error && (
          <div className="mb-4 p-3 rounded-[8px] bg-red-50 border border-red-200 text-red-700 text-xs">
            {error}
          </div>
        )}

        {success && (
          <div className="mb-4 p-3 rounded-[8px] bg-green-50 border border-green-200 text-green-700 text-xs">
            Password changed successfully! Redirecting...
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <FormField label="Current / Temporary Password" required>
            <Input
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
            />
          </FormField>

          <FormField label="New Password" required>
            <Input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
            />
          </FormField>

          <FormField label="Confirm New Password" required>
            <Input
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
            />
          </FormField>

          <div className="flex items-center justify-between pt-2">
            <Button type="button" variant="ghost" onClick={() => logout()}>
              Cancel / Sign Out
            </Button>
            <Button type="submit" isLoading={isLoading}>
              Update Password
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
