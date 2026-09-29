import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { assetService, employeeService, repairService } from '../../api/services';
import { Asset, AssetCategory, AllocationHistory, Employee } from '../../types';
import { Card } from '../../components/common/Card';
import { Button } from '../../components/common/Button';
import { Badge } from '../../components/common/Badge';
import { Modal } from '../../components/common/Modal';
import { FormField, Input, Select, Textarea } from '../../components/common/FormField';
import { useAuth } from '../../context/AuthContext';
import {
  Laptop,
  Search,
  Filter,
  PlusCircle,
  Eye,
  RotateCcw,
  ArrowRightLeft,
  Wrench,
  Calendar,
  IndianRupee,
  ShieldCheck,
  Building,
  User,
  Clock,
  CheckCircle2,
  Trash2,
  Edit
} from 'lucide-react';

export const AssetListPage: React.FC = () => {
  const [assets, setAssets] = useState<Asset[]>([]);
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<number | undefined>(undefined);
  const [selectedStatus, setSelectedStatus] = useState<string>('');
  const { isManager } = useAuth();
  const navigate = useNavigate();

  const loadAssets = async () => {
    try {
      setLoading(true);
      const [res, catRes] = await Promise.all([
        assetService.getAssets({
          search: search || undefined,
          categoryId: selectedCategory || undefined,
          status: selectedStatus || undefined,
          pageSize: 100,
        }),
        assetService.getCategories(),
      ]);
      setAssets(res.items);
      setCategories(catRes);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadAssets();
  }, [selectedCategory, selectedStatus]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    loadAssets();
  };

  return (
    <div className="space-y-6">
      {/* Top action bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">IT Assets Inventory</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Comprehensive tracking of company laptops, desktops, monitors, and peripherals</p>
        </div>
        {isManager && (
          <Button
            leftIcon={<PlusCircle className="w-4 h-4" />}
            onClick={() => navigate('/assets/new')}
          >
            + Add Asset
          </Button>
        )}
      </div>

      {/* Filter and Search Card */}
      <Card>
        <form onSubmit={handleSearch} className="flex items-center gap-3 flex-wrap">
          <div className="relative flex-1 min-w-[220px]">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <Input
              placeholder="Search code, name, serial no, brand..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>

          <div className="w-44">
            <Select
              value={selectedCategory || ''}
              onChange={(e) => setSelectedCategory(e.target.value ? Number(e.target.value) : undefined)}
            >
              <option value="">All Categories</option>
              {categories.map((c) => (
                <option key={c.assetCategoryId} value={c.assetCategoryId}>
                  {c.categoryName}
                </option>
              ))}
            </Select>
          </div>

          <div className="w-44">
            <Select
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
            >
              <option value="">All Statuses</option>
              <option value="Available">Available</option>
              <option value="Assigned">Assigned</option>
              <option value="UnderMaintenance">Under Maintenance</option>
              <option value="InRepair">In Repair</option>
              <option value="Damaged">Damaged</option>
              <option value="Retired">Retired</option>
            </Select>
          </div>

          <Button type="submit" variant="secondary" leftIcon={<Filter className="w-4 h-4" />}>
            Filter
          </Button>
        </form>
      </Card>

      {/* Asset Table matching Section 17 wireframe */}
      <Card>
        {loading ? (
          <div className="py-8 text-center text-xs text-[#64748B]">Loading assets inventory...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Asset Code</th>
                  <th className="px-4 py-3">Asset Name</th>
                  <th className="px-4 py-3">Category</th>
                  <th className="px-4 py-3">Serial No</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Assigned To</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {assets.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-8 text-center text-xs text-[#64748B]">
                      No assets found matching the selected filters.
                    </td>
                  </tr>
                ) : (
                  assets.map((asset) => (
                    <tr key={asset.assetId} className="hover:bg-slate-50/60 transition-colors">
                      <td className="px-4 py-3.5 font-bold text-[#0F172A]">{asset.assetCode}</td>
                      <td className="px-4 py-3.5">
                        <p className="font-semibold text-[#0F172A]">{asset.assetName}</p>
                        <span className="text-[11px] text-[#64748B]">{asset.brand} {asset.model}</span>
                      </td>
                      <td className="px-4 py-3.5 text-slate-700">{asset.categoryName}</td>
                      <td className="px-4 py-3.5 font-mono text-slate-600">{asset.serialNumber}</td>
                      <td className="px-4 py-3.5">
                        <Badge status={asset.statusName} />
                      </td>
                      <td className="px-4 py-3.5">
                        {asset.assignedEmployeeName ? (
                          <div>
                            <p className="font-medium text-[#0F172A]">{asset.assignedEmployeeName}</p>
                            <span className="text-[10px] text-[#64748B]">{asset.assignedEmployeeCode}</span>
                          </div>
                        ) : (
                          <span className="text-slate-400">-</span>
                        )}
                      </td>
                      <td className="px-4 py-3.5 text-right">
                        <Link
                          to={`/assets/${asset.assetId}`}
                          className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-[6px] border border-[#E2E8F0] hover:bg-slate-50 text-[#2563EB] font-medium text-xs shadow-2xs"
                        >
                          <Eye className="w-3.5 h-3.5" />
                          <span>View</span>
                        </Link>
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

export const AssetDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [asset, setAsset] = useState<Asset | null>(null);
  const [history, setHistory] = useState<AllocationHistory[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);
  const { isManager, isExecutive } = useAuth();
  const navigate = useNavigate();

  // Modals
  const [showReturnModal, setShowReturnModal] = useState(false);
  const [showTransferModal, setShowTransferModal] = useState(false);
  const [showRepairModal, setShowRepairModal] = useState(false);
  const [showAssignModal, setShowAssignModal] = useState(false);

  // Form states
  const [targetEmpId, setTargetEmpId] = useState<number>(0);
  const [returnCondition, setReturnCondition] = useState('Good');
  const [returnRemarks, setReturnRemarks] = useState('');
  const [transferRemarks, setTransferRemarks] = useState('');
  const [repairIssue, setRepairIssue] = useState('');
  const [assignRemarks, setAssignRemarks] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const loadData = async () => {
    if (!id) return;
    try {
      setLoading(true);
      const [ast, hist, emps] = await Promise.all([
        assetService.getAssetById(Number(id)),
        assetService.getAllocationHistory(Number(id)),
        employeeService.getEmployees({ pageSize: 100 }),
      ]);
      setAsset(ast);
      setHistory(hist);
      setEmployees(emps.items.filter((e) => e.isActive));
      if (emps.items.length > 0) setTargetEmpId(emps.items[0].employeeId);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [id]);

  const handleReturn = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!asset) return;
    setSubmitting(true);
    try {
      await assetService.returnAsset(asset.assetId, {
        returnDate: new Date().toISOString(),
        conditionAtReturn: returnCondition,
        remarks: returnRemarks,
      });
      setShowReturnModal(false);
      setReturnRemarks('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to return asset.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleTransfer = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!asset || !targetEmpId) return;
    setSubmitting(true);
    try {
      await assetService.transferAsset(asset.assetId, {
        targetEmployeeId: targetEmpId,
        condition: 'Good',
        remarks: transferRemarks,
      });
      setShowTransferModal(false);
      setTransferRemarks('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to transfer asset.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleAssignDirect = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!asset || !targetEmpId) return;
    setSubmitting(true);
    try {
      await assetService.assignAsset(asset.assetId, {
        employeeId: targetEmpId,
        condition: 'Good',
        remarks: assignRemarks || 'Direct assignment from asset inventory',
      });
      setShowAssignModal(false);
      setAssignRemarks('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to assign asset.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleSendToRepair = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!asset || !repairIssue.trim()) return;
    setSubmitting(true);
    try {
      await repairService.createRepair({
        assetId: asset.assetId,
        issueDescription: repairIssue,
      });
      setShowRepairModal(false);
      setRepairIssue('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to send to repair.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) return <div className="p-8 text-center text-sm text-[#64748B]">Loading asset details...</div>;
  if (!asset) return <div className="p-8 text-center text-sm text-red-600">Asset not found.</div>;

  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      {/* Top Header */}
      <div>
        <Link to="/assets" className="text-xs text-[#2563EB] font-medium hover:underline flex items-center gap-1 mb-1">
          ← Back to Assets
        </Link>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <h2 className="text-xl font-bold text-[#0F172A]">{asset.assetName}</h2>
            <Badge status={asset.statusName} />
          </div>
          {isManager && (
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                leftIcon={<Edit className="w-3.5 h-3.5" />}
                onClick={() => navigate(`/assets/${asset.assetId}/edit`)}
              >
                Edit Asset
              </Button>
            </div>
          )}
        </div>
      </div>

      {/* 2-Card Layout matching Section 17 wireframe */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Left Card: Image + Fields */}
        <Card title="Asset Details">
          <div className="flex items-start gap-4 mb-4 pb-4 border-b border-[#E2E8F0]">
            <div className="w-16 h-16 rounded-xl bg-blue-50 border border-blue-100 flex items-center justify-center text-blue-600 shrink-0">
              <Laptop className="w-8 h-8" />
            </div>
            <div>
              <h3 className="font-bold text-sm text-[#0F172A]">{asset.assetName}</h3>
              <p className="text-xs text-[#64748B] mt-0.5">Code: <span className="font-semibold text-slate-800">{asset.assetCode}</span></p>
              <p className="text-xs text-[#64748B]">Category: <span className="font-semibold text-slate-800">{asset.categoryName}</span></p>
            </div>
          </div>

          <dl className="grid grid-cols-2 gap-y-3 gap-x-4 text-xs">
            <div>
              <dt className="text-[#64748B]">Serial Number</dt>
              <dd className="font-mono font-semibold text-[#0F172A] mt-0.5">{asset.serialNumber}</dd>
            </div>
            <div>
              <dt className="text-[#64748B]">Brand / Model</dt>
              <dd className="font-semibold text-[#0F172A] mt-0.5">{asset.brand} {asset.model || '-'}</dd>
            </div>
            <div>
              <dt className="text-[#64748B]">Purchase Cost</dt>
              <dd className="font-semibold text-[#0F172A] mt-0.5">₹{asset.purchaseCost?.toLocaleString('en-IN')}</dd>
            </div>
            <div>
              <dt className="text-[#64748B]">Purchase Date</dt>
              <dd className="font-medium text-[#0F172A] mt-0.5">{asset.purchaseDate ? new Date(asset.purchaseDate).toLocaleDateString() : '-'}</dd>
            </div>
            <div>
              <dt className="text-[#64748B]">Warranty Expiry</dt>
              <dd className="font-medium text-[#0F172A] mt-0.5">{asset.warrantyExpiryDate ? new Date(asset.warrantyExpiryDate).toLocaleDateString() : 'N/A'}</dd>
            </div>
            <div>
              <dt className="text-[#64748B]">Working Condition</dt>
              <dd className="mt-0.5"><Badge status={asset.conditionName} /></dd>
            </div>
            <div className="col-span-2">
              <dt className="text-[#64748B]">Office Location</dt>
              <dd className="font-medium text-[#0F172A] mt-0.5">{asset.location || 'Pune Head Office'}</dd>
            </div>
          </dl>
        </Card>

        {/* Right Card: Current Allocation & Quick Actions */}
        <Card title="Current Allocation Status">
          {asset.status === 'Assigned' && asset.assignedEmployeeName ? (
            <div className="space-y-4">
              <div className="p-4 bg-[#EFF6FF] border border-[#BFDBFE] rounded-[8px] flex items-center gap-3">
                <div className="w-10 h-10 rounded-full bg-blue-600 text-white font-bold flex items-center justify-center shrink-0">
                  {asset.assignedEmployeeName.slice(0, 2).toUpperCase()}
                </div>
                <div>
                  <h4 className="font-bold text-sm text-[#0F172A]">{asset.assignedEmployeeName}</h4>
                  <p className="text-xs text-[#64748B]">
                    {asset.assignedEmployeeCode} • Software Engineering
                  </p>
                </div>
              </div>

              <dl className="grid grid-cols-2 gap-3 text-xs border-y border-[#E2E8F0] py-3">
                <div>
                  <dt className="text-[#64748B]">Allocated On</dt>
                  <dd className="font-semibold text-[#0F172A] mt-0.5">
                    {asset.allocationDate ? new Date(asset.allocationDate).toLocaleDateString() : 'Active'}
                  </dd>
                </div>
                <div>
                  <dt className="text-[#64748B]">Expected Return</dt>
                  <dd className="font-semibold text-[#0F172A] mt-0.5">
                    {asset.expectedReturnDate ? new Date(asset.expectedReturnDate).toLocaleDateString() : 'Ongoing'}
                  </dd>
                </div>
              </dl>

              {/* Action buttons matching wireframe */}
              {(isManager || isExecutive) && (
                <div className="flex items-center gap-2 pt-2 flex-wrap">
                  <Button
                    size="sm"
                    variant="outline"
                    leftIcon={<RotateCcw className="w-3.5 h-3.5 text-slate-600" />}
                    onClick={() => setShowReturnModal(true)}
                  >
                    Return Asset
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    leftIcon={<ArrowRightLeft className="w-3.5 h-3.5 text-blue-600" />}
                    onClick={() => setShowTransferModal(true)}
                  >
                    Transfer
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    leftIcon={<Wrench className="w-3.5 h-3.5 text-orange-600" />}
                    onClick={() => setShowRepairModal(true)}
                  >
                    Send for repair
                  </Button>
                </div>
              )}
            </div>
          ) : (
            <div className="py-6 text-center text-xs text-[#64748B]">
              <ShieldCheck className="w-8 h-8 text-emerald-500 mx-auto mb-2" />
              <p className="font-bold text-sm text-[#0F172A]">Asset is Currently in Stock</p>
              <p className="text-xs text-[#64748B] mt-1">Available for new employee allocation requests.</p>
              {(isManager || isExecutive) && (
                <div className="mt-4 flex items-center justify-center gap-2">
                  <Button size="sm" onClick={() => setShowAssignModal(true)}>
                    Assign to Employee
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => setShowRepairModal(true)}>
                    Send for Repair
                  </Button>
                </div>
              )}
            </div>
          )}
        </Card>
      </div>

      {/* Allocation History Table (never overwritten) */}
      <Card
        title="Allocation History"
        subtitle="Complete immutable chronological record of previous custodians"
      >
        {history.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">#</th>
                  <th className="px-4 py-3">Employee</th>
                  <th className="px-4 py-3">Allocated On</th>
                  <th className="px-4 py-3">Returned On</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Remarks</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {history.map((h, idx) => (
                  <tr key={h.allocationId} className="hover:bg-slate-50/60">
                    <td className="px-4 py-3 font-semibold text-slate-500">{idx + 1}</td>
                    <td className="px-4 py-3">
                      <p className="font-bold text-[#0F172A]">{h.employeeName}</p>
                      <span className="text-[10px] text-[#64748B]">{h.employeeCode}</span>
                    </td>
                    <td className="px-4 py-3 text-slate-700">{new Date(h.allocationDate).toLocaleDateString()}</td>
                    <td className="px-4 py-3 text-slate-700">{h.returnDate ? new Date(h.returnDate).toLocaleDateString() : '-'}</td>
                    <td className="px-4 py-3">
                      <Badge status={h.statusName} />
                    </td>
                    <td className="px-4 py-3 text-[#64748B] max-w-xs truncate">{h.remarks || '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-6 text-xs text-[#64748B]">
            <Clock className="w-6 h-6 text-slate-300 mx-auto mb-1" />
            <p>No historical allocation records logged yet.</p>
          </div>
        )}
      </Card>

      {/* Modal: Return Asset */}
      <Modal
        isOpen={showReturnModal}
        onClose={() => setShowReturnModal(false)}
        title={`Return Asset ${asset.assetCode}`}
        subtitle={`Process return from ${asset.assignedEmployeeName}`}
      >
        <form onSubmit={handleReturn} className="space-y-4">
          <FormField label="Condition at Return" required>
            <Select value={returnCondition} onChange={(e) => setReturnCondition(e.target.value)}>
              <option value="Good">Good (Restock to Available)</option>
              <option value="Fair">Fair (Available)</option>
              <option value="Damaged">Damaged (Move to Damaged)</option>
              <option value="NotWorking">Not Working (Send to Maintenance)</option>
            </Select>
          </FormField>
          <FormField label="Return Inspection Remarks">
            <Textarea
              rows={3}
              placeholder="e.g. Returned upon project milestone completion in good condition."
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

      {/* Modal: Transfer Asset */}
      <Modal
        isOpen={showTransferModal}
        onClose={() => setShowTransferModal(false)}
        title={`Transfer Asset ${asset.assetCode}`}
        subtitle={`Transfer from ${asset.assignedEmployeeName} to another active employee`}
      >
        <form onSubmit={handleTransfer} className="space-y-4">
          <FormField label="Target Employee" required>
            <Select
              value={targetEmpId}
              onChange={(e) => setTargetEmpId(Number(e.target.value))}
              required
            >
              {employees
                .filter((e) => e.employeeId !== asset.currentEmployeeId)
                .map((e) => (
                  <option key={e.employeeId} value={e.employeeId}>
                    {e.fullName} ({e.employeeCode}) - {e.departmentName || 'Staff'}
                  </option>
                ))}
            </Select>
          </FormField>
          <FormField label="Transfer Reason / Handover Remarks">
            <Textarea
              rows={3}
              placeholder="e.g. Reassigned for urgent cross-team project collaboration."
              value={transferRemarks}
              onChange={(e) => setTransferRemarks(e.target.value)}
            />
          </FormField>
          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setShowTransferModal(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Execute Transfer
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Assign Direct */}
      <Modal
        isOpen={showAssignModal}
        onClose={() => setShowAssignModal(false)}
        title={`Assign Asset ${asset.assetCode}`}
        subtitle="Assign this available stock asset directly to an active employee"
      >
        <form onSubmit={handleAssignDirect} className="space-y-4">
          <FormField label="Select Employee" required>
            <Select
              value={targetEmpId}
              onChange={(e) => setTargetEmpId(Number(e.target.value))}
              required
            >
              {employees.map((e) => (
                <option key={e.employeeId} value={e.employeeId}>
                  {e.fullName} ({e.employeeCode}) - {e.departmentName || 'Staff'}
                </option>
              ))}
            </Select>
          </FormField>
          <FormField label="Allocation Remarks">
            <Textarea
              rows={3}
              placeholder="e.g. Issued for development and management tasks."
              value={assignRemarks}
              onChange={(e) => setAssignRemarks(e.target.value)}
            />
          </FormField>
          <div className="flex items-center justify-end gap-3 pt-3">
            <Button type="button" variant="outline" onClick={() => setShowAssignModal(false)}>
              Cancel
            </Button>
            <Button type="submit" isLoading={submitting}>
              Assign Asset
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal: Send to Repair */}
      <Modal
        isOpen={showRepairModal}
        onClose={() => setShowRepairModal(false)}
        title={`Send ${asset.assetCode} to Maintenance`}
        subtitle="Record defect and transition status to UnderMaintenance"
      >
        <form onSubmit={handleSendToRepair} className="space-y-4">
          <FormField label="Defect / Issue Description" required>
            <Textarea
              rows={3}
              placeholder="Describe symptoms or physical defect..."
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
              Send to Repair
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export const AddEditAssetPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const isEditing = Boolean(id);
  const navigate = useNavigate();
  const [categories, setCategories] = useState<AssetCategory[]>([]);
  const [loading, setLoading] = useState(false);
  const [fetching, setFetching] = useState(isEditing);

  const [formData, setFormData] = useState({
    assetCode: '',
    assetName: '',
    assetCategoryId: 1,
    serialNumber: '',
    brand: '',
    model: '',
    purchaseDate: '',
    purchaseCost: 50000,
    warrantyStartDate: '',
    warrantyExpiryDate: '',
    condition: 'Good',
    status: 'Available',
    location: 'Pune Office - Floor 3',
  });

  useEffect(() => {
    const init = async () => {
      try {
        const cats = await assetService.getCategories();
        setCategories(cats);
        if (!isEditing && cats.length > 0) {
          setFormData((prev) => ({ ...prev, assetCategoryId: cats[0].assetCategoryId }));
        }

        if (isEditing && id) {
          const ast = await assetService.getAssetById(Number(id));
          setFormData({
            assetCode: ast.assetCode,
            assetName: ast.assetName,
            assetCategoryId: ast.assetCategoryId,
            serialNumber: ast.serialNumber,
            brand: ast.brand || '',
            model: ast.model || '',
            purchaseDate: ast.purchaseDate ? ast.purchaseDate.slice(0, 10) : '',
            purchaseCost: ast.purchaseCost,
            warrantyStartDate: ast.warrantyStartDate ? ast.warrantyStartDate.slice(0, 10) : '',
            warrantyExpiryDate: ast.warrantyExpiryDate ? ast.warrantyExpiryDate.slice(0, 10) : '',
            condition: ast.condition,
            status: ast.status,
            location: ast.location || '',
          });
        }
      } catch (err) {
        console.error(err);
      } finally {
        setFetching(false);
      }
    };
    init();
  }, [id, isEditing]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    try {
      if (isEditing && id) {
        await assetService.updateAsset(Number(id), {
          assetName: formData.assetName,
          assetCategoryId: formData.assetCategoryId,
          serialNumber: formData.serialNumber,
          brand: formData.brand,
          model: formData.model,
          purchaseDate: formData.purchaseDate ? new Date(formData.purchaseDate).toISOString() : undefined,
          purchaseCost: Number(formData.purchaseCost),
          warrantyStartDate: formData.warrantyStartDate ? new Date(formData.warrantyStartDate).toISOString() : undefined,
          warrantyExpiryDate: formData.warrantyExpiryDate ? new Date(formData.warrantyExpiryDate).toISOString() : undefined,
          condition: formData.condition as any,
          status: formData.status as any,
          location: formData.location,
        });
        navigate(`/assets/${id}`);
      } else {
        const created = await assetService.createAsset({
          assetCode: formData.assetCode,
          assetName: formData.assetName,
          assetCategoryId: formData.assetCategoryId,
          serialNumber: formData.serialNumber,
          brand: formData.brand,
          model: formData.model,
          purchaseDate: formData.purchaseDate ? new Date(formData.purchaseDate).toISOString() : undefined,
          purchaseCost: Number(formData.purchaseCost),
          warrantyStartDate: formData.warrantyStartDate ? new Date(formData.warrantyStartDate).toISOString() : undefined,
          warrantyExpiryDate: formData.warrantyExpiryDate ? new Date(formData.warrantyExpiryDate).toISOString() : undefined,
          condition: formData.condition as any,
          location: formData.location,
        });
        navigate(`/assets/${created.assetId}`);
      }
    } catch (err: any) {
      alert(err.response?.data?.message || 'Failed to save asset.');
    } finally {
      setLoading(false);
    }
  };

  if (fetching) return <div className="p-8 text-center text-xs text-[#64748B]">Loading asset data...</div>;

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div>
        <Link to="/assets" className="text-xs text-[#2563EB] font-medium hover:underline flex items-center gap-1 mb-1">
          ← Back to Assets
        </Link>
        <h2 className="text-xl font-bold text-[#0F172A]">{isEditing ? 'Edit IT Asset' : 'Register New IT Asset'}</h2>
        <p className="text-xs text-[#64748B] mt-0.5">Enter device serial specifications, procurement cost, and warranty dates</p>
      </div>

      <Card>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="Asset Code" required hint="e.g. LAP-0015">
              <Input
                value={formData.assetCode}
                onChange={(e) => setFormData({ ...formData, assetCode: e.target.value })}
                disabled={isEditing}
                required
              />
            </FormField>
            <FormField label="Asset Name" required hint="e.g. Dell Latitude 5440">
              <Input
                value={formData.assetName}
                onChange={(e) => setFormData({ ...formData, assetName: e.target.value })}
                required
              />
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="Asset Category" required>
              <Select
                value={formData.assetCategoryId}
                onChange={(e) => setFormData({ ...formData, assetCategoryId: Number(e.target.value) })}
                required
              >
                {categories.map((c) => (
                  <option key={c.assetCategoryId} value={c.assetCategoryId}>
                    {c.categoryName}
                  </option>
                ))}
              </Select>
            </FormField>
            <FormField label="Serial Number" required hint="e.g. DELL987654">
              <Input
                value={formData.serialNumber}
                onChange={(e) => setFormData({ ...formData, serialNumber: e.target.value })}
                required
              />
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="Manufacturer / Brand">
              <Input
                value={formData.brand}
                onChange={(e) => setFormData({ ...formData, brand: e.target.value })}
                placeholder="Dell, Apple, HP, Lenovo..."
              />
            </FormField>
            <FormField label="Model">
              <Input
                value={formData.model}
                onChange={(e) => setFormData({ ...formData, model: e.target.value })}
                placeholder="Latitude 5440, M3 Pro..."
              />
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="Purchase Cost (INR)" required>
              <Input
                type="number"
                value={formData.purchaseCost}
                onChange={(e) => setFormData({ ...formData, purchaseCost: Number(e.target.value) })}
                required
              />
            </FormField>
            <FormField label="Purchase Date">
              <Input
                type="date"
                value={formData.purchaseDate}
                onChange={(e) => setFormData({ ...formData, purchaseDate: e.target.value })}
              />
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="Warranty Start Date">
              <Input
                type="date"
                value={formData.warrantyStartDate}
                onChange={(e) => setFormData({ ...formData, warrantyStartDate: e.target.value })}
              />
            </FormField>
            <FormField label="Warranty Expiry Date">
              <Input
                type="date"
                value={formData.warrantyExpiryDate}
                onChange={(e) => setFormData({ ...formData, warrantyExpiryDate: e.target.value })}
              />
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <FormField label="Initial Working Condition" required>
              <Select
                value={formData.condition}
                onChange={(e) => setFormData({ ...formData, condition: e.target.value })}
              >
                <option value="Good">Good</option>
                <option value="Fair">Fair</option>
                <option value="Damaged">Damaged</option>
                <option value="NotWorking">Not Working</option>
              </Select>
            </FormField>

            {isEditing && (
              <FormField label="Status" required>
                <Select
                  value={formData.status}
                  onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                >
                  <option value="Available">Available</option>
                  <option value="Assigned">Assigned</option>
                  <option value="UnderMaintenance">Under Maintenance</option>
                  <option value="InRepair">In Repair</option>
                  <option value="Damaged">Damaged</option>
                  <option value="Retired">Retired</option>
                </Select>
              </FormField>
            )}

            <FormField label="Physical Location" className={!isEditing ? 'col-span-1' : ''}>
              <Input
                value={formData.location}
                onChange={(e) => setFormData({ ...formData, location: e.target.value })}
                placeholder="Pune Office - Floor 3"
              />
            </FormField>
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-[#E2E8F0]">
            <Button type="button" variant="outline" onClick={() => navigate('/assets')}>
              Cancel
            </Button>
            <Button type="submit" isLoading={loading}>
              {isEditing ? 'Save Changes' : 'Register Asset'}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
};
