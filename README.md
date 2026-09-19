# National ID Card Management System

An ASP.NET Core MVC web application for managing National ID (NID) applications. The system supports three roles: **Admin**, **Moderator**, and **User**. Users can register with their personal details and upload a photo. Moderators review and approve pending registrations, block/unblock users, and handle user data edit applications. Admins manage moderators. The design is clean, light-themed, and built with Bootstrap.

## Features

- **User Registration**  
  - Collects comprehensive NID-related data (name, parents, DOB, gender, address, blood group, occupation, photo upload).
  - Restricts photo uploads to **JPG, JPEG, PNG** formats (both frontend and backend validation).
  - Automatically assigns the "User" role.
  - New registrations are marked as **Pending Approval**.

- **Role-Based Dashboards**  
  - **Admin Dashboard**: Statistics overview (total moderators, total users, pending users). Separate **Moderators** page for listing, creating, blocking, and unblocking moderators.
  - **Moderator Dashboard**: View own profile, list of pending users, approve users, view all approved users (paginated, 10 per page), block/unblock users, view user details, and manage **Edit Applications**.
  - **User Dashboard**: Display personal information, NID status, approval status, blocked status, and profile photo.

- **Approval Workflow**  
  - Users must be approved by a moderator before they are considered "Active".
  - Pending users appear in the moderator's "Pending Users" list; one click approves them.
  - Upon approval, a **unique 10-digit NID number** is generated and stored for the user.

- **Blocking Mechanism**  
  - Moderators can block/unblock users.
  - Admins can block/unblock moderators.
  - Blocked moderators cannot access any moderator functionality (redirected to a "Blocked" page).
  - Blocked users cannot apply for edits or submit edits; their dashboard reflects the blocked status.

- **PDF NID Download**  
  - Approved and non-blocked users can download a **PDF version of their NID card** from their dashboard.
  - The PDF is a credit-card sized card with a professional layout (dark blue header, white body, photo and details).

- **User Data Edit Application Workflow** *(two-stage moderator review)*  
  - Users can request a change of **any profile field** (Full Name, Father's Name, Mother's Name, DOB, Gender, Nationality, Religion, Occupation, Blood Group, Present/Permanent Address, Photo).
  - **Frequency restriction**: only **one application per month** per user; if an active application exists (Pending/Approved/Edited), a new one cannot be created.
  - Users can **delete** a Pending or Approved application, which then allows a new one in the same month if the monthly window is respected (the monthly window is based on the application's creation date).
  - **Two-stage review**:
    1. **Stage 1 – Request Review**: A moderator either **approves** the request (unlocking the selected fields for the user to edit) or **rejects** it (with a mandatory rejection message).
    2. **Stage 2 – Change Review**: After the user submits their edited values, a moderator reviews the **proposed changes**. The moderator can **approve** (changes are persisted to the user record) or **reject** (with a mandatory rejection message; no changes are applied).
  - **Previous vs. New values**: When a user submits edits, the system stores both the **old value** and the **new value** for each changed field. Moderators see both columns in the details page.
  - **Full audit trail**: each application records who reviewed each stage, when, and with what message. The complete history is preserved and viewable at any time.
  - **Application states**:
    - `Pending` – created, awaiting Stage 1 review
    - `Rejected` – Stage 1 rejected
    - `Approved` – Stage 1 approved, awaiting user edits
    - `Edited` – user submitted edits, awaiting Stage 2 review
    - `Completed` – Stage 2 approved, changes applied to user
    - `ChangeRejected` – Stage 2 rejected
  - **Access control**:
    - Blocked users cannot create or edit applications.
    - Blocked moderators cannot review applications.

- **Authentication & Authorization**  
  - Uses ASP.NET Core Identity with role-based access control.
  - Custom `ApplicationUser` extends IdentityUser with NID fields and an `IsBlocked` flag.
  - Full name is stored as a claim and shown in the navbar.

- **Professional UI**  
  - Light theme, responsive Bootstrap 5 layout.
  - Modern navbar with logo, animated hover underline, and red logout button.
  - Full-height layout with an enhanced multi-column footer.
  - Status badges for approval, blocking, and application states.

## Tech Stack

- **Framework**: ASP.NET Core MVC (.NET 10.0)
- **Database**: Microsoft SQL Server (via Entity Framework Core)
- **Authentication**: ASP.NET Core Identity
- **ORM**: Entity Framework Core (Code First)
- **UI**: Bootstrap 5, Razor Views, Bootstrap Icons
- **File Storage**: Local `wwwroot/uploads` for profile photos
- **PDF Generation**: QuestPDF
- **Image Processing**: SkiaSharp (for re-encoding photos to PNG before PDF embedding)
- **JSON Serialization**: `System.Text.Json` for storing proposed field changes

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
Install-Package QuestPDF
Install-Package SkiaSharp
```

### 4. Apply Migrations

In Package Manager Console:

```powershell
Add-Migration InitialCreate
Update-Database
```

The database will be created with all Identity tables, custom user fields, and the `EditApplications` table.

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
| **Admin** | View statistics dashboard, create moderators, view moderator list, block/unblock moderators |
| **Moderator** | View pending users, approve users (generates NID number), view all approved users (paginated), block/unblock users, view user details, review edit applications (both stages) |
| **User**   | Register, view dashboard/profile, see approval & blocked status, download NID PDF (if approved and not blocked), apply for data edits once per month, delete pending/approved applications, edit approved fields, track full application history |

> Blocked moderators cannot access any moderator functions (redirected to Blocked page).
> Blocked users cannot download PDF, create applications, or edit fields.

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
│   ├── CreateModeratorViewModel.cs
│   ├── AdminDashboardViewModel.cs
│   ├── PaginatedList.cs
│   ├── EditApplication.cs
│   ├── CreateApplicationViewModel.cs
│   ├── UserEditFieldsViewModel.cs
│   └── FieldChange.cs
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   └── AccessDenied.cshtml
│   ├── Admin/
│   │   ├── Dashboard.cshtml          (stats cards)
│   │   ├── Moderators.cshtml         (moderator list with block/unblock)
│   │   └── CreateModerator.cshtml
│   ├── Moderator/
│   │   ├── Dashboard.cshtml
│   │   ├── PendingUsers.cshtml
│   │   ├── AllUsers.cshtml           (paginated)
│   │   ├── UserDetails.cshtml
│   │   ├── Applications.cshtml
│   │   ├── ApplicationDetails.cshtml
│   │   └── Blocked.cshtml
│   ├── User/
│   │   ├── Dashboard.cshtml
│   │   ├── MyApplication.cshtml
│   │   ├── CreateApplication.cshtml
│   │   └── EditFields.cshtml
│   └── Shared/
│       └── _Layout.cshtml
├── wwwroot/
│   ├── css/site.css
│   ├── images/                       (logo)
│   └── uploads/                      (uploaded profile photos)
├── Program.cs
├── appsettings.json
└── README.md
```

## Usage Flow

1. **Admin Login**  
   - Use `admin@nid.gov.bd` / `Admin@123`  
   - Dashboard shows total moderators, total users, and pending users.
   - Click "Total Moderators" card or navbar link to open the Moderators page.
   - Create moderators; block or unblock them.

2. **Moderator Login**  
   - Created by admin.  
   - Dashboard shows links to Pending Users, All Users, and Applications.
   - Approve pending users (auto-generates a unique 10-digit NID number).
   - Block or unblock approved users from All Users (10 per page with pagination).
   - Review edit applications (Stage 1 and Stage 2) from the Applications page.

3. **User Registration**  
   - Click "Register", fill in NID details and upload a photo (JPG/PNG only).  
   - After registration, status is "Pending Approval".  
   - Once approved and not blocked, a "Download NID PDF" button appears on the dashboard.

4. **User Data Edit Request**  
   - User opens "My Applications" from the navbar.
   - If eligible (no active application and last application was more than a month ago), they click "Apply for Edit".
   - They select the fields they want to change and submit the application.
   - A moderator reviews the request (Stage 1). If approved, the user can edit only those fields.
   - After the user submits their edits, a moderator reviews the changes (Stage 2). The moderator sees **Previous Value** and **New Value** side by side.
   - If approved, the changes are applied to the user record; if rejected, a rejection message is stored.
   - The full history (who reviewed, when, and any messages) remains visible on the application details page.

## Additional Notes

- The admin account is seeded automatically on startup; no manual database insertion is required.
- `UserName` is set to the user's email for uniqueness; full name is stored as a claim for navbar display.
- Photo uploads are restricted to `.jpg`, `.jpeg`, `.png` on both frontend (JS) and backend (controller).
- Uploaded photos are stored in `wwwroot/uploads` with a GUID prefix.
- PDF generation uses QuestPDF and SkiaSharp; photos are re-encoded to PNG for reliable embedding.
- The NID card PDF is credit-card sized with a dark-blue header and white body.
- The `EditApplications` table stores the requested fields, review metadata (reviewer, timestamp, message) for both stages, and a JSON blob of `{ OldValue, NewValue }` for each proposed change.
- The monthly restriction is based on the `CreatedAt` timestamp of the most recent application.
- Applications in `Pending` or `Approved` state can be deleted by the user, freeing them to submit a new one (if the monthly window permits).

## Troubleshooting

- **"Element with same key..." error in Visual Studio**: Delete the hidden `.vs` folder and rebuild. Also try running `dotnet run` from command line.
- **Git not recognized**: Install Git for Windows from [git-scm.com](https://git-scm.com/download/win).
- **Database connection failure**: Ensure SQL Server is running and the connection string is correct.
- **Photo not appearing in PDF**: Ensure the uploaded file is a valid JPG/PNG. AVIF and other formats are rejected.
- **Users inserted directly via SQL not showing**: They must also be assigned the `User` role via the `AspNetUserRoles` table.
- **Pagination error `IAsyncQueryProvider`**: Use `PaginatedList<T>.Create(...)` (synchronous) for in-memory lists, not `CreateAsync`.
- **Old edit applications show blank previous value**: Applications created before the `FieldChange` model was introduced have no `OldValue`; the UI shows "-" for them.

## License

This project is for educational purposes. Feel free to use and modify.