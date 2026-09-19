# National ID Card Management System

An ASP.NET Core MVC web application for managing National ID (NID) applications. The system supports three roles: **Admin**, **Moderator**, and **User**. Users can register with their personal details and upload a photo. Moderators review and approve pending registrations, block/unblock users, and handle user data edit applications. Admins manage moderators, monitor system-wide activity through analytics, and run area/position-based votings. The design is clean, light-themed, and built with Bootstrap.

## Features

- **User Registration**  
  - Collects comprehensive NID-related data (name, parents, DOB, gender, address, blood group, occupation, photo upload).
  - Restricts photo uploads to **JPG, JPEG, PNG** formats (both frontend and backend validation).
  - Automatically assigns the "User" role.
  - New registrations are marked as **Pending Approval**.

- **Role-Based Dashboards**  
  - **Admin Dashboard**: Analytics overview with four stat cards (Total Moderators, Total Users, Pending Users, Total Applications), a **pie chart** of applications grouped by status, and a **bar chart** of moderator activity (reviews handled). Separate **Moderators** page for listing, creating, blocking, and unblocking moderators. Also manages **Votings** (create, add nominees, start, close, view results).
  - **Moderator Dashboard**: View own profile, list of pending users, approve users, view all approved users (paginated, 10 per page), block/unblock users, view user details, and manage **Edit Applications**.
  - **User Dashboard**: Display personal information, NID status, approval status, blocked status, and profile photo. Participate in running votings.

- **Approval Workflow**  
  - Users must be approved by a moderator before they are considered "Active".
  - Pending users appear in the moderator's "Pending Users" list; one click approves them.
  - Upon approval, a **unique 10-digit NID number** is generated and stored for the user.

- **Blocking Mechanism**  
  - Moderators can block/unblock users.
  - Admins can block/unblock moderators.
  - Blocked moderators cannot access any moderator functionality (redirected to a "Blocked" page).
  - Blocked users cannot apply for edits, submit edits, or vote; their dashboard reflects the blocked status.

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

- **Voting System** *(admin-managed, user-participated)*  
  - **Admin-side lifecycle**:
    1. **Create a voting** by specifying Title, Area, Position, and optional Description. Newly created voting starts in `Opening` status.
    2. **Add nominees** while the voting is `Opening`. Each nominee requires Name, Sign (a text symbol — the "marka", e.g. *boat*, *ladder*), and optional Team Name and Photo (JPG/PNG only). **Minimum 3, maximum 10** nominees per voting.
    3. **Start the voting** → status becomes `Running`. Nominees are frozen; no additions or edits allowed.
    4. **Close the voting** → status becomes `Closed`. This is a one-way action; a closed voting cannot be reopened or modified.
  - **Voting status states**:
    - `Opening` – admin is adding nominees; users see the voting but cannot vote.
    - `Running` – users can vote; live statistics are visible only to the admin.
    - `Closed` – results are revealed to everyone, including nominees, vote counts, and percentages.
  - **User-side behavior**:
    - Users visit the **Votings** page from the navbar to see all votings.
    - For a `Running` voting they can open the details page and vote for exactly **one nominee**.
    - Their vote is final — no edit, no retraction, no second vote.
    - While the voting is `Running`, users **cannot see** vote counts, percentages, or progress; they only see that they have voted (or can vote).
    - After the admin closes the voting, users see the **final result**: winner(s), each nominee's vote count, and vote percentage.
    - Only **approved and non-blocked** users can vote. Blocked or pending users see a clear message instead of the voting UI.
  - **Admin-side live statistics** (only visible to admin while voting is Running or Closed):
    - Total eligible users (approved + non-blocked).
    - Total votes cast.
    - Remaining users who haven't voted.
    - A progress bar showing turnout percentage.
    - Each nominee's vote count and percentage.
  - **Winner determination**:
    - The nominee with the highest number of votes is the winner.
    - If **multiple nominees tie** with the same highest count, the result is declared a **tie** and all tied nominees are listed. No arbitrary rule (like lowest ID) is used to break ties — the admin decides how to proceed (e.g. re-run, negotiate, accept co-winners).
    - If no votes were cast, no winner is declared.
  - **Access control**:
    - Blocked users cannot vote.
    - Pending (not yet approved) users cannot vote.
    - Only admins can create, modify (while Opening), start, and close votings.
    - Users can only submit a vote when the voting is Running and they haven't voted yet.

- **Admin Analytics Dashboard**  
  - **Stat cards**:
    - Total Moderators (clickable → opens Moderators page)
    - Total Users
    - Pending Users (not yet approved)
    - Total Applications (all edit applications in the system)
  - **Pie chart**: Applications grouped by status (`Pending`, `Approved`, `Rejected`, `Edited`, `Completed`, `ChangeRejected`). Renders only when at least one application exists.
  - **Bar chart**: Moderator activity — number of reviews handled by each moderator (both Stage 1 and Stage 2 combined). Includes every moderator, even those with zero reviews, so performance can be compared at a glance.
  - Charts are rendered with **Chart.js** (loaded via CDN), so no additional NuGet package is required.

- **Authentication & Authorization**  
  - Uses ASP.NET Core Identity with role-based access control.
  - Custom `ApplicationUser` extends IdentityUser with NID fields and an `IsBlocked` flag.
  - Full name is stored as a claim and shown in the navbar.

- **Professional UI**  
  - Light theme, responsive Bootstrap 5 layout.
  - Modern navbar with logo, animated hover underline, and red logout button.
  - Full-height layout with an enhanced multi-column footer.
  - Status badges for approval, blocking, application states, and voting states.
  - Dashboard charts for admin-side analytics.

## Tech Stack

- **Framework**: ASP.NET Core MVC (.NET 10.0)
- **Database**: Microsoft SQL Server (via Entity Framework Core)
- **Authentication**: ASP.NET Core Identity
- **ORM**: Entity Framework Core (Code First)
- **UI**: Bootstrap 5, Razor Views, Bootstrap Icons
- **Charts**: Chart.js (loaded via CDN)
- **File Storage**: Local `wwwroot/uploads` for profile photos and nominee photos
- **PDF Generation**: QuestPDF
- **Image Processing**: SkiaSharp (for re-encoding photos to PNG before PDF embedding)
- **JSON Serialization**: `System.Text.Json` for storing proposed field changes in edit applications

## Prerequisites

- .NET SDK 10.0 (or higher)
- Visual Studio 2022 (or VS Code with C# extension)
- SQL Server (LocalDB or full instance)
- Git (optional, for version control)
- Internet connection (for Chart.js CDN; the app still works without it, but the admin charts won't render)

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

> **Note:** Chart.js is loaded via CDN in the Admin Dashboard view, so no NuGet package is required for the charts.

### 4. Apply Migrations

In Package Manager Console:

```powershell
Add-Migration InitialCreate
Update-Database
```

The database will be created with all Identity tables, custom user fields, the `EditApplications` table, and the voting tables (`Votings`, `Nominees`, `UserBallots`).

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
| **Admin** | View analytics dashboard (stat cards, pie chart, moderator activity bar chart), create moderators, view moderator list, block/unblock moderators, create and manage votings (add nominees, start, close, view live statistics and results) |
| **Moderator** | View pending users, approve users (generates NID number), view all approved users (paginated), block/unblock users, view user details, review edit applications (both stages) |
| **User**   | Register, view dashboard/profile, see approval & blocked status, download NID PDF (if approved and not blocked), apply for data edits once per month, delete pending/approved applications, edit approved fields, track full application history, participate in running votings and view results after close |

> Blocked moderators cannot access any moderator functions (redirected to Blocked page).
> Blocked users cannot download PDF, create applications, edit fields, or vote.

## Project Structure

```
NID_Project/
├── Controllers/
│   ├── AccountController.cs
│   ├── AdminController.cs            (analytics dashboard + moderator management + voting management)
│   ├── ModeratorController.cs
│   ├── UserController.cs
│   └── VotingController.cs           (user-facing voting participation)
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── ApplicationUser.cs
│   ├── RegisterViewModel.cs
│   ├── LoginViewModel.cs
│   ├── CreateModeratorViewModel.cs
│   ├── AdminDashboardViewModel.cs    (stats + chart data)
│   ├── PaginatedList.cs
│   ├── EditApplication.cs
│   ├── CreateApplicationViewModel.cs
│   ├── UserEditFieldsViewModel.cs
│   ├── FieldChange.cs
│   ├── Voting.cs                     (voting entity + VotingStatus enum)
│   ├── Nominee.cs                    (nominee entity)
│   ├── UserBallot.cs                 (single vote per user per voting)
│   ├── CreateVotingViewModel.cs
│   └── AddNomineeViewModel.cs
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   └── AccessDenied.cshtml
│   ├── Admin/
│   │   ├── Dashboard.cshtml          (stat cards + charts)
│   │   ├── Moderators.cshtml         (moderator list with block/unblock)
│   │   ├── CreateModerator.cshtml
│   │   ├── Votings.cshtml            (list of votings)
│   │   ├── CreateVoting.cshtml
│   │   ├── VotingDetails.cshtml      (admin view with live stats + result)
│   │   └── AddNominee.cshtml
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
│   ├── Voting/
│   │   ├── Index.cshtml              (all votings for users)
│   │   └── Details.cshtml            (vote + results after close)
│   └── Shared/
│       └── _Layout.cshtml
├── wwwroot/
│   ├── css/site.css
│   ├── images/                       (logo)
│   └── uploads/                      (uploaded profile photos and nominee photos)
├── Program.cs
├── appsettings.json
└── README.md
```

## Usage Flow

1. **Admin Login**  
   - Use `admin@nid.gov.bd` / `Admin@123`  
   - Dashboard shows:
     - Four stat cards: Total Moderators, Total Users, Pending Users, Total Applications.
     - A **pie chart** breaking down all applications by their current status.
     - A **bar chart** ranking moderators by the number of reviews they've handled.
   - Click "Total Moderators" card or use the navbar "Moderators" link to open the Moderators page.
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

5. **Voting**  
   - **Admin**:  
     1. Open **Admin → Votings → Create Voting**. Fill Title, Area, Position, and optional Description.  
     2. Open the new voting's details page and click **Add Nominee** for each nominee (Name, Team, Sign/marka text, optional photo). At least 3 and at most 10 nominees.  
     3. When ready, click **Start Voting** → status becomes `Running`.  
     4. Watch live progress on the voting details page (turnout, per-nominee vote counts, percentages).  
     5. When finished, click **Close Voting** → status becomes `Closed`. Results are frozen and revealed to everyone.  
   - **User**:  
     1. Open **Votings** from the navbar.  
     2. Open a voting with status `Running`.  
     3. Choose exactly one nominee and click **Vote**. A confirmation dialog appears; the vote is final.  
     4. Once the admin closes the voting, the results page shows the winner (or a tie) along with each nominee's vote count and percentage.

## Additional Notes

- The admin account is seeded automatically on startup; no manual database insertion is required.
- `UserName` is set to the user's email for uniqueness; full name is stored as a claim for navbar display.
- Photo uploads are restricted to `.jpg`, `.jpeg`, `.png` on both frontend (JS) and backend (controller). This applies to user registration photos, edit-application photos, and nominee photos.
- Uploaded photos are stored in `wwwroot/uploads` with a GUID prefix.
- PDF generation uses QuestPDF and SkiaSharp; photos are re-encoded to PNG for reliable embedding.
- The NID card PDF is credit-card sized with a dark-blue header and white body.
- The `EditApplications` table stores the requested fields, review metadata (reviewer, timestamp, message) for both stages, and a JSON blob of `{ OldValue, NewValue }` for each proposed change.
- The monthly restriction is based on the `CreatedAt` timestamp of the most recent application.
- Applications in `Pending` or `Approved` state can be deleted by the user, freeing them to submit a new one (if the monthly window permits).

### Admin Analytics Details

- **Pie chart** (`Applications by Status`) is populated from a `Dictionary<string, int>` in `AdminDashboardViewModel.ApplicationsByStatus`, grouped on `EditApplication.Status`. Colors are assigned via a fixed palette:
  - `Pending` – amber
  - `Approved` – cyan
  - `Rejected` – red
  - `Edited` – blue
  - `Completed` – green
  - `ChangeRejected` – gray
- **Bar chart** (`Moderator Activity`) is populated from `AdminDashboardViewModel.ModeratorActivity`, a dictionary mapping moderator full name → count of reviews handled. The count includes **both** Stage 1 (`ReviewedByModerator`) and Stage 2 (`ChangeReviewedByModerator`) reviews, so a moderator who reviewed an application twice (once per stage) contributes 2 to their total. Moderators with zero reviews appear in the chart with a value of 0.
- Charts are drawn with **Chart.js 4.x** loaded from jsDelivr CDN, and only initialize when their data is non-empty.

### Voting System Details

- **Entities**:
  - `Voting` — Title, Area, Position, Description, Status, CreatedAt, StartedAt, ClosedAt. Has collections `Nominees` and `Ballots`.
  - `Nominee` — Name, TeamName, Sign (text), PhotoPath, FK to `Voting`. Has a `Ballots` collection.
  - `UserBallot` — VotingId, NomineeId, UserId, VotedAt. A **unique index** on `(VotingId, UserId)` guarantees one vote per user per voting.
- **Status enum** `VotingStatus`: `Opening`, `Running`, `Closed`.
- **Transitions**: `Opening → Running → Closed`. No backward transitions. Closing is final.
- **Nominee limits**: enforced in `AdminController.AddNominee` and `StartVoting` — at least 3 and at most 10 nominees are required.
- **Vote submission**: performed by `VotingController.SubmitVote`, which checks the user is approved, not blocked, the voting is `Running`, and the user has not already voted. Attempting any of these invalid conditions returns a friendly error.
- **Live admin stats**: `AdminController.VotingDetails` computes `TotalEligible`, `TotalVoted`, and `Remaining`, and per-nominee vote counts / percentages using the nominee's `Ballots` collection.
- **Result reveal**: user-facing results are only rendered when `Status == Closed`. While `Running`, users see only their own choice (if any) and the nominee cards, without any vote counts or percentages.
- **Tie handling**: the winner is the highest-vote-count nominee. If more than one nominee shares the highest count, the result is displayed as a tie with all top-scoring nominees listed.

## Troubleshooting

- **"Element with same key..." error in Visual Studio**: Delete the hidden `.vs` folder and rebuild. Also try running `dotnet run` from command line.
- **Git not recognized**: Install Git for Windows from [git-scm.com](https://git-scm.com/download/win).
- **Database connection failure**: Ensure SQL Server is running and the connection string is correct.
- **Photo not appearing in PDF**: Ensure the uploaded file is a valid JPG/PNG. AVIF and other formats are rejected.
- **Users inserted directly via SQL not showing**: They must also be assigned the `User` role via the `AspNetUserRoles` table.
- **Pagination error `IAsyncQueryProvider`**: Use `PaginatedList<T>.Create(...)` (synchronous) for in-memory lists, not `CreateAsync`.
- **Old edit applications show blank previous value**: Applications created before the `FieldChange` model was introduced have no `OldValue`; the UI shows "-" for them.
- **Charts not showing on Admin Dashboard**: Verify you have internet access so the Chart.js CDN can load. Open the browser console (F12) to check for CDN load errors. The rest of the dashboard (stat cards) still works without Chart.js.
- **Moderator activity bar chart is empty**: This occurs when no moderator has performed any reviews yet. The chart auto-hides in that case and a "No moderator activity recorded yet." message is displayed.
- **"A local or parameter named 'pct' cannot be declared in this scope"**: In Razor views, do not reuse the same variable name at overlapping scopes. Rename one of them (e.g., `votePercent`).
- **Admin Voting Details doesn't show result after closing**: The result card is only rendered when `Model.Status == VotingStatus.Closed`. Verify the voting was actually closed (Status is `Closed`, not `Running`).
- **Voting start fails**: Ensure at least 3 nominees exist. The "Start Voting" button only appears when the requirement is met, but the backend also enforces it.
- **User cannot vote**: Confirm the user is `IsApproved = true` and `IsBlocked = false`, that the voting is `Running`, and that they haven't already voted. Each of these is checked server-side and produces a specific error.

## License

This project is for educational purposes. Feel free to use and modify.