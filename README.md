# RecruitmentTracker

## Project Description

RecruitmentTracker is a web-based recruitment management system developed using ASP.NET Core MVC. The system supports the recruitment process by providing separate functionality for HR users and candidates.

The system also includes a separate AI API developed using Flask and Python. The ASP.NET Core application communicates with the Flask API when AI-related functionality is required.

## Technologies Used

* ASP.NET Core MVC
* C#
* Entity Framework Core
* SQL Server LocalDB
* Python
* Flask
* HTML
* CSS
* JavaScript
* Bootstrap

## Requirements

Before running the project, make sure the following are installed:

* Visual Studio 2022
* .NET SDK required by the project
* SQL Server LocalDB
* Python 3.x
* Required Python packages for the AI API

## How to Run the Project

### 1. Run the ASP.NET Core Application

1. Clone or download this repository.

2. Open the project folder.

3. Open `RecruitmentTracker.sln` in Visual Studio 2022.

4. Allow Visual Studio to restore the required NuGet packages.

5. Make sure SQL Server LocalDB is installed.

6. Build the solution using:

   `Build > Build Solution`

7. Run the application using the **Run** button in Visual Studio.

8. ASP.NET Core will start the local development server (Kestrel).

9. The application will open using the localhost URL provided by Visual Studio.

### 2. Run the Flask AI API

The project contains an AI API implemented using Flask and Python.

1. Open a separate terminal.

2. Navigate to the AI API folder:

   `cd ai`

3. Install the required Python packages:

   `pip install -r requirements.txt`

4. Start the Flask API using:

   `python api.py`

5. The Flask API will start on its configured local host and port.

6. Keep the Flask API terminal running while using the AI-related features of the RecruitmentTracker application.

### Important

Both the ASP.NET Core application and the Flask AI API should be running when using features that require communication between the main application and the AI API.

## Database

The application uses SQL Server LocalDB.

Database name:

`RecruitmentTrackerDB`

The database connection is configured in `appsettings.json`.

Entity Framework Core migrations are included in the project to manage the database structure.

If required, the database can be created or updated using the available Entity Framework Core migrations.

## Application Execution

The main RecruitmentTracker application runs locally through the ASP.NET Core development server (Kestrel).

The AI functionality runs through a separate Flask development server using the `api.py` file.

Therefore, when AI functionality is required, both services should be running:

**ASP.NET Core Application**

Visual Studio → Run → Localhost

**Flask AI API**

Terminal → `python api.py` → Flask local server

## Sprint 1

This repository contains the implementation completed for Sprint 1, including the relevant ASP.NET Core MVC controllers, models, views, database components, user interface, and AI API components.


