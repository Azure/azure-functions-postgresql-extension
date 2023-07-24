# Azure PostgreSQL bindings for Azure Functions

## Table of Contents
- [Introduction](#introduction)
- [Features](#features)
- [Getting Started](#getting-started)

## Introduction

This repository contains the code for the Azure Functions PostgreSQL Extension. This extension provides a binding for PostgreSQL databases, which allows developers to use PostgreSQL as an input and output binding in their Azure Functions. Devlopers can also find a quick start tutorial and samples illustrating how to use the bindings in different ways. 

## Features
- Input Bindings: Allows you to read data from a PostgreSQL database within your Azure Function. The binding takes an SQL query and inserts the results into object(s).
- Output Bindings: Allows you to write data to a PostgreSQL database from within your Azure Function. The binding inserts object(s) into a PostgreSQL database, by specifying the table, connection string to the database and object(s) to insert.

## Getting Started

### Prerequisites
- Azure Functions Core Tools
- .NET Core 3.1 or above
- PostgreSQL

### Compiling the Code
1. Clone the repository
   ```
   git clone https://github.com/Azure/azure-functions-postgresql-extension.git
   ```
2. Navigate into the cloned repository
   ```
   cd azure-functions-postgresql-extension
   ```
3. Build the solution
   ```
   dotnet build
   ```

### Running the Samples
1. Navigate to the samples directory
   ```
   cd samples
   ```
2. Include your PostgreSQL Connection String
   ```
   Host=host_value;Username=user_value;Password=password_value;Database=database_value;Port=port_value;SSL Mode=ssl_value;
   ```
3. Run the samples
   ```
   dotnet run
   ```

### Running the Tests
1. Navigate to the test directory
   ```
   cd test
   ```
2. Include your PostgreSQL Connection String
   ```
   Host=host_value;Username=user_value;Password=password_value;Database=database_value;Port=port_value;SSL Mode=ssl_value;
   ```
3. Run the tests
   ```
   dotnet test
   ```

## Telemetry

## Privacy Statement

To learn more about our Privacy Statement visit [this link](https://go.microsoft.com/fwlink/?LinkID=824704).

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft’s Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.
