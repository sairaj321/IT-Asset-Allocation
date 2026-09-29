import React, { useEffect } from 'react';
import { useSignalR } from '../../context/SignalRContext';
import { Bell, CheckCircle2, AlertCircle, Info, X } from 'lucide-react';

export const ToastNotification: React.FC = () => {
  const { latestToast, clearToast } = useSignalR();

  useEffect(() => {
    if (latestToast) {
      const timer = setTimeout(() => {
        clearToast();
      }, 5000);
      return () => clearTimeout(timer);
    }
  }, [latestToast, clearToast]);

  if (!latestToast) return null;

  const getIcon = (type: string) => {
    switch (type.toLowerCase()) {
      case 'success':
        return <CheckCircle2 className="w-5 h-5 text-[#16A34A]" />;
      case 'warning':
      case 'error':
        return <AlertCircle className="w-5 h-5 text-[#DC2626]" />;
      default:
        return <Info className="w-5 h-5 text-[#2563EB]" />;
    }
  };

  const getBg = (type: string) => {
    switch (type.toLowerCase()) {
      case 'success':
        return 'border-[#BBF7D0] bg-[#F0FDF4]';
      case 'warning':
      case 'error':
        return 'border-[#FECACA] bg-[#FEF2F2]';
      default:
        return 'border-[#BFDBFE] bg-[#EFF6FF]';
    }
  };

  return (
    <div className="fixed bottom-5 right-5 z-50 max-w-sm w-full animate-bounce-short">
      <div className={`p-4 rounded-[10px] shadow-lg border ${getBg(latestToast.type)} flex items-start gap-3`}>
        <div className="mt-0.5">{getIcon(latestToast.type)}</div>
        <div className="flex-1">
          <h4 className="text-sm font-semibold text-[#0F172A]">{latestToast.title}</h4>
          <p className="text-xs text-[#64748B] mt-0.5">{latestToast.message}</p>
        </div>
        <button onClick={clearToast} className="text-slate-400 hover:text-slate-600 p-1">
          <X className="w-4 h-4" />
        </button>
      </div>
    </div>
  );
};
