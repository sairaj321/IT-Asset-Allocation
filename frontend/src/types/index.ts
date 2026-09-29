export type AssetStatus = 
  | 'Available'
  | 'Assigned'
  | 'UnderMaintenance'
  | 'InRepair'
  | 'Damaged'
  | 'Lost'
  | 'Replaced'
  | 'Retired';

export type AssetCondition = 'Good' | 'Fair' | 'Damaged' | 'NotWorking';

export type RequestStatus = 
  | 'Submitted'
  | 'UnderReview'
  | 'ReturnedForCorrection'
  | 'AssignedToExecutive'
  | 'Checking'
  | 'NotAvailable'
  | 'Allocated'
  | 'Rejected'
  | 'Cancelled';

export type AllocationStatus = 'Active' | 'Returned' | 'Transferred' | 'Cancelled';

export type RepairStatus = 'Reported' | 'UnderReview' | 'InRepair' | 'Completed' | 'ClosedNoRepair';

export type ReplacementStatus = 'Requested' | 'ConditionChecked' | 'Approved' | 'Rejected' | 'Completed';

export type Priority = 'Low' | 'Normal' | 'High';

export type RequestType = 'NewAsset' | 'Replacement' | 'Return' | 'Repair';

export interface User {
  userId: number;
  employeeId: number;
  username: string;
  employeeCode: string;
  fullName: string;
  email: string;
  designation?: string;
  departmentName?: string;
  mustChangePassword: boolean;
  roles: string[];
  permissions: string[];
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface AssetCategory {
  assetCategoryId: number;
  categoryName: string;
  description?: string;
  isActive: boolean;
  totalAssetsCount: number;
  availableAssetsCount: number;
}

export interface Asset {
  assetId: number;
  assetCode: string;
  assetName: string;
  assetCategoryId: number;
  categoryName: string;
  serialNumber: string;
  brand?: string;
  model?: string;
  purchaseDate?: string;
  purchaseCost: number;
  warrantyStartDate?: string;
  warrantyExpiryDate?: string;
  status: AssetStatus;
  statusName: string;
  condition: AssetCondition;
  conditionName: string;
  location?: string;
  currentEmployeeId?: number;
  assignedEmployeeCode?: string;
  assignedEmployeeName?: string;
  assignedEmployeeDepartment?: string;
  allocationDate?: string;
  expectedReturnDate?: string;
  isActive: boolean;
  version: number;
  createdAt: string;
}

export interface CheckResult {
  ruleName: string;
  passed: boolean;
  message: string;
  suggestedAction?: string;
}

export interface AssetCheckResult {
  assetId: number;
  assetCode: string;
  assetName: string;
  isEligible: boolean;
  checks: CheckResult[];
  overallSummary: string;
}

export interface RequestHistory {
  historyId: number;
  fromStatus?: RequestStatus;
  toStatus: RequestStatus;
  actionByName: string;
  remarks?: string;
  createdAt: string;
}

export interface AssetRequest {
  requestId: number;
  requestNumber: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  departmentName?: string;
  assetCategoryId: number;
  categoryName: string;
  reason: string;
  requiredDate?: string;
  priority: Priority;
  priorityName: string;
  status: RequestStatus;
  statusName: string;
  assignedExecutiveId?: number;
  assignedExecutiveName?: string;
  managerRemarks?: string;
  rejectionReason?: string;
  allocatedAssetId?: number;
  allocatedAssetCode?: string;
  allocatedAssetName?: string;
  requestType: RequestType;
  createdAt: string;
  history: RequestHistory[];
}

export interface AllocationHistory {
  allocationId: number;
  assetId: number;
  assetCode: string;
  assetName: string;
  employeeId: number;
  employeeCode: string;
  employeeName: string;
  departmentName?: string;
  allocationDate: string;
  expectedReturnDate?: string;
  returnDate?: string;
  allocationStatus: AllocationStatus;
  statusName: string;
  conditionAtAllocation?: AssetCondition;
  conditionAtReturn?: AssetCondition;
  remarks?: string;
  allocatedByName: string;
}

export interface Employee {
  employeeId: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  mobile?: string;
  departmentId?: number;
  departmentName?: string;
  designation?: string;
  isActive: boolean;
  allocatedAssetsCount: number;
  pendingRequestsCount: number;
}

export interface Executive {
  userId: number;
  employeeId: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  mobile?: string;
  departmentName?: string;
  username: string;
  isActive: boolean;
  openTasksCount: number;
  completedTasksCount: number;
  createdAt: string;
}

export interface AssetRepair {
  repairId: number;
  assetId: number;
  assetCode: string;
  assetName: string;
  reportedByEmployeeId: number;
  reportedByEmployeeName: string;
  handledByExecutiveId?: number;
  handledByExecutiveName?: string;
  issueDescription: string;
  status: RepairStatus;
  statusName: string;
  vendorName?: string;
  repairCost?: number;
  sentDate?: string;
  completedDate?: string;
  conditionAfterRepair?: AssetCondition;
  createdAt: string;
}

export interface AssetReplacement {
  replacementId: number;
  oldAssetId: number;
  oldAssetCode: string;
  oldAssetName: string;
  newAssetId?: number;
  newAssetCode?: string;
  newAssetName?: string;
  employeeId: number;
  employeeName: string;
  reason: string;
  status: ReplacementStatus;
  statusName: string;
  approvedBy?: number;
  approvedByName?: string;
  rejectionReason?: string;
  createdAt: string;
}

export interface NotificationItem {
  notificationId: number;
  title: string;
  message: string;
  type: string;
  relatedEntity?: string;
  relatedId?: number;
  isRead: boolean;
  createdAt: string;
}

export interface CategoryChartItem {
  name: string;
  value: number;
  color: string;
}

export interface StatusChartItem {
  name: string;
  value: number;
  color: string;
}

export interface ManagerDashboardData {
  totalAssets: number;
  assignedAssets: number;
  availableAssets: number;
  underRepairAssets: number;
  damagedAssets: number;
  executiveCount: number;
  pendingRequestsCount: number;
  approvedRequestsCount: number;
  rejectedRequestsCount: number;
  assetsByCategory: CategoryChartItem[];
  assetsByStatus: StatusChartItem[];
  pendingRequests: AssetRequest[];
}

export interface EmployeeDashboardData {
  myRequests: AssetRequest[];
  myAssets: Asset[];
  activeAssetsCount: number;
  pendingRequestsCount: number;
}

export interface ExecutiveDashboardData {
  myPendingTasksCount: number;
  assetRequestsCount: number;
  assetsToAssignCount: number;
  repairRequestsCount: number;
  myAssignedTasks: AssetRequest[];
  myAssignedRepairs: AssetRepair[];
}
