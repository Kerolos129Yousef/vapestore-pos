# VapeShopPos — نظام نقطة بيع لمحل فيب (Offline POS)

A fully **offline, single-machine** Point-of-Sale desktop application for a vape shop,
built to run on **both Windows 7 SP1 and Windows 10** on low-spec hardware.

- **Framework:** C# + WinForms on **.NET Framework 4.8**
- **Database:** **SQLite** (single local file, no server) via `System.Data.SQLite`
- **UI:** Arabic / RTL, large buttons, keyboard-friendly
- **Currency:** Egyptian Pound (ج.م)

---

## How to build & run (Visual Studio)

1. Install **Visual Studio 2019 or 2022** with the **".NET desktop development"** workload.
   Make sure the **.NET Framework 4.8 targeting pack** is installed
   (Visual Studio Installer → Individual components).
2. Open **`VapeShopPos.sln`**.
3. The project uses the NuGet package **`System.Data.SQLite.Core` (1.0.118.0)**.
   Visual Studio restores it automatically on first build. If it doesn't, right-click the
   solution → **Restore NuGet Packages** (or run `nuget restore VapeShopPos.sln`).
4. The solution is configured for the **x86** platform (works on 32- and 64-bit Windows and
   keeps the native SQLite interop simple). Leave the platform set to **x86**.
5. Press **F5** (Debug) or **Ctrl+F5** (Run).

> The build automatically copies the native `SQLite.Interop.dll` into the output folder
> (`bin\Debug` / `bin\Release`) via the SQLite NuGet targets — no manual steps needed.

### Default login accounts (seeded on first run)

| Username  | Password     | Role    |
|-----------|--------------|---------|
| `admin`   | `admin123`   | Manager |
| `cashier` | `cashier123` | Cashier |

> **Change these passwords after first login** (Manager → المستخدمون).

---

## Where is the database file?

On first run the app creates **`vapeshop.db`** **next to the executable**, i.e. in
`VapeShopPos\bin\Debug\vapeshop.db` (or `bin\Release\`). The file name can be changed in
`App.config` (`DatabaseFileName`).

- The schema and sample data are created automatically **only if the file doesn't exist**.
- To start fresh, close the app and delete `vapeshop.db`.
- **Backup:** the main screen has a **"نسخة احتياطية"** button that copies the live `.db`
  file to a timestamped file under a `Backups` folder next to the executable.

---

## Modules

1. **الأصناف والمخزن (Products & Inventory)** — add/edit/delete products, categories,
   purchase & sale prices, opening stock, min-stock alert level.
2. **إضافة مخزون (Restock / Damage)** — record incoming stock (purchase) or damaged items.
3. **شاشة البيع (POS)** — scan barcode or search by name, adjust quantities, apply a
   discount, choose cash/card, and finalize. `F2` = checkout. Each sale decrements stock.
4. **الورديات (Shifts)** — open a shift with opening cash; on close, the app computes
   expected cash (opening + cash sales) and shows the variance (عجز/زيادة) vs. counted cash.
5. **الجرد (Stock-take)** — list theoretical quantities, enter counted quantities, review
   variance, and confirm to reconcile stock with `stocktake_adjustment` movements.
6. **التقارير (Reports)** — daily sales summary, best-sellers, profit, low-stock list.

### Roles

- **Manager (مدير):** full access — products, prices, reports, stock-take, users.
- **Cashier (كاشير):** POS + their own shift only. No purchase prices, profit reports,
  or user management.

---

## Inventory design (important)

`products.current_quantity` is **not** the source of truth. **Every** stock change is
written as a row in **`stock_movements`** (`sale`, `purchase`, `damage`,
`stocktake_adjustment`), and `current_quantity` is updated **in the same transaction**.
The theoretical on-hand is therefore always derivable as
`SUM(change_qty)` per product — which is what makes real stock-taking and
loss/theft detection possible. All stock changes go through a single method,
`ProductService.RecordMovement`, so the two can never drift apart.

---

## Barcode scanner

A standard HID barcode scanner acts like a keyboard: it "types" the code into the focused
field and sends Enter. On the POS screen, keep the scan box focused (it auto-focuses) and
scanning will add items automatically. No driver integration is required.

---

## Project structure

```
VapeShopPos.sln
VapeShopPos/
├─ Program.cs                 App entry point + login loop
├─ App.config                 DB file name setting
├─ packages.config            NuGet: System.Data.SQLite.Core
├─ Data/
│  └─ Database.cs             Connection, schema creation, seed data
├─ Models/                    Plain data classes (Product, Invoice, Shift, ...)
├─ Services/                  Business logic (no UI)
│  ├─ Database access & domain rules: ProductService, SalesService,
│  │  ShiftService, StocktakeService, ReportService, UserService
│  ├─ PasswordHasher.cs       PBKDF2 password hashing (no plaintext)
│  ├─ BackupService.cs        Timestamped .db backup
│  └─ Session.cs              Current user + open shift
└─ Forms/                     WinForms screens (UI built in code, RTL)
   ├─ UiTheme.cs              Shared fonts/colors/EGP formatting
   ├─ LoginForm, MainForm
   ├─ ProductsForm, ProductEditForm, RestockForm
   ├─ SalesForm (POS), ShiftForm, StocktakeForm
   └─ ReportsForm, UsersForm, UserEditForm
```

> **Note on the designer:** the forms are built entirely in C# code (no `.Designer.cs`
> files), which keeps them reliable on both Win7 and Win10. They won't open in the visual
> WinForms designer, but they compile and run normally.

---

## Database schema (created automatically)

- **categories**(id, name)
- **products**(id, name, barcode *unique/nullable*, category_id, purchase_price,
  sale_price, current_quantity, min_stock_level)
- **users**(id, username, password_hash, role)
- **shifts**(id, user_id, opened_at, closed_at, opening_cash, closing_cash, total_sales)
- **invoices**(id, datetime, subtotal, discount, total, payment_method, shift_id, user_id)
- **invoice_items**(id, invoice_id, product_id, quantity, unit_price)
- **stock_movements**(id, product_id, datetime, change_qty, movement_type, reference, user_id)

---

## Error handling

- Unknown barcode on the POS screen → a clear "not found" warning.
- Selling more than available stock → warns; **manager** can override, **cashier** is blocked.
- Duplicate barcode / username → caught and reported.
- DB initialization failure → message box instead of a silent crash.
