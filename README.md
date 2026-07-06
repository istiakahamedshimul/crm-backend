# CRM Backend — ASP.NET Core Web API

Real Estate CRM backend API built with ASP.NET Core and MySQL.

## Tech Stack
- ASP.NET Core 8
- Entity Framework Core
- MySQL (via XAMPP)
- JWT Authentication

## Setup

1. Start Apache & MySQL from XAMPP
2. Create a MySQL database named `real_estate_crm`
3. Update connection string in `appsettings.json` if needed
4. Run:

```powershell
cd backend
dotnet run
```

API runs at `http://localhost:5000`
Swagger docs at `http://localhost:5000/swagger`

## Default Seed Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@crm.local | Admin@12345 |
| Sales Executive | sales@crm.local | Sales@12345 |

## Main Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/auth/login | Login |
| GET | /api/me | Current user profile |
| GET | /api/dashboard | Dashboard summary |
| GET/POST | /api/leads | Leads |
| GET/POST/PUT | /api/leads | Lead management |
| GET/POST | /api/customers | Customers |
| POST | /api/customers/from-lead/{leadId} | Create customer from lead |
| GET/POST | /api/projects | Projects |
| GET/POST | /api/units | Units |
| GET/POST | /api/invoices | Invoices |
| GET/POST | /api/payments | Payments |
| POST | /api/payments/{id}/approve | Approve payment |
| POST | /api/payments/{id}/reject | Reject payment |
| GET | /api/commissions | All commissions |
| GET | /api/commissions/me | My commissions |

## Branch Strategy

```
main    ← production
dev     ← integration/testing
istiak  ← lead dev working branch
```
