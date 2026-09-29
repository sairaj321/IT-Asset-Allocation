using Its.Application.Interfaces;
using Its.Domain.Entities;
using Its.Domain.Enums;
using Its.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Its.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher hasher, ILogger logger)
    {
        if (await context.Roles.AnyAsync())
        {
            return; // Already seeded
        }

        logger.LogInformation("Seeding initial ITS database schema & data...");

        // 1. Permissions
        var permissionCodes = new[]
        {
            ("Asset.View", "View IT asset details"),
            ("Asset.Create", "Create new IT assets"),
            ("Asset.Update", "Update existing IT assets"),
            ("Asset.Delete", "Deactivate IT assets"),
            ("Asset.Check", "Check asset availability and warranty"),
            ("Asset.Assign", "Assign IT asset to employee"),
            ("Asset.Return", "Process IT asset return"),
            ("Asset.Transfer", "Transfer asset between employees"),
            ("Asset.ViewHistory", "View allocation and repair history"),
            ("Request.Create", "Create asset request"),
            ("Request.ViewOwn", "View own asset requests"),
            ("Request.ViewAll", "View all asset requests"),
            ("Request.ViewAssigned", "View requests assigned to executive"),
            ("Request.Validate", "Validate asset requests"),
            ("Request.AssignToExecutive", "Assign request to an executive"),
            ("Request.Approve", "Approve asset requests"),
            ("Request.Reject", "Reject asset requests"),
            ("Repair.Create", "Report asset repair issue"),
            ("Repair.Update", "Update repair status and costs"),
            ("Repair.ViewAll", "View all repair logs"),
            ("Replacement.Request", "Request asset replacement"),
            ("Replacement.Approve", "Approve or reject replacement"),
            ("Executive.Create", "Create ITS Executive accounts"),
            ("Executive.Update", "Update ITS Executive profiles"),
            ("Executive.Deactivate", "Deactivate ITS Executive"),
            ("Executive.View", "View ITS Executive list"),
            ("Employee.View", "View employee list"),
            ("Report.View", "View and export ITS reports"),
            ("Dashboard.Employee", "Access employee dashboard"),
            ("Dashboard.Manager", "Access manager dashboard"),
            ("Dashboard.Executive", "Access executive dashboard")
        };

        var permissions = permissionCodes.Select(p => new Permission
        {
            Code = p.Item1,
            Description = p.Item2
        }).ToList();

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();

        // 2. Roles
        var roleManager = new Role { Name = "ITS_MANAGER", Description = "Full ITS Management Administrator" };
        var roleExecutive = new Role { Name = "ITS_EXECUTIVE", Description = "Task-focused IT Assets Executive" };
        var roleEmployee = new Role { Name = "EMPLOYEE", Description = "Company Employee" };

        await context.Roles.AddRangeAsync(roleManager, roleExecutive, roleEmployee);
        await context.SaveChangesAsync();

        // 3. Role Permissions
        var allPerms = await context.Permissions.ToListAsync();
        var managerPerms = allPerms.Select(p => new RolePermission { RoleId = roleManager.RoleId, PermissionId = p.PermissionId });

        var execCodes = new HashSet<string>
        {
            "Asset.View", "Asset.Check", "Asset.Assign", "Asset.Return", "Asset.Transfer", "Asset.ViewHistory",
            "Request.ViewAssigned", "Request.Reject",
            "Repair.Create", "Repair.Update", "Repair.ViewAll",
            "Executive.View", "Employee.View", "Dashboard.Executive"
        };
        var execPerms = allPerms.Where(p => execCodes.Contains(p.Code))
            .Select(p => new RolePermission { RoleId = roleExecutive.RoleId, PermissionId = p.PermissionId });

        var empCodes = new HashSet<string>
        {
            "Asset.View", "Request.Create", "Request.ViewOwn", "Repair.Create", "Replacement.Request", "Dashboard.Employee"
        };
        var empPerms = allPerms.Where(p => empCodes.Contains(p.Code))
            .Select(p => new RolePermission { RoleId = roleEmployee.RoleId, PermissionId = p.PermissionId });

        await context.RolePermissions.AddRangeAsync(managerPerms);
        await context.RolePermissions.AddRangeAsync(execPerms);
        await context.RolePermissions.AddRangeAsync(empPerms);
        await context.SaveChangesAsync();

        // 4. Departments
        var depts = new[]
        {
            new Department { Name = "Information Technology", IsActive = true },
            new Department { Name = "Software Engineering", IsActive = true },
            new Department { Name = "Human Resources", IsActive = true },
            new Department { Name = "Finance & Accounting", IsActive = true },
            new Department { Name = "Sales & Marketing", IsActive = true },
            new Department { Name = "Operations & Logistics", IsActive = true }
        };
        await context.Departments.AddRangeAsync(depts);
        await context.SaveChangesAsync();

        // 5. Asset Categories
        var categories = new[]
        {
            new AssetCategory { CategoryName = "Laptop", Description = "Portable computing laptops (Dell, HP, Apple, Lenovo)", IsActive = true },
            new AssetCategory { CategoryName = "Desktop", Description = "Stationary desktop workstations and towers", IsActive = true },
            new AssetCategory { CategoryName = "Monitor", Description = "External 24-inch to 32-inch displays", IsActive = true },
            new AssetCategory { CategoryName = "Keyboard", Description = "Ergonomic, wireless, and mechanical keyboards", IsActive = true },
            new AssetCategory { CategoryName = "Mouse", Description = "Optical and laser wireless mice", IsActive = true },
            new AssetCategory { CategoryName = "Mobile", Description = "Company corporate smartphones (iOS/Android)", IsActive = true },
            new AssetCategory { CategoryName = "Headset", Description = "Noise-cancelling headsets for conferencing", IsActive = true },
            new AssetCategory { CategoryName = "Printer", Description = "Network office printers and scanners", IsActive = true },
            new AssetCategory { CategoryName = "Accessories", Description = "Docks, USB hubs, adapters, and power cables", IsActive = true }
        };
        await context.AssetCategories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        // 6. Seed Employees & Users
        // Manager
        var mgrEmp = new Employee
        {
            EmployeeCode = "EMP001",
            FirstName = "Sairaj",
            LastName = "Kadlag",
            Email = "sairaj.kadlag@company.com",
            Mobile = "+91 9876543210",
            DepartmentId = depts[0].DepartmentId,
            Designation = "ITS Operations Manager",
            IsActive = true
        };
        await context.Employees.AddAsync(mgrEmp);
        await context.SaveChangesAsync();

        var mgrUser = new User
        {
            EmployeeId = mgrEmp.EmployeeId,
            Username = "manager",
            PasswordHash = hasher.HashPassword("Manager@123"),
            MustChangePassword = false,
            IsActive = true
        };
        await context.Users.AddAsync(mgrUser);
        await context.SaveChangesAsync();

        await context.UserRoles.AddAsync(new UserRole { UserId = mgrUser.UserId, RoleId = roleManager.RoleId });

        // 3 ITS Executives
        var execData = new[]
        {
            ("EMP002", "Priya", "Sharma", "priya.sharma@company.com", "+91 9876543211", "exec1", "Exec@123", "ITS Executive"),
            ("EMP003", "Amit", "Verma", "amit.verma@company.com", "+91 9876543212", "exec2", "Exec@123", "ITS Senior Executive"),
            ("EMP004", "Neha", "Gupta", "neha.gupta@company.com", "+91 9876543213", "exec3", "Exec@123", "ITS Executive")
        };

        var execUsers = new List<User>();
        foreach (var (code, first, last, email, mobile, uname, pwd, desig) in execData)
        {
            var emp = new Employee
            {
                EmployeeCode = code,
                FirstName = first,
                LastName = last,
                Email = email,
                Mobile = mobile,
                DepartmentId = depts[0].DepartmentId,
                Designation = desig,
                IsActive = true
            };
            await context.Employees.AddAsync(emp);
            await context.SaveChangesAsync();

            var user = new User
            {
                EmployeeId = emp.EmployeeId,
                Username = uname,
                PasswordHash = hasher.HashPassword(pwd),
                MustChangePassword = false,
                IsActive = true
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            await context.UserRoles.AddAsync(new UserRole { UserId = user.UserId, RoleId = roleExecutive.RoleId });
            execUsers.Add(user);
        }

        // 10 Employees
        var empData = new[]
        {
            ("EMP005", "Rahul", "Sharma", "rahul.sharma@company.com", "+91 9811100001", depts[1].DepartmentId, "Senior Software Engineer"),
            ("EMP006", "Ananya", "Patel", "ananya.patel@company.com", "+91 9811100002", depts[1].DepartmentId, "Frontend Engineer"),
            ("EMP007", "Rohan", "Mehta", "rohan.mehta@company.com", "+91 9811100003", depts[2].DepartmentId, "HR Specialist"),
            ("EMP008", "Sneha", "Rao", "sneha.rao@company.com", "+91 9811100004", depts[3].DepartmentId, "Financial Analyst"),
            ("EMP009", "Vikram", "Singh", "vikram.singh@company.com", "+91 9811100005", depts[4].DepartmentId, "Sales Lead"),
            ("EMP010", "Pooja", "Joshi", "pooja.joshi@company.com", "+91 9811100006", depts[4].DepartmentId, "Marketing Manager"),
            ("EMP011", "Karan", "Nair", "karan.nair@company.com", "+91 9811100007", depts[1].DepartmentId, "DevOps Engineer"),
            ("EMP012", "Divya", "Deshmukh", "divya.deshmukh@company.com", "+91 9811100008", depts[5].DepartmentId, "Operations Executive"),
            ("EMP013", "Arjun", "Reddy", "arjun.reddy@company.com", "+91 9811100009", depts[1].DepartmentId, "QA Automation Engineer"),
            ("EMP014", "Meera", "Iyer", "meera.iyer@company.com", "+91 9811100010", depts[2].DepartmentId, "Talent Acquisition Lead")
        };

        var allEmpEntities = new List<Employee> { mgrEmp };
        var empUsers = new List<User>();

        foreach (var (code, first, last, email, mobile, depId, desig) in empData)
        {
            var emp = new Employee
            {
                EmployeeCode = code,
                FirstName = first,
                LastName = last,
                Email = email,
                Mobile = mobile,
                DepartmentId = depId,
                Designation = desig,
                IsActive = true
            };
            await context.Employees.AddAsync(emp);
            await context.SaveChangesAsync();
            allEmpEntities.Add(emp);

            var user = new User
            {
                EmployeeId = emp.EmployeeId,
                Username = code.ToLower(),
                PasswordHash = hasher.HashPassword("Emp@123"),
                MustChangePassword = false,
                IsActive = true
            };
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            await context.UserRoles.AddAsync(new UserRole { UserId = user.UserId, RoleId = roleEmployee.RoleId });
            empUsers.Add(user);
        }

        await context.SaveChangesAsync();

        // 7. Seed 25 Sample IT Assets
        var catLaptop = categories[0].AssetCategoryId;
        var catDesktop = categories[1].AssetCategoryId;
        var catMonitor = categories[2].AssetCategoryId;
        var catMobile = categories[5].AssetCategoryId;
        var catKey = categories[3].AssetCategoryId;
        var catHeadset = categories[6].AssetCategoryId;

        var sampleAssets = new List<ITAsset>
        {
            // Laptops (8)
            new() { AssetCode = "LAP-0001", AssetName = "Dell Latitude 5440", AssetCategoryId = catLaptop, SerialNumber = "DELL123456", Brand = "Dell", Model = "Latitude 5440", PurchaseDate = DateTime.UtcNow.AddMonths(-18), PurchaseCost = 75000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-18), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(18), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Pune Office - Floor 3", CurrentEmployeeId = mgrEmp.EmployeeId, IsActive = true },
            new() { AssetCode = "LAP-0002", AssetName = "HP ProBook 450 G10", AssetCategoryId = catLaptop, SerialNumber = "HP789123", Brand = "HP", Model = "ProBook 450", PurchaseDate = DateTime.UtcNow.AddMonths(-6), PurchaseCost = 68000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-6), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(30), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Pune Office - IT Store", IsActive = true },
            new() { AssetCode = "LAP-0003", AssetName = "Lenovo ThinkPad T14 Gen 4", AssetCategoryId = catLaptop, SerialNumber = "LEN456789", Brand = "Lenovo", Model = "ThinkPad T14", PurchaseDate = DateTime.UtcNow.AddMonths(-10), PurchaseCost = 82000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-10), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(26), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Pune Office - IT Store", IsActive = true },
            new() { AssetCode = "LAP-0004", AssetName = "Apple MacBook Pro 14 M3", AssetCategoryId = catLaptop, SerialNumber = "APP998877", Brand = "Apple", Model = "MacBook Pro 14", PurchaseDate = DateTime.UtcNow.AddMonths(-4), PurchaseCost = 169000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-4), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(20), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Pune Office - Floor 4", CurrentEmployeeId = allEmpEntities[1].EmployeeId, IsActive = true },
            new() { AssetCode = "LAP-0005", AssetName = "Dell XPS 15 9530", AssetCategoryId = catLaptop, SerialNumber = "DELL554433", Brand = "Dell", Model = "XPS 15", PurchaseDate = DateTime.UtcNow.AddMonths(-8), PurchaseCost = 145000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-8), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(16), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Pune Office - Floor 3", CurrentEmployeeId = allEmpEntities[2].EmployeeId, IsActive = true },
            new() { AssetCode = "LAP-0006", AssetName = "Asus ZenBook 14 OLED", AssetCategoryId = catLaptop, SerialNumber = "ASU112233", Brand = "Asus", Model = "ZenBook 14", PurchaseDate = DateTime.UtcNow.AddMonths(-2), PurchaseCost = 79000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-2), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(22), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Pune Office - IT Store", IsActive = true },
            new() { AssetCode = "LAP-0007", AssetName = "Dell Latitude 5430", AssetCategoryId = catLaptop, SerialNumber = "DELL776655", Brand = "Dell", Model = "Latitude 5430", PurchaseDate = DateTime.UtcNow.AddMonths(-24), PurchaseCost = 62000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-24), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(12), Status = AssetStatus.UnderMaintenance, Condition = AssetCondition.NotWorking, Location = "IT Repair Lab", IsActive = true },
            new() { AssetCode = "LAP-0008", AssetName = "HP EliteBook 840 G9", AssetCategoryId = catLaptop, SerialNumber = "HP334455", Brand = "HP", Model = "EliteBook 840", PurchaseDate = DateTime.UtcNow.AddMonths(-34), PurchaseCost = 71000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-34), WarrantyExpiryDate = DateTime.UtcNow.AddDays(15), Status = AssetStatus.Available, Condition = AssetCondition.Fair, Location = "Pune Office - IT Store", IsActive = true }, // Near warranty expiry!

            // Desktops (3)
            new() { AssetCode = "DES-0001", AssetName = "HP ProDesk 600 G6", AssetCategoryId = catDesktop, SerialNumber = "HPD123987", Brand = "HP", Model = "ProDesk 600", PurchaseDate = DateTime.UtcNow.AddMonths(-14), PurchaseCost = 52000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-14), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(22), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Workstation Pod B", IsActive = true },
            new() { AssetCode = "DES-0002", AssetName = "Dell OptiPlex 7090", AssetCategoryId = catDesktop, SerialNumber = "DEL778899", Brand = "Dell", Model = "OptiPlex 7090", PurchaseDate = DateTime.UtcNow.AddMonths(-16), PurchaseCost = 58000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-16), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(20), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Finance Wing", CurrentEmployeeId = allEmpEntities[4].EmployeeId, IsActive = true },
            new() { AssetCode = "DES-0003", AssetName = "Lenovo ThinkCentre M70q", AssetCategoryId = catDesktop, SerialNumber = "LEN332211", Brand = "Lenovo", Model = "ThinkCentre M70q", PurchaseDate = DateTime.UtcNow.AddMonths(-11), PurchaseCost = 48000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-11), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(25), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Pune Office - IT Store", IsActive = true },

            // Monitors (5)
            new() { AssetCode = "MON-0001", AssetName = "Dell 24\" FHD Monitor P2422H", AssetCategoryId = catMonitor, SerialNumber = "MON123456", Brand = "Dell", Model = "P2422H", PurchaseDate = DateTime.UtcNow.AddMonths(-12), PurchaseCost = 14500, WarrantyStartDate = DateTime.UtcNow.AddMonths(-12), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(24), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Pune Office - Floor 3", CurrentEmployeeId = mgrEmp.EmployeeId, IsActive = true },
            new() { AssetCode = "MON-0002", AssetName = "Dell 24\" Monitor SE2422HX", AssetCategoryId = catMonitor, SerialNumber = "MON654321", Brand = "Dell", Model = "SE2422HX", PurchaseDate = DateTime.UtcNow.AddMonths(-8), PurchaseCost = 12000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-8), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(28), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Pune Office - Floor 4", CurrentEmployeeId = allEmpEntities[1].EmployeeId, IsActive = true },
            new() { AssetCode = "MON-0003", AssetName = "LG 27\" 4K UHD 27UL500", AssetCategoryId = catMonitor, SerialNumber = "LG778899", Brand = "LG", Model = "27UL500", PurchaseDate = DateTime.UtcNow.AddMonths(-5), PurchaseCost = 26500, WarrantyStartDate = DateTime.UtcNow.AddMonths(-5), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(31), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Pune Office - IT Store", IsActive = true },
            new() { AssetCode = "MON-0004", AssetName = "Samsung 24\" Curved CR50", AssetCategoryId = catMonitor, SerialNumber = "SAM112233", Brand = "Samsung", Model = "CR50", PurchaseDate = DateTime.UtcNow.AddMonths(-15), PurchaseCost = 11500, WarrantyStartDate = DateTime.UtcNow.AddMonths(-15), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(21), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Pune Office - IT Store", IsActive = true },
            new() { AssetCode = "MON-0005", AssetName = "BenQ 27\" Designer Monitor PD2705Q", AssetCategoryId = catMonitor, SerialNumber = "BEN998877", Brand = "BenQ", Model = "PD2705Q", PurchaseDate = DateTime.UtcNow.AddMonths(-30), PurchaseCost = 34000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-30), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(-6), Status = AssetStatus.Damaged, Condition = AssetCondition.Damaged, Location = "Scrap & Disposal Room", IsActive = true }, // Damaged & expired!

            // Mobiles (3)
            new() { AssetCode = "MOB-0001", AssetName = "Apple iPhone 15 128GB", AssetCategoryId = catMobile, SerialNumber = "APP123456", Brand = "Apple", Model = "iPhone 15", PurchaseDate = DateTime.UtcNow.AddMonths(-6), PurchaseCost = 72000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-6), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(6), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Sales Wing", CurrentEmployeeId = allEmpEntities[5].EmployeeId, IsActive = true },
            new() { AssetCode = "MOB-0002", AssetName = "Samsung Galaxy S24 256GB", AssetCategoryId = catMobile, SerialNumber = "SAM445566", Brand = "Samsung", Model = "Galaxy S24", PurchaseDate = DateTime.UtcNow.AddMonths(-3), PurchaseCost = 69000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-3), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(9), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "IT Safe", IsActive = true },
            new() { AssetCode = "MOB-0003", AssetName = "Google Pixel 8 128GB", AssetCategoryId = catMobile, SerialNumber = "GOO778899", Brand = "Google", Model = "Pixel 8", PurchaseDate = DateTime.UtcNow.AddMonths(-9), PurchaseCost = 59000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-9), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(3), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "IT Safe", IsActive = true },

            // Keyboards & Mice (3)
            new() { AssetCode = "KEY-0001", AssetName = "Logitech MX Keys Advanced Wireless", AssetCategoryId = catKey, SerialNumber = "LOG156789", Brand = "Logitech", Model = "MX Keys", PurchaseDate = DateTime.UtcNow.AddMonths(-7), PurchaseCost = 9500, WarrantyStartDate = DateTime.UtcNow.AddMonths(-7), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(17), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Pune Office - IT Store", IsActive = true },
            new() { AssetCode = "KEY-0002", AssetName = "Keychron K2 Mechanical Keyboard", AssetCategoryId = catKey, SerialNumber = "KEY223344", Brand = "Keychron", Model = "K2", PurchaseDate = DateTime.UtcNow.AddMonths(-10), PurchaseCost = 8200, WarrantyStartDate = DateTime.UtcNow.AddMonths(-10), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(14), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Floor 4 - Eng Desk", CurrentEmployeeId = allEmpEntities[1].EmployeeId, IsActive = true },
            new() { AssetCode = "KEY-0003", AssetName = "Dell Pro Wireless Keyboard & Mouse Combo", AssetCategoryId = catKey, SerialNumber = "DEL990011", Brand = "Dell", Model = "KM5221W", PurchaseDate = DateTime.UtcNow.AddMonths(-4), PurchaseCost = 3500, WarrantyStartDate = DateTime.UtcNow.AddMonths(-4), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(32), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "Pune Office - IT Store", IsActive = true },

            // Headsets (3)
            new() { AssetCode = "HED-0001", AssetName = "Jabra Evolve 65 UC Wireless Headset", AssetCategoryId = catHeadset, SerialNumber = "JAB123456", Brand = "Jabra", Model = "Evolve 65", PurchaseDate = DateTime.UtcNow.AddMonths(-11), PurchaseCost = 16500, WarrantyStartDate = DateTime.UtcNow.AddMonths(-11), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(13), Status = AssetStatus.Available, Condition = AssetCondition.Good, Location = "IT Store", IsActive = true },
            new() { AssetCode = "HED-0002", AssetName = "Sony WH-1000XM4 Noise Cancelling", AssetCategoryId = catHeadset, SerialNumber = "SON445566", Brand = "Sony", Model = "WH-1000XM4", PurchaseDate = DateTime.UtcNow.AddMonths(-8), PurchaseCost = 22000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-8), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(16), Status = AssetStatus.Assigned, Condition = AssetCondition.Good, Location = "Floor 3", CurrentEmployeeId = mgrEmp.EmployeeId, IsActive = true },
            new() { AssetCode = "HED-0003", AssetName = "Logitech Zone Wireless Headset", AssetCategoryId = catHeadset, SerialNumber = "LOG889900", Brand = "Logitech", Model = "Zone Wireless", PurchaseDate = DateTime.UtcNow.AddMonths(-14), PurchaseCost = 18000, WarrantyStartDate = DateTime.UtcNow.AddMonths(-14), WarrantyExpiryDate = DateTime.UtcNow.AddMonths(10), Status = AssetStatus.InRepair, Condition = AssetCondition.NotWorking, Location = "Vendor Repair Service", IsActive = true }
        };

        await context.Assets.AddRangeAsync(sampleAssets);
        await context.SaveChangesAsync();

        // 8. Seed Sample Allocations (History and Active)
        // LAP-0001 historical allocation: Rahul Sharma from 10-Jan-2026 to 14-Aug-2026, then Sairaj Kadlag from 15-Aug-2026 Active
        var allocHist1 = new AssetAllocation
        {
            AssetId = sampleAssets[0].AssetId,
            EmployeeId = allEmpEntities[1].EmployeeId,
            AllocatedBy = mgrUser.UserId,
            AllocationDate = new DateTime(2026, 1, 10, 10, 0, 0, DateTimeKind.Utc),
            ReturnDate = new DateTime(2026, 8, 14, 17, 30, 0, DateTimeKind.Utc),
            AllocationStatus = AllocationStatus.Returned,
            ConditionAtAllocation = AssetCondition.Good,
            ConditionAtReturn = AssetCondition.Good,
            Remarks = "Project completed; returned in good condition."
        };

        var allocActive1 = new AssetAllocation
        {
            AssetId = sampleAssets[0].AssetId,
            EmployeeId = mgrEmp.EmployeeId,
            AllocatedBy = execUsers[0].UserId,
            AllocationDate = new DateTime(2026, 8, 15, 9, 30, 0, DateTimeKind.Utc),
            ExpectedReturnDate = new DateTime(2027, 8, 15, 0, 0, 0, DateTimeKind.Utc),
            AllocationStatus = AllocationStatus.Active,
            ConditionAtAllocation = AssetCondition.Good,
            Remarks = "Issued for development and management operations."
        };

        var allocActive2 = new AssetAllocation
        {
            AssetId = sampleAssets[3].AssetId,
            EmployeeId = allEmpEntities[1].EmployeeId,
            AllocatedBy = execUsers[0].UserId,
            AllocationDate = DateTime.UtcNow.AddMonths(-3),
            ExpectedReturnDate = DateTime.UtcNow.AddMonths(9),
            AllocationStatus = AllocationStatus.Active,
            ConditionAtAllocation = AssetCondition.Good,
            Remarks = "Standard issue for software engineer."
        };

        var allocActive3 = new AssetAllocation
        {
            AssetId = sampleAssets[11].AssetId, // MON-0001
            EmployeeId = mgrEmp.EmployeeId,
            AllocatedBy = mgrUser.UserId,
            AllocationDate = DateTime.UtcNow.AddMonths(-6),
            ExpectedReturnDate = DateTime.UtcNow.AddMonths(6),
            AllocationStatus = AllocationStatus.Active,
            ConditionAtAllocation = AssetCondition.Good,
            Remarks = "Desk display."
        };

        await context.AssetAllocations.AddRangeAsync(allocHist1, allocActive1, allocActive2, allocActive3);
        await context.SaveChangesAsync();

        // 9. Seed Sample Requests with Timeline
        var req1 = new AssetRequest
        {
            RequestNumber = "REQ-0001",
            EmployeeId = mgrEmp.EmployeeId,
            AssetCategoryId = catLaptop,
            Reason = "Development and management workstation requirements",
            RequiredDate = DateTime.UtcNow.AddDays(5),
            Priority = Priority.High,
            Status = RequestStatus.Allocated,
            AssignedExecutiveId = execUsers[0].UserId,
            ManagerRemarks = "Approved, high priority development work.",
            AllocatedAssetId = sampleAssets[0].AssetId,
            RequestType = RequestType.NewAsset,
            CreatedAt = new DateTime(2026, 8, 10, 11, 0, 0, DateTimeKind.Utc)
        };

        var req2 = new AssetRequest
        {
            RequestNumber = "REQ-0002",
            EmployeeId = allEmpEntities[2].EmployeeId, // Ananya
            AssetCategoryId = catMonitor,
            Reason = "Dual display setup needed for frontend testing and responsive layouts.",
            RequiredDate = DateTime.UtcNow.AddDays(3),
            Priority = Priority.Normal,
            Status = RequestStatus.AssignedToExecutive,
            AssignedExecutiveId = execUsers[0].UserId,
            ManagerRemarks = "Valid request. Priya, please check monitor inventory and allocate.",
            RequestType = RequestType.NewAsset,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var req3 = new AssetRequest
        {
            RequestNumber = "REQ-0003",
            EmployeeId = allEmpEntities[3].EmployeeId, // Rohan
            AssetCategoryId = catLaptop,
            Reason = "Replacement laptop needed as current one is slow.",
            RequiredDate = DateTime.UtcNow.AddDays(7),
            Priority = Priority.Normal,
            Status = RequestStatus.Submitted,
            RequestType = RequestType.NewAsset,
            CreatedAt = DateTime.UtcNow.AddHours(-6)
        };

        await context.AssetRequests.AddRangeAsync(req1, req2, req3);
        await context.SaveChangesAsync();

        // Request Histories
        var reqHistories = new[]
        {
            new AssetRequestHistory { RequestId = req1.RequestId, FromStatus = null, ToStatus = RequestStatus.Submitted, ActionBy = mgrUser.UserId, Remarks = "Request submitted.", CreatedAt = req1.CreatedAt },
            new AssetRequestHistory { RequestId = req1.RequestId, FromStatus = RequestStatus.Submitted, ToStatus = RequestStatus.AssignedToExecutive, ActionBy = mgrUser.UserId, Remarks = "Approved and assigned to Priya.", CreatedAt = req1.CreatedAt.AddHours(2) },
            new AssetRequestHistory { RequestId = req1.RequestId, FromStatus = RequestStatus.AssignedToExecutive, ToStatus = RequestStatus.Checking, ActionBy = execUsers[0].UserId, Remarks = "Checked inventory and verified LAP-0001.", CreatedAt = req1.CreatedAt.AddHours(4) },
            new AssetRequestHistory { RequestId = req1.RequestId, FromStatus = RequestStatus.Checking, ToStatus = RequestStatus.Allocated, ActionBy = execUsers[0].UserId, Remarks = "Allocated Dell Latitude 5440 (LAP-0001).", CreatedAt = req1.CreatedAt.AddHours(5) },

            new AssetRequestHistory { RequestId = req2.RequestId, FromStatus = null, ToStatus = RequestStatus.Submitted, ActionBy = empUsers[1].UserId, Remarks = "Request submitted by Ananya Patel.", CreatedAt = req2.CreatedAt },
            new AssetRequestHistory { RequestId = req2.RequestId, FromStatus = RequestStatus.Submitted, ToStatus = RequestStatus.AssignedToExecutive, ActionBy = mgrUser.UserId, Remarks = "Manager verified request and assigned to Priya Sharma.", CreatedAt = req2.CreatedAt.AddHours(3) },

            new AssetRequestHistory { RequestId = req3.RequestId, FromStatus = null, ToStatus = RequestStatus.Submitted, ActionBy = empUsers[2].UserId, Remarks = "Request submitted by Rohan Mehta.", CreatedAt = req3.CreatedAt }
        };

        await context.RequestHistories.AddRangeAsync(reqHistories);

        // 10. Sample Repairs
        var repair1 = new AssetRepair
        {
            AssetId = sampleAssets[6].AssetId, // LAP-0007
            ReportedByEmployeeId = allEmpEntities[1].EmployeeId,
            HandledByExecutiveId = execUsers[0].UserId,
            IssueDescription = "Display backlight flickering and intermittent power cutoff.",
            Status = RepairStatus.UnderReview,
            VendorName = "Dell Authorized Service Center, Pune",
            RepairCost = 4500,
            SentDate = DateTime.UtcNow.AddDays(-1),
            ConditionAfterRepair = null
        };
        await context.AssetRepairs.AddAsync(repair1);

        // 11. Sample Notifications
        var notif1 = new Notification
        {
            UserId = mgrUser.UserId,
            Title = "New Asset Request REQ-0003",
            Message = "Rohan Mehta submitted a new request for Laptop.",
            Type = "Info",
            RelatedEntity = "AssetRequest",
            RelatedId = req3.RequestId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow.AddHours(-6)
        };

        var notif2 = new Notification
        {
            UserId = execUsers[0].UserId,
            Title = "New Task Assigned REQ-0002",
            Message = "Manager assigned Monitor request REQ-0002 to you for checking and allocation.",
            Type = "Info",
            RelatedEntity = "AssetRequest",
            RelatedId = req2.RequestId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow.AddHours(-12)
        };

        var notif3 = new Notification
        {
            UserId = mgrUser.UserId,
            Title = "Asset LAP-0001 Allocated",
            Message = "New Dell Latitude 5440 (LAP-0001) has been successfully allocated to Sairaj Kadlag.",
            Type = "Success",
            RelatedEntity = "ITAsset",
            RelatedId = sampleAssets[0].AssetId,
            IsRead = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        await context.Notifications.AddRangeAsync(notif1, notif2, notif3);

        // 12. Audit Log sample
        var audit = new AuditLog
        {
            UserId = mgrUser.UserId,
            Action = "Asset Assigned",
            EntityName = "ITAsset",
            EntityId = sampleAssets[0].AssetId.ToString(),
            OldValue = "{\"Status\":\"Available\"}",
            NewValue = "{\"Status\":\"Assigned\",\"Employee\":\"Sairaj Kadlag\",\"AssetCode\":\"LAP-0001\"}",
            IpAddress = "127.0.0.1",
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        await context.AuditLogs.AddAsync(audit);

        await context.SaveChangesAsync();
        logger.LogInformation("Database seeding completed successfully with Manager, Executives, Employees, Categories, Assets, and Requests.");
    }
}
