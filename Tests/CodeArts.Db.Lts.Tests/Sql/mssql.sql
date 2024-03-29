/*
 Navicat Premium Data Transfer

 Source Server         : 测试库
 Source Server Type    : SQL Server
 Source Server Version : 10501600
 Source Host           : 120.78.143.144:1433
 Source Catalog        : yep_sky_orm
 Source Schema         : dbo

 Target Server Type    : SQL Server
 Target Server Version : 10501600
 File Encoding         : 65001

 Date: 16/10/2019 09:36:23
*/

USE master;

IF NOT EXISTS(SELECT * FROM sysdatabases WHERE name = 'yep_sky_orm')
BEGIN
	CREATE DATABASE yep_sky_orm;
END

USE yep_sky_orm;

-- ----------------------------
-- Table structure for fei_users
-- ----------------------------
IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[fei_users]') AND type IN ('U'))
BEGIN
	CREATE TABLE [dbo].[fei_users] (
	  [uid] int  IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
	  [bcid] int DEFAULT ((0)) NOT NULL,
	  [username] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [email] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [mobile] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [password] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [mallagid] smallint DEFAULT ((0)) NOT NULL,
	  [salt] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [userstatus] int DEFAULT ((0)) NOT NULL,
	  [created_time] datetime DEFAULT (getdate()) NOT NULL,
	  [modified_time] datetime DEFAULT (getdate()) NOT NULL,
	  [actionlist] text COLLATE Chinese_PRC_CI_AS  NULL
	)
END

-- ----------------------------
-- Table structure for fei_userdetails
-- ----------------------------
IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[fei_userdetails]') AND type IN ('U'))
BEGIN
	CREATE TABLE [dbo].[fei_userdetails] (
	  [uid] int DEFAULT ((0)) NOT NULL,
	  [lastvisittime] datetime DEFAULT (getdate()) NOT NULL,
	  [lastvisitip] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [lastvisitrgid] smallint DEFAULT ((-1)) NOT NULL,
	  [registertime] datetime DEFAULT (getdate()) NOT NULL,
	  [registerip] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [registerrgid] smallint DEFAULT ((-1)) NOT NULL,
	  [gender] tinyint DEFAULT ((0)) NOT NULL,
	  [bday] datetime DEFAULT ('1900-1-1') NOT NULL,
	  [idcard] nvarchar(20) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [regionid] smallint DEFAULT ((0)) NOT NULL,
	  [address] nvarchar(150) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [bio] nvarchar(300) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [avatar] nvarchar(200) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [realname] nvarchar(20) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [nickname] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL
	)
END


-- ----------------------------
-- Table structure for fei_user_wx_account_info
-- ----------------------------
IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[fei_user_wx_account_info]') AND type IN ('U'))
BEGIN
	CREATE TABLE [dbo].[fei_user_wx_account_info] (
	  [id] int  IDENTITY(1,1) NOT FOR REPLICATION NOT NULL,
	  [uid] int  NOT NULL,
	  [appid] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [openid] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [unionid] nvarchar(50) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [code] nvarchar(10) COLLATE Chinese_PRC_CI_AS DEFAULT '' NOT NULL,
	  [status] int DEFAULT ((0)) NOT NULL
	)
END

IF NOT EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[oss_buckets]') AND type IN ('U'))
BEGIN
	CREATE TABLE [dbo].[oss_buckets] (
	  [id] bigint  NOT NULL,
	  [app_key] varchar(50) COLLATE Chinese_PRC_CI_AS  NOT NULL,
	  [app_secret] varchar(60) COLLATE Chinese_PRC_CI_AS  NOT NULL,
	  [name] varchar(65) COLLATE Chinese_PRC_CI_AS  NOT NULL,
	  [region] varchar(50) COLLATE Chinese_PRC_CI_AS  NOT NULL,
	  [storage] int  NOT NULL,
	  [acl] int  NOT NULL,
	  [multiversions] bit  NOT NULL,
	  [open_sls] bit  NOT NULL,
	  [open_hbr] bit  NOT NULL,
	  [algorithm] int  NOT NULL,
	  [kms_encryption_algorithm] int  NOT NULL,
	  [domain] varchar(100) COLLATE Chinese_PRC_CI_AS  NOT NULL,
	  [enabled] bit DEFAULT 1 NOT NULL,
	  [limit_size] bigint  NOT NULL,
	  [create_time] datetime  NOT NULL
	)
END

