import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './context/AuthContext';
import { AppLayout } from './components/layout/AppLayout';
import { ProtectedRoute } from './components/routes/ProtectedRoute';

// Auth Pages
import { LoginPage, ChangePasswordPage } from './pages/auth/AuthPages';

// Employee Pages
import { EmployeeDashboard, RequestDetailPage } from './pages/employee/EmployeePages';

// Manager Pages
import { ManagerDashboard, ExecutivesPage, AddExecutivePage, ReportsPage } from './pages/manager/ManagerPages';

// Executive Pages
import { ExecutiveDashboard, TaskDetailPage } from './pages/executive/ExecutivePages';

// Asset Pages
import { AssetListPage, AssetDetailPage, AddEditAssetPage } from './pages/assets/AssetPages';

// Employee Directory
import { EmployeeListPage, EmployeeDetailPage } from './pages/employees/EmployeePages';

// Operations Pages
import {
  RequestsListPage,
  AllocationsListPage,
  RepairsListPage,
  ReplacementsListPage,
  NotificationsPage,
} from './pages/operations/OperationsPages';

const RootRedirect: React.FC = () => {
  const { user, getDefaultRoute } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  return <Navigate to={getDefaultRoute()} replace />;
};

export const App: React.FC = () => {
  return (
    <Routes>
      {/* Public Routes */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/change-password" element={<ChangePasswordPage />} />

      {/* Authenticated Layout Routes */}
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          {/* Default Redirect */}
          <Route path="/" element={<RootRedirect />} />

          {/* Employee Routes */}
          <Route path="/employee/dashboard" element={<EmployeeDashboard />} />
          <Route path="/employee/requests" element={<RequestsListPage />} />
          <Route path="/employee/requests/:id" element={<RequestDetailPage />} />
          <Route path="/employee/assets" element={<EmployeeDashboard />} />

          {/* Manager Routes */}
          <Route element={<ProtectedRoute roles={['ITS_MANAGER']} />}>
            <Route path="/manager/dashboard" element={<ManagerDashboard />} />
            <Route path="/manager/executives" element={<ExecutivesPage />} />
            <Route path="/manager/executives/new" element={<AddExecutivePage />} />
            <Route path="/reports" element={<ReportsPage />} />
            <Route path="/assets/new" element={<AddEditAssetPage />} />
            <Route path="/assets/:id/edit" element={<AddEditAssetPage />} />
          </Route>

          {/* Executive Routes */}
          <Route element={<ProtectedRoute roles={['ITS_EXECUTIVE', 'ITS_MANAGER']} />}>
            <Route path="/executive/dashboard" element={<ExecutiveDashboard />} />
            <Route path="/executive/tasks/:id" element={<TaskDetailPage />} />
          </Route>

          {/* Shared Operations Routes */}
          <Route path="/assets" element={<AssetListPage />} />
          <Route path="/assets/:id" element={<AssetDetailPage />} />
          <Route path="/assets/:id/history" element={<AssetDetailPage />} />
          <Route path="/allocations" element={<AllocationsListPage />} />
          <Route path="/employees" element={<EmployeeListPage />} />
          <Route path="/employees/:id" element={<EmployeeDetailPage />} />
          <Route path="/requests" element={<RequestsListPage />} />
          <Route path="/repairs" element={<RepairsListPage />} />
          <Route path="/replacements" element={<ReplacementsListPage />} />
          <Route path="/notifications" element={<NotificationsPage />} />
        </Route>
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};

export default App;
