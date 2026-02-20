# Haldiram Barcode Scanning System

.NET 6 Web Application for Haldiram barcode scanning and production order tracking.

## Overview

This system tracks production orders and barcode transfers between plants (Kasana Plant and Komal Plant). Barcodes are generated at Kasana Plant and transferred to Komal Plant through a channel with scanners on both sides.

## Features

- Production Order Batch Report
- Barcode scanning sync between local and central databases
- Dashboard statistics
- Shift reports
- Line performance tracking

## Reports

### Production Order Batch Report

Shows batch-wise summary:
- **Order Qty** - Total order quantity from ProductionOrder
- **Printed** - Current quantity (CurQTY) from ProductionOrder
- **Total Transfer** - Scanned count from SorterScans_Sync
- **Pending** - Balance quantity (BalQTY) from ProductionOrder

Click on a batch to view order details in a modal.

## Database

- **ProductionOrder** - Production orders with order quantities
- **BarcodePrint** - Printed barcodes per batch
- **SorterScans_Sync** - Scanner transfer data

## Setup

1. Restore packages: `dotnet restore`
2. Build: `dotnet build`
3. Run: `dotnet run`

## SQL Scripts

Run SQL scripts from `/SQL` folder to create stored procedures.
