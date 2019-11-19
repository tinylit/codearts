using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkyBuilding.ORM.MySql;
using SkyBuilding.ORM.SqlServer;
using SkyBuilding.SqlServer.Formatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkyBuilding.ORM.Tests
{
    [TestClass]
    public class SQLTest
    {
        [TestMethod]
        public void Test()
        {
            string sql = "select * from fei_users a , fei_data , fei_userdetails b on a.uid=b.uid where a.uid < 100";

            SQL sQL = sql;

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
                PRIMARY KEY (id) USING BTREE
            ) ENGINE = InnoDB CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Dynamic;

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

            var settings = new MySqlCorrectSettings();

            var query = sQL.Add(sQL1).ToString(settings);

            var sqlSettings = new SqlServerCorrectSettings();

            sqlSettings.Formatters.Add(new CreateIfFormatter());
            sqlSettings.Formatters.Add(new DropIfFormatter());

            var query2 = sQL.ToString(sqlSettings);

            var sql3 = new SQL("SELECT ywdjid as requestid FROM dzfp  GROUP BY ywdjid");
        }
    }
}
