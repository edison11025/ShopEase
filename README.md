----------------------------------------------
ShopEase Ap

----------------------------------------------
This is how to Simulate ShopEase App.
ShopEase App is an e-commerce App. It's main function is managing CartItem with authentication.
It performs like a professional e-commerce. Further development needs to complete this project
What you can do inside this App :
Login, Register, View Product Detail for all User including anonymous user.
AddtoCart, View CartItem, Adjust quantity in CartItem, remove a product in CartItem, Logout for User : Customer and AdminUser(Authenticated User)
AddNewProduct for only AdminUser.

I Implement RBAC and jwt for this project. to enhance Authentiction, authorization and also security
## ⚙️ Database Setup

1. **Install MySQL Server** (8.x recommended).  
   - Windows: download from [MySQL Community Downloads](https://dev.mysql.com/downloads/mysql/)  
   - macOS/Linux: install via package manager (`brew install mysql`, `apt install mysql-server`, etc.)

2. **Create the database**
   ```sql
   CREATE DATABASE ShopEaseDb;
   ```

3. **Update connection string**  
   In `ShopEase.Server/appsettings.json`, set your MySQL username and password:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "server=localhost;port=3306;database=ShopEaseDb;user=<your-username>;password=<your-password>"
   }
   ```

---

## 🚀 Project Setup

```bash
dotnet restore
dotnet ef database update
dotnet build ShopEase.slnx
dotnet run --project ShopEase.Server
```

---

## 🌐 Access the App

- Open browser at: [https://localhost:5001](https://localhost:5001)  
- Log in with Admin credentials (already seeded):  
  - **Email:** `admin@shopease.com`  
  - **Password:** `1234567890`

---

## 🛒 Demo Products

The database is seeded with two products so you can test immediately:  
- **Sample Pencil** (Stationery) → `images/pencil.png`  
- **Coffee Mug** (Kitchenware) → `images/mug.png`

---