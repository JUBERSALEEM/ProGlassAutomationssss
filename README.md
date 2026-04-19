# 🏭 Pro Glass Automation ERP

A professional Windows desktop ERP system for glass industry automation including SGU and DGU pricing calculation modules.

Built using:
- WPF (.NET)
- MVVM Architecture
- SQLite Database
- Live Calculation Engine

---

# 📌 PROJECT OVERVIEW

This system automates glass pricing and production estimation for:

✔ SGU (Single Glass Unit)  
✔ DGU (Double Glass Unit)  
✔ Live calculation system  
✔ Profit-based pricing logic  
✔ History tracking system  
✔ SQLite database storage  

---

# 🟢 SGU MODULE (SINGLE GLASS UNIT)

## FEATURES

- Thickness selection (6mm–19mm)
- Color selection (Clear, HD variants)
- Sheet price input
- Cutting cost input
- Tempering cost input
- Profit percentage selection
- Live calculation engine
- Save history system
- Scrollable history list

---

## SGU CALCULATION LOGIC

Formula:

Base Cost = Sheet Price + Cutting + Tempering  
Final Cost = Base Cost + (Base Cost × Profit %)

Example:

Sheet Price = 100  
Cutting = 10  
Tempering = 5  
Profit = 20%

Base = 115  
Final = 115 + (115 × 0.20) = 138.00

---

## SGU HISTORY FORMAT

Thickness + Color + Result + DateTime

Example:
6mm HD Grey FT Glass + 138.00 AED - 19-April-2026 - 12:43PM

---

## SGU DATABASE

Table: SGURecords

Fields:
- Thickness
- Color
- Result
- CreatedAt

---

# 🟠 DGU MODULE (DOUBLE GLASS UNIT)

## FEATURES

- Glass 1 & Glass 2 configuration
- Thickness selection per glass
- Color selection per glass
- Sheet price input
- ASP spacer system
- Profit factor system
- Profit margin system
- Live calculation engine
- History tracking system
- SQLite storage

---

## DGU CALCULATION LOGIC

STEP 1:
Base = Glass1 + Glass2

STEP 2:
Adjusted = Base / Profit Factor

Profit Factor Table:
15% → 0.85  
20% → 0.80  
25% → 0.75  
30% → 0.70  
35% → 0.65  

STEP 3:
Step3 = Adjusted + ASP Value

ASP Values:
6mm = 45  
8mm = 45  
10mm = 45  
12mm = 45  
14mm = 48  
16mm = 50  
18mm = 52  
20mm = 55  
22mm = 58  
24mm = 60  

STEP 4:
Final = Step3 + (Step3 × Profit Margin)

Profit Margin Options:
15%, 20%, 25%, 30%, 35%

---

## DGU FULL EXAMPLE

Sheet1 = 46  
Sheet2 = 29  
ASP = 12mm (45)  
Profit Factor = 0.85  
Profit Margin = 15%

Step 1: 46 + 29 = 75  
Step 2: 75 / 0.85 = 88.23  
Step 3: 88.23 + 45 = 133.23  
Step 4: 133.23 + (133.23 × 0.15) = 153.21

---

## DGU HISTORY FORMAT

Glass1 + ASP + Glass2 - Result - Date - Time

Example:
6mm HD Grey FT Glass + 12mm ASP + 6mm Clear FT Glass - 153.21 AED - 19-April-2026 - 12:43PM

---

## DGU DATABASE

Table: DGURecords

Fields:
- Thickness1
- Color1
- Thickness2
- Color2
- Spacer
- Result
- CreatedAt

---

# 🏗️ SYSTEM ARCHITECTURE

- MVVM Pattern
- WPF UI
- ObservableCollection for live UI updates
- SQLite local database
- Modular ERP structure

---

# 📊 FEATURES SUMMARY

✔ SGU calculator system  
✔ DGU calculator system  
✔ Live calculation engine  
✔ Profit factor + margin system  
✔ ASP pricing system  
✔ History tracking  
✔ SQLite database  
✔ Modular ERP design  

---

# 🚧 FUTURE MODULE

- Lamination module (planned)
- Dashboard analytics
- PDF export
- Invoice system
- Job tracking system

---

# 🏭 STATUS

SGU → Stable ✔  
DGU → Stable ✔  
Lamination → Future ⏳
