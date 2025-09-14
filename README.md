
# E-CommerceWebApp  

An **E-Commerce Web Application** built with **ASP.NET Core MVC (.NET 8)** using **N-Tier Architecture**.  
This project demonstrates **Entity Framework Core, Identity, Repository Pattern, and Stripe Payment Integration** with a modular and scalable architecture.  

---

## Project Structure  

```
E-CommerceWebApp
│
├── ECommerce.DataAccess       # Data access layer (EF Core, DB Initialization, Repository Pattern)
│   ├── DBInitializer          # Database seeding and initialization
│   ├── Data                   # ApplicationDbContext and configurations
│   ├── Migrations             # EF Core migrations
│   └── Repository             # Generic and specific repositories
│
├── ECommerce.Models           # Entity & ViewModels
│   ├── ViewModels             # Custom ViewModels for views
│   ├── ApplicationUser.cs     # Extended Identity user
│   ├── Category.cs            # Category entity
│   ├── Company.cs             # Company entity
│   ├── OrderDetail.cs         # Order details entity
│   ├── OrderHeader.cs         # Order header entity
│   ├── Product.cs             # Product entity
│   └── ShoppingCard.cs        # Shopping cart entity
│
├── ECommerce.Utility          # Utility classes and helpers
│   ├── EmailSender.cs         # Email notification service
│   ├── SD.cs                  # Static details (constants, roles, statuses)
│   └── StripSettings.cs       # Stripe configuration
│
└── ECommerce (Main Web App)   # ASP.NET Core MVC project
    ├── Areas                  # Admin & Customer modules
    ├── Properties             # Project properties
    ├── Views                  # Razor views
    └── wwwroot                # Static files (CSS, JS, Images)
```

---

## Features  

- ASP.NET Core MVC (.NET 8) with Clean Architecture  
- Entity Framework Core with Code-First Migrations  
- Identity Integration with custom fields  
- Repository & Unit of Work Pattern  
- Stripe Payment Gateway Integration  
- Role Management (Admin, User)  
- Email Notification Service  
- Database Seeding & Initialization  
- Modular structure for scalability  
- Razor Views with Bootstrap 5 for UI  

---

## Tech Stack  

- **Backend:** ASP.NET Core 8, C#  
- **Frontend:** Bootstrap 5, JavaScript  
- **Database:** SQL Server, EF Core (Code-First)  
- **Authentication:** ASP.NET Core Identity, JWT  
- **Payments:** Stripe  

---

##  Getting Started  

1. Clone the repository:  
   ```bash
   git clone https://github.com/yourusername/E-CommerceWebApp.git
   ```

2. Update `appsettings.json` with your **SQL Server** & **Stripe** credentials.  

3. Apply migrations:  
   ```bash
   dotnet ef database update
   ```

4. Run the application:  
   ```bash
   dotnet run
   ```
