import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { dashboardService, requestService, executiveService, reportService } from '../../api/services';
import { ManagerDashboardData, Executive, AssetRequest } from '../../types';
import { Card, StatCard } from '../../components/common/Card';
import { Button } from '../../components/common/Button';
import { Badge } from '../../components/common/Badge';
import { Modal } from '../../components/common/Modal';
import { FormField, Input, Select, Textarea } from '../../components/common/FormField';
import {
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  Legend
} from 'recharts';
import {
  Laptop,
  CheckCircle2,
  PackageCheck,
  Wrench,
  AlertOctagon,
  UserCheck,
  PlusCircle,
  Clock,
  ArrowRight,
  Download,
  FileSpreadsheet,
  UserX,
  UserPlus
} from 'lucide-react';

export const ManagerDashboard: React.FC = () => {
  const [data, setData] = useState<ManagerDashboardData | null>(null);
  const [executives, setExecutives] = useState<Executive[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  // Assign Executive Modal
  const [assignModalOpen, setAssignModalOpen] = useState(false);
  const [selectedRequest, setSelectedRequest] = useState<AssetRequest | null>(null);
  const [selectedExecutiveId, setSelectedExecutiveId] = useState<number>(0);
  const [managerRemarks, setManagerRemarks] = useState('');
  const [submitting, setSubmitting] = useState(false);

  // Reject Modal
  const [rejectModalOpen, setRejectModalOpen] = useState(false);
  const [rejectionReason, setRejectionReason] = useState('');

  const loadData = async () => {
    try {
      setLoading(true);
      const [dash, execs] = await Promise.all([
        dashboardService.getManagerDashboard(),
        executiveService.getExecutives({ pageSize: 50 }),
      ]);
      setData(dash);
      setExecutives(execs.items.filter((e) => e.isActive));
      if (execs.items.length > 0) {
        setSelectedExecutiveId(execs.items[0].userId);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleAssignExecutive = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRequest || !selectedExecutiveId) return;
    setSubmitting(true);
    try {
      await requestService.assignExecutive(selectedRequest.requestId, {
        executiveId: selectedExecutiveId,
        remarks: managerRemarks,
      });
      setAssignModalOpen(false);
      setManagerRemarks('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to assign executive.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleRejectRequest = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRequest || !rejectionReason.trim()) return;
    setSubmitting(true);
    try {
      await requestService.rejectRequest(selectedRequest.requestId, {
        reason: rejectionReason,
      });
      setRejectModalOpen(false);
      setRejectionReason('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to reject request.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="p-8 text-center text-sm text-[#64748B]">Loading manager dashboard...</div>;

  return (
    <div className="space-y-6">
      {/* Header & Quick Action */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">IT Asset Management Overview</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Inventory distribution, request queues, and resource allocations</p>
        </div>
        <div className="flex items-center gap-2.5">
          <Button
            variant="outline"
            leftIcon={<UserPlus className="w-4 h-4 text-[#2563EB]" />}
            onClick={() => navigate('/manager/executives/new')}
          >
            + Add ITS Executive
          </Button>
          <Button
            leftIcon={<PlusCircle className="w-4 h-4" />}
            onClick={() => navigate('/assets/new')}
          >
            + Add Asset
          </Button>
        </div>
      </div>

      {/* 6 Stat Cards from Section 15.3 wireframe */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3.5">
        <StatCard
          title="Total Assets"
          value={data?.totalAssets || 0}
          icon={<Laptop className="w-4 h-4 text-blue-600" />}
          iconBgColor="bg-blue-50"
        />
        <StatCard
          title="Assigned"
          value={data?.assignedAssets || 0}
          icon={<CheckCircle2 className="w-4 h-4 text-emerald-600" />}
          iconBgColor="bg-emerald-50"
        />
        <StatCard
          title="Available"
          value={data?.availableAssets || 0}
          icon={<PackageCheck className="w-4 h-4 text-amber-600" />}
          iconBgColor="bg-amber-50"
        />
        <StatCard
          title="Maintenance"
          value={data?.underRepairAssets || 0}
          icon={<Wrench className="w-4 h-4 text-orange-600" />}
          iconBgColor="bg-orange-50"
        />
        <StatCard
          title="Damaged"
          value={data?.damagedAssets || 0}
          icon={<AlertOctagon className="w-4 h-4 text-red-600" />}
          iconBgColor="bg-red-50"
        />
        <StatCard
          title="Executives"
          value={data?.executiveCount || 0}
          icon={<UserCheck className="w-4 h-4 text-indigo-600" />}
          iconBgColor="bg-indigo-50"
        />
      </div>

      {/* Charts Section: Assets by Category Donut & Assets by Status Bar Chart */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Assets by Category Donut */}
        <Card title="Assets by Category" subtitle="Inventory breakdown across device types">
          <div className="h-64 flex items-center justify-center">
            {data?.assetsByCategory && data.assetsByCategory.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={data.assetsByCategory}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={85}
                    paddingAngle={4}
                    dataKey="value"
                  >
                    {data.assetsByCategory.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value: any) => [`${value} units`, 'Quantity']} />
                  <Legend verticalAlign="bottom" height={36} iconType="circle" />
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <p className="text-xs text-[#64748B]">No category data available</p>
            )}
          </div>
        </Card>

        {/* Asset Status Bar Chart */}
        <Card title="Asset Status Distribution" subtitle="Active operational readiness status">
          <div className="h-64 flex items-center justify-center">
            {data?.assetsByStatus && data.assetsByStatus.length > 0 ? (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data.assetsByStatus} margin={{ top: 20, right: 30, left: 0, bottom: 5 }}>
                  <XAxis dataKey="name" stroke="#64748B" fontSize={12} tickLine={false} />
                  <YAxis stroke="#64748B" fontSize={12} tickLine={false} allowDecimals={false} />
                  <Tooltip cursor={{ fill: '#F1F5F9' }} />
                  <Bar dataKey="value" radius={[6, 6, 0, 0]}>
                    {data.assetsByStatus.map((entry, index) => (
                      <Cell key={`bar-${index}`} fill={entry.color} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <p className="text-xs text-[#64748B]">No status data available</p>
            )}
          </div>
        </Card>
      </div>

      {/* Pending Requests Table (Manager assigns to Executive or Rejects) */}
      <Card
        title="Pending Incoming Requests"
        subtitle={`Requests requiring verification and executive task assignment (${data?.pendingRequestsCount || 0} total)`}
        action={
          <Link to="/requests" className="text-xs font-semibold text-[#2563EB] hover:underline flex items-center gap-1">
            View All Requests <ArrowRight className="w-3.5 h-3.5" />
          </Link>
        }
      >
        {data?.pendingRequests && data.pendingRequests.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Request ID</th>
                  <th className="px-4 py-3">Employee</th>
                  <th className="px-4 py-3">Asset Type</th>
                  <th className="px-4 py-3">Reason</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {data.pendingRequests.map((req) => (
                  <tr key={req.requestId} className="hover:bg-slate-50/60 transition-colors">
                    <td className="px-4 py-3.5 font-bold text-[#0F172A]">{req.requestNumber}</td>
                    <td className="px-4 py-3.5">
                      <p className="font-semibold text-[#0F172A]">{req.employeeName}</p>
                      <span className="text-[11px] text-[#64748B]">{req.employeeCode}</span>
                    </td>
                    <td className="px-4 py-3.5 font-medium text-[#0F172A]">{req.categoryName}</td>
                    <td className="px-4 py-3.5 text-[#64748B] max-w-xs truncate">{req.reason}</td>
                    <td className="px-4 py-3.5">
                      <Badge status={req.statusName} />
                    </td>
                    <td className="px-4 py-3.5 text-right space-x-2">
                      <Button
                        size="sm"
                        onClick={() => {
                          setSelectedRequest(req);
                          setAssignModalOpen(true);
                        }}
                      >
                        Assign Task
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => {
                          setSelectedRequest(req);
                          setRejectModalOpen(true);
                        }}
                      >
                        Reject
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-8 text-xs text-[#64748B]">
            <CheckCircle2 className="w-8 h-8 text-emerald-400 mx-auto mb-2" />
            <p>All incoming requests have been validated and assigned.</p>
          </div>
        )}
      </Card>

      {/* Modal: Assign Task to ITS Executive */}
      <Modal
        isOpen={assignModalOpen}
        onClose={() => setAssignModalOpen(false)}
        title={`Assign Task for ${selectedRequest?.requestNumber}`}
        subtitle={`Delegate request fulfillment for ${selectedRequest?.employeeName} (${selectedRequest?.categoryName})`}
      >
        <form onSubmit={handleAssignExecutive} className="space-y-4">
          <FormField label="Select ITS Executive" required>
            <Select
              value={selectedExecutiveId}
              onChange={(e) => setSelectedExecutiveId(Number(e.target.value))}
              required
            >
              {executives.map((ex) => (
                <option key={ex.userId} value={ex.userId}>
                  {ex.fullName} ({ex.openTasksCount} active tasks)
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Manager Instructions / Remarks">
            <Textarea
              rows={3}
              placeholder="e.g. Approved. Please verify laptop stock and assign an available device."
              value={managerRemarks}
              onChange={(e) => setManagerRemarks(e.target.value)}
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setAssignModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Assign Task
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Reject Request */}
      <Modal
        isOpen={rejectModalOpen}
        onClose={() => setRejectModalOpen(false)}
        title={`Reject Request ${selectedRequest?.requestNumber}`}
        subtitle="A clear rejection reason is mandatory and will be notified to the employee"
      >
        <form onSubmit={handleRejectRequest} className="space-y-4">
          <FormField label="Rejection Reason" required>
            <Textarea
              rows={4}
              placeholder="Specify rationale for rejection (e.g. duplicate request, unapproved project role, policy limit reached)..."
              value={rejectionReason}
              onChange={(e) => setRejectionReason(e.target.value)}
              required
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setRejectModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="danger" isLoading={submitting}>
              Confirm Rejection
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export const ExecutivesPage: React.FC = () => {
  const [executives, setExecutives] = useState<Executive[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  const load = async () => {
    try {
      setLoading(true);
      const res = await executiveService.getExecutives({ pageSize: 50 });
      setExecutives(res.items);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleToggleStatus = async (exec: Executive) => {
    if (exec.openTasksCount > 0 && exec.isActive) {
      alert(`Cannot deactivate ${exec.fullName} while they have ${exec.openTasksCount} open task(s). Reassign tasks first.`);
      return;
    }
    if (!confirm(`Are you sure you want to ${exec.isActive ? 'deactivate' : 'activate'} ${exec.fullName}?`)) return;

    try {
      await executiveService.changeStatus(exec.userId, { isActive: !exec.isActive });
      await load();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update executive status.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">ITS Executives Management</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Manage operational staff accounts responsible for asset checks and allocations</p>
        </div>
        <Button
          leftIcon={<PlusCircle className="w-4 h-4" />}
          onClick={() => navigate('/manager/executives/new')}
        >
          + Add ITS Executive
        </Button>
      </div>

      <Card>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
              <tr>
                <th className="px-4 py-3">Executive Name</th>
                <th className="px-4 py-3">Employee Code</th>
                <th className="px-4 py-3">Username</th>
                <th className="px-4 py-3">Email & Mobile</th>
                <th className="px-4 py-3">Open Tasks</th>
                <th className="px-4 py-3">Completed</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#E2E8F0]">
              {executives.map((ex) => (
                <tr key={ex.userId} className="hover:bg-slate-50/60">
                  <td className="px-4 py-3.5 font-bold text-[#0F172A]">{ex.fullName}</td>
                  <td className="px-4 py-3.5 font-mono text-slate-700">{ex.employeeCode}</td>
                  <td className="px-4 py-3.5 text-slate-700">{ex.username}</td>
                  <td className="px-4 py-3.5 text-[#64748B]">
                    <div>{ex.email}</div>
                    <div className="text-[11px]">{ex.mobile || '-'}</div>
                  </td>
                  <td className="px-4 py-3.5 font-semibold text-blue-600">{ex.openTasksCount}</td>
                  <td className="px-4 py-3.5 font-semibold text-emerald-600">{ex.completedTasksCount}</td>
                  <td className="px-4 py-3.5">
                    <Badge status={ex.isActive ? 'Active' : 'Inactive'} />
                  </td>
                  <td className="px-4 py-3.5 text-right">
                    <Button
                      size="sm"
                      variant={ex.isActive ? 'outline' : 'success'}
                      onClick={() => handleToggleStatus(ex)}
                    >
                      {ex.isActive ? 'Deactivate' : 'Activate'}
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
};

export const AddExecutivePage: React.FC = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    firstName: '',
    lastName: '',
    employeeCode: '',
    email: '',
    mobile: '',
    username: '',
    temporaryPassword: 'Password@123',
    isActive: true,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await executiveService.createExecutive(formData);
      navigate('/manager/executives');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create ITS Executive.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div>
        <Link to="/manager/executives" className="text-xs text-[#2563EB] font-medium hover:underline flex items-center gap-1 mb-1">
          ← Back to Executives
        </Link>
        <h2 className="text-xl font-bold text-[#0F172A]">Add ITS Executive</h2>
        <p className="text-xs text-[#64748B] mt-0.5">
          Link to existing employee record or create employee and user account with ITS_EXECUTIVE role
        </p>
      </div>

      <Card>
        {error && (
          <div className="mb-4 p-3 rounded-[8px] bg-red-50 border border-red-200 text-red-700 text-xs">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="First Name" required>
              <Input
                value={formData.firstName}
                onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                required
              />
            </FormField>
            <FormField label="Last Name" required>
              <Input
                value={formData.lastName}
                onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                required
              />
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="Employee Code" required hint="e.g. EMP020">
              <Input
                value={formData.employeeCode}
                onChange={(e) => setFormData({ ...formData, employeeCode: e.target.value })}
                required
              />
            </FormField>
            <FormField label="Email Address" required>
              <Input
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                required
              />
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="Mobile Number">
              <Input
                value={formData.mobile}
                onChange={(e) => setFormData({ ...formData, mobile: e.target.value })}
              />
            </FormField>
            <FormField label="Username" required>
              <Input
                value={formData.username}
                onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                required
              />
            </FormField>
          </div>

          <FormField label="Temporary Password" required hint="Executive will be prompted to change password upon first login.">
            <Input
              type="text"
              value={formData.temporaryPassword}
              onChange={(e) => setFormData({ ...formData, temporaryPassword: e.target.value })}
              required
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-[#E2E8F0]">
            <Button type="button" variant="outline" onClick={() => navigate('/manager/executives')}>
              Cancel
            </Button>
            <Button type="submit" isLoading={loading}>
              Create Executive Account
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
};

export const ReportsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'assets' | 'allocations' | 'repairs'>('assets');
  const [assetData, setAssetData] = useState<any[]>([]);
  const [allocData, setAllocData] = useState<any[]>([]);
  const [repairData, setRepairData] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetch = async () => {
      setLoading(true);
      try {
        if (activeTab === 'assets') {
          const res = await reportService.getAssetReportData();
          setAssetData(res);
        } else if (activeTab === 'allocations') {
          const res = await reportService.getAllocationReportData();
          setAllocData(res);
        } else {
          const res = await reportService.getRepairReportData();
          setRepairData(res);
        }
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetch();
  }, [activeTab]);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">ITS Reports & CSV Export</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Generate and download audit reports for assets, allocations, and repairs</p>
        </div>
        <div className="flex items-center gap-2">
          {activeTab === 'assets' && (
            <Button
              variant="outline"
              leftIcon={<Download className="w-4 h-4 text-blue-600" />}
              onClick={() => reportService.downloadAssetReportCsv()}
            >
              Export Assets CSV
            </Button>
          )}
          {activeTab === 'allocations' && (
            <Button
              variant="outline"
              leftIcon={<Download className="w-4 h-4 text-emerald-600" />}
              onClick={() => reportService.downloadAllocationReportCsv()}
            >
              Export Allocations CSV
            </Button>
          )}
          {activeTab === 'repairs' && (
            <Button
              variant="outline"
              leftIcon={<Download className="w-4 h-4 text-orange-600" />}
              onClick={() => reportService.downloadRepairReportCsv()}
            >
              Export Repairs CSV
            </Button>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b border-[#E2E8F0]">
        <button
          onClick={() => setActiveTab('assets')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-all ${
            activeTab === 'assets' ? 'border-[#2563EB] text-[#2563EB]' : 'border-transparent text-[#64748B] hover:text-[#0F172A]'
          }`}
        >
          Asset Inventory Report
        </button>
        <button
          onClick={() => setActiveTab('allocations')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-all ${
            activeTab === 'allocations' ? 'border-[#2563EB] text-[#2563EB]' : 'border-transparent text-[#64748B] hover:text-[#0F172A]'
          }`}
        >
          Allocation History Report
        </button>
        <button
          onClick={() => setActiveTab('repairs')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-all ${
            activeTab === 'repairs' ? 'border-[#2563EB] text-[#2563EB]' : 'border-transparent text-[#64748B] hover:text-[#0F172A]'
          }`}
        >
          Maintenance & Repairs Report
        </button>
      </div>

      {/* Table Data */}
      <Card>
        {loading ? (
          <div className="py-8 text-center text-xs text-[#64748B]">Loading report records...</div>
        ) : activeTab === 'assets' ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Asset Code</th>
                  <th className="px-4 py-3">Asset Name</th>
                  <th className="px-4 py-3">Category</th>
                  <th className="px-4 py-3">Serial No</th>
                  <th className="px-4 py-3">Cost (INR)</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Assigned To</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {assetData.map((a: any) => (
                  <tr key={a.assetId} className="hover:bg-slate-50">
                    <td className="px-4 py-3 font-bold text-[#0F172A]">{a.assetCode}</td>
                    <td className="px-4 py-3">{a.assetName}</td>
                    <td className="px-4 py-3">{a.category}</td>
                    <td className="px-4 py-3 font-mono text-slate-600">{a.serialNumber}</td>
                    <td className="px-4 py-3">₹{a.purchaseCost?.toLocaleString('en-IN')}</td>
                    <td className="px-4 py-3"><Badge status={a.status} /></td>
                    <td className="px-4 py-3 font-medium text-slate-700">{a.assignedTo}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : activeTab === 'allocations' ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Asset</th>
                  <th className="px-4 py-3">Employee</th>
                  <th className="px-4 py-3">Allocation Date</th>
                  <th className="px-4 py-3">Return Date</th>
                  <th className="px-4 py-3">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {allocData.map((al: any) => (
                  <tr key={al.allocationId} className="hover:bg-slate-50">
                    <td className="px-4 py-3 font-bold text-[#0F172A]">{al.assetCode} ({al.assetName})</td>
                    <td className="px-4 py-3">{al.employeeName} ({al.employeeCode})</td>
                    <td className="px-4 py-3">{al.allocationDate}</td>
                    <td className="px-4 py-3">{al.returnDate || '-'}</td>
                    <td className="px-4 py-3"><Badge status={al.status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Asset</th>
                  <th className="px-4 py-3">Reported By</th>
                  <th className="px-4 py-3">Issue Description</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Vendor</th>
                  <th className="px-4 py-3">Cost (INR)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {repairData.map((r: any) => (
                  <tr key={r.repairId} className="hover:bg-slate-50">
                    <td className="px-4 py-3 font-bold text-[#0F172A]">{r.assetCode}</td>
                    <td className="px-4 py-3">{r.reportedBy}</td>
                    <td className="px-4 py-3">{r.issueDescription}</td>
                    <td className="px-4 py-3"><Badge status={r.status} /></td>
                    <td className="px-4 py-3">{r.vendorName || '-'}</td>
                    <td className="px-4 py-3">{r.repairCost ? `₹${r.repairCost}` : '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
};
