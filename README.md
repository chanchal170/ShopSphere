# ShopSphere - E-Commerce Web Application (ASP.NET Core 8 MVC)

A complete online shop: customers browse, search, add to cart, apply coupons, checkout and track orders.
Admins manage products, categories, orders and coupons from a dashboard with charts.

## Features

**Customer side**
- Home page with hero, animated stats, categories, featured products, hot deals, new arrivals
- Shop page with **live AJAX search**, category filter, price range, 7 sort options and pagination
- **Smart search**: every word must match (so "headphones wireless" works), plurals are understood ("shoes" finds "Running Shoes"),
  matches product name, category and description, results are ranked by best match, and the navbar search shows
  **live suggestions with photos** (use arrow keys + Enter)
- Product page: ratings, reviews (one per user, can be edited), related products, quantity picker
- **Shopping cart** (session based) with AJAX quantity update, remove, free-shipping progress bar
- **Coupon codes** (percent or flat, minimum order, max discount, expiry, usage limit)
- **Checkout** with validation, saved address from profile, COD / UPI / Card (demo - no real payment)
- **Orders**: history, live status tracker, **cancel** (stock is returned automatically)
- **Wishlist** (heart icon, AJAX)
- Register / Login / Logout, profile, change password, **account lockout** after 5 wrong passwords
- Dark mode, toast notifications, responsive layout

**Admin side** (`/Admin`)
- Dashboard: revenue, orders, customers, 7-day revenue chart, orders-by-status chart, top products, low stock
- Products: add / edit / delete with **image upload** and preview (products used in old orders are hidden instead of deleted)
- Categories, Orders (search, filter, update status), Coupons (create, enable/disable, delete)

**Developer features**
- **REST API** with Swagger docs: open `/swagger` (Development mode)
  - `GET /api/products?q=&categoryId=&page=&pageSize=`  `GET /api/products/{id}`  `GET /api/categories`
- Health check endpoint: `/health`
- ASP.NET Core Identity (roles, hashed passwords), EF Core + SQLite, dependency injection with services,
  view components, areas, transactions, anti-forgery protection on every POST, custom 404 / error pages

## Product images

All 20 sample products come with a picture **already bundled** in `wwwroot/images/products` (no internet needed).
On startup the app links each picture to its product automatically, so just run it and refresh the page.

- These are drawn illustrations (generated for this project), not photographs.
- To use real photos: Admin panel > Products > Edit > upload a picture, **or** copy your own image into
  `wwwroot/images/products` with the same file name (for example `headphones.jpg`) and restart the app.
- If a product has no linked file the app tries to download a photo (needs internet); otherwise it shows a coloured icon.
- Products you add yourself in the admin panel use the picture you upload.

## How to run

1. Install the **.NET 8 SDK** (or newer) from https://dotnet.microsoft.com/download
2. Open a terminal in this folder (the one with `ShopSphere.csproj`) and run:

   ```
   dotnet restore
   dotnet run
   ```
3. Open **http://localhost:5050** in your browser.

Or open the folder in Visual Studio 2022 and press F5.
The database file `shopsphere.db` and all sample data are created automatically on first run.
Delete `shopsphere.db` to reset everything.

## Demo accounts

| Role     | Email                    | Password     |
|----------|--------------------------|--------------|
| Admin    | admin@shopsphere.com     | Admin@123    |
| Customer | customer@shopsphere.com  | Customer@123 |

Coupons to try: `WELCOME10`, `FLAT100`, `SAVE20`.

## Quick demo flow (for presentation)

1. Home page > Shop > search "watch" > open a product > Add to cart
2. Cart > apply `WELCOME10` > Checkout (login as customer) > Place order
3. My orders > open the order > see the tracker
4. Login as admin > Admin panel > Orders > change status to Shipped
5. Admin > Products > Add product with an image > see it in the shop
6. Open `/swagger` and try `GET /api/products`

## Project structure

```
ShopSphere/
├── Program.cs                 app setup (Identity, session, services, Swagger, routes)
├── Models/                    Product, Category, Order, Review, Coupon, ApplicationUser...
├── Data/                      AppDbContext (EF Core) + SeedData
├── Services/                  CartService, OrderService, CouponService, ImageService
├── Controllers/               Home, Shop, Cart, Checkout, Orders, Wishlist, Account + Api/
├── Areas/Admin/               Controllers + Views for the admin panel
├── ViewComponents/            Cart badge, category dropdown in the navbar
├── ViewModels/ Helpers/       data passed to views, formatting helpers
├── Views/                     Razor pages
└── wwwroot/                   css/site.css, js/site.js, uploads/products (uploaded images)
```

## Concepts used (good for viva)

MVC, Areas, Identity + roles, EF Core relationships and LINQ, dependency injection, session state,
database transactions, AJAX with `fetch`, partial views and view components, REST API + Swagger,
anti-forgery (CSRF) protection, file upload validation, server-side validation with data annotations.

## Ideas to extend

Real payment gateway, email notifications, product variants, order invoices (PDF), admin user management.
