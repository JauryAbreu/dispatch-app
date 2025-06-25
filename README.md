# Dispatch App

![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet) ![C#](https://img.shields.io/badge/C%23-12.0-green) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-orange) ![License](https://img.shields.io/badge/license-MIT-blue)

**Dispatch App** is a web application built with ASP.NET Core MVC for managing dispatch and delivery orders. It allows authenticated users (with roles such as Admin, Supervisor, and Dispatcher) to manage customers, process dispatch transactions, generate reports, and handle user accounts. The application features an interactive interface for moving items between inventory and dispatch, with support for BIN locations and quantities, and a timer for automated dispatch processing.

## Key Features

- **Authentication and Authorization**:
  - Login with JWT and hierarchical roles (Admin > Supervisor > Dispatcher).
  - Password reset via email.
  - Automatic assignment of the "Dispatcher" role to users without a role.

- **Customer Management**:
  - Create, edit, and delete customers (restricted to Admins).
  - View a list of customers with details such as name, company, and email.

- **Transaction Management**:
  - Interactive interface for dispatching orders, moving items from inventory to dispatch.
  - Support for BIN (storage location) and quantity validation.
  - Change order status (Pending, Partial Delivery, Completed Delivery).
  - Assign dispatchers to orders (restricted to Supervisors and Admins).

- **Reports**:
  - Generate reports of dispatched items by date range (restricted to Supervisors and Admins).
  - View detailed order history.

- **User Management**:
  - List users with their roles (restricted to Admins).
  - Display details such as name, email, and assigned role.

- **User Interface**:
  - Interactive tables with DataTables for orders, supporting search and pagination.
  - Modals for entering BIN, quantity, and action confirmation.
  - Real-time toast notifications for user feedback.
  - Timer in the dispatch view that auto-redirects after 120 seconds of inactivity.

## Project Structure

```
DispatchApp/
├── wwwroot/
│   ├── css/
│   │   ├── dispatch.css                 # 
│   │   ├── error.css                    # 
│   │   ├── options.css                  # 
│   │   ├── styles.min.css               # 
│   ├── js/
│   │   ├── dashboard.js                 #
│   │   ├── dispatch.js                  #
│   │   ├── options.js                   #
├── Controllers/
│   ├── AccountController.cs             # Profile information
│   ├── CustomersController.cs           # Customer management
│   ├── ErrorController.cs               # Error management
│   ├── HomeController.cs                # Main dashboard with statistics
│   ├── ReportController.cs              # Report generation
│   ├── SecurityController.cs            # Authentication and password reset
│   ├── StoreController.cs               # Store List
│   ├── TransactionsController.cs        # Order and dispatch management
│   ├── UsersController.cs               # User management
├── Helper/
│   ├── RoleHelper.cs                    # Role Level
├── Models/
│   ├── Home/
│   │   ├── MainData.cs                  # Dashboard models
│   ├── Transactions/
│   │   ├── Customer.cs                  # Customer model
│   │   ├── Delivery.cs                  #
│   │   ├── DeliveryStatusEnum.cs        #
│   │   ├── DispatchItemsReportModel.cs  #
│   │   ├── Fiscal.cs                    # Fiscal data model
│   │   ├── Header.cs                    # Order header model
│   │   ├── Lines.cs                     # Order lines model
│   │   ├── LinesModel.cs                #
│   │   ├── LinesRequest.cs              #
│   │   ├── Store.cs                     #
│   │   ├── TransactionModel.cs          # Transaction models
│   ├── User/
│   │   ├── ApplicationUser.cs           # User model with Identity
│   │   ├── ApplicationRole.cs           # Role model with Identity
│   │   ├── CreateUserModel.cs           #
│   │   ├── ForgotPasswordModel.cs       #
│   │   ├── JwtTokenResult.cs            #
│   │   ├── LoginModel.cs                #
│   │   ├── ProfileModel.cs              #
│   │   ├── RegisterModel.cs             #
│   │   ├── ResetPasswordModel.cs        #
│   │   ├── SmtpSettings.cs              #
│   │   ├── TwoFactorModel.cs            #
│   │   ├── UpdatePasswordModel.cs       #
│   │   ├── UpdateProfileModel.cs        #
│   ├── ApplicationDbContext.cs          #
│   ├── ErrorViewModel.cs                #
│   ├── MessageEnum.cs                   #
│   ├── Notification.cs                  #
├── Utils/
│   ├── AuthUtil.cs                      # JWT generation and validation
│   ├── EmailUtil.cs                     # Email sending with MailKit
│   ├── MinimumRoleAuthorizeAttribute.cs # Role-based authorization filter
│   ├── ProfileUtil.cs                   # Role verification
│   ├── RoleLevel.cs                     #
│   ├── TransactionUtil.cs               # Transaction logic
├── Views/
│   ├── Account/
│   │   ├── EditPassword.cshtml          # Update password
│   │   ├── EditProfile.cshtml           # Update information profile
│   ├── Customers/
│   │   ├── Create.cshtml                # Customer creation form
│   │   ├── Delete.cshtml                # Customer deletion confirmation
│   │   ├── Edit.cshtml                  # Customer edit form
│   │   ├── Index.cshtml                 # Customer list
│   ├── Error/
│   │   ├── Error.cshtml                 # Generic error
│   │   ├── NotFound.cshtml              # Page not found 404
│   ├── Home/
│   │   ├── Index.cshtml                 # Main dashboard
│   ├── Report/
│   │   ├── DispatchReport.cshtml        # Dispatch report
│   │   ├── DispatchItemsReport.cshtml   # Dispatched items report
│   ├── Security/
│   │   ├── AccessDenied.cshtml          # Access denied page
│   │   ├── ForgotPassword.cshtml        # Password reset request
│   │   ├── Login.cshtml                 # Login form
│   │   ├── ResetPassword.cshtml         # New password form
│   │   ├── Verify2FA.cshtml             #
│   ├── Shared/
│   │   ├── _Layout.cshtml               # 
│   ├── Stores/
│   │   ├── Index.cshtml                 # Store list
│   ├── Transactions/
│   │   ├── Create.cshtml                # Dispatch interface
│   │   ├── Index.cshtml                 # Orden pending to dispatch 
│   │   ├── Orders.cshtml                # Order history
│   │   ├── UserOrders.cshtml            # User's orders
│   │   ├── OrderDetails.cshtml          # Order details
│   │   ├── OrderAssign.cshtml           # Dispatcher assignment
│   ├── Users/
│   │   ├── Create.cshtml                # User creation form
│   │   ├── Delete.cshtml                # User deletion confirmation
│   │   ├── Edit.cshtml                  # User edit form
│   │   ├── Index.cshtml                 # User list
├── appsettings.json                     # Configuration (SMTP, JWT, DB connection)
├── Program.cs                           # Service and pipeline configuration
```

## Requirements

- **.NET SDK**: Version 6.0 or higher
- **Database**: SQL Server (configurable in `appsettings.json`)
- **Dependencies**:
  - ASP.NET Core Identity
  - Entity Framework Core
  - MailKit (for email sending)
  - Bootstrap 5 (for UI)
  - DataTables (for interactive tables)
  - jQuery
- **SMTP Server**: Configured in `appsettings.json` (e.g., Gmail with app-specific password)

## Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/DispatchApp.git
   cd DispatchApp
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Configure `appsettings.json`**:
   - Set up the SQL Server connection string:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=DispatchAppDb;Trusted_Connection=True;"
     }
     ```
   - Configure SMTP credentials:
     ```json
     "SmtpSettings": {
       "Server": "smtp.gmail.com",
       "Port": 587,
       "SenderName": "Dispatch App",
       "SenderEmail": "your-email@gmail.com",
       "Username": "your-email@gmail.com",
       "Password": "your-app-specific-password"
     }
     ```
   - Configure JWT:
     ```json
     "Jwt": {
       "Key": "your-secure-32-character-key",
       "Issuer": "your-issuer",
       "Audience": "your-audience"
     }
     ```

4. **Apply database migrations**:
   ```bash
   dotnet ef database update
   ```

5. **Run the application**:
   ```bash
   dotnet run
   ```
   Access `https://localhost:5001` in your browser.

## Usage

1. **Log In**:
   - Navigate to `/Security/Login`.
   - Use valid credentials or request a password reset at `/Security/ForgotPassword`.
   - Users without a role are automatically assigned the "Dispatcher" role.

2. **Dashboard** (`/Home/Index`):
   - Displays order statistics (last 1, 7, 30 days), dispatch times, and charts.

3. **Customer Management** (`/Customers`):
   - Restricted to Admins.
   - Create, edit, or delete customers from the list.

4. **Order Dispatch** (`/Transactions/Create`):
   - Select items from the inventory (left table) and move them to dispatch (right table).
   - Enter BIN and quantity via modals.
   - Change the status of dispatched items (Delivery/Domicile).
   - Complete the process to save the transaction.

5. **Order History** (`/Transactions/Orders`):
   - Filter orders by date range.
   - Accessible to Supervisors and Admins.

6. **Reports** (`/Report`):
   - Generate reports of dispatched items by date or detailed order history.

7. **User Management** (`/Users`):
   - List users with their roles (Admin, Supervisor, Dispatcher).
   - Restricted to Admins.

8. **Automated Dispatch** (`/Dispatch/Index`):
   - Displays pending orders with DataTables.
   - A 120-second timer auto-redirects to the next order.

## Contributing

1. **Fork the repository** and create a branch for your feature:
   ```bash
   git checkout -b feature/new-functionality
   ```

2. **Make your changes** and ensure:
   - Follow the existing code style (English or Spanish variable names, clear comments).
   - Add unit tests if possible.
   - Update the documentation in `README.md` for new features.

3. **Commit and push**:
   ```bash
   git commit -m "Add new functionality X"
   git push origin feature/new-functionality
   ```

4. **Create a Pull Request** describing your changes.

## Known Issues

- **Users without roles**: Automatically assigned "Dispatcher," but additional role configuration may be needed.
- **Performance**: Large queries in `TransactionsController` could benefit from pagination.
- **SMTP Email**: Requires valid configuration; email sending errors are logged but do not interrupt the flow.

## License

This project is licensed under the [MIT License](LICENSE).

## Contact

For support or inquiries, contact [jauryabreu@gmail.com](mailto:jauryabreu@gmail.com).

---

**Developed with by [Jaury Abreu]**
