using Microsoft.VisualStudio.TestTools.UnitTesting;
using CodeArts.ORM.MySql;
using CodeArts.ORM.SqlServer;
using CodeArts.SqlServer;
using CodeArts.SqlServer.Formatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeArts.ORM.Tests
{
    /// <summary>
    /// 发票类型枚举
    /// </summary>
    [Flags]
    public enum InvoiceTypeEnum
    {
        /// <summary> 电票 </summary>
        Electric = 1 << 0,

        /// <summary> 普票 </summary>
        Normal = 1 << 1,

        /// <summary> 专票 </summary>
        Special = 1 << 2,

        /// <summary> 卷票 </summary>
        Roll = 1 << 3
    }
    /// <summary>
    /// 请求平台
    /// </summary>
    public enum RequestPlatformEnum
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal,
        /// <summary>
        /// 微信
        /// </summary>
        WeChat,
        /// <summary>
        /// 支付宝
        /// </summary>
        Alipay
    }

    /// <summary>
    /// 特殊票种枚举
    /// </summary>
    public enum TspzTypeEnum
    {
        Normal = 0,

        Oil = 1
    }
    /// <summary>
    /// 发票DTO
    /// </summary>
    public class ApplyDto
    {
        /// <summary>
        /// 商铺ID
        /// </summary>
        public ulong ShopId { get; set; }

        /// <summary>
        /// 盘编号(默认：获取公司注册的第一个开票设备，当前公司或商铺有多个开票设备时，请指定开票机号)
        /// </summary>
        public string MachineCode { get; set; }

        /// <summary>
        /// 开票类型
        /// </summary>
        public InvoiceTypeEnum InvoiceType { get; set; }

        /// <summary>
        /// 请求平台
        /// </summary>
        public RequestPlatformEnum RequestPlatform { get; set; }

        /// <summary>
        /// 发票代码
        /// </summary>
        public string InvoiceCode { get; set; }

        /// <summary>
        /// 发票号码
        /// </summary>
        public string InvoiceNo { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string Ddbh { get; set; }

        /// <summary>
        /// 特殊票种
        /// </summary>
        public TspzTypeEnum Tspz { get; set; }

        /// <summary>
        /// 业务日期
        /// </summary>
        public DateTime Ywrq { get; set; }

        /// <summary>
        /// 自动开票 => {0:手动,1:自动}
        /// </summary>
        public int AutoKp { get; set; }

        /// <summary>
        /// 购买方纳税人识别号
        /// </summary>
        public string Gmfsbh { get; set; }

        /// <summary>
        /// 购买方名称
        /// </summary>
        public string Gmfmc { get; set; }

        /// <summary>
        /// 购买方地址及电话
        /// </summary>
        public string Gmfdzdh { get; set; }

        /// <summary>
        /// 购买方开户行及账号
        /// </summary>
        public string Gmfkhhjzh { get; set; }

        /// <summary>
        /// 收票人手机号
        /// </summary>
        public string Sprsjh { get; set; }

        /// <summary>
        /// 收票人邮箱
        /// </summary>
        public string Spryx { get; set; }

        /// <summary>
        /// 价税合计金额
        /// </summary>
        public decimal Jshj { get; set; }

        /// <summary>
        /// 收款人
        /// </summary>
        public string Skr { get; set; }

        /// <summary>
        /// 复核人
        /// </summary>
        public string Fhr { get; set; }

        /// <summary>
        /// 开票人
        /// </summary>
        public string Kpr { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Bz { get; set; }

        /// <summary>
        /// 明细
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

            SQL insert2 = new SQL(insert);

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

            SQL sQL2 = new SQL(sql2);

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

            var settings = new MySqlCorrectSettings();

            var query = sQL.Add(sQL1).ToString(settings);

            var sqlSettings = new SqlServerCorrectSettings();

            sqlSettings.Formatters.Add(new CreateIfFormatter());
            sqlSettings.Formatters.Add(new DropIfFormatter());

            var query2 = sQL.ToString(sqlSettings);

            var sql3 = new SQL("SELECT ywdjid as requestid FROM dzfp  GROUP BY ywdjid");
        }

        [TestMethod]
        public void TestGroupBy()
        {
            string sqlstr = @"select
            a.invoice_id  as ddbh
            from BEK_AR_INVOICE_NEW a
            where a.amt<>0  and  a.it_id not in ('30769','99999')
            and to_char(ct,'yyyy-mm-dd')>to_char(sysdate-5,'yyyy-mm-dd')
            and not exists(select  t.invoice_id  from AR_INVOICE t where t.invoice_id=a.invoice_id and REALINVOICE_NO is not null)
            group by a.invoice_id";

            var sql = new SQL(sqlstr);

            var settings = new Oracle.OracleCorrectSettings();

            var scalar = sql.ToString(settings);
        }

        //[TestMethod]
        public void TestMapTo()
        {
            var config = new ConnectionConfig
            {
                ProviderName = "SQLServer",
                ConnectionString = ""
            };

            DbConnectionManager.AddAdapter(new SqlServerAdapter());
            DbConnectionManager.AddProvider<SkyProvider>();

            var adapter = DbConnectionManager.Create(config.ProviderName);
            var connection = adapter.Create(config.ConnectionString);
            var provider = DbConnectionManager.Create(adapter);

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
                param[adapter.Settings.ParamterName(token.Name)] = requestid;
            });

            var applyDto = provider.QueryFirst<ApplyDto>(connection, sql.ToString(adapter.Settings), param);
        }

        [TestMethod]
        public void TestOracleSubstring()
        {
            var sqlstr = @"select 
            '0' as fphxz,
            replace(itname,' ','') as spmc,
            replace(amt,' ','') as spsl ,
            replace(sale_price,' ','') as dj,
            nvl(invoicemny,0) as je ,
            CASE WHEN SALE_TAX=0.17 THEN 0.16
               WHEN SALE_TAX=0.11 THEN 0.10
               ELSE SALE_TAX END as sl ,
            replace(unit,' ','') as dw,
            rpad(it_taxcode,19,'0') as spbm,
            replace(spec,' ','') as ggxh,
            CASE WHEN substr(DSDH,1,2)='SS' THEN '' ELSE DSDH end as dsddh
            from   BEK_AR_INVOICE_NEW 
            where invoice_id=@ddbh";

            var sql = new SQL(sqlstr);

            var settings = new Oracle.OracleCorrectSettings();

            var scalar = sql.ToString(settings);
        }

        [TestMethod]
        public void TestOracle()
        {
            var sqlstr = @"update  AR_INVOICE 
            set REALINVOICE_NO=replace(replace(REALINVOICE_NO,'~'||@invoiceNo||'~','~'),' ','')||'~'||@invoiceNo
            ,invoice_time=to_date(@kprq,'yyyy-mm-dd hh24:mi:ss')
             where invoice_id=@ddbh";

            var sql = new SQL(sqlstr);

            var settings = new Oracle.OracleCorrectSettings();

            var scalar = sql.ToString(settings);
        }
    }
}
