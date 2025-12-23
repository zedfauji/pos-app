# Staff Training & Handover Guide

## 1. End of Day Procedure (New!)
**Goal**: Generate the daily financial report (Z-Report).

1.  Go to **Settings** (Gear Icon).
2.  Scroll down to **Operations (Admin)**.
3.  Click **"Print Z-Report"**.
4.  Wait for the printer to print the slip.
    - *Note: This aggregates all sales for the current operational day.*

## 2. Split Bill & Payment
**Goal**: Accept payment for a table.

1.  Tap a table to open the Order view.
2.  Tap **"Close Session"**.
3.  The **Payment Dialog** will appear.
    - *Note: The Total shown here is now calculated by the Server and includes Tax.*
4.  Enter the amount tendered or select Split options.
5.  Upon success, a receipt will print automatically.

## 3. Printer Configuration (Admin)
**Current Status**: Virtual Printer (Logs to Console).

To connect a **Real Thermal Printer**:
1.  Open `EscPosPrinterService.cs` (or future Settings UI).
2.  Update `_printerIp` to the static IP of your Epson/Star printer.
3.  Ensure the printer and tablet are on the same Wi-Fi.

## 4. Troubleshooting
- **"Report Failed"**: Check internet connection. The report logic lives on the server.
- **"Printer Error"**: Check paper roll and IP address.
