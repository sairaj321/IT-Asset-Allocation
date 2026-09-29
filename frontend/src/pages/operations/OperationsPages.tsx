import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  requestService,
  assetService,
  repairService,
  replacementService,
  notificationService
} from '../../api/services';
import {
  AssetRequest,
  AllocationHistory,
  AssetRepair,
  AssetReplacement,
  NotificationItem,
  Asset
} from '../../types';
import { Card } from '../../components/common/Card';
import { Button } from '../../components/common/Button';
import { Badge } from '../../components/common/Badge';
import { Modal } from '../../components/common/Modal';
import { FormField, Input, Select, Textarea } from '../../components/common/FormField';
import { useAuth } from '../../context/AuthContext';
import { useSignalR } from '../../context/SignalRContext';
import {
  FileText,
  ArrowRightLeft,
  Wrench,
  RefreshCw,
  Bell,
  Search,
  CheckCircle2,
  XCircle,
  Clock,
  Eye,
  Check,
  CheckCheck
} from 'lucide-react';

export const RequestsListPage: React.FC = () => {
  const [requests, setRequests] = useState<AssetRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [selectedStatus, setSelectedStatus] = useState<string>('');
  const { isManager, isExecutive } = useAuth();

  const load = async () => {
    try {
      setLoading(true);
      const res = isManager
        ? await requestService.getAllRequests({ search: search || undefined, status: selectedStatus || undefined, pageSize: 100 })
        : isExecutive
        ? await requestService.getAssignedRequests({ search: search || undefined, pageSize: 100 })
        : await requestService.getMyRequests({ search: search || undefined, pageSize: 100 });
      setRequests(res.items);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, [selectedStatus]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    load();
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">Asset Requisition Requests</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Track and process all employee hardware requests and lifecycle approvals</p>
        </div>
      </div>

      <Card>
        <form onSubmit={handleSearch} className="flex items-center gap-3 flex-wrap">
          <div className="relative flex-1 min-w-[220px]">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <Input
              placeholder="Search request number, employee, reason..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>

          <div className="w-44">
            <Select value={selectedStatus} onChange={(e) => setSelectedStatus(e.target.value)}>
              <option value="">All Statuses</option>
              <option value="Submitted">Submitted</option>
              <option value="UnderReview">Under Review</option>
              <option value="AssignedToExecutive">Assigned to Executive</option>
              <option value="Checking">Checking</option>
              <option value="Allocated">Allocated</option>
              <option value="Rejected">Rejected</option>
            </Select>
          </div>

          <Button type="submit" variant="secondary">
            Filter
          </Button>
        </form>
      </Card>

      <Card>
        {loading ? (
          <div className="py-8 text-center text-xs text-[#64748B]">Loading requests...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Request No</th>
                  <th className="px-4 py-3">Employee</th>
                  <th className="px-4 py-3">Asset Type</th>
                  <th className="px-4 py-3">Priority</th>
                  <th className="px-4 py-3">Assigned Executive</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Created Date</th>
                  <th className="px-4 py-3 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {requests.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="px-4 py-8 text-center text-xs text-[#64748B]">
                      No requests found matching criteria.
                    </td>
                  </tr>
                ) : (
                  requests.map((r) => (
                    <tr key={r.requestId} className="hover:bg-slate-50/60">
                      <td className="px-4 py-3.5 font-bold text-[#0F172A]">{r.requestNumber}</td>
                      <td className="px-4 py-3.5">
                        <p className="font-semibold text-[#0F172A]">{r.employeeName}</p>
                        <span className="text-[10px] text-[#64748B]">{r.employeeCode}</span>
                      </td>
                      <td className="px-4 py-3.5 font-medium">{r.categoryName}</td>
                      <td className="px-4 py-3.5"><Badge status={r.priorityName} /></td>
                      <td className="px-4 py-3.5 text-slate-700">{r.assignedExecutiveName || '-'}</td>
                      <td className="px-4 py-3.5"><Badge status={r.statusName} /></td>
                      <td className="px-4 py-3.5 text-slate-600">{new Date(r.createdAt).toLocaleDateString()}</td>
                      <td className="px-4 py-3.5 text-right">
                        {isExecutive && (r.status === 'AssignedToExecutive' || r.status === 'Checking') ? (
                          <Link
                            to={`/executive/tasks/${r.requestId}`}
                            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-[6px] bg-[#2563EB] text-white font-medium text-xs shadow-2xs"
                          >
                            <span>Check & Assign</span>
                          </Link>
                        ) : (
                          <Link
                            to={`/employee/requests/${r.requestId}`}
                            className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-[6px] border border-[#E2E8F0] hover:bg-slate-50 text-[#2563EB] font-medium text-xs"
                          >
                            <span>View</span>
                          </Link>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
};

export const AllocationsListPage: React.FC = () => {
  const [allocations, setAllocations] = useState<AllocationHistory[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  const load = async () => {
    try {
      setLoading(true);
      const res = await assetService.getAllAllocations({ search: search || undefined, pageSize: 100 });
      setAllocations(res.items);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">Asset Allocations Registry</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Historical and active custodian records for all physical hardware assets</p>
        </div>
      </div>

      <Card>
        <div className="flex items-center gap-3">
          <div className="relative flex-1">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <Input
              placeholder="Search asset code, employee name..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>
          <Button variant="secondary" onClick={() => load()}>
            Search
          </Button>
        </div>
      </Card>

      <Card>
        {loading ? (
          <div className="py-8 text-center text-xs text-[#64748B]">Loading allocations registry...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Asset Code & Name</th>
                  <th className="px-4 py-3">Custodian Employee</th>
                  <th className="px-4 py-3">Allocated On</th>
                  <th className="px-4 py-3">Expected Return</th>
                  <th className="px-4 py-3">Returned On</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Allocated By</th>
                  <th className="px-4 py-3">Remarks</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {allocations.map((a) => (
                  <tr key={a.allocationId} className="hover:bg-slate-50/60">
                    <td className="px-4 py-3.5">
                      <Link to={`/assets/${a.assetId}`} className="font-bold text-[#2563EB] hover:underline">
                        {a.assetCode}
                      </Link>
                      <p className="text-[11px] text-[#64748B]">{a.assetName}</p>
                    </td>
                    <td className="px-4 py-3.5">
                      <p className="font-semibold text-[#0F172A]">{a.employeeName}</p>
                      <span className="text-[10px] text-[#64748B]">{a.employeeCode}</span>
                    </td>
                    <td className="px-4 py-3.5 text-slate-700">{new Date(a.allocationDate).toLocaleDateString()}</td>
                    <td className="px-4 py-3.5 text-slate-700">{a.expectedReturnDate ? new Date(a.expectedReturnDate).toLocaleDateString() : 'Ongoing'}</td>
                    <td className="px-4 py-3.5 text-slate-700">{a.returnDate ? new Date(a.returnDate).toLocaleDateString() : '-'}</td>
                    <td className="px-4 py-3.5"><Badge status={a.statusName} /></td>
                    <td className="px-4 py-3.5 text-slate-600 font-medium">{a.allocatedByName}</td>
                    <td className="px-4 py-3.5 text-[#64748B] max-w-xs truncate">{a.remarks || '-'}</td>
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

export const RepairsListPage: React.FC = () => {
  const [repairs, setRepairs] = useState<AssetRepair[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [updateModalOpen, setUpdateModalOpen] = useState(false);
  const [selectedRepair, setSelectedRepair] = useState<AssetRepair | null>(null);
  const { isManager, isExecutive } = useAuth();

  const [newStatus, setNewStatus] = useState('InRepair');
  const [vendorName, setVendorName] = useState('');
  const [repairCost, setRepairCost] = useState<number>(0);
  const [conditionAfter, setConditionAfter] = useState('Good');
  const [submitting, setSubmitting] = useState(false);

  const load = async () => {
    try {
      setLoading(true);
      const res = await repairService.getRepairs({ search: search || undefined, pageSize: 100 });
      setRepairs(res.items);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleUpdateStatus = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRepair) return;
    setSubmitting(true);
    try {
      await repairService.updateRepairStatus(selectedRepair.repairId, {
        status: newStatus,
        vendorName: vendorName || undefined,
        repairCost: repairCost ? Number(repairCost) : undefined,
        conditionAfterRepair: conditionAfter,
        completedDate: newStatus === 'Completed' ? new Date().toISOString() : undefined,
      });
      setUpdateModalOpen(false);
      await load();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to update repair.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">Asset Maintenance & Repairs</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Track hardware defects, vendor repair service orders, and maintenance costs</p>
        </div>
      </div>

      <Card>
        {loading ? (
          <div className="py-8 text-center text-xs text-[#64748B]">Loading repair records...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Asset</th>
                  <th className="px-4 py-3">Reported By</th>
                  <th className="px-4 py-3">Defect Description</th>
                  <th className="px-4 py-3">Vendor / Service Center</th>
                  <th className="px-4 py-3">Repair Cost</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {repairs.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-8 text-center text-xs text-[#64748B]">
                      No maintenance or repair tickets recorded.
                    </td>
                  </tr>
                ) : (
                  repairs.map((r) => (
                    <tr key={r.repairId} className="hover:bg-slate-50/60">
                      <td className="px-4 py-3.5 font-bold text-[#0F172A]">{r.assetCode} ({r.assetName})</td>
                      <td className="px-4 py-3.5 text-slate-700">{r.reportedByEmployeeName}</td>
                      <td className="px-4 py-3.5 text-[#64748B] max-w-xs">{r.issueDescription}</td>
                      <td className="px-4 py-3.5 text-slate-700">{r.vendorName || '-'}</td>
                      <td className="px-4 py-3.5 font-semibold text-slate-800">{r.repairCost ? `₹${r.repairCost}` : '-'}</td>
                      <td className="px-4 py-3.5"><Badge status={r.statusName} /></td>
                      <td className="px-4 py-3.5 text-right">
                        {(isManager || isExecutive) && r.status !== 'Completed' && r.status !== 'ClosedNoRepair' && (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              setSelectedRepair(r);
                              setNewStatus(r.status === 'Reported' ? 'UnderReview' : r.status === 'UnderReview' ? 'InRepair' : 'Completed');
                              setVendorName(r.vendorName || '');
                              setRepairCost(r.repairCost || 0);
                              setUpdateModalOpen(true);
                            }}
                          >
                            Update Status
                          </Button>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Modal: Update Repair Status */}
      <Modal
        isOpen={updateModalOpen}
        onClose={() => setUpdateModalOpen(false)}
        title={`Update Repair Order for ${selectedRepair?.assetCode}`}
        subtitle="Progress status through maintenance lifecycle"
      >
        <form onSubmit={handleUpdateStatus} className="space-y-4">
          <FormField label="Maintenance Status" required>
            <Select value={newStatus} onChange={(e) => setNewStatus(e.target.value)}>
              <option value="UnderReview">Under Review</option>
              <option value="InRepair">In Repair (Sent to Vendor)</option>
              <option value="Completed">Completed (Fixed & Restocked to Available)</option>
              <option value="ClosedNoRepair">Closed (No Repair Needed / Unrepairable)</option>
            </Select>
          </FormField>

          <FormField label="Vendor Name / Authorized Service Center">
            <Input
              placeholder="e.g. Dell Authorized Service Center, Pune"
              value={vendorName}
              onChange={(e) => setVendorName(e.target.value)}
            />
          </FormField>

          <FormField label="Repair Cost (INR)">
            <Input
              type="number"
              value={repairCost}
              onChange={(e) => setRepairCost(Number(e.target.value))}
            />
          </FormField>

          {newStatus === 'Completed' && (
            <FormField label="Condition After Repair" required>
              <Select value={conditionAfter} onChange={(e) => setConditionAfter(e.target.value)}>
                <option value="Good">Good</option>
                <option value="Fair">Fair</option>
              </Select>
            </FormField>
          )}

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setUpdateModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Save Update
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export const ReplacementsListPage: React.FC = () => {
  const [replacements, setReplacements] = useState<AssetReplacement[]>([]);
  const [availableAssets, setAvailableAssets] = useState<Asset[]>([]);
  const [loading, setLoading] = useState(true);
  const [approveModalOpen, setApproveModalOpen] = useState(false);
  const [selectedRep, setSelectedRep] = useState<AssetReplacement | null>(null);
  const [selectedNewAssetId, setSelectedNewAssetId] = useState<number>(0);
  const [rejectReason, setRejectReason] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const { isManager } = useAuth();

  const load = async () => {
    try {
      setLoading(true);
      const [res, avail] = await Promise.all([
        replacementService.getReplacements({ pageSize: 100 }),
        assetService.getAvailableAssets(),
      ]);
      setReplacements(res.items);
      setAvailableAssets(avail);
      if (avail.length > 0) setSelectedNewAssetId(avail[0].assetId);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleApprove = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRep || !selectedNewAssetId) return;
    setSubmitting(true);
    try {
      await replacementService.processDecision(selectedRep.replacementId, {
        approved: true,
        newAssetId: selectedNewAssetId,
      });
      setApproveModalOpen(false);
      await load();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to approve replacement.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleReject = async (repId: number) => {
    const reason = prompt('Please specify rejection reason:');
    if (!reason) return;
    try {
      await replacementService.processDecision(repId, {
        approved: false,
        rejectionReason: reason,
      });
      await load();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to reject replacement.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">Asset Replacement Requests</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Manager approvals for swapping out end-of-life or damaged devices</p>
        </div>
      </div>

      <Card>
        {loading ? (
          <div className="py-8 text-center text-xs text-[#64748B]">Loading replacement tickets...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Existing Asset</th>
                  <th className="px-4 py-3">Employee</th>
                  <th className="px-4 py-3">Reason</th>
                  <th className="px-4 py-3">Replacement Asset</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {replacements.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="px-4 py-8 text-center text-xs text-[#64748B]">
                      No replacement requests found.
                    </td>
                  </tr>
                ) : (
                  replacements.map((r) => (
                    <tr key={r.replacementId} className="hover:bg-slate-50/60">
                      <td className="px-4 py-3.5 font-bold text-[#0F172A]">{r.oldAssetCode} ({r.oldAssetName})</td>
                      <td className="px-4 py-3.5 text-slate-700">{r.employeeName}</td>
                      <td className="px-4 py-3.5 text-[#64748B] max-w-xs">{r.reason}</td>
                      <td className="px-4 py-3.5 font-medium text-emerald-700">
                        {r.newAssetCode ? `${r.newAssetCode} (${r.newAssetName})` : '-'}
                      </td>
                      <td className="px-4 py-3.5"><Badge status={r.statusName} /></td>
                      <td className="px-4 py-3.5 text-right space-x-2">
                        {isManager && r.status === 'Requested' && (
                          <>
                            <Button
                              size="sm"
                              onClick={() => {
                                setSelectedRep(r);
                                setApproveModalOpen(true);
                              }}
                            >
                              Approve & Swap
                            </Button>
                            <Button
                              size="sm"
                              variant="outline"
                              onClick={() => handleReject(r.replacementId)}
                            >
                              Reject
                            </Button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      {/* Modal: Approve Replacement */}
      <Modal
        isOpen={approveModalOpen}
        onClose={() => setApproveModalOpen(false)}
        title={`Approve Replacement for ${selectedRep?.oldAssetCode}`}
        subtitle="Select a new available stock asset to allocate in place of current device"
      >
        <form onSubmit={handleApprove} className="space-y-4">
          <FormField label="Select New Stock Asset to Allocate" required>
            <Select
              value={selectedNewAssetId}
              onChange={(e) => setSelectedNewAssetId(Number(e.target.value))}
              required
            >
              {availableAssets.map((a) => (
                <option key={a.assetId} value={a.assetId}>
                  {a.assetCode} - {a.assetName} ({a.categoryName})
                </option>
              ))}
            </Select>
          </FormField>

          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setApproveModalOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Approve & Assign New Asset
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export const NotificationsPage: React.FC = () => {
  const { notifications, markAsRead, markAllAsRead, refreshNotifications } = useSignalR();

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">Notifications Inbox</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Real-time alerts for asset requisitions, assignments, and status updates</p>
        </div>
        <Button variant="outline" size="sm" onClick={() => markAllAsRead()}>
          Mark All as Read
        </Button>
      </div>

      <Card>
        {notifications.length === 0 ? (
          <div className="text-center py-12 text-xs text-[#64748B]">
            <Bell className="w-8 h-8 text-slate-300 mx-auto mb-2" />
            <p>Your notification tray is empty.</p>
          </div>
        ) : (
          <div className="divide-y divide-[#E2E8F0]">
            {notifications.map((n) => (
              <div
                key={n.notificationId}
                onClick={() => markAsRead(n.notificationId)}
                className={`p-4 flex items-start justify-between cursor-pointer hover:bg-slate-50 transition-colors ${
                  !n.isRead ? 'bg-blue-50/40' : ''
                }`}
              >
                <div>
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-sm text-[#0F172A]">{n.title}</span>
                    {!n.isRead && <span className="w-2 h-2 rounded-full bg-[#2563EB]" />}
                  </div>
                  <p className="text-xs text-[#64748B] mt-1">{n.message}</p>
                  <span className="text-[11px] text-slate-400 mt-2 block">
                    {new Date(n.createdAt).toLocaleString()}
                  </span>
                </div>
                {!n.isRead && (
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      markAsRead(n.notificationId);
                    }}
                    className="text-xs text-[#2563EB] hover:underline font-medium shrink-0"
                  >
                    Mark read
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
};
