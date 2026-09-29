import React, { useState, useEffect } from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { dashboardService, requestService, assetService, repairService, replacementService } from '../../api/services';
import { EmployeeDashboardData, AssetRequest, Asset, AssetCategory } from '../../types';
import { Card, StatCard } from '../../components/common/Card';
import { Button } from '../../components/common/Button';
import { Badge } from '../../components/common/Badge';
import { Modal } from '../../components/common/Modal';
import { FormField, Input, Select, Textarea } from '../../components/common/FormField';
import {
  Laptop,
  FileText,
  PlusCircle,
  AlertTriangle,
  RefreshCw,
  RotateCcw,
  Clock,
  CheckCircle,
  XCircle,
  Eye,
  Calendar,
  IndianRupee,
  Layers,
  ArrowRight,
  ShieldAlert
} from 'lucide-react';

export const EmployeeDashboard: React.FC = () => {
  const { user } = useAuth();
  const [data, setData] = useState<EmployeeDashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const navigate = useNavigate();

  // Modals
  const [showRequestModal, setShowRequestModal] = useState(false);
  const [showRepairModal, setShowRepairModal] = useState(false);
  const [showReplaceModal, setShowReplaceModal] = useState(false);
  const [showReturnModal, setShowReturnModal] = useState(false);
  const [selectedAssetId, setSelectedAssetId] = useState<number | null>(null);

  // Form states
  const [requestCatId, setRequestCatId] = useState<number>(1);
  const [requestReason, setRequestReason] = useState('');
  const [requestDate, setRequestDate] = useState('');
  const [requestPriority, setRequestPriority] = useState('Normal');
  const [issueDescription, setIssueDescription] = useState('');
  const [replaceReason, setReplaceReason] = useState('');
  const [returnRemarks, setReturnRemarks] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const loadDashboard = async () => {
    try {
      setLoading(true);
      const [dashData, catData] = await Promise.all([
        dashboardService.getEmployeeDashboard(),
        assetService.getCategories(),
      ]);
      setData(dashData);
      setCategories(catData);
      if (catData.length > 0) setRequestCatId(catData[0].assetCategoryId);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadDashboard();
  }, []);

  const handleCreateRequest = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    try {
      await requestService.createRequest({
        assetCategoryId: requestCatId,
        reason: requestReason,
        requiredDate: requestDate ? new Date(requestDate).toISOString() : undefined,
        priority: requestPriority,
      });
      setShowRequestModal(false);
      setRequestReason('');
      setRequestDate('');
      await loadDashboard();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to submit asset request.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleReportIssue = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAssetId) return;
    setSubmitting(true);
    try {
      await repairService.createRepair({
        assetId: selectedAssetId,
        issueDescription,
      });
      setShowRepairModal(false);
      setIssueDescription('');
      await loadDashboard();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to report repair issue.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleRequestReplacement = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAssetId) return;
    setSubmitting(true);
    try {
      await replacementService.createReplacement({
        oldAssetId: selectedAssetId,
        reason: replaceReason,
      });
      setShowReplaceModal(false);
      setReplaceReason('');
      await loadDashboard();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to request replacement.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleReturnAsset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAssetId) return;
    setSubmitting(true);
    try {
      await assetService.returnAsset(selectedAssetId, {
        returnDate: new Date().toISOString(),
        conditionAtReturn: 'Good',
        remarks: returnRemarks,
      });
      setShowReturnModal(false);
      setReturnRemarks('');
      await loadDashboard();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to return asset.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <div className="p-8 text-center text-sm text-[#64748B]">Loading employee dashboard...</div>;
  }

  return (
    <div className="space-y-6">
      {/* Welcome Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-white border border-[#E2E8F0] rounded-[10px] p-6 shadow-sm">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">Welcome, {user?.fullName}!</h2>
          <p className="text-xs text-[#64748B] mt-1">
            Employee Code: <span className="font-semibold text-slate-800">{user?.employeeCode}</span> • {user?.designation || 'Staff'} • {user?.departmentName || 'Operations'}
          </p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <Button
            leftIcon={<PlusCircle className="w-4 h-4" />}
            onClick={() => setShowRequestModal(true)}
          >
            Request Asset
          </Button>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <StatCard
          title="My Allocated Assets"
          value={data?.activeAssetsCount || 0}
          icon={<Laptop className="w-5 h-5 text-blue-600" />}
          iconBgColor="bg-blue-50"
          description="Hardware items currently assigned to you"
        />
        <StatCard
          title="Pending Requests"
          value={data?.pendingRequestsCount || 0}
          icon={<Clock className="w-5 h-5 text-amber-600" />}
          iconBgColor="bg-amber-50"
          description="Requests waiting for manager or executive action"
        />
        <StatCard
          title="Total IT Requests"
          value={data?.myRequests?.length || 0}
          icon={<FileText className="w-5 h-5 text-emerald-600" />}
          iconBgColor="bg-emerald-50"
          description="Lifetime requested items"
        />
      </div>

      {/* Grid: My IT Requests & My Assets */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* My IT Requests */}
        <Card
          title="My IT Requests"
          subtitle="Track your asset requisition status"
          action={
            <Button size="sm" variant="outline" onClick={() => setShowRequestModal(true)}>
              + New Request
            </Button>
          }
        >
          {data?.myRequests && data.myRequests.length > 0 ? (
            <div className="divide-y divide-[#E2E8F0]">
              {data.myRequests.slice(0, 5).map((req) => (
                <div key={req.requestId} className="py-3.5 flex items-center justify-between">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-bold text-[#0F172A]">{req.requestNumber}</span>
                      <span className="text-xs text-[#64748B]">• {req.categoryName}</span>
                    </div>
                    <p className="text-xs text-[#64748B] mt-0.5 line-clamp-1">{req.reason}</p>
                    <span className="text-[11px] text-slate-400 mt-1 block">
                      Submitted on {new Date(req.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                  <div className="flex items-center gap-3">
                    <Badge status={req.statusName} />
                    <Link
                      to={`/employee/requests/${req.requestId}`}
                      className="p-1.5 rounded-lg border border-[#E2E8F0] hover:bg-slate-50 text-[#2563EB] text-xs font-medium inline-flex items-center gap-1"
                    >
                      <Eye className="w-3.5 h-3.5" />
                      <span>View</span>
                    </Link>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-center py-8 text-xs text-[#64748B]">
              <FileText className="w-8 h-8 text-slate-300 mx-auto mb-2" />
              <p>No active requests raised yet.</p>
            </div>
          )}
        </Card>

        {/* My Assets */}
        <Card
          title="My Assets"
          subtitle="Hardware currently assigned to your profile"
          action={
            data?.myAssets && data.myAssets.length > 0 ? (
              <span className="text-xs font-semibold text-[#16A34A] bg-[#DCFCE7] px-2 py-0.5 rounded-full">
                {data.myAssets.length} Active
              </span>
            ) : null
          }
        >
          {data?.myAssets && data.myAssets.length > 0 ? (
            <div className="space-y-4">
              {data.myAssets.map((asset) => (
                <div
                  key={asset.assetId}
                  className="p-4 rounded-[8px] border border-[#E2E8F0] bg-[#F8FAFC]/50 flex flex-col gap-3"
                >
                  <div className="flex items-start justify-between">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-bold text-[#0F172A]">{asset.assetName}</span>
                        <Badge status={asset.statusName} />
                      </div>
                      <p className="text-xs text-[#64748B] mt-0.5">
                        Code: <span className="font-semibold text-slate-800">{asset.assetCode}</span> • S/N: {asset.serialNumber}
                      </p>
                    </div>
                    <span className="text-xs font-semibold text-slate-700">
                      ₹{asset.purchaseCost?.toLocaleString('en-IN')}
                    </span>
                  </div>

                  <div className="text-xs text-[#64748B] flex items-center gap-4 flex-wrap border-t border-slate-200/60 pt-2">
                    <span>Category: <strong>{asset.categoryName}</strong></span>
                    <span>Location: <strong>{asset.location || 'Assigned'}</strong></span>
                    {asset.expectedReturnDate && (
                      <span>Expected Return: <strong>{new Date(asset.expectedReturnDate).toLocaleDateString()}</strong></span>
                    )}
                  </div>

                  {/* Actions for this asset */}
                  <div className="flex items-center gap-2 pt-1 border-t border-slate-200/60 flex-wrap">
                    <Button
                      size="sm"
                      variant="outline"
                      leftIcon={<AlertTriangle className="w-3.5 h-3.5 text-amber-600" />}
                      onClick={() => {
                        setSelectedAssetId(asset.assetId);
                        setShowRepairModal(true);
                      }}
                    >
                      Report Issue
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      leftIcon={<RefreshCw className="w-3.5 h-3.5 text-blue-600" />}
                      onClick={() => {
                        setSelectedAssetId(asset.assetId);
                        setShowReplaceModal(true);
                      }}
                    >
                      Request Replacement
                    </Button>
                    <Button
                      size="sm"
                      variant="outline"
                      leftIcon={<RotateCcw className="w-3.5 h-3.5 text-slate-600" />}
                      onClick={() => {
                        setSelectedAssetId(asset.assetId);
                        setShowReturnModal(true);
                      }}
                    >
                      Return Asset
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="text-center py-8 text-xs text-[#64748B]">
              <Laptop className="w-8 h-8 text-slate-300 mx-auto mb-2" />
              <p>You currently do not have any assigned assets.</p>
            </div>
          )}
        </Card>
      </div>

      {/* Modal: Request Asset */}
      <Modal
        isOpen={showRequestModal}
        onClose={() => setShowRequestModal(false)}
        title="Request IT Asset"
        subtitle="Submit a new hardware request to the ITS department"
      >
        <form onSubmit={handleCreateRequest} className="space-y-4">
          <FormField label="Asset Type / Category" required>
            <Select
              value={requestCatId}
              onChange={(e) => setRequestCatId(Number(e.target.value))}
              required
            >
              {categories.map((c) => (
                <option key={c.assetCategoryId} value={c.assetCategoryId}>
                  {c.categoryName} ({c.availableAssetsCount} in stock)
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Priority" required>
            <Select value={requestPriority} onChange={(e) => setRequestPriority(e.target.value)}>
              <option value="Low">Low</option>
              <option value="Normal">Normal</option>
              <option value="High">High</option>
            </Select>
          </FormField>

          <FormField label="Required Date">
            <Input
              type="date"
              value={requestDate}
              onChange={(e) => setRequestDate(e.target.value)}
            />
          </FormField>

          <FormField label="Business Reason & Specification Needs" required>
            <Textarea
              rows={3}
              placeholder="Explain why you need this asset (e.g. development work, testing, peripheral setup)..."
              value={requestReason}
              onChange={(e) => setRequestReason(e.target.value)}
              required
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setShowRequestModal(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Submit Request
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Report Repair Issue */}
      <Modal
        isOpen={showRepairModal}
        onClose={() => setShowRepairModal(false)}
        title="Report Asset Issue"
        subtitle="Notify ITS maintenance team of hardware failure or defect"
      >
        <form onSubmit={handleReportIssue} className="space-y-4">
          <FormField label="Describe the issue / malfunction in detail" required>
            <Textarea
              rows={4}
              placeholder="Describe what is not working (e.g., screen flickering, battery not charging, port damage)..."
              value={issueDescription}
              onChange={(e) => setIssueDescription(e.target.value)}
              required
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setShowRepairModal(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="danger" isLoading={submitting}>
              Report Issue
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Request Replacement */}
      <Modal
        isOpen={showReplaceModal}
        onClose={() => setShowReplaceModal(false)}
        title="Request Asset Replacement"
        subtitle="Ask Manager approval to swap current asset for an alternate device"
      >
        <form onSubmit={handleRequestReplacement} className="space-y-4">
          <FormField label="Reason for Replacement" required>
            <Textarea
              rows={4}
              placeholder="State justification (e.g., outdated hardware, continuous failures, upgraded project role requirement)..."
              value={replaceReason}
              onChange={(e) => setReplaceReason(e.target.value)}
              required
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setShowReplaceModal(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Submit for Manager Approval
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Return Asset */}
      <Modal
        isOpen={showReturnModal}
        onClose={() => setShowReturnModal(false)}
        title="Return IT Asset"
        subtitle="Initiate return of asset back to IT inventory stock"
      >
        <form onSubmit={handleReturnAsset} className="space-y-4">
          <FormField label="Return Remarks">
            <Textarea
              rows={3}
              placeholder="e.g. Project completed, no longer needed, leaving team..."
              value={returnRemarks}
              onChange={(e) => setReturnRemarks(e.target.value)}
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setShowReturnModal(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Confirm Return
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export const RequestDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [request, setRequest] = useState<AssetRequest | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetch = async () => {
      if (!id) return;
      try {
        setLoading(true);
        const data = await requestService.getRequestById(Number(id));
        setRequest(data);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetch();
  }, [id]);

  if (loading) return <div className="p-8 text-center text-sm text-[#64748B]">Loading request details...</div>;
  if (!request) return <div className="p-8 text-center text-sm text-red-600">Request not found.</div>;

  // Timeline steps
  const steps = [
    { title: 'Submitted', desc: 'Employee created request', status: 'done' },
    {
      title: 'Validation & Assignment',
      desc: request.assignedExecutiveName ? `Assigned to ${request.assignedExecutiveName}` : 'Under Manager Review',
      status: request.status === 'Submitted' ? 'current' : request.status === 'Rejected' ? 'rejected' : 'done',
    },
    {
      title: 'Inventory & Warranty Check',
      desc: 'Executive checks availability, condition, warranty',
      status: request.status === 'AssignedToExecutive' || request.status === 'Checking' ? 'current' : request.status === 'Allocated' ? 'done' : request.status === 'NotAvailable' ? 'warning' : 'pending',
    },
    {
      title: 'Asset Allocation',
      desc: request.allocatedAssetCode ? `Allocated ${request.allocatedAssetCode}` : 'Asset Handover',
      status: request.status === 'Allocated' ? 'done' : request.status === 'Rejected' ? 'rejected' : 'pending',
    },
  ];

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      <div className="flex items-center justify-between">
        <div>
          <Link to="/employee/dashboard" className="text-xs text-[#2563EB] font-medium hover:underline flex items-center gap-1 mb-1">
            ← Back to Dashboard
          </Link>
          <div className="flex items-center gap-3">
            <h2 className="text-xl font-bold text-[#0F172A]">Request {request.requestNumber}</h2>
            <Badge status={request.statusName} />
          </div>
        </div>
      </div>

      {/* Stepper Timeline */}
      <Card title="Request Lifecycle Timeline">
        <div className="py-2">
          <div className="relative flex items-center justify-between">
            <div className="absolute left-0 top-1/2 -translate-y-1/2 h-0.5 w-full bg-slate-200 z-0" />
            {steps.map((step, idx) => (
              <div key={idx} className="relative z-10 flex flex-col items-center text-center">
                <div
                  className={`w-8 h-8 rounded-full flex items-center justify-center font-bold text-xs ${
                    step.status === 'done'
                      ? 'bg-[#16A34A] text-white'
                      : step.status === 'current'
                      ? 'bg-[#2563EB] text-white ring-4 ring-blue-100'
                      : step.status === 'rejected'
                      ? 'bg-[#DC2626] text-white'
                      : step.status === 'warning'
                      ? 'bg-[#D97706] text-white'
                      : 'bg-slate-200 text-slate-500'
                  }`}
                >
                  {idx + 1}
                </div>
                <span className="text-xs font-bold text-[#0F172A] mt-2">{step.title}</span>
                <span className="text-[11px] text-[#64748B] max-w-[150px] mt-0.5">{step.desc}</span>
              </div>
            ))}
          </div>
        </div>
      </Card>

      {/* Details Card */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Card title="Request Information">
          <dl className="space-y-3 text-xs">
            <div>
              <dt className="text-[#64748B]">Asset Type</dt>
              <dd className="font-semibold text-[#0F172A] text-sm mt-0.5">{request.categoryName}</dd>
            </div>
            <div>
              <dt className="text-[#64748B]">Business Reason</dt>
              <dd className="font-medium text-[#0F172A] mt-0.5 bg-slate-50 p-2.5 rounded-[8px] border border-slate-100">{request.reason}</dd>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <dt className="text-[#64748B]">Priority</dt>
                <dd className="mt-0.5"><Badge status={request.priorityName} /></dd>
              </div>
              <div>
                <dt className="text-[#64748B]">Required By</dt>
                <dd className="font-medium text-[#0F172A] mt-0.5">{request.requiredDate ? new Date(request.requiredDate).toLocaleDateString() : 'ASAP'}</dd>
              </div>
            </div>
            {request.rejectionReason && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-[8px]">
                <dt className="font-bold text-red-800">Rejection Reason</dt>
                <dd className="text-red-700 mt-1">{request.rejectionReason}</dd>
              </div>
            )}
          </dl>
        </Card>

        <Card title="Allocation Details">
          {request.allocatedAssetId ? (
            <div className="space-y-3 text-xs">
              <div className="p-4 bg-emerald-50 border border-emerald-200 rounded-[8px]">
                <span className="text-xs font-bold text-emerald-800 block">Asset Successfully Allocated</span>
                <span className="text-sm font-semibold text-emerald-900 mt-1 block">
                  {request.allocatedAssetName} ({request.allocatedAssetCode})
                </span>
              </div>
              <div>
                <span className="text-[#64748B] block">Assigned ITS Executive</span>
                <span className="font-semibold text-[#0F172A] mt-0.5 block">{request.assignedExecutiveName || 'ITS Team'}</span>
              </div>
            </div>
          ) : (
            <div className="text-xs text-[#64748B] py-6 text-center">
              <Clock className="w-8 h-8 text-slate-300 mx-auto mb-2" />
              <p>Asset has not been allocated yet.</p>
              <p className="text-[11px] text-slate-400 mt-1">Status: {request.statusName}</p>
            </div>
          )}
        </Card>
      </div>

      {/* History Timeline Logs */}
      {request.history && request.history.length > 0 && (
        <Card title="Audit History Log">
          <div className="divide-y divide-slate-100 text-xs">
            {request.history.map((h) => (
              <div key={h.historyId} className="py-2.5 flex items-start justify-between">
                <div>
                  <span className="font-semibold text-[#0F172A]">{h.remarks || `Status transitioned to ${h.toStatus}`}</span>
                  <span className="text-[11px] text-[#64748B] block mt-0.5">By {h.actionByName}</span>
                </div>
                <span className="text-[11px] text-slate-400">{new Date(h.createdAt).toLocaleString()}</span>
              </div>
            ))}
          </div>
        </Card>
      )}
    </div>
  );
};
