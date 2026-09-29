import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { dashboardService, requestService, assetService, repairService } from '../../api/services';
import {
  ExecutiveDashboardData,
  AssetRequest,
  Asset,
  AssetCheckResult,
  AssetRepair
} from '../../types';
import { Card, StatCard } from '../../components/common/Card';
import { Button } from '../../components/common/Button';
import { Badge } from '../../components/common/Badge';
import { Modal } from '../../components/common/Modal';
import { FormField, Input, Select, Textarea } from '../../components/common/FormField';
import {
  CheckSquare,
  PackageCheck,
  Wrench,
  Clock,
  ArrowRight,
  ShieldCheck,
  AlertTriangle,
  RotateCcw,
  CheckCircle2,
  XCircle,
  Search,
  Eye,
  Send,
  AlertCircle
} from 'lucide-react';

export const ExecutiveDashboard: React.FC = () => {
  const [data, setData] = useState<ExecutiveDashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  // Return Modal & Repair Modal for quick actions
  const [showReturnModal, setShowReturnModal] = useState(false);
  const [showRepairModal, setShowRepairModal] = useState(false);
  const [availableAssets, setAvailableAssets] = useState<Asset[]>([]);
  const [allAssets, setAllAssets] = useState<Asset[]>([]);
  const [selectedAssetId, setSelectedAssetId] = useState<number>(0);
  const [returnRemarks, setReturnRemarks] = useState('');
  const [returnCondition, setReturnCondition] = useState('Good');
  const [repairIssue, setRepairIssue] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const loadData = async () => {
    try {
      setLoading(true);
      const [dash, avail, assets] = await Promise.all([
        dashboardService.getExecutiveDashboard(),
        assetService.getAvailableAssets(),
        assetService.getAssets({ pageSize: 100 }),
      ]);
      setData(dash);
      setAvailableAssets(avail);
      setAllAssets(assets.items);
      if (assets.items.length > 0) setSelectedAssetId(assets.items[0].assetId);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleReturnAsset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAssetId) return;
    setSubmitting(true);
    try {
      await assetService.returnAsset(selectedAssetId, {
        returnDate: new Date().toISOString(),
        conditionAtReturn: returnCondition,
        remarks: returnRemarks,
      });
      setShowReturnModal(false);
      setReturnRemarks('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to process return.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleCreateRepair = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedAssetId || !repairIssue.trim()) return;
    setSubmitting(true);
    try {
      await repairService.createRepair({
        assetId: selectedAssetId,
        issueDescription: repairIssue,
      });
      setShowRepairModal(false);
      setRepairIssue('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to report repair.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="p-8 text-center text-sm text-[#64748B]">Loading executive dashboard...</div>;

  return (
    <div className="space-y-6">
      {/* Header & Quick Action Buttons per wireframe */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">ITS Executive Workspace</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Validate inventory readiness, run warranty audits, and fulfill employee hardware tasks</p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            leftIcon={<RotateCcw className="w-4 h-4 text-slate-600" />}
            onClick={() => setShowReturnModal(true)}
          >
            Process Return
          </Button>
          <Button
            variant="outline"
            size="sm"
            leftIcon={<Wrench className="w-4 h-4 text-orange-600" />}
            onClick={() => setShowRepairModal(true)}
          >
            Send For Repair
          </Button>
        </div>
      </div>

      {/* 4 Stat Cards from Section 16 wireframe */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          title="My Pending Tasks"
          value={data?.myPendingTasksCount || 0}
          icon={<CheckSquare className="w-5 h-5 text-blue-600" />}
          iconBgColor="bg-blue-50"
          description="Requests waiting for your validation"
        />
        <StatCard
          title="Asset Requests"
          value={data?.assetRequestsCount || 0}
          icon={<Clock className="w-5 h-5 text-amber-600" />}
          iconBgColor="bg-amber-50"
          description="Assigned allocation queues"
        />
        <StatCard
          title="Assets to Assign"
          value={data?.assetsToAssignCount || 0}
          icon={<PackageCheck className="w-5 h-5 text-emerald-600" />}
          iconBgColor="bg-emerald-50"
          description="Stock items in Available status"
        />
        <StatCard
          title="Repair Requests"
          value={data?.repairRequestsCount || 0}
          icon={<Wrench className="w-5 h-5 text-orange-600" />}
          iconBgColor="bg-orange-50"
          description="Hardware under maintenance"
        />
      </div>

      {/* My Tasks Table (REQ-001 | Employee | Asset Type | Reason | Status | [Check Asset]) */}
      <Card
        title="My Assigned Tasks Queue"
        subtitle="Validate stock availability, device working condition, and warranty before allocating"
      >
        {data?.myAssignedTasks && data.myAssignedTasks.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Task / Request ID</th>
                  <th className="px-4 py-3">Employee</th>
                  <th className="px-4 py-3">Required Asset</th>
                  <th className="px-4 py-3">Business Reason</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {data.myAssignedTasks.map((task) => (
                  <tr key={task.requestId} className="hover:bg-slate-50/60 transition-colors">
                    <td className="px-4 py-3.5 font-bold text-[#0F172A]">{task.requestNumber}</td>
                    <td className="px-4 py-3.5">
                      <p className="font-semibold text-[#0F172A]">{task.employeeName}</p>
                      <span className="text-[11px] text-[#64748B]">{task.employeeCode}</span>
                    </td>
                    <td className="px-4 py-3.5 font-medium text-[#0F172A]">{task.categoryName}</td>
                    <td className="px-4 py-3.5 text-[#64748B] max-w-xs truncate">{task.reason}</td>
                    <td className="px-4 py-3.5">
                      <Badge status={task.statusName} />
                    </td>
                    <td className="px-4 py-3.5 text-right">
                      <Link
                        to={`/executive/tasks/${task.requestId}`}
                        className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-[8px] bg-[#2563EB] hover:bg-[#1D4ED8] text-white font-medium text-xs shadow-sm transition-all"
                      >
                        <ShieldCheck className="w-3.5 h-3.5" />
                        <span>Check Asset</span>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-8 text-xs text-[#64748B]">
            <CheckCircle2 className="w-8 h-8 text-emerald-400 mx-auto mb-2" />
            <p>You have no pending tasks. Great job!</p>
          </div>
        )}
      </Card>

      {/* Modal: Process Return */}
      <Modal
        isOpen={showReturnModal}
        onClose={() => setShowReturnModal(false)}
        title="Process Asset Return"
        subtitle="Inspect and receive returned hardware back into ITS inventory"
      >
        <form onSubmit={handleReturnAsset} className="space-y-4">
          <FormField label="Select Asset to Return" required>
            <Select
              value={selectedAssetId}
              onChange={(e) => setSelectedAssetId(Number(e.target.value))}
              required
            >
              {allAssets
                .filter((a) => a.status === 'Assigned')
                .map((a) => (
                  <option key={a.assetId} value={a.assetId}>
                    {a.assetCode} - {a.assetName} (Assigned to {a.assignedEmployeeName || 'Employee'})
                  </option>
                ))}
            </Select>
          </FormField>

          <FormField label="Condition at Return" required>
            <Select value={returnCondition} onChange={(e) => setReturnCondition(e.target.value)}>
              <option value="Good">Good (Restock to Available)</option>
              <option value="Fair">Fair (Minor wear, Available)</option>
              <option value="Damaged">Damaged (Move to Damaged)</option>
              <option value="NotWorking">Not Working (Send to Maintenance)</option>
            </Select>
          </FormField>

          <FormField label="Return Inspection Remarks">
            <Textarea
              rows={3}
              placeholder="Physical inspection condition, charger present, wiped..."
              value={returnRemarks}
              onChange={(e) => setReturnRemarks(e.target.value)}
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setShowReturnModal(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Confirm & Return to Stock
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Send For Repair */}
      <Modal
        isOpen={showRepairModal}
        onClose={() => setShowRepairModal(false)}
        title="Create Asset Repair Order"
        subtitle="Move defective hardware to UnderMaintenance / InRepair"
      >
        <form onSubmit={handleCreateRepair} className="space-y-4">
          <FormField label="Select Asset" required>
            <Select
              value={selectedAssetId}
              onChange={(e) => setSelectedAssetId(Number(e.target.value))}
              required
            >
              {allAssets.map((a) => (
                <option key={a.assetId} value={a.assetId}>
                  {a.assetCode} - {a.assetName} ({a.status})
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Issue Description" required>
            <Textarea
              rows={3}
              placeholder="Detail the failure symptoms and vendor service required..."
              value={repairIssue}
              onChange={(e) => setRepairIssue(e.target.value)}
              required
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setShowRepairModal(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="danger" isLoading={submitting}>
              Submit Maintenance Order
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export const TaskDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [request, setRequest] = useState<AssetRequest | null>(null);
  const [candidateAssets, setCandidateAssets] = useState<Asset[]>([]);
  const [checksMap, setChecksMap] = useState<Record<number, AssetCheckResult>>({});
  const [loading, setLoading] = useState(true);
  const [checking, setChecking] = useState(false);
  const [allocatingId, setAllocatingId] = useState<number | null>(null);
  const [rejectModalOpen, setRejectModalOpen] = useState(false);
  const [rejectionReason, setRejectionReason] = useState('');
  const [submittingReject, setSubmittingReject] = useState(false);
  const navigate = useNavigate();

  const loadTask = async () => {
    if (!id) return;
    try {
      setLoading(true);
      const req = await requestService.getRequestById(Number(id));
      setRequest(req);

      // Fetch all assets in matching category
      const assets = await assetService.getAssets({
        categoryId: req.assetCategoryId,
        pageSize: 50,
      });
      setCandidateAssets(assets.items);

      // Automatically run checks for all matching assets
      const results: Record<number, AssetCheckResult> = {};
      for (const ast of assets.items) {
        try {
          const chk = await assetService.checkAsset(ast.assetId, req.employeeId);
          results[ast.assetId] = chk;
        } catch {
          // ignore
        }
      }
      setChecksMap(results);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadTask();
  }, [id]);

  const handleAllocateAsset = async (assetId: number) => {
    if (!request) return;
    if (!confirm(`Are you sure you want to allocate this asset to ${request.employeeName}?`)) return;

    setAllocatingId(assetId);
    try {
      await requestService.allocateRequest(request.requestId, {
        assetId,
        condition: 'Good',
        remarks: `Allocated by ITS Executive in response to ${request.requestNumber}`,
      });
      alert('Asset allocated successfully!');
      navigate('/executive/dashboard');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Allocation failed.');
    } finally {
      setAllocatingId(null);
    }
  };

  const handleSendToRepair = async (assetId: number) => {
    try {
      await repairService.createRepair({
        assetId,
        issueDescription: 'Flagged as damaged during allocation eligibility inspection.',
      });
      alert('Asset marked as Under Maintenance and sent to repair.');
      await loadTask();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update asset status.');
    }
  };

  const handleReject = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!request || !rejectionReason.trim()) return;
    setSubmittingReject(true);
    try {
      await requestService.rejectRequest(request.requestId, {
        reason: rejectionReason,
      });
      setRejectModalOpen(false);
      navigate('/executive/dashboard');
    } catch (err: any) {
      alert(err.response?.data?.message || 'Rejection failed.');
    } finally {
      setSubmittingReject(false);
    }
  };

  if (loading) return <div className="p-8 text-center text-sm text-[#64748B]">Running asset inventory verification...</div>;
  if (!request) return <div className="p-8 text-center text-sm text-red-600">Task request not found.</div>;

  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      {/* Back button & Title */}
      <div>
        <Link to="/executive/dashboard" className="text-xs text-[#2563EB] font-medium hover:underline flex items-center gap-1 mb-1">
          ← Back to Tasks
        </Link>
        <div className="flex items-center justify-between">
          <div>
            <div className="flex items-center gap-3">
              <h2 className="text-xl font-bold text-[#0F172A]">Task Detail: Check Asset</h2>
              <Badge status={request.statusName} />
            </div>
            <p className="text-xs text-[#64748B] mt-0.5">
              Request <span className="font-bold text-slate-800">{request.requestNumber}</span> • Employee: <span className="font-bold text-slate-800">{request.employeeName}</span> ({request.employeeCode}) • Asset Type: <span className="font-bold text-slate-800">{request.categoryName}</span>
            </p>
          </div>
          <Button
            variant="danger"
            size="sm"
            onClick={() => setRejectModalOpen(true)}
          >
            Reject Request
          </Button>
        </div>
      </div>

      {/* Request Details Box */}
      <Card title="Requisition Overview">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-xs">
          <div>
            <span className="text-[#64748B] block">Requested Category</span>
            <span className="font-bold text-[#0F172A] text-sm mt-0.5 block">{request.categoryName}</span>
          </div>
          <div>
            <span className="text-[#64748B] block">Reason for Requisition</span>
            <span className="font-medium text-[#0F172A] mt-0.5 block">{request.reason}</span>
          </div>
          <div>
            <span className="text-[#64748B] block">Manager Remarks</span>
            <span className="font-medium text-blue-700 mt-0.5 block">{request.managerRemarks || 'Validated and assigned.'}</span>
          </div>
        </div>
      </Card>

      {/* Matching Assets Table per Section 17 wireframe */}
      <Card
        title={`Matching Inventory Assets (${candidateAssets.length} found)`}
        subtitle="Automatic checks: [Availability] [Working Condition] [Warranty / Expiry]"
      >
        {candidateAssets.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Asset Code & Name</th>
                  <th className="px-4 py-3 text-center">Available</th>
                  <th className="px-4 py-3 text-center">Working</th>
                  <th className="px-4 py-3 text-center">Warranty</th>
                  <th className="px-4 py-3">Result / Diagnosis</th>
                  <th className="px-4 py-3 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {candidateAssets.map((asset) => {
                  const check = checksMap[asset.assetId];
                  const availCheck = check?.checks.find((c) => c.ruleName.includes('Availability'));
                  const condCheck = check?.checks.find((c) => c.ruleName.includes('Condition'));
                  const warCheck = check?.checks.find((c) => c.ruleName.includes('Warranty'));

                  const isEligible = check?.isEligible ?? false;

                  return (
                    <tr key={asset.assetId} className="hover:bg-slate-50/60 transition-colors">
                      <td className="px-4 py-3.5">
                        <p className="font-bold text-[#0F172A]">{asset.assetCode}</p>
                        <p className="text-[#64748B] text-[11px]">{asset.assetName}</p>
                        <span className="text-[10px] text-slate-400 font-mono">S/N: {asset.serialNumber}</span>
                      </td>

                      {/* Available Check */}
                      <td className="px-4 py-3.5 text-center">
                        {availCheck?.passed ? (
                          <span className="inline-flex items-center gap-1 font-bold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded text-[11px]">
                            [ok]
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 font-bold text-red-600 bg-red-50 px-2 py-0.5 rounded text-[11px]">
                            [x] {asset.status}
                          </span>
                        )}
                      </td>

                      {/* Working Condition Check */}
                      <td className="px-4 py-3.5 text-center">
                        {condCheck?.passed ? (
                          <span className="inline-flex items-center gap-1 font-bold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded text-[11px]">
                            [ok]
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 font-bold text-red-600 bg-red-50 px-2 py-0.5 rounded text-[11px]">
                            [x] {asset.condition}
                          </span>
                        )}
                      </td>

                      {/* Warranty Check */}
                      <td className="px-4 py-3.5 text-center">
                        {warCheck?.passed ? (
                          <span className="inline-flex items-center gap-1 font-bold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded text-[11px]">
                            [ok]
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1 font-bold text-amber-700 bg-amber-50 px-2 py-0.5 rounded text-[11px]">
                            [near exp / expired]
                          </span>
                        )}
                      </td>

                      {/* Diagnosis / Summary */}
                      <td className="px-4 py-3.5 text-[#64748B] max-w-xs">
                        {isEligible ? (
                          <span className="text-emerald-700 font-medium">Ready for allocation</span>
                        ) : (
                          <span className="text-slate-600 text-[11px]">
                            {check?.checks.find((c) => !c.passed)?.message || 'Not eligible'}
                          </span>
                        )}
                      </td>

                      {/* Action */}
                      <td className="px-4 py-3.5 text-right space-x-2">
                        {isEligible ? (
                          <Button
                            size="sm"
                            isLoading={allocatingId === asset.assetId}
                            onClick={() => handleAllocateAsset(asset.assetId)}
                          >
                            [Assign]
                          </Button>
                        ) : !condCheck?.passed ? (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleSendToRepair(asset.assetId)}
                          >
                            Send to repair
                          </Button>
                        ) : (
                          <span className="text-[11px] font-semibold text-slate-400">Not suitable</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-8 text-xs text-[#64748B]">
            <PackageCheck className="w-8 h-8 text-slate-300 mx-auto mb-2" />
            <p>No inventory items registered under category '{request.categoryName}'.</p>
          </div>
        )}
      </Card>

      {/* Rejection Modal */}
      <Modal
        isOpen={rejectModalOpen}
        onClose={() => setRejectModalOpen(false)}
        title={`Reject Request ${request.requestNumber}`}
        subtitle="Mandatory reason required (e.g. Out of stock, duplicate, or policy violation)"
      >
        <form onSubmit={handleReject} className="space-y-4">
          <FormField label="Rejection Reason" required>
            <Textarea
              rows={4}
              placeholder="e.g. No laptops meeting minimum specifications available in Pune inventory..."
              value={rejectionReason}
              onChange={(e) => setRejectionReason(e.target.value)}
              required
            />
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setRejectModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant="danger" isLoading={submittingReject}>
              Confirm Rejection
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
