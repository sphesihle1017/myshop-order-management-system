# MyShop E-commerce Platform (.NET 8)

A modern, production-ready ASP.NET Core 8 e-commerce platform with complete order management and real-time analytics.

##  Features
###  Customer Features
- Responsive product catalog with categories
- ShopPing cart with real-time updates
- Secure checkout process
- Order tracking with live status

###  Admin Features

- Bulk order processing (100+ orders simultaneously)
- Real-time analytics dashboard
- Advanced reporting (CSV/Excel/PDF exports)
- Multi-admin role-based permissions

###  Technical Features

-.NET 8 - Latest framework with performance improvements
-ASP.NET Core MVC - Modern web architecture
-Entity Framework Core 8 - Advanced database operations
-Bootstrap 5.3 - Responsive, accessible UI
-RESTful API - Clean API endpoints




## Clone repository
git clone https://github.com/sphesihle1017/myshop-dotnet8.git
cd myshop-dotnet8

## Restore dependencies
dotnet restore

## Apply database migrations
dotnet ef database update

# Run the application
dotnet run


# HOME PAGE

<img width="960" height="540" alt="Home" src="https://github.com/user-attachments/assets/485d36ef-9df5-4a73-bf1c-39e4b09531cf" />

# CART

<img width="960" height="540" alt="Cart" src="https://github.com/user-attachments/assets/f687b005-8a4f-40b1-ad7e-b3d8ac6c0e04" />

# TRACK ORDERS

<img width="960" height="540" alt="Track" src="https://github.com/user-attachments/assets/6a348903-951d-471e-a797-6db6037eeecc" />

# ORDER MANAGEMENT (Requres Admin Privileges)

<img width="960" height="540" alt="Order Manager" src="https://github.com/user-attachments/assets/61776ca4-7e90-428c-91c9-78711425fb15" />

# PRODUCT MANAGEMENT (Requres Admin Privileges)

<img width="960" height="540" alt="pRODUCT" src="https://github.com/user-attachments/assets/a242a5fb-555a-4b3f-9572-422e10da8887" />

# USER REGISTRATION (NOTE THAT THE USER CAN ONLY BE REGISTERED BY THE ADMIN)

<img width="960" height="540" alt="Register" src="https://github.com/user-attachments/assets/b857acf4-07b0-4a2a-afb3-86d24a5d5131" />



MyShop/
├── Controllers/          ##  Controllers
│   └── AccountController.cs
│   └── CartController.cs
│   └── CategoryController.cs
│   └── CheckoutController.cs
│   └── HomeController.cs
│   └── OrderManagerController.cs
│   └── ProductController.cs
│   └── TrackController.cs
│   └── UserManagerController.cs 

├── Models/  ##  Models
│   ├── ApplicationUser.cs
│   └── CartItem.cs
│   ├── Category.cs
│   └── Checkout.cs
│   ├── OrderItem.cs
│   └── CartItem.cs
│   ├── Product.cs

├── ViewModels/  ##  Models
│   ├── AccountViewModel.cs
│   └── RegisterViewModel.cs
│   ├── UserDetailViewModel.cs
│   └── UserRoleViewModel.cs

├── Views/               ##  Views
│   └── Account/
│       ├── AccessDenied.cshtml
│       ├── Index.cshtml
│       ├── Login.cshtml
│       └── Register.cshtml
│   └── Cart/
│       ├── Index.cshtml
│   └── Category/
│       ├── Create.cshtml
│       ├── Delete.cshtml
│       ├── Edit.cshtml
│       └── Index.cshtml
│   └── Checkout/
│       ├── Confirmation.cshtml
│       ├── Index.cshtml
│       ├── OrderDetails.cshtml    
│   └── Home/
│       ├── Index.cshtml
│   └── OrderManger/
│       ├── AdjustStock.cshtml
│       ├── Create.cshtml
│       ├── Delete.cshtml
│       ├── Details.cshtml
│       ├── Edit.cshtml
│       ├── Index.cshtml
│       ├── LowStock.cshtml
│   └── Shared/
│       ├── Layout.cshtml
│       ├── _LoginPartial.cshtml
│       ├── _ValidationScriptsPartial.cshtml
│       ├── Errror.cshtml
│   └── Track/
│       ├── Details.cshtml
│       ├── Index.cshtml
│       ├── Results.cshtml
│       ├── Errror.cshtml
│   └── UserManager/
│       ├── Details.cshtml
│       ├── Index.cshtml

├── Data/ 
│       ├── ApplicationDBContext.cshtml
│       ├── SeedData.cshtml
# Migrations
├── Program.cs           # .NET 8 Minimal API setup
└── appsettings.json     # Configuration

## For Collaborations and queries please feel free to write an email @ sphesihlesbani@gmail.com
