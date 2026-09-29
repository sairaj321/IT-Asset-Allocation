import React from 'react';

interface BadgeProps {
  status: string;
  variant?: 'status' | 'condition' | 'priority' | 'role' | 'default';
  className?: string;
}

export const Badge: React.FC<BadgeProps> = ({ status, variant = 'status', className = '' }) => {
  const getBadgeStyle = (val: string) => {
    const s = val?.toLowerCase() || '';

    // Status mapping per section 15.1
    if (s === 'available' || s === 'allocated' || s === 'approved' || s === 'completed' || s === 'active' || s === 'good' || s === 'high') {
      return 'bg-[#DCFCE7] text-[#15803D] border border-[#BBF7D0]'; // Green soft
    }
    if (s === 'assigned' || s === 'its_manager') {
      return 'bg-[#DBEAFE] text-[#1D4ED8] border border-[#BFDBFE]'; // Blue soft
    }
    if (s === 'undermaintenance' || s === 'under maintenance' || s === 'inrepair' || s === 'in repair' || s === 'its_executive') {
      return 'bg-[#EDE9FE] text-[#7C3AED] border border-[#DDD6FE]'; // Purple soft
    }
    if (s === 'damaged' || s === 'lost' || s === 'rejected' || s === 'notworking' || s === 'not working' || s === 'inactive') {
      return 'bg-[#FEE2E2] text-[#DC2626] border border-[#FECACA]'; // Red soft
    }
    if (s === 'pending' || s === 'submitted' || s === 'underreview' || s === 'under review' || s === 'checking' || s === 'fair' || s === 'normal' || s === 'returnedforcorrection') {
      return 'bg-[#FEF3C7] text-[#D97706] border border-[#FDE68A]'; // Amber soft
    }
    if (s === 'retired' || s === 'cancelled' || s === 'transferred' || s === 'returned' || s === 'closednorepair' || s === 'low' || s === 'employee') {
      return 'bg-[#F1F5F9] text-[#64748B] border border-[#E2E8F0]'; // Gray soft
    }

    return 'bg-gray-100 text-gray-700 border border-gray-200';
  };

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold tracking-wide ${getBadgeStyle(
        status
      )} ${className}`}
    >
      {status}
    </span>
  );
};
