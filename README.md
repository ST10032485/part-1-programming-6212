# RaceDay Database

This repository contains the SQL script to create and populate the **RaceDay** database. The database is designed to manage running events, organisers, categories, participants, enrolments, and results.

## Overview

The RaceDay database supports:

- **Organisers** – individuals or organisations hosting events.
- **Categories** – event types with a defined distance (e.g., 5KM, 10KM, Half Marathon).
- **Events** – specific races scheduled on a date and location.
- **Participants** – runners who register for events.
- **Enrolments** – registrations linking participants to events, including payment and status tracking.
- **Results** – official finish times, positions, and status per enrolment.

The schema enforces data integrity via primary/foreign keys, check constraints, and unique constraints.

## Database Schema

### Table Structure

| Table         | Description |
|---------------|-------------|
| `Organiser`   | Stores organiser details (name, email, phone, organisation). |
| `Category`    | Defines race categories with distance (km) and description. |
| `Event`       | Holds event information, linked to an organiser and a category. |
| `Participant` | Participant profile with personal details and credentials. |
| `Enrolment`   | Registration record linking a participant to an event, with status and payment flags. |
| `Result`      | Results per enrolment (finish time, position, status). |

### Relationships

- Each **Event** belongs to one **Organiser** and one **Category**.
- Each **Enrolment** is for one **Participant** and one **Event** (unique pair).
- Each **Result** corresponds to exactly one **Enrolment** (one-to-one).

### Key Constraints

- `Event.Status`: `'Upcoming'`, `'Open'`, `'Closed'`, `'Completed'`, `'Cancelled'`
- `Enrolment.Status`: `'Active'`, `'Cancelled'`, `'Completed'`
- `Enrolment.PaymentStatus`: `'Pending'`, `'Paid'`, `'Refunded'`
- `Result.ResultStatus`: `'Pending'`, `'Finished'`, `'DNF'`, `'DNS'`

## Setup Instructions

### Prerequisites

- Microsoft SQL Server (2012 or later) or Azure SQL Database.
- SQL Server Management Studio (SSMS) or Azure Data Studio.
- Permissions to create tables and insert data.

### Steps

1. **Create a database** (if not already exists).  
   You can run:
   ```sql
   CREATE DATABASE RaceDay;
   GO
   USE RaceDay;
