# AcxiomCRM — ASP.NET Core MVC CRM Project

This source project follows the supplied AcxiomCRM assignment brief. It uses ASP.NET Core 8 MVC, ASP.NET Core Identity, EF Core with SQLite, Bootstrap, and Chart.js.

## Included modules
- Identity registration/login/logout, password policy, lockout, cookie authentication, seeded roles and initial admin.
- Role-based dashboards and scoped customer/lead/opportunity/follow-up views.
- Customer, lead, opportunity and follow-up management with server-side business validation.
- Activity tracking, audit log, user activation and role administration.
- Dashboard KPI cards and Chart.js lead/opportunity charts.
- REST endpoints for customers, leads, and opportunities under `/api/...`.
- Search/filter screens and summary reports.

## Requirements
- .NET 8 SDK
- Internet access on first run if Bootstrap/Chart.js/jQuery are loaded from CDN

## Run locally
1. Install the .NET 8 SDK.
2. Extract this folder and open a terminal in the `AcxiomCRM` directory.
3. Review `appsettings.json`. Change `SeedAdmin:Password` before using this outside a local demo.
4. Run:
   ```bash
   dotnet restore
   dotnet run
   ```
5. Open the HTTPS/HTTP URL printed by the application.

The app creates the SQLite database (`acxiomcrm.db`) on first run and seeds the three roles. Initial local demo admin:
- Email: `admin@acxiomcrm.local`
- Password: `ChangeMe123!`

**Important:** change this password and the seed credentials before any shared/demo deployment. For a production deployment, use environment variables or a secret manager, HTTPS, migrations rather than `EnsureCreated`, a proper email-confirmation/password-reset workflow, and a database server appropriate for the deployment.

## Main navigation
Dashboard, Customers, Leads, Opportunities, Follow-ups, Activities, Reports; Admin-only navigation includes Users and Audit Log.

## API quick checks
Sign in first using the app's authentication cookie, then call:
- `GET /api/customers`
- `GET /api/customers/{id}`
- `POST /api/customers`
- `PUT /api/customers/{id}`
- `DELETE /api/customers/{id}` (deactivates)
- `GET /api/leads`, `POST /api/leads`
- `GET /api/opportunities`, `POST /api/opportunities`

Example customer POST JSON:
```json
{
  "customerName": "Asha Sharma",
  "email": "asha@example.com",
  "phone": "9876543210",
  "companyName": "Example Pvt Ltd",
  "address": "Bhubaneswar",
  "city": "Bhubaneswar",
  "state": "Odisha"
}
```

## Notes and known limitations
- This is a generated project baseline and has **not been compiled or runtime-tested in this environment**, because the .NET SDK is unavailable here. Run `dotnet restore` and `dotnet build` locally and resolve any SDK/compiler feedback before submission.
- The default database is SQLite for easy local setup. The assignment allows a database-backed implementation; switch the provider if your evaluator specifically requires SQL Server.
- The audit log captures key operations, but should be hardened for production with stricter append-only database permissions and centralized logging.
- Role assignment is implemented for Admin, Manager, and SalesExecutive. Manager visibility is team-wide in this baseline; if the assignment requires formal team membership, add a team/manager relationship to the user model.
- Reports are summary reports; CSV export, pagination on every list, advanced date-range filters, full password reset/email verification, and a dedicated API token/JWT flow are appropriate extensions if explicitly required by the evaluator.
