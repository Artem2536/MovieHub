# MovieHub

Aplikacja webowa stworzona w **ASP.NET Core MVC** do przeglądania informacji o filmach.  
Projekt korzysta z **TMDb API**, lokalnej bazy **SQLite** oraz **Entity Framework Core**.

---

## Technologie
- ASP.NET Core MVC  
- Entity Framework Core  
- SQLite  
- TMDb API  
- C# / .NET  

---

## Struktura projektu
- **Controllers** – logika obsługi żądań  
- **Models** – modele danych  
- **Views** – widoki Razor  
- **Services** – komunikacja z TMDb API  
- **Data / Migrations** – baza danych i migracje  

---

## Uruchamianie

1. Zainstaluj zależności w Visual Studio  
2. Utwórz lub zaktualizuj bazę danych:

```bash
dotnet ef database update
```
3. Uruchom aplikację:

```bash
dotnet run
```

## Cel projektu
MovieHub to projekt edukacyjny stworzony w celu nauki ASP.NET Core, pracy z API oraz bazami danych.
