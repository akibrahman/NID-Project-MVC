# National ID Card Management System

An ASP.NET Core MVC web application for managing National ID (NID) applications. The system supports three roles: **Admin**, **Moderator**, and **User**. Users can register with their personal details and upload a photo. Moderators review and approve pending registrations, and can block/unblock users. Admins manage moderators (create, block/unblock). The design is clean, light-themed, and built with Bootstrap.

## Features

- **User Registration**  
  - Collects comprehensive NID-related data (name, parents, DOB, gender, address, blood group, occupation, photo upload).
  - Restricts photo uploads to **JPG, JPEG, PNG** formats (both frontend and backend validation).
  - Automatically assigns the "User" role.
  - New registrations are marked as **Pending Approval**.

- **Role-Based Dashboards**  
  - **Admin Dashboard**: Create moderators, view list of moderators, block/unblock moderators.
  - **Moderator Dashboard**: View own profile, list of pending users, approve users, view all approved users, block/unblock users.
  - **User Dashboard**: Display personal information, NID status, approval status, blocked status, and profile photo.

- **Approval Workflow**  
  - Users must be approved by a moderator before they are considered "Active".
  - Pending users appear in the moderator's "Pending Users" list; one click approves them.
  - Upon approval, a **unique 10-digit NID number** is generated and stored for the user.

- **Blocking Mechanism**  
  - Moderators can block/unblock users.
  - Admins can block/unblock moderators.
  - Blocked moderators cannot access dashboard or any management actions (redirected to a "Blocked" page).
  - Blocked status is reflected on the user's dashboard.

- **PDF NID Download**  
  - Approved and non-blocked users can download a **PDF version of their NID card** from their dashboard.
  - The PDF is designed as a credit-card sized card with a professional layout (dark blue header, white background, user's photo and details).

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
- **PDF Generation**: QuestPDF
- **Image Processing**: SkiaSharp (for photo encoding in PDF)

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