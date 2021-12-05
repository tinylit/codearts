using CodeArts.Db.Formatters;
using CodeArts.Db.Lts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.Db.Tests
{
    /// <summary>
    /// 发票类型枚举。
    /// </summary>
    [Flags]
    public enum InvoiceTypeEnum
    {
        /// <summary> 电票。 </summary>
        Electric = 1 << 0,

        /// <summary> 普票。 </summary>
        Normal = 1 << 1,

        /// <summary> 专票。 </summary>
        Special = 1 << 2,

        /// <summary> 卷票。 </summary>
        Roll = 1 << 3
    }
    /// <summary>
    /// 请求平台。
    /// </summary>
    public enum RequestPlatformEnum
    {
        /// <summary>
        /// 正常。
        /// </summary>
        Normal,
        /// <summary>
        /// 微信。
        /// </summary>
        WeChat,
        /// <summary>
        /// 支付宝。
        /// </summary>
        Alipay
    }

    /// <summary>
    /// 特殊票种枚举。
    /// </summary>
    public enum TspzTypeEnum
    {
        Normal = 0,

        Oil = 1
    }
    /// <summary>
    /// 发票DTO。
    /// </summary>
    public class ApplyDto
    {
        /// <summary>
        /// 商铺ID。
        /// </summary>
        public ulong ShopId { get; set; }

        /// <summary>
        /// 盘编号(默认：获取公司注册的第一个开票设备，当前公司或商铺有多个开票设备时，请指定开票机号)。
        /// </summary>
        public string MachineCode { get; set; }

        /// <summary>
        /// 开票类型。
        /// </summary>
        public InvoiceTypeEnum InvoiceType { get; set; }

        /// <summary>
        /// 请求平台。
        /// </summary>
        public RequestPlatformEnum RequestPlatform { get; set; }

        /// <summary>
        /// 发票代码。
        /// </summary>
        public string InvoiceCode { get; set; }

        /// <summary>
        /// 发票号码。
        /// </summary>
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 订单编号。
        /// </summary>
        public string Ddbh { get; set; }

        /// <summary>
        /// 特殊票种。
        /// </summary>
        public TspzTypeEnum Tspz { get; set; }

        /// <summary>
        /// 业务日期。
        /// </summary>
        public DateTime Ywrq { get; set; }

        /// <summary>
        /// 自动开票 => {0:手动,1:自动}。
        /// </summary>
        public int AutoKp { get; set; }

        /// <summary>
        /// 购买方纳税人识别号。
        /// </summary>
        public string Gmfsbh { get; set; }

        /// <summary>
        /// 购买方名称。
        /// </summary>
        public string Gmfmc { get; set; }

        /// <summary>
        /// 购买方地址及电话。
        /// </summary>
        public string Gmfdzdh { get; set; }

        /// <summary>
        /// 购买方开户行及账号。
        /// </summary>
        public string Gmfkhhjzh { get; set; }

        /// <summary>
        /// 收票人手机号。
        /// </summary>
        public string Sprsjh { get; set; }

        /// <summary>
        /// 收票人邮箱。
        /// </summary>
        public string Spryx { get; set; }

        /// <summary>
        /// 价税合计金额。
        /// </summary>
        public decimal Jshj { get; set; }

        /// <summary>
        /// 收款人。
        /// </summary>
        public string Skr { get; set; }

        /// <summary>
        /// 复核人。
        /// </summary>
        public string Fhr { get; set; }

        /// <summary>
        /// 开票人。
        /// </summary>
        public string Kpr { get; set; }

        /// <summary>
        /// 备注。
        /// </summary>
        public string Bz { get; set; }

        /// <summary>
        /// 明细。
        /// </summary>
        public List<int> Mx { get; set; }
    }

    [TestClass]
    public class SQLTest
    {
        [TestMethod]
        public void Test()
        {
            var insert = "insert into dzfphx VALUES('invoiceNo',@requestId);";

            _ = new SQL(insert);

            string sql = "select * from fei_users a , fei_data , fei_userdetails b on a.uid=b.uid where a.uid < 100";

            SQL sQL = sql;

            var sql2 = @"select  
                replace(ywdjid,' ','') as ddbh
                case when not=1 then status else not end
                from  [dbo].[xsfp] 
                where ywrq >dateadd(day,-5,getdate()) 
                 and  ywdjid>'FPXLSY00071582'
                 and not = 1
                 and not exists (select  ywdjid from cwk where yikaifp!='是')
                group by ywdjid
                having sum(spje)!=0 ";

            _ = new SQL(sql2);

            SQL sQL1 = new SQL(@"
            /*
                Navicat Premium Data Transfer

                Source Server         : MySQL
                Source Server Type    : MySQL
                Source Server Version : 80013
                Source Host           : 47.107.124.30:3306
                Source Schema         : yep.v3.auth

                Target Server Type    : MySQL
                Target Server Version : 80013
                File Encoding         : 65001

                Date: 16/10/2019 09:30:09
            */

            SET NAMES utf8mb4;
            SET FOREIGN_KEY_CHECKS = 0;

            -- ----------------------------
            -- Table structure for yep_companys
            -- ----------------------------
            DROP TABLE IF EXISTS [yep_companys];
            -- ----------------------------
            -- Table structure for yep_auth_ship
            -- ----------------------------
            CREATE TABLE IF NOT EXISTS yep_auth_ship  (
                id bigint(20) NOT NULL,
                owner_id bigint(20) UNSIGNED NOT NULL,
                auth_id int(11) NOT NULL,
                type tinyint(4) NOT NULL,
                status int(11) NOT NULL,
                created timestamp(0) NULL,
                modified timestamp(0) NULL DEFAULT NULL,
                PRIMARY KEY (id) USING BTREE,
                CONSTRAINT FK_yep_companys_id FOREIGN KEY (owner_id) REFERENCES yep_companys (id) ON DELETE SET NULL ON UPDATE CASCADE 
            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic

            DELETE FROM yep_auth_ship;

            -- ----------------------------
            -- Table structure for yep_auth_tree
            -- ----------------------------
            CREATE TABLE IF NOT EXISTS yep_auth_tree  (
                Id int(11) NOT NULL AUTO_INCREMENT,
                parent_id int(11) NOT NULL,
                disp_order int(11) NOT NULL,
                has_child bit(1) NOT NULL,
                code varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                name varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                url varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                type tinyint(4) NOT NULL COMMENT '0：项目\r\n1：导航\r\n2：菜单\r\n4：页面\r\n8：功能\r\n16：板块\r\n32：提示\r\n64：标记',
                status tinyint(255) NOT NULL,
                created timestamp(0) NULL,
                modified timestamp(0) NULL,
                PRIMARY KEY (Id) USING BTREE
            ) ENGINE = InnoDB AUTO_INCREMENT = 125 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

            -- ----------------------------
            -- Table structure for yep_org_tree
            -- ----------------------------
            CREATE TABLE IF NOT EXISTS yep_org_tree  (
                Id bigint(20) UNSIGNED NOT NULL,
                parent_id bigint(20) UNSIGNED NOT NULL,
                disp_order int(11) NOT NULL,
                has_child bit(1) NOT NULL,
                code varchar(30) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                name varchar(150) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                type tinyint(4) NOT NULL COMMENT '1：集团\r\n2：单位\r\n4：部门\r\n8：商铺\r\n16：虚拟节点',
                status tinyint(4) NOT NULL,
                created timestamp(0) NULL,
                modified timestamp(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0),
                PRIMARY KEY (Id) USING BTREE
            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

            -- ----------------------------
            -- Table structure for yep_orm_test
            -- ----------------------------
            CREATE TABLE IF NOT EXISTS yep_orm_test  (
                Id bigint(20) NOT NULL,
                Name varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                Status varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                Created timestamp(0) NULL,
                Modified timestamp(3) NULL,
                PRIMARY KEY (Id) USING BTREE
            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

            -- ----------------------------
            -- Table structure for yep_users
            -- ----------------------------
            CREATE TABLE IF NOT EXISTS yep_users  (
                Id bigint(20) UNSIGNED NOT NULL,
                org_id bigint(20) UNSIGNED NOT NULL COMMENT '所在机构树节点ID',
                company_id bigint(20) UNSIGNED NOT NULL COMMENT '所在单位',
                account varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '账户',
                role int(11) NOT NULL COMMENT '角色',
                name varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '名称',
                wechat_id varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '微信ID',
                alipay_id varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '支付宝ID',
                tel varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '联系电话',
                mail varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '邮箱地址',
                avatar varchar(160) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '头像',
                sex tinyint(4) NOT NULL COMMENT '性别',
                password varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '密码',
                salt varchar(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '加盐',
                extends_enum int(11) NOT NULL COMMENT '扩展信息',
                status int(11) NOT NULL COMMENT '状态',
                registered timestamp(0) NULL DEFAULT NULL COMMENT '注册日期时间戳',
                modified timestamp(3) NULL DEFAULT NULL,
                PRIMARY KEY (Id) USING BTREE
            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

            -- ----------------------------
            -- Table structure for yep_tax_code
            -- ----------------------------
            CREATE TABLE IF NOT EXISTS yep_tax_code  (
                id varchar(19) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                parent_id varchar(19) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                level int(11) NOT NULL,
                name varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                short_name varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                specification varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                unit varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                price decimal(18, 2) NOT NULL,
                use_policy bit(1) NOT NULL,
                policy_type tinyint(4) NOT NULL,
                tax_rate double NOT NULL,
                free_tax_type tinyint(4) NOT NULL,
                has_tax bit(1) NOT NULL,
                special_manage varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                introduction varchar(2048) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
                status tinyint(4) NOT NULL,
                is_last_children bit(1) NOT NULL DEFAULT b'0',
                create_time timestamp(0) NULL DEFAULT NULL,
                modify_time timestamp(0) NULL DEFAULT NULL,
                PRIMARY KEY (id) USING BTREE
            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

            SET FOREIGN_KEY_CHECKS = 1;");

            /**
            *            SET NAMES utf8mb4;
            *            SET FOREIGN_KEY_CHECKS = 0;
            *            DROP TABLE IF EXISTS {DROP#yep_companys};
            *            CREATE TABLE IF NOT EXISTS {CREATE#yep_auth_ship}  (
            *                [id] bigint(20) NOT NULL,
            *                [owner_id] bigint(20) UNSIGNED NOT NULL,
            *                [auth_id] int(11) NOT NULL,
            *                [type] tinyint(4) NOT NULL,
            *                [status] int(11) NOT NULL,
            *                [created] timestamp(0) NULL,
            *                [modified] timestamp(0) NULL DEFAULT NULL,
            *                PRIMARY KEY ([id]) USING BTREE,
            *                CONSTRAINT FK_yep_companys_id FOREIGN KEY ([owner_id]) REFERENCES yep_companys ([id]) ON DELETE SET NULL ON UPDATE CASCADE 
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic
            *            DELETE FROM yep_auth_ship;
            *            CREATE TABLE IF NOT EXISTS {CREATE#yep_auth_tree}  (
            *                [Id] int(11) NOT NULL AUTO_INCREMENT,
            *                [parent_id] int(11) NOT NULL,
            *                [disp_order] int(11) NOT NULL,
            *                [has_child] bit(1) NOT NULL,
            *                [code] varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [name] varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [url] varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [type] tinyint(4) NOT NULL COMMENT '0：项目\r\n1：导航\r\n2：菜单\r\n4：页面\r\n8：功能\r\n16：板块\r\n32：提示\r\n64：标记',
            *                [status] tinyint(255) NOT NULL,
            *                [created] timestamp(0) NULL,
            *                [modified] timestamp(0) NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB AUTO_INCREMENT = 125 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            CREATE TABLE IF NOT EXISTS {CREATE#yep_org_tree}  (
            *                [Id] bigint(20) UNSIGNED NOT NULL,
            *                [parent_id] bigint(20) UNSIGNED NOT NULL,
            *                [disp_order] int(11) NOT NULL,
            *                [has_child] bit(1) NOT NULL,
            *                [code] varchar(30) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [name] varchar(150) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [type] tinyint(4) NOT NULL COMMENT '1：集团\r\n2：单位\r\n4：部门\r\n8：商铺\r\n16：虚拟节点',
            *                [status] tinyint(4) NOT NULL,
            *                [created] timestamp(0) NULL,
            *                [modified] timestamp(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0),
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            CREATE TABLE IF NOT EXISTS {CREATE#yep_orm_test}  (
            *                [Id] bigint(20) NOT NULL,
            *                [Name] varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [Status] varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [Created] timestamp(0) NULL,
            *                [Modified] timestamp(3) NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            CREATE TABLE IF NOT EXISTS {CREATE#yep_users}  (
            *                [Id] bigint(20) UNSIGNED NOT NULL,
            *                [org_id] bigint(20) UNSIGNED NOT NULL COMMENT '所在机构树节点ID',
            *                [company_id] bigint(20) UNSIGNED NOT NULL COMMENT '所在单位',
            *                [account] varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '账户',
            *                [role] int(11) NOT NULL COMMENT '角色',
            *                [name] varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '名称',
            *                [wechat_id] varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '微信ID',
            *                [alipay_id] varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '支付宝ID',
            *                [tel] varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '联系电话',
            *                [mail] varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '邮箱地址',
            *                [avatar] varchar(160) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '头像',
            *                [sex] tinyint(4) NOT NULL COMMENT '性别',
            *                [password] varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '密码',
            *                [salt] varchar(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '加盐',
            *                [extends_enum] int(11) NOT NULL COMMENT '扩展信息',
            *                [status] int(11) NOT NULL COMMENT '状态',
            *                [registered] timestamp(0) NULL DEFAULT NULL COMMENT '注册日期时间戳',
            *                [modified] timestamp(3) NULL DEFAULT NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            CREATE TABLE IF NOT EXISTS {CREATE#yep_tax_code}  (
            *                [id] varchar(19) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [parent_id] varchar(19) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [level] int(11) NOT NULL,
            *                [name] varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [short_name] varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [specification] varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [unit] varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [price] decimal(18, 2) NOT NULL,
            *                [use_policy] bit(1) NOT NULL,
            *                [policy_type] tinyint(4) NOT NULL,
            *                [tax_rate] double NOT NULL,
            *                [free_tax_type] tinyint(4) NOT NULL,
            *                [has_tax] bit(1) NOT NULL,
            *                [special_manage] varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [introduction] varchar(2048) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [status] tinyint(4) NOT NULL,
            *                [is_last_children] bit(1) NOT NULL DEFAULT b'0',
            *                [create_time] timestamp(0) NULL DEFAULT NULL,
            *                [modify_time] timestamp(0) NULL DEFAULT NULL,
            *                PRIMARY KEY ([id]) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            SET FOREIGN_KEY_CHECKS = 1;
            */

            var settings = new MySqlCorrectSettings();

            _ = sQL.Add(sQL1).ToString(settings);
            /**
            *select * from `fei_users` `a` , `fei_data` , `fei_userdetails` `b` on `a`.`uid`=`b`.`uid` where `a`.`uid` < 100;
            *            SET NAMES utf8mb4;
            *            SET FOREIGN_KEY_CHECKS = 0;
            *            DROP TABLE IF EXISTS `yep_companys`;
            *            CREATE TABLE IF NOT EXISTS `yep_auth_ship`  (
            *                `id` bigint(20) NOT NULL,
            *                `owner_id` bigint(20) UNSIGNED NOT NULL,
            *                `auth_id` int(11) NOT NULL,
            *                `type` tinyint(4) NOT NULL,
            *                `status` int(11) NOT NULL,
            *                `created` timestamp(0) NULL,
            *                `modified` timestamp(0) NULL DEFAULT NULL,
            *                PRIMARY KEY (`id`) USING BTREE,
            *                CONSTRAINT FK_yep_companys_id FOREIGN KEY (`owner_id`) REFERENCES yep_companys (`id`) ON DELETE SET NULL ON UPDATE CASCADE 
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic
            *            DELETE FROM yep_auth_ship;
            *            CREATE TABLE IF NOT EXISTS `yep_auth_tree`  (
            *                `Id` int(11) NOT NULL AUTO_INCREMENT,
            *                `parent_id` int(11) NOT NULL,
            *                `disp_order` int(11) NOT NULL,
            *                `has_child` bit(1) NOT NULL,
            *                `code` varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `name` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `url` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `type` tinyint(4) NOT NULL COMMENT '0：项目\r\n1：导航\r\n2：菜单\r\n4：页面\r\n8：功能\r\n16：板块\r\n32：提示\r\n64：标记',
            *                `status` tinyint(255) NOT NULL,
            *                `created` timestamp(0) NULL,
            *                `modified` timestamp(0) NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB AUTO_INCREMENT = 125 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            CREATE TABLE IF NOT EXISTS `yep_org_tree`  (
            *                `Id` bigint(20) UNSIGNED NOT NULL,
            *                `parent_id` bigint(20) UNSIGNED NOT NULL,
            *                `disp_order` int(11) NOT NULL,
            *                `has_child` bit(1) NOT NULL,
            *                `code` varchar(30) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `name` varchar(150) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `type` tinyint(4) NOT NULL COMMENT '1：集团\r\n2：单位\r\n4：部门\r\n8：商铺\r\n16：虚拟节点',
            *                `status` tinyint(4) NOT NULL,
            *                `created` timestamp(0) NULL,
            *                `modified` timestamp(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0),
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            CREATE TABLE IF NOT EXISTS `yep_orm_test`  (
            *                `Id` bigint(20) NOT NULL,
            *                `Name` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `Status` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `Created` timestamp(0) NULL,
            *                `Modified` timestamp(3) NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            CREATE TABLE IF NOT EXISTS `yep_users`  (
            *                `Id` bigint(20) UNSIGNED NOT NULL,
            *                `org_id` bigint(20) UNSIGNED NOT NULL COMMENT '所在机构树节点ID',
            *                `company_id` bigint(20) UNSIGNED NOT NULL COMMENT '所在单位',
            *                `account` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '账户',
            *                `role` int(11) NOT NULL COMMENT '角色',
            *                `name` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '名称',
            *                `wechat_id` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '微信ID',
            *                `alipay_id` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '支付宝ID',
            *                `tel` varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '联系电话',
            *                `mail` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '邮箱地址',
            *                `avatar` varchar(160) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '头像',
            *                `sex` tinyint(4) NOT NULL COMMENT '性别',
            *                `password` varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '密码',
            *                `salt` varchar(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '加盐',
            *                `extends_enum` int(11) NOT NULL COMMENT '扩展信息',
            *                `status` int(11) NOT NULL COMMENT '状态',
            *                `registered` timestamp(0) NULL DEFAULT NULL COMMENT '注册日期时间戳',
            *                `modified` timestamp(3) NULL DEFAULT NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            CREATE TABLE IF NOT EXISTS `yep_tax_code`  (
            *                `id` varchar(19) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `parent_id` varchar(19) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `level` int(11) NOT NULL,
            *                `name` varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `short_name` varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `specification` varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `unit` varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `price` decimal(18, 2) NOT NULL,
            *                `use_policy` bit(1) NOT NULL,
            *                `policy_type` tinyint(4) NOT NULL,
            *                `tax_rate` double NOT NULL,
            *                `free_tax_type` tinyint(4) NOT NULL,
            *                `has_tax` bit(1) NOT NULL,
            *                `special_manage` varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `introduction` varchar(2048) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                `status` tinyint(4) NOT NULL,
            *                `is_last_children` bit(1) NOT NULL DEFAULT b'0',
            *                `create_time` timestamp(0) NULL DEFAULT NULL,
            *                `modify_time` timestamp(0) NULL DEFAULT NULL,
            *                PRIMARY KEY (`id`) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *            SET FOREIGN_KEY_CHECKS = 1;
            */

            var sqlSettings = new SqlServerCorrectSettings();

            sqlSettings.Formatters.Add(new CreateIfFormatter());
            sqlSettings.Formatters.Add(new DropIfFormatter());

            _ = sQL.ToString(sqlSettings);
            /**
            *select * from [fei_users] [a] , [fei_data] , [fei_userdetails] [b] on [a].[uid]=[b].[uid] where [a].[uid] < 100;
            *            SET NAMES utf8mb4;
            *            SET FOREIGN_KEY_CHECKS = 0;
            *            IF EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' and [name] ='yep_companys') BEGIN
            *DROP TABLE [yep_companys];
            *END GO
            *
            *            IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' AND [name] ='yep_auth_ship') BEGIN
            *CREATE TABLE [yep_auth_ship]  (
            *                [id] bigint(20) NOT NULL,
            *                [owner_id] bigint(20) UNSIGNED NOT NULL,
            *                [auth_id] int(11) NOT NULL,
            *                [type] tinyint(4) NOT NULL,
            *                [status] int(11) NOT NULL,
            *                [created] timestamp(0) NULL,
            *                [modified] timestamp(0) NULL DEFAULT NULL,
            *                PRIMARY KEY ([id]) USING BTREE,
            *                CONSTRAINT FK_yep_companys_id FOREIGN KEY ([owner_id]) REFERENCES yep_companys ([id]) ON DELETE SET NULL ON UPDATE CASCADE 
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic
            *            
            *END GO
            *DELETE FROM yep_auth_ship;
            *            IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' AND [name] ='yep_auth_tree') BEGIN
            *CREATE TABLE [yep_auth_tree]  (
            *                [Id] int(11) NOT NULL AUTO_INCREMENT,
            *                [parent_id] int(11) NOT NULL,
            *                [disp_order] int(11) NOT NULL,
            *                [has_child] bit(1) NOT NULL,
            *                [code] varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [name] varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [url] varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [type] tinyint(4) NOT NULL COMMENT '0：项目\r\n1：导航\r\n2：菜单\r\n4：页面\r\n8：功能\r\n16：板块\r\n32：提示\r\n64：标记',
            *                [status] tinyint(255) NOT NULL,
            *                [created] timestamp(0) NULL,
            *                [modified] timestamp(0) NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB AUTO_INCREMENT = 125 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *END GO
            *
            *            IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' AND [name] ='yep_org_tree') BEGIN
            *CREATE TABLE [yep_org_tree]  (
            *                [Id] bigint(20) UNSIGNED NOT NULL,
            *                [parent_id] bigint(20) UNSIGNED NOT NULL,
            *                [disp_order] int(11) NOT NULL,
            *                [has_child] bit(1) NOT NULL,
            *                [code] varchar(30) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [name] varchar(150) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [type] tinyint(4) NOT NULL COMMENT '1：集团\r\n2：单位\r\n4：部门\r\n8：商铺\r\n16：虚拟节点',
            *                [status] tinyint(4) NOT NULL,
            *                [created] timestamp(0) NULL,
            *                [modified] timestamp(0) NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP(0),
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *END GO
            *
            *            IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' AND [name] ='yep_orm_test') BEGIN
            *CREATE TABLE [yep_orm_test]  (
            *                [Id] bigint(20) NOT NULL,
            *                [Name] varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [Status] varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [Created] timestamp(0) NULL,
            *                [Modified] timestamp(3) NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *END GO
            *
            *            IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' AND [name] ='yep_users') BEGIN
            *CREATE TABLE [yep_users]  (
            *                [Id] bigint(20) UNSIGNED NOT NULL,
            *                [org_id] bigint(20) UNSIGNED NOT NULL COMMENT '所在机构树节点ID',
            *                [company_id] bigint(20) UNSIGNED NOT NULL COMMENT '所在单位',
            *                [account] varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '账户',
            *                [role] int(11) NOT NULL COMMENT '角色',
            *                [name] varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '名称',
            *                [wechat_id] varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '微信ID',
            *                [alipay_id] varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '支付宝ID',
            *                [tel] varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '联系电话',
            *                [mail] varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '邮箱地址',
            *                [avatar] varchar(160) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '头像',
            *                [sex] tinyint(4) NOT NULL COMMENT '性别',
            *                [password] varchar(32) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '密码',
            *                [salt] varchar(36) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL COMMENT '加盐',
            *                [extends_enum] int(11) NOT NULL COMMENT '扩展信息',
            *                [status] int(11) NOT NULL COMMENT '状态',
            *                [registered] timestamp(0) NULL DEFAULT NULL COMMENT '注册日期时间戳',
            *                [modified] timestamp(3) NULL DEFAULT NULL,
            *                PRIMARY KEY (Id) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *END GO
            *
            *            IF NOT EXIXSTS(SELECT * FROM [sysobjects] WHERE [xtype]='U' AND [name] ='yep_tax_code') BEGIN
            *CREATE TABLE [yep_tax_code]  (
            *                [id] varchar(19) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [parent_id] varchar(19) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [level] int(11) NOT NULL,
            *                [name] varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [short_name] varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [specification] varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [unit] varchar(16) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [price] decimal(18, 2) NOT NULL,
            *                [use_policy] bit(1) NOT NULL,
            *                [policy_type] tinyint(4) NOT NULL,
            *                [tax_rate] double NOT NULL,
            *                [free_tax_type] tinyint(4) NOT NULL,
            *                [has_tax] bit(1) NOT NULL,
            *                [special_manage] varchar(128) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [introduction] varchar(2048) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
            *                [status] tinyint(4) NOT NULL,
            *                [is_last_children] bit(1) NOT NULL DEFAULT b'0',
            *                [create_time] timestamp(0) NULL DEFAULT NULL,
            *                [modify_time] timestamp(0) NULL DEFAULT NULL,
            *                PRIMARY KEY ([id]) USING BTREE
            *            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;
            *END GO
            *
            *            SET FOREIGN_KEY_CHECKS = 1;
            */

            _ = new SQL("SELECT ywdjid as requestid FROM dzfp  GROUP BY ywdjid");
        }

        [TestMethod]
        public void TestMapTo()
        {
            var config = new ConnectionConfig
            {
                ProviderName = "SQLServer",
                ConnectionString = ""
            };

            DbConnectionManager.RegisterAdapter(new SqlServerLtsAdapter());
            DbConnectionManager.RegisterDatabaseFor<DapperFor>();

            var adapter = DbConnectionManager.Get(config.ProviderName);

            var sql = new SQL(@"select 
                replace(max(gmfmc),' ','') as gmfmc,
                max(ywrq) as wrq,
                max(kplx) as autoKp,
                replace(max(sprsjh),' ','') as sprsjh,
                replace(max(sprmc),' ','') as  skr,   
                replace(max(spryx),' ','') as spryx,
                replace(max(addtel),' ','') as gmfdzdh,
                replace(max(gmfsh),' ','') as gmfsbh,
                replace(max(kfhzh),' ','') as gmfkhhjzh,  
                max(bz) as bz,
                sum(spje) as jshj,
                max(dsddh) as ddbh,
                max(fplx) as invoiceType
                from dzfp
                where ywdjid=@requestid");

            string requestid = "201909060000050";

            //var results = provider.Query<string>(connection, sql.ToString(adapter.Settings));

            var param = new Dictionary<string, object>();

            sql.Parameters.ForEach(token =>
            {
                param[adapter.Settings.ParamterName(token)] = requestid;
            });

            //var applyDto = provider.QueryFirst<ApplyDto>(connection, sql.ToString(adapter.Settings), param);
        }

        [TestMethod]
        public void TestSQL()
        {
            string sqlstr = @"if not exists(select * from t_ZBDZ_returnFPCode where billid=@ddbh and newFPcode=@invoiceNo)
INSERT INTO t_ZBDZ_returnFPCode
(billid,newFPcode)
VALUES
(@ddbh,@invoiceNo+''+convert(char(10),@kprq,23))";

            var sql = new SQL(sqlstr);


            var settings = new SqlServerCorrectSettings();

            _ = new SQL(@"PRAGMA foreign_keys = OFF;

                                -- ----------------------------
                                -- Table structure for yep_work_cache
                                -- ----------------------------
	                            CREATE TABLE IF NOT EXISTS [yep_work_cache] (
		                            [Id] TEXT NOT NULL,
		                            [Timestamp]  INTEGER,
		                            PRIMARY KEY ([Id])
                                )");

            _ = sql.ToString(settings);
        }


        [TestMethod]
        public void TestCursor()
        {
            string sqlstr = @"DECLARE @username varchar(20),@UserId varchar(100)
            DECLARE cursor_name CURSOR FOR --定义游标
                SELECT TOP 10 UserId,UserName FROM UserInfo
                ORDER BY UserId DESC
            OPEN cursor_name --打开游标
            FETCH NEXT FROM cursor_name INTO  @UserId,@username  --抓取下一行游标数据
            WHILE @@FETCH_STATUS = 0
                BEGIN
                    PRINT '用户ID：'+@UserId+'            '+'用户名：'+@username
                    FETCH NEXT FROM cursor_name INTO @UserId,@username
                END
            CLOSE cursor_name --关闭游标
            DEALLOCATE cursor_name --释放游标";

            var sql = new SQL(sqlstr);

            /**
             * DECLARE {username} varchar(20),{UserId} varchar(100)
             * DECLARE cursor_name CURSOR FOR 
             *  SELECT TOP 10 [UserId],[UserName] FROM {SELECT#UserInfo}
             *   ORDER BY [UserId] DESC
             * OPEN cursor_name 
             * FETCH NEXT FROM {SELECT#cursor_name} INTO  {UserId},{username}  
             * WHILE @@FETCH_STATUS = 0
             *   BEGIN
             *       PRINT '用户ID：'+{UserId}+'            '+'用户名：'+{username}
             *       FETCH NEXT FROM cursor_name INTO {UserId},{username}
             *   END
             * CLOSE cursor_name 
             * DEALLOCATE cursor_name 
             */
        }
        [TestMethod]
        public void TestYsmt()
        {

            var settings = new SqlServerCorrectSettings();

            _ = new SQL(@"select 
            replace(ywdjid,' ','') as ddbh
            from  vw_youe_dzfp
            where datediff(dd, ywrq, getdate()) < 10
            and ywrq>'2020-03-05'
            group by ywdjid    
            having sum(spje) != 0 ")
                .ToString(settings);

            _ = new SQL(@"select
replace(max(GMFMC),' ','') as gmfmc,
sum(spje) as jshj,
max(YWRQ) as ywrq,
max(YWDJID)  as ddbh,
replace(max(KPLX),' ','') as autoKp,
replace(max(SPRSJH),' ','') as sprsjh,
'' as spryx,
replace(max(addtel),' ','') as gmfdzdh,
CASE WHEN rtrim(max(GMFSH))='1' THEN  NULL ELSE rtrim(max(GMFSH)) END  as gmfsbh,
replace(max(kfhzh),' ','') as gmfkhhjzh,
max(bz) as bz,
'1' as invoiceType,
'499098834059'  as machineCode
from  vw_youe_dzfp
WHERE YWDJID =@ddbh
group by YWDJID
having sum(SPJE)<>0").ToString(settings);

            _ = new SQL(@"select
'0' as fphxz,
replace(max(SPMC),' ','') as spmc,
case when SUM(SPSL)=0 then 1 else SUM(SPSL) end as spsl,
case when SUM(SPSL)=0 then sum(SPJE) else round( sum(SPJE)/SUM(SPSL),6) end as dj,
max( Convert(decimal(18,2),SPSLV)) as sl,
replace(max(SPDW),' ','') as dw,
replace(max(SPGG),' ','') as ggxh,
replace(max(DSDDH),' ','') as dsddh,
left(replace(max(spswbm),' ','')+'0000000000',19) as spbm,
sum(SPJE)  as je
from  vw_youe_dzfp
WHERE YWDJID =@ddbh
group by YWDJID,SPID 
having sum(SPJE)<>0")
                .ToString(settings);
        }

        [TestMethod]
        public void TestYY()
        {
            string sqlstr = @"select   max(c.customer_name)  as gmfmc,
 isnull(max(c.tax_id),'') as gmfsbh,
 isnull(max(c.tel),'') as gmfdzdh,
 isnull(max(c.bank),'') + isnull(max(c.account_id),'') as gmfkhhjzh,
 isnull(max(d.remark),max(a.str_out_bill_id)) as ddbh,
 '' as  bm,
 '' as ywy,
 '1' AS invoiceType,
 '1' AS autoKp,
 sum(d.qty * d.price) as jshj,
'661568807294' AS machineCode,
 isnull(max(c.tel),'') as sprsjh,
 (select isnull(dd.zdw_erpRawPreOrderId,replace(bill_id,'YS','YSB' )) from sls_quotation_bill dd with(readpast),str_out_bill aa with(readpast)
         where aa.xsdd_id = dd.bill_id and aa.str_out_bill_id = isnull(max(d.remark),max(a.str_out_bill_id)) ) as dsddh,
 max(a.remark) as bz,
 (select aa.sls_tax_id from str_out_bill aa where aa.str_out_bill_id = isnull(max(d.remark),max(a.str_out_bill_id)) ) as invoicecode,
 (select aa.sls_tax_no from str_out_bill aa where aa.str_out_bill_id = isnull(max(d.remark),max(a.str_out_bill_id)) ) as invoiceno,
 (select aa.sls_tax_date from str_out_bill aa where aa.str_out_bill_id = isnull(max(d.remark),max(a.str_out_bill_id)) ) as ywrq
from  str_out_bill a  join customer c on a.come_to = c.customer_id,
 str_out_bill_detail d join goods on d.goods_id = goods.goods_id
where  a.str_out_type_id in('4','B')
and a.str_out_bill_id = d.str_out_bill_id
and    djlx_id in ('02','03')
and  a.str_out_bill_id=@ddbh
group by  a.str_out_bill_id";
            var sql = new SQL(sqlstr);

            var sqlSettings = new SqlServerCorrectSettings();

            sqlSettings.Formatters.Add(new CreateIfFormatter());
            sqlSettings.Formatters.Add(new DropIfFormatter());

            _ = sql.ToString(sqlSettings);
        }

        [TestMethod]
        public void TestOO()
        {
            var settings = new SqlServerCorrectSettings();

            var sql = new SQL(@"SELECT DISTINCT
 isnull( d.remark, a.str_out_bill_id ) AS ddbh 
FROM
 str_out_bill a
 JOIN customer c ON a.come_to = c.customer_id
 JOIN str_out_bill_detail d ON a.str_out_bill_id = d.str_out_bill_id
 JOIN goods g ON d.goods_id = g.goods_id 
WHERE
 a.str_out_type_id IN ( '4', 'B' ) 
 AND djlx_id IN ( '02', '03' ) 
 AND a.sls_tax_date > '2020-06-01'");

            _ = sql.ToString(settings);

        }

        [TestMethod]
        public void InsertSQL()
        {
            var settings = new SqlServerCorrectSettings();

            SQL sql = new SQL("INSERT INTO fphzhd_a VALUES (@ddbh,'1', @invoiceNo, Convert(decimal(14,4),@jshj), convert(varchar(10),convert(datetime,@kprq),120),@invoiceCode, @pdf,0);");

            _ = sql.ToString(settings);
        }

        [TestMethod]
        public void ToOrderByCountSQL()
        {
            var settings = new SqlServerCorrectSettings();

            string sqlstr = @"select   max(c.customer_name)  as gmfmc,


 isnull(max(c.tax_id),'') as gmfsbh,
 isnull(max(c.tel),'') as gmfdzdh,
 isnull(max(c.bank),'') + isnull(max(c.account_id),'') as gmfkhhjzh,
 isnull(max(d.remark) as remark,max(a.str_out_bill_id)) as ddbh,
 '' as  bm,
 '' as ywy,
 '1' AS invoiceType,
 '1' AS autoKp,


 sum(d.qty * d.price) as jshj,
'661568807294' AS machineCode,
 isnull(max(c.tel),'') as sprsjh,
 (select isnull(dd.zdw_erpRawPreOrderId,replace(bill_id,'YS','YSB' )) from sls_quotation_bill dd with(readpast),str_out_bill aa with(readpast)
         where aa.xsdd_id = dd.bill_id and aa.str_out_bill_id = isnull(max(d.remark),max(a.str_out_bill_id)) ) as dsddh,
 max(a.remark) as bz,
 (select aa.sls_tax_id from str_out_bill aa where aa.str_out_bill_id = isnull(max(d.remark),max(a.str_out_bill_id)) ) as invoicecode,
 (select aa.sls_tax_no from str_out_bill aa where aa.str_out_bill_id = isnull(max(d.remark),max(a.str_out_bill_id)) ) as invoiceno,
 (select aa.sls_tax_date from str_out_bill aa where aa.str_out_bill_id = isnull(max(d.remark),max(a.str_out_bill_id)) ) as ywrq
from  str_out_bill a  join customer c on a.come_to = c.customer_id,
 str_out_bill_detail d join goods on d.goods_id = goods.goods_id
where  a.str_out_type_id in('4','B')
and a.str_out_bill_id = d.str_out_bill_id
and    djlx_id in ('02','03')
and  a.str_out_bill_id=@ddbh
group by  a.str_out_bill_id
order by a.str_out_bill_id desc";

            SQL sql = new SQL(sqlstr);

            var countSql = sql.ToCountSQL();

            var pagedSql = sql.ToSQL(0, 10);

            _ = countSql.ToString(settings);
        }

        [TestMethod]
        public void ToCountSQL()
        {
            var settings = new SqlServerCorrectSettings();

            string sqlstr = @"select distinct Max(c.customer_name)  as gmfmc from  str_out_bill a  join customer c on a.come_to = c.customer_id,
 str_out_bill_detail d join goods on d.goods_id = goods.goods_id
where  a.str_out_type_id in('4','B')
and a.str_out_bill_id = d.str_out_bill_id
and    djlx_id in ('02','03')
and  a.str_out_bill_id=@ddbh
group by  a.str_out_bill_id";

            SQL sql = new SQL(sqlstr);

            var countSql = sql.ToCountSQL();

            string sqlstr2 = @"select distinct REPLACE(c.customer_name, 'a', 'A') as gmfmc from  str_out_bill a  join customer c on a.come_to = c.customer_id,
 str_out_bill_detail d join goods on d.goods_id = goods.goods_id
where  a.str_out_type_id in('4','B')
and a.str_out_bill_id = d.str_out_bill_id
and    djlx_id in ('02','03')
and  a.str_out_bill_id=@ddbh
group by  a.str_out_bill_id";

            sql = new SQL(sqlstr2);

            var countSql2 = sql.ToCountSQL();

            string sqlstr3 = @"select distinct c.customer_name + c.tel as gmfmc from  str_out_bill a  join customer c on a.come_to = c.customer_id,
 str_out_bill_detail d join goods on d.goods_id = goods.goods_id
where  a.str_out_type_id in('4','B')
and a.str_out_bill_id = d.str_out_bill_id
and    djlx_id in ('02','03')
and  a.str_out_bill_id=@ddbh
group by  a.str_out_bill_id";

            sql = new SQL(sqlstr3);

            var countSql3 = sql.ToCountSQL();

            string sqlstr4 = @"select distinct REPLACE(c.customer_name + c.tel, 'a', 'A') as gmfmc from  str_out_bill a  join customer c on a.come_to = c.customer_id,
 str_out_bill_detail d join goods on d.goods_id = goods.goods_id
where  a.str_out_type_id in('4','B')
and a.str_out_bill_id = d.str_out_bill_id
and    djlx_id in ('02','03')
and  a.str_out_bill_id=@ddbh
group by  a.str_out_bill_id";

            sql = new SQL(sqlstr4);

            var countSql4 = sql.ToCountSQL();

            var pagedSql = sql.ToSQL(0, 10);

            _ = countSql.ToString(settings);
        }

        [TestMethod]
        public void MyTestMethod()
        {
            string mainSql = "select * from table";

            int i = 6;/* 跳过 SELECT 的计算 */

            for (int length = mainSql.Length; i < length; i++)
            {
                char c = mainSql[i];

                if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z'))
                {
                    break;
                }
            }

            var sb = new StringBuilder();

            sb.Append(mainSql.Substring(0, i));

            if (mainSql[i] != ' ')
            {
                sb.Append(' ');
            }

            string value = sb.Append("SQL_CALC_FOUND_ROWS")
                 .Append(mainSql.Substring(i))
                 .Append(';')
                 .Append("SELECT FOUND_ROWS()")
                 .ToString();
        }

        [TestMethod]
        public void TestWithAs()
        {
            string sqlStr = @"with cte1 as
                (
                    select * from table1 where name like 'abc%'
                ),
                cte2 as
                (
                    select * from table2 where id > 20
                ),
                cte3 as
                (
                    select * from table3 where price < 100
                )
                select a.* from cte1 a, cte2 b, cte3 c where a.id = b.id and a.id = c.id";

            var sql = new SQL(sqlStr);

            var countSql = sql.ToCountSQL();

            var pagedSql = sql.ToSQL(0, 10);
        }

        [TestMethod]
        public void TestWithAs2()
        {
            string sqlStr = @"with cte1 as
                (
                    select * from table1 where name like 'abc%'
                ),
                cte2 as
                (
                    select * from table2 where id > 20
                ),
                cte3 as
                (
                    select * from table3 where price < 100
                )
                select a.* from cte1 a, cte2 b, cte3 c where a.id = b.id and a.id = c.id

                select a.* from cte1 a, cte2 b, cte3 c where a.id = b.id and a.id = c.id";

            var sql = new SQL(sqlStr);
        }

        [TestMethod]
        public void TestWithAs3()
        {
            string sqlStr = @"with cte1 as
                (
                    select * from table1 where name like 'abc%'
                ),
                cte2 as
                (
                    select * from ct3 where id > 20
                ),
                cte3 as
                (
                    select * from cte1 where price < 100
                )

                select a.* from cte1 a, cte2 b, cte3 c where a.id = b.id and a.id = c.id";

            var sql = new SQL(sqlStr);

            var countSql = sql.ToCountSQL();

            var pagedSql = sql.ToSQL(0, 10);
        }

        [TestMethod]
        public void TestWithAsUnionAll()
        {
            string sqlStr = @"with cte1 as
                (
                    select * from table1 where name like 'abc%'
                ),
                cte2 as
                (
                    select * from ct3 where id > 20
                ),
                cte3 as
                (
                    select * from cte1 where price < 100
                )

                select a.* from cte1 a, cte2 b, cte3 c where a.id = b.id and a.id = c.id
                union all
                select a.* from cte1 a where a.id > 100
                ";

            var sql = new SQL(sqlStr);

            var countSql = sql.ToCountSQL();

            var pagedSql = sql.ToSQL(0, 10);
        }
    }
}
