-- Migration: Add User Personal Information Fields
-- Date: 2024
-- Description: Adds DateOfBirth, PhoneNumber, and Address fields to Users table

-- For MySQL/MariaDB
ALTER TABLE `Users` 
ADD COLUMN `DateOfBirth` DATETIME NULL AFTER `PasswordResetTokenExpiresAt`,
ADD COLUMN `PhoneNumber` VARCHAR(255) NULL AFTER `DateOfBirth`,
ADD COLUMN `Address` TEXT NULL AFTER `PhoneNumber`;

-- For SQL Server (if using SQL Server instead)
-- ALTER TABLE [Users]
-- ADD [DateOfBirth] DATETIME2 NULL,
--     [PhoneNumber] NVARCHAR(255) NULL,
--     [Address] NVARCHAR(MAX) NULL;

