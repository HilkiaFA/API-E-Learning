# 🎓 E-Learning Mini API

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)](https://www.microsoft.com/en-us/sql-server)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Status](https://img.shields.io/badge/status-Active-success)]()
[![Build](https://img.shields.io/badge/build-Passing-brightgreen)]()

---

## 📘 Deskripsi

**E-Learning Mini API** adalah sistem backend berbasis **ASP.NET Core Web API (.NET 6)** dan **SQL Server**  
yang berfungsi sebagai sistem pembelajaran daring sederhana.

API ini menyediakan fitur untuk **manajemen kursus, modul, kuis, soal, jawaban, dan skor siswa**,  
dengan dukungan **autentikasi JWT** untuk performa tinggi.  
Tersedia juga fitur **ekspor laporan ke PDF dan Excel** menggunakan **Rotativa** dan **ClosedXML**.

---\

## ⚙️ Teknologi yang Digunakan

| Layer          | Teknologi                     |
| -------------- | ----------------------------- |
| Framework      | ASP.NET Core Web API (.NET 6) |
| Database       | Microsoft SQL Server          |
| Authentication | JWT (JSON Web Token)          |

---

## 🗂️ Struktur Database

| Table       | Deskripsi                                  |
| ----------- | ------------------------------------------ |
| `Users`     | Data pengguna (Admin, Instructor, Student) |
| `Courses`   | Kursus utama dengan relasi ke instruktur   |
| `Modules`   | Modul pembelajaran per kursus              |
| `Quizzes`   | Daftar kuis dalam modul                    |
| `Questions` | Pertanyaan dalam kuis                      |
| `Answers`   | Jawaban dari pertanyaan                    |
| `Scores`    | Hasil skor siswa                           |

---

## 🚀 Endpoint API

### 🔐 **Autentikasi**

| Endpoint             | Method | Deskripsi                        |
| -------------------- | ------ | -------------------------------- |
| `/api/auth/register` | POST   | Registrasi pengguna baru         |
| `/api/auth/login`    | POST   | Login dan menghasilkan JWT token |

#### 📤 Contoh Request (Register)

```json
{
  "fullName": "Siti Rahma",
  "email": "siti@elearn.com",
  "password": "123456",
  "role": "Student"
}

📥 Contoh Response
{
  "message": "User registered successfully",
  "userId": 3
}
👨‍🏫 Kursus & Modul
| Endpoint            | Method | Deskripsi           |
| ------------------- | ------ | ------------------- |
| `/api/courses`      | GET    | Ambil semua kursus  |
| `/api/courses/{id}` | GET    | Ambil detail kursus |
| `/api/courses`      | POST   | Tambah kursus baru  |
| `/api/modules`      | GET    | Ambil semua modul   |
| `/api/modules`      | POST   | Tambah modul baru   |


📤 Contoh Request (Tambah Kursus)
{
  "title": "Dasar Pemrograman C#",
  "description": "Belajar logika dasar C#",
  "instructorId": 2
}
📥 Contoh Response
{
  "courseId": 1,
  "title": "Dasar Pemrograman C#",
  "description": "Belajar logika dasar C#",
  "createdAt": "2025-11-08T10:00:00"
}
🧠 Kuis, Pertanyaan & Jawaban
| Endpoint                   | Method | Deskripsi                            |
| -------------------------- | ------ | ------------------------------------ |
| `/api/quizzes`             | GET    | Ambil daftar kuis                    |
| `/api/quizzes/{id}/detail` | GET    | Ambil pertanyaan & jawaban dari kuis |
| `/api/questions`           | POST   | Tambah pertanyaan baru               |
| `/api/answers`             | POST   | Tambah jawaban baru                  |


📥 Contoh Response /api/quizzes/1/detail
{
  "quizId": 1,
  "title": "Kuis Pengenalan",
  "questions": [
    {
      "questionId": 1,
      "text": "Apa ekstensi file C#?",
      "answers": [
        { "answerId": 1, "text": ".cs", "isCorrect": true },
        { "answerId": 2, "text": ".cpp", "isCorrect": false }
      ]
    }
  ]
}
🧾 Penilaian & Laporan
| Endpoint              | Method | Deskripsi                    |
| --------------------- | ------ | ---------------------------- |
| `/api/scores`         | POST   | Simpan skor siswa            |
| `/api/scores/average` | GET    | Hitung rata-rata nilai siswa |
| `/api/reports/pdf`    | GET    | Ekspor laporan skor ke PDF   |
| `/api/reports/excel`  | GET    | Ekspor laporan skor ke Excel |


📥 Contoh Response /api/scores/average

[
  { "userId": 3, "fullName": "Siti Rahma", "averageScore": 85.00 }
]
💾 Contoh Data Dummy (SQL Server)
INSERT INTO Users (FullName, Email, PasswordHash, Role)
VALUES
('Admin Sistem', 'admin@elearn.com', 'hash1', 'Admin'),
('Budi Santoso', 'budi@elearn.com', 'hash2', 'Instructor'),
('Siti Rahma', 'siti@elearn.com', 'hash3', 'Student');

INSERT INTO Courses (Title, Description, InstructorId)
VALUES
('Dasar Pemrograman C#', 'Belajar logika dasar dan sintaks C#', 2),
('SQL Server Lanjutan', 'Optimasi query dan stored procedure', 2);

INSERT INTO Modules (CourseId, Title, Content, OrderIndex)
VALUES
(1, 'Pengenalan C#', 'Materi tentang sintaks dasar', 1),
(1, 'Struktur Kontrol', 'Materi tentang if dan loop', 2);

INSERT INTO Quizzes (ModuleId, Title, DurationMinutes)
VALUES
(1, 'Kuis Pengenalan', 10),
(2, 'Kuis Struktur Kontrol', 15);
🔐 Autentikasi (JWT)
Semua endpoint yang memerlukan autentikasi harus menggunakan header:

Authorization: Bearer <your-jwt-token>
Contoh response saat login:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5..."
}
📄 Ekspor Laporan
Format	Endpoint	Library
PDF	/api/reports/pdf	Rotativa
Excel	/api/reports/excel	ClosedXML

🧰 Cara Menjalankan Proyek
1️⃣ Clone Repository

git clone https://github.com/yourusername/elearning-mini-api.git
cd elearning-mini-api
2️⃣ Buat Database di SQL Server

CREATE DATABASE ElearningMiniDB;
3️⃣ Atur Koneksi di appsettings.json

"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ElearningMiniDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
4️⃣ Restore & Jalankan API

dotnet restore
dotnet run
5️⃣ Akses Swagger UI

https://localhost:5001/swagger
🧩 Lisensi
MIT License © 2025
Developed with ❤️ by Hilkia Farrel
```
