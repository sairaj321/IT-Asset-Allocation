import React from 'react';

interface FormFieldProps {
  label: string;
  required?: boolean;
  error?: string;
  hint?: string;
  children: React.ReactNode;
  className?: string;
}

export const FormField: React.FC<FormFieldProps> = ({
  label,
  required = false,
  error,
  hint,
  children,
  className = '',
}) => {
  return (
    <div className={`flex flex-col gap-1.5 ${className}`}>
      <label className="text-xs font-semibold text-[#0F172A]">
        {label} {required && <span className="text-[#DC2626]">*</span>}
      </label>
      {children}
      {hint && !error && <span className="text-xs text-[#64748B]">{hint}</span>}
      {error && <span className="text-xs font-medium text-[#DC2626]">{error}</span>}
    </div>
  );
};

export const Input = React.forwardRef<
  HTMLInputElement,
  React.InputHTMLAttributes<HTMLInputElement> & { error?: boolean }
>(({ error, className = '', ...props }, ref) => {
  return (
    <input
      ref={ref}
      className={`w-full px-3 py-2 text-sm bg-white border ${
        error ? 'border-[#DC2626] focus:ring-[#DC2626]/20' : 'border-[#E2E8F0] focus:border-[#2563EB] focus:ring-[#2563EB]/20'
      } rounded-[8px] text-[#0F172A] placeholder-slate-400 focus:outline-none focus:ring-2 transition-all disabled:bg-slate-50 disabled:text-slate-500 ${className}`}
      {...props}
    />
  );
});

export const Select = React.forwardRef<
  HTMLSelectElement,
  React.SelectHTMLAttributes<HTMLSelectElement> & { error?: boolean }
>(({ error, className = '', children, ...props }, ref) => {
  return (
    <select
      ref={ref}
      className={`w-full px-3 py-2 text-sm bg-white border ${
        error ? 'border-[#DC2626] focus:ring-[#DC2626]/20' : 'border-[#E2E8F0] focus:border-[#2563EB] focus:ring-[#2563EB]/20'
      } rounded-[8px] text-[#0F172A] focus:outline-none focus:ring-2 transition-all disabled:bg-slate-50 disabled:text-slate-500 ${className}`}
      {...props}
    >
      {children}
    </select>
  );
});

export const Textarea = React.forwardRef<
  HTMLTextAreaElement,
  React.TextareaHTMLAttributes<HTMLTextAreaElement> & { error?: boolean }
>(({ error, className = '', ...props }, ref) => {
  return (
    <textarea
      ref={ref}
      className={`w-full px-3 py-2 text-sm bg-white border ${
        error ? 'border-[#DC2626] focus:ring-[#DC2626]/20' : 'border-[#E2E8F0] focus:border-[#2563EB] focus:ring-[#2563EB]/20'
      } rounded-[8px] text-[#0F172A] placeholder-slate-400 focus:outline-none focus:ring-2 transition-all disabled:bg-slate-50 disabled:text-slate-500 ${className}`}
      {...props}
    />
  );
});
