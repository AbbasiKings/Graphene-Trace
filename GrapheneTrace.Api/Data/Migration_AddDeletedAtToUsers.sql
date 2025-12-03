-- Migration: Add DeletedAt field to Users table
-- Date: 2024
-- Description: Adds DeletedAt field for soft delete functionality

-- For MySQL/MariaDB
ALTER TABLE `Users` 
ADD COLUMN `DeletedAt` DATETIME NULL AFTER `Address`;

-- For SQL Server (if using SQL Server instead)
-- ALTER TABLE [Users]
-- ADD [DeletedAt] DATETIME2 NULL;

