import { apiClient } from './client';
import {
  Asset,
  AssetCategory,
  AssetCheckResult,
  AssetRepair,
  AssetReplacement,
  AssetRequest,
  AllocationHistory,
  Employee,
  Executive,
  EmployeeDashboardData,
  ManagerDashboardData,
  ExecutiveDashboardData,
  LoginResponse,
  NotificationItem,
  PagedResult,
  User,
} from '../types';

export const authService = {
  login: async (credentials: { username: string; password: string }): Promise<LoginResponse> => {
    const res = await apiClient.post<LoginResponse>('/auth/login', credentials);
    return res.data;
  },
  logout: async (): Promise<void> => {
    await apiClient.post('/auth/logout');
  },
  changePassword: async (data: { currentPassword: string; newPassword: string }): Promise<void> => {
    await apiClient.post('/auth/change-password', data);
  },
  getCurrentUser: async (): Promise<User> => {
    const res = await apiClient.get<User>('/auth/me');
    return res.data;
  },
};

export const dashboardService = {
  getEmployeeDashboard: async (): Promise<EmployeeDashboardData> => {
    const res = await apiClient.get<EmployeeDashboardData>('/dashboard/employee');
    return res.data;
  },
  getManagerDashboard: async (): Promise<ManagerDashboardData> => {
    const res = await apiClient.get<ManagerDashboardData>('/dashboard/manager');
    return res.data;
  },
  getExecutiveDashboard: async (): Promise<ExecutiveDashboardData> => {
    const res = await apiClient.get<ExecutiveDashboardData>('/dashboard/executive');
    return res.data;
  },
};

export const assetService = {
  getAssets: async (params?: { pageNumber?: number; pageSize?: number; search?: string; categoryId?: number; status?: string; condition?: string; expiringWarranty?: boolean }): Promise<PagedResult<Asset>> => {
    const res = await apiClient.get<PagedResult<Asset>>('/assets', { params });
    return res.data;
  },
  getAssetById: async (id: number): Promise<Asset> => {
    const res = await apiClient.get<Asset>(`/assets/${id}`);
    return res.data;
  },
  getAvailableAssets: async (categoryId?: number): Promise<Asset[]> => {
    const res = await apiClient.get<Asset[]>('/assets/available', { params: { categoryId } });
    return res.data;
  },
  createAsset: async (data: Partial<Asset>): Promise<Asset> => {
    const res = await apiClient.post<Asset>('/assets', data);
    return res.data;
  },
  updateAsset: async (id: number, data: Partial<Asset>): Promise<Asset> => {
    const res = await apiClient.put<Asset>(`/assets/${id}`, data);
    return res.data;
  },
  deactivateAsset: async (id: number): Promise<void> => {
    await apiClient.delete(`/assets/${id}`);
  },
  checkAsset: async (id: number, employeeId?: number): Promise<AssetCheckResult> => {
    const res = await apiClient.post<AssetCheckResult>(`/assets/${id}/check`, null, { params: { employeeId } });
    return res.data;
  },
  assignAsset: async (id: number, data: { employeeId: number; requestId?: number; expectedReturnDate?: string; condition?: string; remarks?: string }): Promise<Asset> => {
    const res = await apiClient.post<Asset>(`/assets/${id}/assign`, data);
    return res.data;
  },
  returnAsset: async (id: number, data: { returnDate: string; conditionAtReturn: string; remarks?: string; resultingStatus?: string }): Promise<Asset> => {
    const res = await apiClient.post<Asset>(`/assets/${id}/return`, data);
    return res.data;
  },
  transferAsset: async (id: number, data: { targetEmployeeId: number; expectedReturnDate?: string; condition?: string; remarks?: string }): Promise<Asset> => {
    const res = await apiClient.post<Asset>(`/assets/${id}/transfer`, data);
    return res.data;
  },
  getAllocationHistory: async (id: number): Promise<AllocationHistory[]> => {
    const res = await apiClient.get<AllocationHistory[]>(`/assets/${id}/history`);
    return res.data;
  },
  getAllAllocations: async (params?: { pageNumber?: number; pageSize?: number; search?: string }): Promise<PagedResult<AllocationHistory>> => {
    const res = await apiClient.get<PagedResult<AllocationHistory>>('/assets/allocations', { params });
    return res.data;
  },
  getCategories: async (): Promise<AssetCategory[]> => {
    const res = await apiClient.get<AssetCategory[]>('/asset-categories');
    return res.data;
  },
  createCategory: async (data: Partial<AssetCategory>): Promise<AssetCategory> => {
    const res = await apiClient.post<AssetCategory>('/asset-categories', data);
    return res.data;
  },
  updateCategory: async (id: number, data: Partial<AssetCategory>): Promise<AssetCategory> => {
    const res = await apiClient.put<AssetCategory>(`/asset-categories/${id}`, data);
    return res.data;
  },
};

export const requestService = {
  getAllRequests: async (params?: { pageNumber?: number; pageSize?: number; search?: string; status?: string }): Promise<PagedResult<AssetRequest>> => {
    const res = await apiClient.get<PagedResult<AssetRequest>>('/requests', { params });
    return res.data;
  },
  getMyRequests: async (params?: { pageNumber?: number; pageSize?: number; search?: string }): Promise<PagedResult<AssetRequest>> => {
    const res = await apiClient.get<PagedResult<AssetRequest>>('/requests/my', { params });
    return res.data;
  },
  getAssignedRequests: async (params?: { pageNumber?: number; pageSize?: number; search?: string }): Promise<PagedResult<AssetRequest>> => {
    const res = await apiClient.get<PagedResult<AssetRequest>>('/requests/assigned', { params });
    return res.data;
  },
  getRequestById: async (id: number): Promise<AssetRequest> => {
    const res = await apiClient.get<AssetRequest>(`/requests/${id}`);
    return res.data;
  },
  createRequest: async (data: { assetCategoryId: number; reason: string; requiredDate?: string; priority?: string; requestType?: string }): Promise<AssetRequest> => {
    const res = await apiClient.post<AssetRequest>('/requests', data);
    return res.data;
  },
  updateRequest: async (id: number, data: { assetCategoryId: number; reason: string; requiredDate?: string; priority?: string }): Promise<AssetRequest> => {
    const res = await apiClient.put<AssetRequest>(`/requests/${id}`, data);
    return res.data;
  },
  validateRequest: async (id: number, data: { isValid: boolean; executiveId?: number; remarks?: string; rejectionReason?: string; returnForCorrection?: boolean }): Promise<AssetRequest> => {
    const res = await apiClient.post<AssetRequest>(`/requests/${id}/validate`, data);
    return res.data;
  },
  assignExecutive: async (id: number, data: { executiveId: number; remarks?: string }): Promise<AssetRequest> => {
    const res = await apiClient.post<AssetRequest>(`/requests/${id}/assign-executive`, data);
    return res.data;
  },
  rejectRequest: async (id: number, data: { reason: string }): Promise<AssetRequest> => {
    const res = await apiClient.post<AssetRequest>(`/requests/${id}/reject`, data);
    return res.data;
  },
  allocateRequest: async (id: number, data: { assetId: number; expectedReturnDate?: string; condition?: string; remarks?: string }): Promise<AssetRequest> => {
    const res = await apiClient.post<AssetRequest>(`/requests/${id}/allocate`, data);
    return res.data;
  },
};

export const employeeService = {
  getEmployees: async (params?: { pageNumber?: number; pageSize?: number; search?: string }): Promise<PagedResult<Employee>> => {
    const res = await apiClient.get<PagedResult<Employee>>('/employees', { params });
    return res.data;
  },
  getEmployeeById: async (id: number): Promise<Employee> => {
    const res = await apiClient.get<Employee>(`/employees/${id}`);
    return res.data;
  },
  getEmployeeAssets: async (id: number): Promise<Asset[]> => {
    const res = await apiClient.get<Asset[]>(`/employees/${id}/assets`);
    return res.data;
  },
  createEmployee: async (data: Partial<Employee>): Promise<Employee> => {
    const res = await apiClient.post<Employee>('/employees', data);
    return res.data;
  },
  updateEmployee: async (id: number, data: Partial<Employee>): Promise<Employee> => {
    const res = await apiClient.put<Employee>(`/employees/${id}`, data);
    return res.data;
  },
};

export const executiveService = {
  getExecutives: async (params?: { pageNumber?: number; pageSize?: number; search?: string }): Promise<PagedResult<Executive>> => {
    const res = await apiClient.get<PagedResult<Executive>>('/its-executives', { params });
    return res.data;
  },
  getExecutiveById: async (id: number): Promise<Executive> => {
    const res = await apiClient.get<Executive>(`/its-executives/${id}`);
    return res.data;
  },
  createExecutive: async (data: { firstName: string; lastName: string; employeeCode: string; email: string; mobile?: string; departmentId?: number; username: string; temporaryPassword: string; isActive: boolean }): Promise<Executive> => {
    const res = await apiClient.post<Executive>('/its-executives', data);
    return res.data;
  },
  updateExecutive: async (id: number, data: Partial<Executive>): Promise<Executive> => {
    const res = await apiClient.put<Executive>(`/its-executives/${id}`, data);
    return res.data;
  },
  changeStatus: async (id: number, data: { isActive: boolean; reassignTasksToExecutiveId?: number }): Promise<void> => {
    await apiClient.patch(`/its-executives/${id}/status`, data);
  },
};

export const repairService = {
  getRepairs: async (params?: { pageNumber?: number; pageSize?: number; search?: string; status?: string }): Promise<PagedResult<AssetRepair>> => {
    const res = await apiClient.get<PagedResult<AssetRepair>>('/repairs', { params });
    return res.data;
  },
  getRepairById: async (id: number): Promise<AssetRepair> => {
    const res = await apiClient.get<AssetRepair>(`/repairs/${id}`);
    return res.data;
  },
  createRepair: async (data: { assetId: number; issueDescription: string }): Promise<AssetRepair> => {
    const res = await apiClient.post<AssetRepair>('/repairs', data);
    return res.data;
  },
  updateRepairStatus: async (id: number, data: { status: string; vendorName?: string; repairCost?: number; sentDate?: string; completedDate?: string; conditionAfterRepair?: string; resultingAssetStatus?: string; remarks?: string }): Promise<AssetRepair> => {
    const res = await apiClient.put<AssetRepair>(`/repairs/${id}/status`, data);
    return res.data;
  },
};

export const replacementService = {
  getReplacements: async (params?: { pageNumber?: number; pageSize?: number; status?: string }): Promise<PagedResult<AssetReplacement>> => {
    const res = await apiClient.get<PagedResult<AssetReplacement>>('/replacements', { params });
    return res.data;
  },
  getReplacementById: async (id: number): Promise<AssetReplacement> => {
    const res = await apiClient.get<AssetReplacement>(`/replacements/${id}`);
    return res.data;
  },
  createReplacement: async (data: { oldAssetId: number; reason: string }): Promise<AssetReplacement> => {
    const res = await apiClient.post<AssetReplacement>('/replacements', data);
    return res.data;
  },
  processDecision: async (id: number, data: { approved: boolean; rejectionReason?: string; newAssetId?: number }): Promise<AssetReplacement> => {
    const res = await apiClient.put<AssetReplacement>(`/replacements/${id}/decision`, data);
    return res.data;
  },
};

export const notificationService = {
  getNotifications: async (): Promise<NotificationItem[]> => {
    const res = await apiClient.get<NotificationItem[]>('/notifications');
    return res.data;
  },
  markAsRead: async (id: number): Promise<void> => {
    await apiClient.patch(`/notifications/${id}/read`);
  },
  markAllAsRead: async (): Promise<void> => {
    await apiClient.post('/notifications/read-all');
  },
};

export const reportService = {
  downloadAssetReportCsv: async (): Promise<void> => {
    const res = await apiClient.get('/reports/assets', { params: { format: 'csv' }, responseType: 'blob' });
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `ITS_Assets_Report_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
  },
  downloadAllocationReportCsv: async (): Promise<void> => {
    const res = await apiClient.get('/reports/allocations', { params: { format: 'csv' }, responseType: 'blob' });
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `ITS_Allocations_Report_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
  },
  downloadRepairReportCsv: async (): Promise<void> => {
    const res = await apiClient.get('/reports/repairs', { params: { format: 'csv' }, responseType: 'blob' });
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `ITS_Repairs_Report_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    link.remove();
  },
  getAssetReportData: async (): Promise<any[]> => {
    const res = await apiClient.get('/reports/assets');
    return res.data;
  },
  getAllocationReportData: async (): Promise<any[]> => {
    const res = await apiClient.get('/reports/allocations');
    return res.data;
  },
  getRepairReportData: async (): Promise<any[]> => {
    const res = await apiClient.get('/reports/repairs');
    return res.data;
  },
};
