import React from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';
import { ToastNotification } from '../common/ToastNotification';

export const AppLayout: React.FC = () => {
  return (
    <div className="flex min-h-screen bg-[#F8FAFC]">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <Topbar />
        <main className="flex-1 p-8 overflow-y-auto max-w-7xl w-full mx-auto">
          <Outlet />
        </main>
      </div>
      <ToastNotification />
    </div>
  );
};
