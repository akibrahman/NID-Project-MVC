# National ID Card Management System

An ASP.NET Core MVC web application for managing National ID (NID) applications. The system supports three roles: **Admin**, **Moderator**, and **User**. Users can register with their personal details and upload a photo. Moderators review and approve pending registrations, and can block/unblock users. Admins manage moderators (create, block/unblock). The design is clean, light-themed, and built with Bootstrap.

## Features

- **User Registration**  
  - Collects comprehensive NID-related data (name, parents, DOB, gender, address, blood group, occupation, photo upload).
  - Automatically assigns the "User" role.
  - New registrations are marked as **Pending Approval**.

- **Role-Based Dashboards**  
  - **Admin Dashboard**: Create moderators, view list of moderators, block/unblock moderators.
  - **Moderator Dashboard**: View own profile, list of pending users, approve users, view all approved users, block/unblock users.
  - **User Dashboard**: Display personal information, NID status, approval status, blocked status, and profile photo.

- **Approval Workflow**  
  - Users must be approved by a moderator before they are considered "Active".
  - Pending users appear in the moderator's "Pending Users" list; one click approves them.

- **Blocking Mechanism**  
  - Moderators can block/unblock users.
  - Admins can block/unblock moderators.
  - Blocked moderators cannot access dashboard or any management actions (redirected to a "Blocked" page).
  - Blocked status is reflected on the user's dashboard.

- **Authentication & Authorization**  
  - Uses ASP.NET Core Identity with role-based access control.
  - Custom `ApplicationUser` extends IdentityUser with NID fields.
  - Full name is stored as a claim and shown in the navbar instead of email.

- **Professional UI**  
  - Light theme, responsive Bootstrap 5 layout.
  - Red logout button, proper navbar alignment.
  - Status badges for approval and blocking states.

## Tech Stack

- **Framework**: ASP.NET Core MVC (.NET 10.0)
- **Database**: Microsoft SQL Server (via Entity Framework Core)
- **Authentication**: ASP.NET Core Identity
- **ORM**: Entity Framework Core (Code First)
- **UI**: Bootstrap 5, Razor Views
- **File Storage**: Local `wwwroot/uploads` for profile photos

## Prerequisites

- .NET SDK 10.0 (or higher)
- Visual Studio 2022 (or VS Code with C# extension)
- SQL Server (LocalDB or full instance)
- Git (optional, for version control)

## Setup Instructions

### 1. Clone or Create the Project

If you have the code, open it in Visual Studio. Otherwise, create a new ASP.NET Core MVC project and copy the provided files.

### 2. Configure the Database

- Open SQL Server Management Studio (SSMS) or use LocalDB.
- Create a new database named `NIDManagementDb` (or any name you prefer).

Update the connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=NIDManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Adjust `Server` if using a named instance (e.g., `localhost\\SQLEXPRESS`).

### 3. Install NuGet Packages

Run these commands in Package Manager Console:

```powershell
Install-Package Microsoft.EntityFrameworkCore.SqlServer
Install-Package Microsoft.EntityFrameworkCore.Tools
Install-Package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

### 4. Apply Migrations

In Package Manager Console:

```powershell
Add-Migration InitialCreate
Update-Database
```

The database will be created with all Identity tables plus custom fields.

### 5. Run the Application

Press F5 or run `dotnet run`. The application will automatically:
- Create the three roles: Admin, Moderator, User.
- Seed an admin account if it doesn't exist.

### 6. Default Admin Credentials

- **Email**: `admin@nid.gov.bd`
- **Password**: `Admin@123`

Log in as admin to create moderator accounts.

## Roles & Permissions

| Role      | Permissions                                                                 |
|-----------|-----------------------------------------------------------------------------|
| **Admin** | Create moderators, block/unblock moderators, view moderator list, all admin actions |
| **Moderator** | View pending users, approve users, view all approved users, block/unblock users (if not blocked) |
| **User**   | Register, view own dashboard/profile, see approval & blocked status          |

> Blocked moderators cannot access any moderator functions (redirected to Blocked page).
> Blocked users still can log in but their status shows blocked; future enhancements can prevent login.

## Project Structure

```
NID_Project/
├── Controllers/
│   ├── AccountController.cs
│   ├── AdminController.cs
│   ├── ModeratorController.cs
│   └── UserController.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── ApplicationUser.cs
│   ├── RegisterViewModel.cs
│   ├── LoginViewModel.cs
│   └── CreateModeratorViewModel.cs
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   └── AccessDenied.cshtml
│   ├── Admin/
│   │   ├── Dashboard.cshtml
│   │   └── CreateModerator.cshtml
│   ├── Moderator/
│   │   ├── Dashboard.cshtml
│   │   ├── PendingUsers.cshtml
│   │   ├── AllUsers.cshtml
│   │   └── Blocked.cshtml
│   ├── User/
│   │   └── Dashboard.cshtml
│   └── Shared/
│       └── _Layout.cshtml
├── wwwroot/
│   └── uploads/          (uploaded profile photos)
├── Program.cs
├── appsettings.json
└── README.md
```

## Usage Flow

1. **Admin Login**  
   - Use `admin@nid.gov.bd` / `Admin@123`  
   - Navigate to "Create Moderator" to add moderator accounts.  
   - View moderators and block/unblock them.

2. **Moderator Login**  
   - Created by admin.  
   - Dashboard shows links to "Pending Users" and "All Users".  
   - Approve pending users by clicking "Approve".  
   - Block or unblock approved users from "All Users".

3. **User Registration**  
   - Click "Register" on homepage, fill out NID details and upload photo.  
   - After registration, user is logged in but status is "Pending Approval".  
   - Check own dashboard for status updates.

## Additional Notes

- The admin account is seeded automatically on startup; no manual database insertion is required.
- The `UserName` is set to the user's email for uniqueness; full name is stored in a claim for display.
- Photo uploads are stored in `wwwroot/uploads` with a GUID prefix to avoid name collisions.
- To add more features (e.g., editing profile, generating digital NID), extend the existing controllers and models.

## Troubleshooting

- **"Element with same key..." error in Visual Studio**: Delete the hidden `.vs` folder and rebuild. Also try running `dotnet run` from command line to isolate.
- **Git not recognized**: Install Git for Windows from [git-scm.com](https://git-scm.com/download/win).
- **Database connection failure**: Ensure SQL Server is running and connection string is correct.

## License

This project is for educational purposes. Feel free to use and modify.