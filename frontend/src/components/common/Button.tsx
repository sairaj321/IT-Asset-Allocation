import React from 'react';
import { Loader2 } from 'lucide-react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'danger' | 'success' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  leftIcon,
  rightIcon,
  disabled,
  className = '',
  ...props
}) => {
  const baseStyles =
    'inline-flex items-center justify-center font-medium rounded-[8px] transition-colors focus:outline-none focus:ring-2 focus:ring-offset-1 disabled:opacity-50 disabled:cursor-not-allowed';

  const sizeStyles = {
    sm: 'px-3 py-1.5 text-xs gap-1.5',
    md: 'px-4 py-2 text-sm gap-2',
    lg: 'px-5 py-2.5 text-base gap-2.5',
  };

  const variantStyles = {
    primary: 'bg-[#2563EB] hover:bg-[#1D4ED8] text-white focus:ring-[#2563EB]/40 shadow-sm',
    secondary: 'bg-slate-100 hover:bg-slate-200 text-slate-700 focus:ring-slate-300 border border-[#E2E8F0]',
    outline: 'bg-white hover:bg-slate-50 text-slate-700 border border-[#E2E8F0] focus:ring-slate-200',
    danger: 'bg-[#DC2626] hover:bg-[#B91C1C] text-white focus:ring-[#DC2626]/40 shadow-sm',
    success: 'bg-[#16A34A] hover:bg-[#15803D] text-white focus:ring-[#16A34A]/40 shadow-sm',
    ghost: 'bg-transparent hover:bg-slate-100 text-slate-600 focus:ring-slate-200',
  };

  return (
    <button
      className={`${baseStyles} ${sizeStyles[size]} ${variantStyles[variant]} ${className}`}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : leftIcon}
      {children}
      {!isLoading && rightIcon}
    </button>
  );
};
