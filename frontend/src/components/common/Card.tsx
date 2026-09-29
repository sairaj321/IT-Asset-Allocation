import React from 'react';

interface CardProps {
  title?: React.ReactNode;
  subtitle?: string;
  action?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
  bodyClassName?: string;
}

export const Card: React.FC<CardProps> = ({
  title,
  subtitle,
  action,
  children,
  className = '',
  bodyClassName = '',
}) => {
  return (
    <div className={`bg-white border border-[#E2E8F0] rounded-[10px] shadow-sm ${className}`}>
      {(title || action) && (
        <div className="flex items-center justify-between px-6 py-4 border-b border-[#E2E8F0]">
          <div>
            {typeof title === 'string' ? (
              <h3 className="text-base font-semibold text-[#0F172A]">{title}</h3>
            ) : (
              title
            )}
            {subtitle && <p className="text-xs text-[#64748B] mt-0.5">{subtitle}</p>}
          </div>
          {action && <div>{action}</div>}
        </div>
      )}
      <div className={`p-6 ${bodyClassName}`}>{children}</div>
    </div>
  );
};

export const StatCard: React.FC<{
  title: string;
  value: string | number;
  icon: React.ReactNode;
  iconBgColor?: string;
  description?: string;
  trend?: string;
  onClick?: () => void;
}> = ({ title, value, icon, iconBgColor = 'bg-blue-50 text-blue-600', description, trend, onClick }) => {
  return (
    <div
      onClick={onClick}
      className={`bg-white border border-[#E2E8F0] rounded-[10px] p-5 shadow-sm hover:border-blue-300 transition-all ${
        onClick ? 'cursor-pointer hover:shadow-md' : ''
      }`}
    >
      <div className="flex items-center justify-between">
        <div>
          <p className="text-xs font-medium text-[#64748B] uppercase tracking-wider">{title}</p>
          <p className="text-2xl font-bold text-[#0F172A] mt-1.5">{value}</p>
          {description && <p className="text-xs text-[#64748B] mt-1">{description}</p>}
        </div>
        <div className={`p-3 rounded-lg ${iconBgColor}`}>{icon}</div>
      </div>
      {trend && (
        <div className="mt-3 pt-3 border-t border-slate-100 flex items-center text-xs text-slate-500">
          <span>{trend}</span>
        </div>
      )}
    </div>
  );
};
