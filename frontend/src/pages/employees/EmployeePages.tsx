import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { employeeService } from '../../api/services';
import { Employee, Asset } from '../../types';
import { Card } from '../../components/common/Card';
import { Button } from '../../components/common/Button';
import { Badge } from '../../components/common/Badge';
import { FormField, Input } from '../../components/common/FormField';
import { Search, Users, Laptop, FileText, Mail, Phone, Building } from 'lucide-react';

export const EmployeeListPage: React.FC = () => {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  const load = async () => {
    try {
      setLoading(true);
      const res = await employeeService.getEmployees({ search: search || undefined, pageSize: 100 });
      setEmployees(res.items);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    load();
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold text-[#0F172A]">Employee Directory</h2>
          <p className="text-xs text-[#64748B] mt-0.5">Corporate employee records with active hardware allocation counts</p>
        </div>
      </div>

      <Card>
        <form onSubmit={handleSearch} className="flex items-center gap-3">
          <div className="relative flex-1">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <Input
              placeholder="Search employee code, name, email, designation..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="pl-9"
            />
          </div>
          <Button type="submit" variant="secondary">
            Search
          </Button>
        </form>
      </Card>

      <Card>
        {loading ? (
          <div className="py-8 text-center text-xs text-[#64748B]">Loading employee directory...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Employee Code</th>
                  <th className="px-4 py-3">Name</th>
                  <th className="px-4 py-3">Department</th>
                  <th className="px-4 py-3">Designation</th>
                  <th className="px-4 py-3">Allocated Assets</th>
                  <th className="px-4 py-3">Pending Requests</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {employees.map((emp) => (
                  <tr key={emp.employeeId} className="hover:bg-slate-50/60">
                    <td className="px-4 py-3.5 font-bold font-mono text-[#0F172A]">{emp.employeeCode}</td>
                    <td className="px-4 py-3.5">
                      <p className="font-bold text-[#0F172A]">{emp.fullName}</p>
                      <span className="text-[11px] text-[#64748B]">{emp.email}</span>
                    </td>
                    <td className="px-4 py-3.5 font-medium text-slate-700">{emp.departmentName || 'General'}</td>
                    <td className="px-4 py-3.5 text-[#64748B]">{emp.designation || '-'}</td>
                    <td className="px-4 py-3.5">
                      <span className="font-bold text-blue-600 bg-blue-50 px-2 py-0.5 rounded text-[11px]">
                        {emp.allocatedAssetsCount} Assets
                      </span>
                    </td>
                    <td className="px-4 py-3.5">
                      <span className="font-bold text-amber-600 bg-amber-50 px-2 py-0.5 rounded text-[11px]">
                        {emp.pendingRequestsCount} Pending
                      </span>
                    </td>
                    <td className="px-4 py-3.5">
                      <Badge status={emp.isActive ? 'Active' : 'Inactive'} />
                    </td>
                    <td className="px-4 py-3.5 text-right">
                      <Link
                        to={`/employees/${emp.employeeId}`}
                        className="inline-flex items-center gap-1 px-2.5 py-1.5 rounded-[6px] border border-[#E2E8F0] hover:bg-slate-50 text-[#2563EB] font-medium text-xs"
                      >
                        <span>View Profile</span>
                      </Link>
                    </td>
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

export const EmployeeDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [employee, setEmployee] = useState<Employee | null>(null);
  const [assets, setAssets] = useState<Asset[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetch = async () => {
      if (!id) return;
      try {
        setLoading(true);
        const [emp, asts] = await Promise.all([
          employeeService.getEmployeeById(Number(id)),
          employeeService.getEmployeeAssets(Number(id)),
        ]);
        setEmployee(emp);
        setAssets(asts);
      } catch (err) {
        console.error(err);
      } finally {
        setLoading(false);
      }
    };
    fetch();
  }, [id]);

  if (loading) return <div className="p-8 text-center text-xs text-[#64748B]">Loading employee profile...</div>;
  if (!employee) return <div className="p-8 text-center text-xs text-red-600">Employee not found.</div>;

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      <div>
        <Link to="/employees" className="text-xs text-[#2563EB] font-medium hover:underline flex items-center gap-1 mb-1">
          ← Back to Employees
        </Link>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <h2 className="text-xl font-bold text-[#0F172A]">{employee.fullName}</h2>
            <Badge status={employee.isActive ? 'Active' : 'Inactive'} />
          </div>
        </div>
      </div>

      {/* Employee Profile Card */}
      <Card title="Employee Profile Information">
        <dl className="grid grid-cols-2 md:grid-cols-3 gap-4 text-xs">
          <div>
            <dt className="text-[#64748B]">Employee Code</dt>
            <dd className="font-mono font-bold text-sm text-[#0F172A] mt-0.5">{employee.employeeCode}</dd>
          </div>
          <div>
            <dt className="text-[#64748B]">Email Address</dt>
            <dd className="font-medium text-[#0F172A] mt-0.5">{employee.email}</dd>
          </div>
          <div>
            <dt className="text-[#64748B]">Mobile Number</dt>
            <dd className="font-medium text-[#0F172A] mt-0.5">{employee.mobile || 'N/A'}</dd>
          </div>
          <div>
            <dt className="text-[#64748B]">Department</dt>
            <dd className="font-semibold text-[#0F172A] mt-0.5">{employee.departmentName || 'Operations'}</dd>
          </div>
          <div>
            <dt className="text-[#64748B]">Designation</dt>
            <dd className="font-medium text-[#0F172A] mt-0.5">{employee.designation || 'Staff'}</dd>
          </div>
          <div>
            <dt className="text-[#64748B]">Active Assets</dt>
            <dd className="font-bold text-blue-600 mt-0.5">{assets.length} items</dd>
          </div>
        </dl>
      </Card>

      {/* Allocated Assets Table */}
      <Card title={`Currently Assigned IT Assets (${assets.length})`}>
        {assets.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[#F8FAFC] text-[#64748B] uppercase font-semibold border-b border-[#E2E8F0]">
                <tr>
                  <th className="px-4 py-3">Asset Code</th>
                  <th className="px-4 py-3">Asset Name</th>
                  <th className="px-4 py-3">Category</th>
                  <th className="px-4 py-3">Serial No</th>
                  <th className="px-4 py-3">Allocation Date</th>
                  <th className="px-4 py-3">Cost (INR)</th>
                  <th className="px-4 py-3 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E2E8F0]">
                {assets.map((a) => (
                  <tr key={a.assetId} className="hover:bg-slate-50">
                    <td className="px-4 py-3 font-bold text-[#0F172A]">{a.assetCode}</td>
                    <td className="px-4 py-3">{a.assetName}</td>
                    <td className="px-4 py-3">{a.categoryName}</td>
                    <td className="px-4 py-3 font-mono text-slate-600">{a.serialNumber}</td>
                    <td className="px-4 py-3 text-slate-700">{a.allocationDate ? new Date(a.allocationDate).toLocaleDateString() : 'Active'}</td>
                    <td className="px-4 py-3 font-semibold">₹{a.purchaseCost?.toLocaleString('en-IN')}</td>
                    <td className="px-4 py-3 text-right">
                      <Link to={`/assets/${a.assetId}`} className="text-[#2563EB] hover:underline font-semibold">
                        View Asset
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-center py-6 text-xs text-[#64748B]">
            <Laptop className="w-6 h-6 text-slate-300 mx-auto mb-1" />
            <p>No assets currently allocated to this employee.</p>
          </div>
        )}
      </Card>
    </div>
  );
};
