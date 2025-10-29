/*
 Navicat Premium Dump SQL

 Source Server         : 本机MySql数据库
 Source Server Type    : MySQL
 Source Server Version : 80011 (8.0.11)
 Source Host           : localhost:3306
 Source Schema         : uexam

 Target Server Type    : MySQL
 Target Server Version : 80011 (8.0.11)
 File Encoding         : 65001

 Date: 26/10/2025 23:45:49
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for t_building
-- ----------------------------
DROP TABLE IF EXISTS `t_building`;
CREATE TABLE `t_building` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `city` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `city_cn` varchar(255) CHARACTER SET utf8 COLLATE utf8_unicode_ci DEFAULT NULL,
  `org_city` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL,
  `org_area` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL,
  `org_name` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL,
  `org_type` tinyint(4) DEFAULT '1' COMMENT '1-队站；2-专职队；3-重点建筑',
  `addr` text COLLATE utf8_unicode_ci,
  `gps` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL,
  `creator` int(11) DEFAULT NULL,
  `create_date` datetime DEFAULT CURRENT_TIMESTAMP,
  `update_date` datetime DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `deleted` bit(1) DEFAULT b'0',
  `amap` varchar(255) COLLATE utf8_unicode_ci DEFAULT NULL,
  `location` geometry DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=18687 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;
