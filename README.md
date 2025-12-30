# 🏥 Four Rock Hospital - Smart Healthcare Management & Appointment Booking System

## 📖 Project Overview

Four Rock Hospital is a comprehensive digital transformation platform for healthcare facilities, built on ASP.NET Core MVC. The project aims to address overcrowding issues in medical facilities by bringing appointment scheduling, payment processing, and consultation services online.

The system goes beyond simple appointment booking by integrating modern technologies such as **Voice Search** and **AI Chatbot** to enhance user experience, especially for elderly patients or those who are not tech-savvy.

## 🚀 Key Features

### 1. 🔐 Role-Based Access Control & User Management

The system uses Session-based authentication with strict role separation:

- **Guest**: View news, browse doctor information, view services
- **Patient**: Book appointments, view medical records, top up wallet, chat with doctors
- **Staff (CSKH)**: Access management dashboard, provide online chat support to patients

### 2. 💰 Digital Wallet & Payment System

Integrated internal wallet system for quick patient payments:

- **Real-time Balance**: Balance is checked directly from the Database (`TaiKhoanBenhNhan`) through Dependency Injection at the View level
- **Smart Display**: Balance displayed neatly in account menu with convenient "Top Up Now" button

### 3. 🎙️ Voice Search

Leverages Web Speech API for hands-free searching:

- Accurate Vietnamese voice recognition
- "Listening" status with pulse animation effects
- Auto-fills keywords and submits search form

### 4. 🤖 AI Chatbot & Live Chat Support

Omnichannel communication system:

**Live Chat (Staff Support):**
- Direct connection with support staff
- Real-time chat interface with message history storage

**AI Chatbot:**
- 24/7 automated responses
- Markdown formatting support (bold, italic, links)
- Quick reply suggestions as buttons (e.g., `[button:Book Appointment|link]`)
- Natural "Typing..." effects

### 5. 🔔 Notification System

Uses Polling mechanism (Long Polling) with `setInterval` every 30 seconds to check for new notifications:

- Red badge displaying unread notification count on the menu bar
- Preview of 5 most recent notifications directly in dropdown

### 6. 📱 Responsive Interface & UX/UI

- **Sticky Navbar**: Menu always visible when scrolling, using glass morphism effect (`backdrop-filter: blur`)
- **Dynamic Active Menu**: Smart `IsActive()` helper function automatically highlights parent menu when accessing child menu (e.g., accessing "Doctors" keeps "About" menu highlighted)
- **Mobile-First**: Fully optimized for mobile display

## 🛠️ Tech Stack

### Backend

- **Framework**: ASP.NET Core MVC (.NET 6/7/8)
- **ORM**: Entity Framework Core (Code First or Database First)
- **Database**: SQL Server
- **Security**: Anti-Forgery Token (CSRF Protection), Session Management

### Frontend

- **View Engine**: Razor Views (.cshtml)
- **CSS Framework**: Bootstrap 5.3.3 (Customized CSS Variables)
- **Libraries**:
  - jQuery 3.7.1: AJAX and DOM manipulation
  - FontAwesome 6.6.0: Icon system
  - Marked.js: Markdown rendering for chat interface
  - Google Fonts (Inter): Modern, readable font

## 📂 Project Structure

```plaintext
FourRockHospital/
├── Controllers/           # Business logic handlers
│   ├── HomeController.cs
│   ├── UserController.cs
│   ├── PaymentController.cs
│   └── CSKHController.cs
├── Models/                # Data Models & EF Context
│   ├── BookingContext.cs
│   └── ViewModels/
├── Views/                 # User interface
│   ├── Shared/
│   │   └── _Layout.cshtml # Master page
│   ├── Home/
│   └── User/
├── wwwroot/               # Static files
│   ├── css/
│   ├── js/
│   └── images/
├── Program.cs             # Middleware & DI container configuration
└── appsettings.json       # Database connection strings
```

## 🔌 API Endpoints (Internal)

The project uses AJAX calls to internal endpoints for background tasks:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/User/Notifications/Count` | Count unread notifications for badge display |
| GET | `/User/Notifications/List` | Get notification list (JSON) |
| POST | `/User/Notifications/MarkRead` | Mark notification as read |
| POST | `/api/patient/sendMessage` | Send message from patient to staff |
| POST | `/api/patient/getMessages` | Get chat history between two users |
| POST | `/api/chatbot` | Send question to AI and receive response |

## ⚙️ Installation Guide

To run the project locally, follow these steps:

### 1. System Requirements

- Visual Studio 2022 or later
- .NET SDK (version compatible with project)
- SQL Server Management Studio (SSMS)

### 2. Installation Steps

#### Step 1: Clone Repository

```bash
git clone https://github.com/your-username/FourRockHospital.git
cd FourRockHospital
```

#### Step 2: Configure Database

Open `appsettings.json` and edit the ConnectionStrings:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=FourRockDB;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

#### Step 3: Migration & Update Database

Open Package Manager Console in Visual Studio and run:

```powershell
Add-Migration InitialCreate
Update-Database
```

#### Step 4: Install Frontend Libraries (if needed)

Libraries (Bootstrap, jQuery) are currently using CDN. If you want to run offline, download them to the `wwwroot/lib` folder.

#### Step 5: Run Application

Press `F5` or `Ctrl + F5` to launch the application in your browser (default: `https://localhost:port`)

## 🎯 Usage

1. **For Patients**: Register an account, browse doctors, book appointments, and manage your medical records
2. **For Staff**: Log in to access the management dashboard and provide customer support
3. **For Administrators**: Manage users, appointments, and system configurations

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- ASP.NET Core community
- Bootstrap team
- All contributors who helped with this project

---

Made with ❤️ by Four Rock Team
