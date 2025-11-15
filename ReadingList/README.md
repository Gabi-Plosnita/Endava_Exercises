# 📚 ReadingList Unit Tests - README & Usage Guide

## 🧩 Overview

This project contains comprehensive unit tests for the **ReadingList** application, covering:
- Core domain model validation (`Book`)
- Domain extension methods (`BookExtensions`)
- Infrastructure services (`BookImportService`, `InMemoryRepository`)
- Parsing logic (`CsvBookParser`)

Each test ensures correctness, robustness, and maintainability of the domain and infrastructure layers.  
All tests use **MSTest** as the framework and **AwesomeAssertions** for fluent assertion syntax.

---

## ⚙️ Running the Tests

You don’t need to install anything manually. All dependencies are already included in the project.

From the project root, simply run:
```bash
dotnet test
